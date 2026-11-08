# Athenex.Whimclean.Infrastructure.EntityFrameworkCore

[English](README.md)

ชั้น persistence ของ EF Core แบบไม่ผูกกับ provider สำหรับ audit stamp และ soft delete แบบ shadow property, การแปลง concurrency exception, การ dispatch domain event และการลงทะเบียน unit of work

## การติดตั้ง

```shell
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer # example provider
```

package นี้อ้างอิง `Microsoft.EntityFrameworkCore` และ `Microsoft.EntityFrameworkCore.Relational` ผู้ใช้ต้องเพิ่ม provider เอง เมื่อฐานข้อมูลเป้าหมายเป็น PostgreSQL ให้ใช้ adapter สำหรับ PostgreSQL แทนการอ้างอิง Npgsql โดยตรง

## สร้าง business context

```csharp
using Athenex.Whimclean.Application.Context;
using Athenex.Whimclean.Infrastructure.Auditing;
using Athenex.Whimclean.Infrastructure.DomainEvents;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    TimeProvider timeProvider,
    ICurrentUser<Guid> currentUser,
    IAuditActorResolver<Guid, Guid?> auditActorResolver,
    IDomainEventDispatcher domainEventDispatcher)
    : AuditableDbContext<Guid, Guid?>(
        options,
        timeProvider,
        currentUser,
        auditActorResolver,
        domainEventDispatcher)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<Order>(builder =>
        {
            builder.HasAuditStamps();
            builder.HasSoftDelete();
        });
    }
}
```

`TUserId` คือตัวระบุผู้ใช้ปัจจุบันที่เชื่อถือได้ `TActor` คือค่าที่เก็บในคอลัมน์ actor ของ audit stamp และอาจเป็น nullable ได้ `OnModelCreating` ถูก seal ไว้ โดยจะเรียก `ConfigureModel` ก่อน จากนั้นเพิ่ม shadow property ของ audit stamp และ soft delete พร้อม query filter ให้ entity type ที่เลือกเข้าร่วม แล้วจึงเรียก `ConfigureAdditionalInfrastructureModel` (จุดต่อขยายที่ context ของ multitenancy ใช้) override `ValidateApplicationWrites` เพื่อตรวจสอบการเปลี่ยนแปลงที่ถูกติดตามก่อนการประทับค่าและ throw เพื่อยกเลิกการ commit

## audit stamp และ soft delete

โมเดล domain ไม่ประกาศฟิลด์เหล่านี้ entity type เลือกเข้าร่วมใน `ConfigureModel` ด้วย extension บน `EntityTypeBuilder` จาก `Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Persistence` แล้ว context เพิ่ม shadow property ด้วย `TActor` ของตัวเอง:

| extension | shadow property | ชนิด |
| --- | --- | --- |
| `HasAuditStamps()` | `CreatedAt` | `DateTimeOffset` |
| | `CreatedBy` | `TActor`; required เมื่อ `TActor` เป็น value type ที่ไม่ nullable เช่น `Guid` นอกนั้น nullable (รวม reference type ทุกชนิดเช่น `string`) |
| | `UpdatedAt` | `DateTimeOffset?` |
| | `UpdatedBy` | `TActor` แบบ nullable |
| `HasSoftDelete()` | `IsDeleted` | `bool` |
| | `DeletedAt` | `DateTimeOffset?` |
| | `DeletedBy` | `TActor` แบบ nullable |

ชื่อ property เป็นค่าคงที่ใน `AuditStampNames` และ `SoftDeleteNames` ส่วน annotation ที่บันทึกการเลือกเข้าร่วมอยู่ใน `PersistenceAnnotations` (`Whimclean:AuditStamps`, `Whimclean:SoftDelete`) อ่านค่าใน query ด้วย `EF.Property` และตั้งชื่อคอลัมน์ได้ตามปกติ เพราะ context ใช้ property ที่มีอยู่แล้วซ้ำแทนการเพิ่มใหม่:

```csharp
builder.Property<DateTimeOffset>(AuditStampNames.CreatedAt).HasColumnName("created_at");

var recent = await dbContext.Orders
    .OrderByDescending(order => EF.Property<DateTimeOffset>(order, AuditStampNames.CreatedAt))
    .ToListAsync(cancellationToken);
```

หาก entity ประกาศ CLR property ชื่อและชนิดเดียวกันเอง (เช่น domain ต้องใช้ `CreatedAt` ในกฎของตัวเอง) context จะประทับค่าลง property นั้นแทนการเพิ่ม shadow property ทั้งสอง extension ต้องเรียกบน root entity type ของลำดับชั้นการสืบทอด เพราะ EF Core ยอมให้ตั้ง query filter ได้เฉพาะ root type

### soft delete

การ `Remove` entity ของ type ที่ `HasSoftDelete()` ไม่ลบแถว การบันทึกแปลงการลบนั้นเป็นการตั้ง `IsDeleted` พร้อมประทับ `DeletedAt`/`DeletedBy` (และ `UpdatedAt`/`UpdatedBy` เมื่อ type นั้นมี audit stamp ด้วย) จากนั้น query filter `SoftDeleteFilter` ซ่อนแถวนั้นจาก query ทั่วไป

```csharp
var order = await dbContext.Orders.SingleAsync(order => order.Id == id, cancellationToken);
dbContext.Orders.Remove(order);              // soft delete เมื่อบันทึก
await dbContext.CommitAsync(cancellationToken);

var deleted = await dbContext.Orders
    .IncludeSoftDeleted()                    // ต้องเปิด filter ก่อนจึงเห็นแถวที่ลบ
    .SingleAsync(order => order.Id == id, cancellationToken);
dbContext.Restore(deleted);                  // ล้าง IsDeleted; การบันทึกล้าง DeletedAt/DeletedBy ให้
await dbContext.CommitAsync(cancellationToken);

dbContext.HardDelete(deleted);               // ลบแถวจริง สำหรับงานล้างข้อมูลที่ได้รับอนุญาต
await dbContext.CommitAsync(cancellationToken);
```

เนื่องจากแถวยังอยู่ในตาราง cascade ของฐานข้อมูลจึงไม่ทำงาน: entity ลูกที่ถูกติดตามอยู่ใน context เดียวกันจะถูก soft delete ตาม (หรือลบจริงถ้า type นั้นไม่ได้ `HasSoftDelete()`) ส่วนลูกที่ไม่ได้โหลดมาจะไม่ถูกแตะและยังมองเห็นอยู่ ให้โหลดลูกที่ต้องการให้หายไปด้วยก่อนเรียก `Remove`

`IsSoftDeleted(entity)` อ่าน flag ของ entity ที่ถูกติดตาม `Restore` throw `InvalidOperationException` เมื่อ type นั้นไม่ได้ `HasSoftDelete()` การนำเข้าแถวที่ถูกลบไปแล้วในอดีตทำได้โดยตั้ง flag หลัง `Add`: `dbContext.Entry(order).Property(SoftDeleteNames.IsDeleted).CurrentValue = true;` ค่าที่ระบุมากับ record ที่เพิ่มจะคงไว้ตามเดิม

## ลงทะเบียนกับ EF Core provider ใดก็ได้

```csharp
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore;

services.AddSingleton(TimeProvider.System);
services.AddScoped<ICurrentUser<Guid>, HttpCurrentUser>();
services.AddNullableAuditActorResolver<Guid>();
services.AddDomainEventDispatcher();

services.AddEntityFrameworkCoreUnitOfWork<ApplicationDbContext>((_, options) =>
    options.UseSqlServer(connectionString));
```

`AddEntityFrameworkCoreUnitOfWork<TContext>` กำหนดให้ context ต้อง implement `IUnitOfWork` และลงทะเบียน context แบบ scoped เป็น interface นั้นด้วย ใช้ `AddEntityFrameworkCoreContext<TContext>` สำหรับ context ที่ framework เป็นเจ้าของและไม่ควรกลายเป็น unit of work ของแอปพลิเคชัน โดยจะลงทะเบียนเฉพาะ DbContext เท่านั้น ในแอปพลิเคชัน ASP.NET Core package `Athenex.Whimclean.Api.AspNetCore` มี `ICurrentUser<TUserId>` แบบอิง claim ให้ผ่าน `AddClaimsCurrentUser<TUserId>`

## พฤติกรรมของ persistence

- ทุก record ที่ถูกเพิ่ม แก้ไข หรือลบ ต้องมีผู้ใช้ปัจจุบัน มิฉะนั้นการบันทึกจะ throw `CurrentUserRequiredException` งานเบื้องหลังควรระบุผู้ใช้ระบบอย่างชัดเจน เช่น `StaticCurrentUser<TUserId>` จาก `Athenex.Whimclean.Application.Context`
- entity type ที่ `HasAuditStamps()` ได้ `CreatedAt`/`CreatedBy` ตอนเพิ่ม และ `UpdatedAt`/`UpdatedBy` ตอนแก้ไข จาก `TimeProvider` และ `IAuditActorResolver`
- entity type ที่ `HasSoftDelete()` และไม่ใช่ owned type ได้ query filter ชื่อ `SoftDeleteFilter` (`QueryFilterNames.SoftDelete`) `Remove` ถูกแปลงเป็น soft delete ตอนบันทึก `HardDelete` ลบแถวจริง และ `Restore` กู้คืน
- ค่าประทับการลบ (`DeletedAt`/`DeletedBy`) จะถูกเขียนเมื่อ flag `IsDeleted` เปลี่ยนค่าระหว่างการแก้ไข และถูกล้างเมื่อกู้คืน record ที่ถูกเพิ่มในสถานะลบแล้วจะคงค่าที่ระบุมา ดังนั้นการนำเข้าข้อมูลจึงรักษาความจริงในอดีตไว้ได้
- `DbUpdateConcurrencyException` จะกลายเป็น `PersistenceConcurrencyException`
- domain event ของ aggregate จะถูก snapshot ก่อนการบันทึก ถูกล้างออกจาก aggregate เมื่อบันทึกสำเร็จ แล้วจึง dispatch ด้วย `CancellationToken.None` หาก handler ล้มเหลวจะ throw `DomainEventDispatchException` พร้อม event ที่ยังไม่ถูกส่งใน `UndeliveredEvents` ห้ามบันทึกซ้ำ
- `SaveChanges` แบบ synchronous ไม่รองรับโดยเจตนา ให้ใช้ `SaveChangesAsync` หรือ `IUnitOfWork.CommitAsync`

สำหรับ outbox หรือการรับประกันการส่งข้อความแบบ atomic ให้ implement รูปแบบเฉพาะของแอปพลิเคชันนั้นใน context ฝั่งผู้ใช้ การ dispatch event หลังบันทึกโดยตรงทำงานเฉพาะใน process เท่านั้น

## query สำหรับงานดูแลระบบ

named filter ของ EF Core 10 ทำให้ query ที่ได้รับอนุญาตเป็นพิเศษสามารถปิดเฉพาะการกรอง soft delete ได้:

```csharp
var includingDeleted = dbContext.Orders
    .IgnoreQueryFilters(["SoftDeleteFilter"]);
```

`IncludeSoftDeleted()` จาก `Athenex.Whimclean.Infrastructure.EntityFrameworkCore` ทำสิ่งเดียวกันโดยอ้างอิงชื่อ filter ควรใช้วิธีใดวิธีหนึ่งนี้แทน `IgnoreQueryFilters()` เปล่า ๆ ซึ่งจะปิดการแยกข้อมูลของ tenant ไปด้วย

package พื้นฐานนี้ไม่มี dependency ต่อ PostgreSQL, Identity, FastEndpoints หรือ multitenancy

## query รายการ

helper ใน `Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Queries` เปลี่ยน `ListQueryOptions` ให้เป็น query ที่อ่านได้ตามลำดับ filter, search, count, order, page, project:

```csharp
using Athenex.Whimclean.Application.Pagination;
using Athenex.Whimclean.Application.Queries;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Queries;
using Microsoft.EntityFrameworkCore;

private static readonly SortFields<Tenant> Sort = new()
{
    { "slug", tenant => tenant.Slug },
    { "name", tenant => tenant.Name },
    { "status", tenant => tenant.Status }
};

public async ValueTask<OffsetPagedResult<TenantResponse>> ListAsync(ListQueryOptions options, CancellationToken ct)
{
    var query = dbContext.Tenants.AsNoTracking();

    foreach (var filter in options.Filters)
    {
        query = (filter.Field, filter.Operator) switch
        {
            ("status", FilterOperator.Equal) when EnumFilter.TryParse<TenantStatus>(filter.Value, out var status) =>
                query.Where(tenant => tenant.Status == status),
            ("status", FilterOperator.In) => ApplyStatusIn(query, EnumFilter.Parse<TenantStatus>(filter.Values)),
            _ => query
        };
    }

    query = query.SearchLike(options.SearchTerm, tenant => tenant.Slug, tenant => tenant.Name);

    var total = await query.LongCountAsync(ct);
    var items = await Sort
        .Order(query, options.Sort, tenant => tenant.Name, tenant => tenant.Id)
        .Page(options)
        .Select(tenant => new TenantResponse(tenant.Id, tenant.Slug, tenant.Name, tenant.Status))
        .ToListAsync(ct);

    return items.ToPage(options, total);
}
```

- `SortFields<T>` จับคู่ชื่อฟิลด์เรียงลำดับที่ endpoint ยอมรับเข้ากับ key selector และนำ term ไปใช้ตามลำดับ โดยข้ามชื่อที่ไม่รู้จัก overload ของ `Order` ที่รับ key เริ่มต้นและ tie-breaker จะไม่คืนค่า null และทำให้หน้าผลลัพธ์คงที่
- `SearchLike` จับคู่สมาชิกชนิด string ตัวใดตัวหนึ่งที่ระบุด้วย `EF.Functions.Like` โดย escape `%`, `_` และ `\` ในคำค้นผ่าน `LikePattern` pattern ถูกส่งเป็น query parameter ทำให้ compiled query ถูกนำกลับมาใช้ซ้ำได้ข้ามคำค้น ความแตกต่างของตัวพิมพ์เป็นไปตาม collation ของคอลัมน์ package สำหรับ PostgreSQL มี `SearchILike` ที่ไม่สนใจตัวพิมพ์เพิ่มให้
- `Search` รับตัวสร้าง predicate ของคุณเอง ซึ่งจะได้รับ contains pattern ที่ escape แล้ว เมื่อการค้นหาต้องการมากกว่า LIKE `SearchPredicate.AnyMatch` ประกอบ predicate หลายฟิลด์บนฟังก์ชันจับคู่ใดก็ได้
- `Page` นำ `Offset` และ `Size` ไปใช้กับ query ที่เรียงลำดับแล้ว `ToPage` ห่อรายการที่ materialize แล้วด้วยหมายเลขหน้า ขนาดหน้า และจำนวนรวม
- `EnumFilter` จาก package Application แปลงค่าของ filter เป็นสมาชิกของ enum โดยข้ามชื่อที่ไม่รู้จักซึ่ง validator ปฏิเสธไปแล้ว

## นำ migration ไปใช้จาก image ของแอปพลิเคชัน

```csharp
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Operations;

var app = builder.Build();

if (DatabaseMigration.IsRequested(args))
{
    await app.Services.MigrateDatabaseAsync<ApplicationDbContext>();
    return;
}
```

จากนั้นการ deploy จะรัน container เดียวกันหนึ่งครั้งด้วย `--migrate` (`DatabaseMigration.CommandLineSwitch` เทียบแบบไม่สนใจตัวพิมพ์) ก่อนเริ่มแอปพลิเคชัน แทนการส่งเครื่องมือ EF ไปยังเซิร์ฟเวอร์ migration ที่ค้างอยู่จะถูก log ตามชื่อ และ `MigrateDatabaseAsync` คืนค่าจำนวน migration ที่ถูกนำไปใช้

Repository และคู่มือสถาปัตยกรรมฉบับเต็ม: https://github.com/athene-x/Whimclean
