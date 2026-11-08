# Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy

[English](README.md)

การแยกข้อมูลตาม tenant บน EF Core แบบเลือกใช้ได้และไม่ผูกกับ provider สร้างบน `AuditableDbContext`

## การติดตั้ง

```shell
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy
```

ติดตั้ง provider ฐานข้อมูลแยกต่างหาก เช่น `Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL` package นี้อ้างอิง package EF Core แบบไม่ผูกกับ provider และ `Athenex.Whimclean.Multitenancy.Abstractions`

## สร้าง context ที่รองรับ tenant

```csharp
using Athenex.Whimclean.Application.Context;
using Athenex.Whimclean.Infrastructure.Auditing;
using Athenex.Whimclean.Infrastructure.DomainEvents;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy.Persistence;
using Athenex.Whimclean.Multitenancy;
using Microsoft.EntityFrameworkCore;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    TimeProvider timeProvider,
    ICurrentUser<Guid> currentUser,
    IAuditActorResolver<Guid, Guid?> auditActorResolver,
    IDomainEventDispatcher domainEventDispatcher,
    ICurrentTenant<Guid> currentTenant)
    : MultiTenantDbContext<Guid, Guid?, Guid>(
        options,
        timeProvider,
        currentUser,
        auditActorResolver,
        domainEventDispatcher,
        currentTenant)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}
```

`MultiTenantDbContext<TUserId, TActor, TTenantId>` seal `ConfigureAdditionalInfrastructureModel` และ `ValidateInfrastructureWrites` ไว้เพื่อติดตั้ง query filter ของ tenant และการตรวจสอบการเขียน ส่วน `ConfigureModel` และ `ValidateApplicationWrites` ยังคงเปิดให้แอปพลิเคชันใช้ได้

entity ที่ tenant เป็นเจ้าของต้อง implement `IMultiTenant<TTenantId>`:

```csharp
public sealed class Order : IMultiTenant<Guid>
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
}
```

## ลงทะเบียน context

package นี้ไม่มีเมธอดลงทะเบียนของตัวเอง ให้ใช้เมธอดของ provider ที่เลือก

PostgreSQL:

```csharp
services.AddPostgreSqlUnitOfWork<ApplicationDbContext>(connectionString);
```

EF provider อื่น:

```csharp
services.AddEntityFrameworkCoreUnitOfWork<ApplicationDbContext>((_, options) =>
    options.UseSqlServer(connectionString));
```

ลงทะเบียน implementation ของ `ICurrentTenant<Guid>` adapter สำหรับ ASP.NET Core multitenancy จัดหาให้ผ่าน `AddAspNetCoreMultitenancy<TUserId, TTenantId>` หลังจากการอนุญาตตามสมาชิกภาพผ่านแล้ว host ที่ไม่ใช่ HTTP สามารถลงทะเบียน `AmbientCurrentTenant<TTenantId>` จาก package abstractions ได้

## พฤติกรรมการบังคับใช้

- entity type ระดับ root ที่เป็น `IMultiTenant<TTenantId>` จะได้รับ query filter ชื่อ `TenantFilter` (`QueryFilterNames.Tenant`) ส่วน owned type จะถูกข้าม
- query จะไม่คืน record ที่ tenant เป็นเจ้าของเลยเมื่อไม่มี tenant ปัจจุบันที่ได้รับอนุญาต filter ต้องการ tenant ที่ถูกกำหนดแล้วและ `TenantId` ที่ตรงกัน จึงเป็นแบบ fail closed
- record ที่ tenant เป็นเจ้าของซึ่งถูกเพิ่ม แก้ไข หรือลบ ต้องมี tenant ปัจจุบัน มิฉะนั้นการบันทึกจะ throw `CurrentTenantRequiredException` การบันทึกที่ไม่แตะ entity ที่ tenant เป็นเจ้าของจะไม่ถูกตรวจสอบ
- record ที่ `TenantId` ต่างจาก tenant ปัจจุบัน หรือที่ `TenantId` ถูกแก้ไขไม่ว่าในทิศทางใด จะทำให้เกิด `TenantAccessViolationException` การย้าย record ไปยัง tenant อื่นไม่ใช่การเขียนที่ถูกต้องในทุกกรณี
- context ตรวจสอบ `TenantId` แต่ไม่เคยกำหนดหรือเขียนทับค่านี้
- audit stamp และ soft delete แบบ shadow property (เลือกใช้ด้วย `HasAuditStamps()`/`HasSoftDelete()` ใน `ConfigureModel`), การบังคับให้มีผู้ใช้ปัจจุบัน, การแปลง concurrency exception และพฤติกรรมของ domain event ยังคงสืบทอดมาจาก `AuditableDbContext`

query สำหรับงานดูแลระบบที่ได้รับอนุญาตอย่างชัดเจนสามารถปิดเฉพาะ query filter ของ tenant ได้:

```csharp
var acrossTenants = dbContext.Orders
    .IgnoreQueryFilters(["TenantFilter"]);
```

การปิด filter ไม่ใช่การอนุญาต ให้เก็บการทำงานข้าม tenant ไว้หลัง use case และ policy สำหรับผู้ดูแลระบบที่แยกต่างหาก `IncludeSoftDeleted()` ยังคง query filter ของ tenant ไว้ ส่วน `IgnoreQueryFilters()` เปล่า ๆ จะปิด filter ทั้งสอง

package นี้ไม่ resolve tenant ไม่จัดการสมาชิกภาพ และไม่ขึ้นกับ PostgreSQL หรือ Identity

Repository และคู่มือสถาปัตยกรรมฉบับเต็ม: https://github.com/athene-x/Whimclean
