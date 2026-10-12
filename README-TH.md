# Athenex.Whimclean

[English](README.md)

ชุด building block สำหรับ .NET 10 ครอบคลุม domain-driven design, งานในชั้น application, EF Core แบบไม่ผูกกับ provider, PostgreSQL, multitenancy แบบเลือกใช้ได้ และ API แบบ FastEndpoints การยืนยันตัวตนเป็นแบบนำมาเอง: แอปพลิเคชันยืนยันตัวตนกับ identity provider ภายนอก (Keycloak, Microsoft Entra ID, Auth0 หรือ ASP.NET Core Identity ที่ต่อโดยตรง) แล้ว Whimclean รับ principal ที่ได้ผ่าน `ICurrentUser<TUserId>` โดยแพ็กเกจเสริม `Identity.Keycloak` มีการลงทะเบียน Keycloak ให้พร้อมใช้ repository นี้ผลิตแพ็กเกจต่อไปนี้:

- [`Athenex.Whimclean.Domain`](src/Athenex.Whimclean.Domain/README-TH.md) ให้ entity, aggregate root, domain event, การ audit, soft delete, ผลลัพธ์ของการทำงาน (result) และการ normalize ข้อความ
- [`Athenex.Whimclean.Application`](src/Athenex.Whimclean.Application/README-TH.md) ให้สัญญาของ context ปัจจุบัน, abstraction สำหรับอีเมล, โมเดล pagination, สัญญาด้าน persistence และ query options ที่ผ่านการตรวจสอบพร้อมไวยากรณ์ของ `ListQueryParser` ขึ้นกับ `Athenex.Whimclean.Domain`
- [`Athenex.Whimclean.Infrastructure`](src/Athenex.Whimclean.Infrastructure/README-TH.md) ให้การแปลง audit actor แบบไม่ผูกกับ provider, pipeline ของ domain event ภายในโปรเซส และตัวระบุสำหรับ seed แบบกำหนดผลได้ (deterministic) ขึ้นกับ `Athenex.Whimclean.Domain`
- [`Athenex.Whimclean.Infrastructure.EntityFrameworkCore`](src/Athenex.Whimclean.Infrastructure.EntityFrameworkCore/README-TH.md) ให้การเขียนที่บังคับต้องมีผู้ใช้ปัจจุบัน, การ audit, query filter สำหรับ soft delete แบบมีชื่อ, การจัดการ concurrency, การส่ง domain event แบบ asynchronous, ตัวช่วยสำหรับ list query, สวิตช์ `--migrate` และการลงทะเบียน `DbContext`/`IUnitOfWork` ร่วมกัน ขึ้นกับ `Application`, `Infrastructure` และ EF Core (รวม `Microsoft.EntityFrameworkCore.Relational`) แต่ไม่ขึ้นกับ database provider ใด
- [`Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL`](src/Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL/README-TH.md) ให้ dependency ของ Npgsql, การลงทะเบียน PostgreSQL และตัวช่วยค้นหาแบบไม่สนตัวพิมพ์ `SearchILike` ขึ้นกับ `Infrastructure.EntityFrameworkCore`
- [`Athenex.Whimclean.Api.AspNetCore`](src/Athenex.Whimclean.Api.AspNetCore/README-TH.md) ให้ข้อตกลงของ API ที่ไม่ผูกกับ framework: สัญญา error ที่สม่ำเสมอ, การจัดการ exception, การตอบ status code, การแปลง result เป็น HTTP, การแปลงผู้ใช้ปัจจุบันจาก claim และจากตารางผู้ใช้ภายใน, health check endpoint, rate limit ต่อผู้เรียก และ options ที่ผ่านการตรวจสอบ ขึ้นกับ `Athenex.Whimclean.Application`
- [`Athenex.Whimclean.Api.FastEndpoints`](src/Athenex.Whimclean.Api.FastEndpoints/README-TH.md) ให้การลงทะเบียน FastEndpoints และ OpenAPI พร้อมการตอบผล validation บนสัญญา error ร่วม รวมถึงสัญญา `ListQueryRequest` และ validator ของมัน ขึ้นกับ `Api.AspNetCore`
- [`Athenex.Whimclean.Mediator`](src/Athenex.Whimclean.Mediator/README-TH.md) เป็นตัวเลือกสำหรับผสานกับ [Mediator](https://github.com/martinothamar/Mediator): pipeline behavior ที่ commit unit of work หลังแต่ละ command และการส่ง domain event เป็น notification ของ mediator ขึ้นกับ `Application`, `Infrastructure` และ `Mediator.Abstractions` ส่วนแอปพลิเคชันเป็นผู้จัดหา `Mediator.SourceGenerator`
- [`Athenex.Whimclean.Multitenancy.Abstractions`](src/Athenex.Whimclean.Multitenancy.Abstractions/README-TH.md) เป็นตัวเลือกที่ให้สัญญาสำหรับ marker ของ tenant/context ปัจจุบัน, การตรวจสอบสิทธิ์เข้าถึง, current tenant แบบ ambient สำหรับ host ที่ไม่ใช่ HTTP และ exception ของขอบเขต tenant ไม่มี dependency ต่อแพ็กเกจใด
- [`Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy`](src/Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy/README-TH.md) เป็นตัวเลือกที่เพิ่ม query filter ของ tenant แบบ fail-closed และการตรวจสอบการเขียนให้กับ EF Core context ที่ไม่ผูกกับ provider
- [`Athenex.Whimclean.Api.AspNetCore.Multitenancy`](src/Athenex.Whimclean.Api.AspNetCore.Multitenancy/README-TH.md) เป็นตัวเลือกที่แปลง tenant ที่ถูกร้องขอ (ยังไม่เชื่อถือ) จาก header ของ id หรือจาก header ของ slug ผ่าน slug lookup ที่ผู้ใช้งานเป็นเจ้าของ แล้วจึงตั้ง current tenant ก็ต่อเมื่อ membership validator ที่ผู้ใช้งานเป็นเจ้าของอนุญาตแล้วเท่านั้น ใช้ได้ทั้งกับ FastEndpoints และ Minimal APIs
- [`Athenex.Whimclean.Auditing`](src/Athenex.Whimclean.Auditing/README-TH.md) เป็นตัวเลือกที่บันทึกผลลัพธ์ actor และการเปลี่ยนแปลงของฟิลด์ของทุก command ที่ถูก audit ลงใน audit log ซึ่งเขียนนอก unit of work ขึ้นกับ `Application`, `Domain` และ `Mediator.Abstractions` ส่วน [`Athenex.Whimclean.Auditing.AspNetCore`](src/Athenex.Whimclean.Auditing.AspNetCore/README-TH.md) ป้อนชื่อผู้เรียก, correlation id, ที่อยู่ และ user agent จาก request ให้
- [`Athenex.Whimclean.Infrastructure.Email.Smtp`](src/Athenex.Whimclean.Infrastructure.Email.Smtp/README-TH.md) implement `IEmailSender` บน SMTP พร้อม options ที่ผ่านการตรวจสอบ ขึ้นกับ `Application`
- [`Athenex.Whimclean.Identity.Keycloak`](src/Athenex.Whimclean.Identity.Keycloak/README-TH.md) ลงทะเบียนการยืนยันตัวตนแบบ JWT bearer ตามธรรมเนียม claim ของ Keycloak และให้ client ของ admin API สำหรับสร้างผู้ใช้ ขึ้นกับ `Domain`

## การติดตั้ง

### Package feed

แพ็กเกจเผยแพร่บน NuGet feed ของ athene-x ที่ [GitHub Packages](https://github.com/orgs/athene-x/packages) ไม่ใช่บน nuget.org การใช้งานต้องเป็นสมาชิกขององค์กร athene-x:

1. สร้าง [personal access token (classic)](https://github.com/settings/tokens) ที่มี scope `read:packages`
2. ลงทะเบียน feed หนึ่งครั้งต่อเครื่อง:

   ```shell
   dotnet nuget add source \
     --username YOUR_GITHUB_USERNAME \
     --password YOUR_GITHUB_TOKEN \
     --store-password-in-clear-text \
     --name github \
     "https://nuget.pkg.github.com/athene-x/index.json"
   ```

3. หาก repository ที่ใช้งานมี `packageSourceMapping` ใน `nuget.config` การเพิ่ม source อย่างเดียวไม่พอ — restore จะไม่พิจารณา source ที่ไม่ได้ map ต้อง map prefix ของแพ็กเกจด้วย:

   ```xml
   <packageSourceMapping>
     <packageSource key="github">
       <package pattern="Athenex.Whimclean.*" />
     </packageSource>
   </packageSourceMapping>
   ```

### แพ็กเกจ

```shell
dotnet add package Athenex.Whimclean.Domain
dotnet add package Athenex.Whimclean.Application
dotnet add package Athenex.Whimclean.Infrastructure
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL
dotnet add package Athenex.Whimclean.Api.AspNetCore
dotnet add package Athenex.Whimclean.Api.FastEndpoints
dotnet add package Athenex.Whimclean.Mediator
dotnet add package Athenex.Whimclean.Multitenancy.Abstractions
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy
dotnet add package Athenex.Whimclean.Api.AspNetCore.Multitenancy
dotnet add package Athenex.Whimclean.Auditing
dotnet add package Athenex.Whimclean.Auditing.AspNetCore
dotnet add package Athenex.Whimclean.Infrastructure.Email.Smtp
dotnet add package Athenex.Whimclean.Identity.Keycloak
```

ติดตั้งเฉพาะแพ็กเกจระดับบนสุดที่แอปพลิเคชันใช้ เช่น API ที่ใช้ PostgreSQL อ้างอิง `Infrastructure.EntityFrameworkCore.PostgreSQL` และ `Api.FastEndpoints` ก็พอ NuGet จะดึง dependency ระดับล่างมาให้แบบ transitive

### ชุดแพ็กเกจที่ใช้บ่อย

| ความต้องการของแอปพลิเคชัน | แพ็กเกจที่อ้างอิงโดยตรง |
| --- | --- |
| ใช้เฉพาะสัญญาของ domain และ application | `Domain`, `Application` |
| API ธุรกิจบน PostgreSQL | `Infrastructure.EntityFrameworkCore.PostgreSQL`, `Api.FastEndpoints` |
| ใช้ Minimal APIs แทน FastEndpoints | `Infrastructure.EntityFrameworkCore.PostgreSQL`, `Api.AspNetCore` |
| เพิ่มการแยกข้อมูลตาม tenant ใน EF | แพ็กเกจก่อนหน้า บวก `Infrastructure.EntityFrameworkCore.Multitenancy` |
| เพิ่มการแปลง tenant จาก HTTP และการอนุญาตตาม membership | แพ็กเกจก่อนหน้า บวก `Api.AspNetCore.Multitenancy` |
| commit แต่ละ command ผ่าน Mediator | แพ็กเกจก่อนหน้า บวก `Mediator` (และ `Mediator.SourceGenerator` จากฝั่งแอปพลิเคชัน) |
| audit ทุก command | แพ็กเกจก่อนหน้า บวก `Auditing` และ `Auditing.AspNetCore` |
| ยืนยันตัวตนกับ Keycloak และสร้างผู้ใช้ของมัน | แพ็กเกจก่อนหน้า บวก `Identity.Keycloak` |
| ส่งอีเมลผ่าน SMTP | แพ็กเกจก่อนหน้า บวก `Infrastructure.Email.Smtp` |

แต่ละไดเรกทอรีของแพ็กเกจมี README เฉพาะทางที่ครอบคลุมการติดตั้ง ขอบเขตความรับผิดชอบ และตัวอย่างการใช้งาน ส่วนที่เหลือของคู่มือหลักนี้แสดงวิธีที่แพ็กเกจประกอบกันในแอปพลิเคชันเดียว

## ขอบเขตของแพ็กเกจ infrastructure

ทิศทางของ dependency ตั้งใจให้เป็นทางเดียว:

```text
Infrastructure.EntityFrameworkCore.PostgreSQL
    -> Infrastructure.EntityFrameworkCore
        -> Infrastructure
        -> Application
            -> Domain

Infrastructure.EntityFrameworkCore.Multitenancy
    -> Infrastructure.EntityFrameworkCore
    -> Multitenancy.Abstractions

Infrastructure.Email.Smtp
    -> Application

Api.FastEndpoints
    -> Api.AspNetCore
        -> Application

Api.AspNetCore.Multitenancy
    -> Api.AspNetCore
    -> Multitenancy.Abstractions

Mediator
    -> Application
    -> Infrastructure

Auditing.AspNetCore
    -> Auditing
        -> Application
        -> Domain

Identity.Keycloak
    -> Domain
```

โปรเจกต์ PostgreSQL ไม่มี pipeline ของการ audit, tenant, soft delete, concurrency หรือ domain event พฤติกรรมด้าน persistence ที่ใช้ร่วมกันอยู่ในแพ็กเกจ EF Core ที่ไม่ผูกกับ provider ส่วนพฤติกรรมของ tenant เป็นแพ็กเกจ decorator แบบเลือกใช้ได้ ดังนั้นแพ็กเกจ provider ในอนาคตต้องการเพียง dependency ของ provider นั้นกับเมธอดลงทะเบียนที่ส่งต่อไปยัง `AddEntityFrameworkCoreUnitOfWork<TContext>`

## Entity และ aggregate root

```csharp
using Athenex.Whimclean.Domain.Entities;
using Athenex.Whimclean.Domain.Events;

public sealed record OrderCreated(Guid OrderId) : DomainEvent;

public sealed class Order : AggregateRoot<Guid>
{
    public Order(Guid id) : base(id)
    {
        RaiseDomainEvent(new OrderCreated(id));
    }
}
```

event ที่ raise แล้วเปิดให้เข้าถึงผ่าน `DomainEvents` การล้าง event เป็นเรื่องของ infrastructure: `ClearDomainEvents()` เข้าถึงได้ผ่าน `IHasDomainEvents` เท่านั้น และ EF Core context จะเรียกมันหลังบันทึกสำเร็จ ก่อนส่ง event ออกไป

## การ audit

ชนิดของ actor กำหนด nullability ได้ในตัว ทำให้แต่ละแอปพลิเคชันเลือกใช้ตัวระบุ ชื่อ หรือชนิด actor ที่กำหนดเองได้

```csharp
using Athenex.Whimclean.Domain.Auditables;

public sealed class OrderAudit : IAuditable<Guid?>
{
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? CreatedBy { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public Guid? UpdatedBy { get; init; }
}

public sealed class ImportAudit : IAuditable<string?>
{
    public DateTimeOffset CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}
```

`AuditableEntity<TId, TActor>` และ `AuditableAggregateRoot<TId, TActor>` ประกาศฟิลด์ทั้งสี่นี้ไว้ครั้งเดียว entity ที่สืบทอดจากมันจึงไม่ต้องประกาศซ้ำ EF Core context จะประทับค่าให้ตอนบันทึก

แพ็กเกจเสริม `Auditing` บันทึกผลลัพธ์ของ command ไม่ใช่ฟิลด์ของแถว command ของ Mediator implement `IAuditedCommand` แล้ว `AuditCommandBehavior` จะเขียนรายการ `AuditLog<TUserId, TTenantId>` ผ่าน `IAuditLogWriter<TUserId, TTenantId>` ของแอปพลิเคชันนอก unit of work ของ command นั้น ส่วน handler ของ domain event เพิ่มการเปลี่ยนแปลงของฟิลด์ผ่าน `IAuditChangeCollector`:

```csharp
using Athenex.Whimclean.Auditing;
using Athenex.Whimclean.Auditing.AspNetCore;
using Athenex.Whimclean.Domain.Results;
using Athenex.Whimclean.Mediator;
using Mediator;

public sealed record SuspendTenantCommand(Guid TenantId, string Reason)
    : ICommand<Result>, IAuditedCommand
{
    public AuditDescriptor Describe() => new(
        Action: "Tenant.Suspend",
        EntityType: "Tenant",
        EntityId: TenantId.ToString(),
        TenantId: TenantId,
        Reason: Reason);
}

builder.Services.AddAuditing<Guid, Guid>();
builder.Services.AddUnitOfWorkBehavior();
builder.Services.AddScoped<IAuditLogWriter<Guid, Guid>, AuditLogWriter>();
builder.Services.AddHttpAuditRequestContext();
```

`AddAuditing` ต้องเรียกก่อน `AddUnitOfWorkBehavior` เพื่อให้ audit behavior ครอบ unit of work และเห็นผลลัพธ์ที่ commit แล้ว `AddHttpAuditRequestContext` จาก `Auditing.AspNetCore` แทนที่ `NullAuditRequestContext` ค่าเริ่มต้นด้วยชื่อ actor, correlation id (`X-Correlation-Id`, `X-Request-Id` หรือ trace identifier), ที่อยู่ปลายทาง และ user agent ของ request

## Result

```csharp
using Athenex.Whimclean.Domain.Results;

Result<Guid> CreateOrder(bool isValid) => isValid
    ? Result<Guid>.Success(Guid.NewGuid())
    : Result<Guid>.Failure(
        Error.Validation("Order.Invalid", "The order is invalid."));
```

การเข้าถึง `Value` ของ result ที่ล้มเหลวจะ throw `InvalidOperationException` ให้ตรวจ `IsSuccess` หรือ `IsFailure` ก่อนอ่านค่า

## Query ในชั้น application

```csharp
using Athenex.Whimclean.Application.Queries;

var options = new ListQueryOptions(
    searchTerm: "pending",
    sort: [new SortTerm("createdAt", SortDirection.Descending)],
    filters: [new FilterTerm("status", "Pending")],
    page: 1,
    size: 20);
```

query options ตรวจสอบขีดจำกัดของตัวเองและเปิดเผย collection แบบอ่านอย่างเดียวที่ materialize แล้ว ทำให้โค้ดฝั่ง infrastructure แปลงเฉพาะฟิลด์และ operator ที่อนุมัติแล้วไปเป็น query ของฐานข้อมูล

`ListQueryParser` สร้าง options ชุดเดียวกันจากค่าดิบใน query string filter ใช้รูปแบบ `field:operator:value` ด้วย operator `eq`, `ne`, `gt`, `ge`, `lt`, `le`, `cn`, `sw`, `ew` และ `in` (ค่าคั่นด้วยจุลภาค) ส่วน sort เป็นรายชื่อฟิลด์คั่นด้วยจุลภาค ใส่ `-` นำหน้าเพื่อเรียงจากมากไปน้อย `EnumFilter` ใน namespace เดียวกันแปลงค่าของ filter เป็นสมาชิกของ enum สำหรับนำไปแปลต่อ:

```csharp
var parsed = ListQueryParser.Parse(
    searchTerm: "pending",
    sort: "-createdAt",
    filters: ["status:in:Pending,Paid"],
    page: 1,
    size: 20);
```

ใน API แบบ FastEndpoints ให้สืบทอด request จาก `ListQueryRequest` และ validator จาก `ListQueryRequestValidator<TRequest>` เพื่อกำหนดรายการฟิลด์ sort และ filter ที่อนุญาตของแต่ละ endpoint จากนั้น `ToListQueryOptions()` จะ parse request ที่ผ่านการตรวจสอบแล้ว:

```csharp
using Athenex.Whimclean.Api.FastEndpoints.Queries;
using Athenex.Whimclean.Application.Queries;

public sealed class ListOrdersRequest : ListQueryRequest;

public sealed class ListOrdersRequestValidator
    : ListQueryRequestValidator<ListOrdersRequest>
{
    public ListOrdersRequestValidator()
    {
        AllowSortFields("createdAt", "total");
        AllowEnumFilterField<OrderStatus>("status");
        AllowFilterField(
            "createdAt",
            FilterOperator.GreaterThanOrEqual,
            FilterOperator.LessThan);
    }
}
```

แพ็กเกจ EF Core เป็นผู้แปล options: `SortFields<T>` map ชื่อ sort ที่อนุญาตไปยัง key selector, `SearchLike` (หรือ `SearchILike` จากแพ็กเกจ PostgreSQL) ค้นหาหลายคอลัมน์โดย escape wildcard ให้ และ `Page`/`ToPage` ใส่ offset และห่อผลลัพธ์:

```csharp
using Athenex.Whimclean.Application.Queries;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL.Queries;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Queries;
using Microsoft.EntityFrameworkCore;

private static readonly SortFields<Order> OrderSort = new()
{
    { "createdAt", order => order.CreatedAt },
    { "total", order => order.Total }
};

var query = dbContext.Orders
    .SearchILike(options.SearchTerm, order => order.Number, order => order.CustomerName);

var totalItems = await query.LongCountAsync(cancellationToken);

var items = await OrderSort
    .Order(
        query,
        options.Sort,
        order => order.CreatedAt,
        order => order.Id,
        SortDirection.Descending)
    .Page(options)
    .ToListAsync(cancellationToken);

return items.ToPage(options, totalItems);
```

## Pagination

```csharp
using Athenex.Whimclean.Application.Pagination;

var result = new OffsetPagedResult<string>(
    items: ["first", "second"],
    page: 1,
    size: 20,
    totalItems: 2);
```

`Map` แปลงรายการในหน้าโดยคงหมายเลขหน้า ขนาด และยอดรวมไว้ ส่วน `KeysetPagedResult<T, TKey>` คือคู่ขนานแบบใช้ cursor

## อีเมล

implement `IEmailSender` ในชั้น infrastructure หรือลงทะเบียน implementation แบบ SMTP ที่มาพร้อมแพ็กเกจ แล้วเรียกใช้จาก use case ในชั้น application:

```csharp
var message = new EmailMessage(
    recipient: "member@example.com",
    subject: "Welcome",
    textBody: "Your account is ready.");

await emailSender.SendAsync(message, cancellationToken);
```

`Infrastructure.Email.Smtp` ส่งข้อความผ่าน `System.Net.Mail` พร้อม options สำหรับ host, port, credential, TLS และผู้ส่งเริ่มต้น ให้ bind `SmtpEmailOptions` จาก section `Email` และตรวจสอบตอนเริ่มระบบ:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Configuration;
using Athenex.Whimclean.Infrastructure.Email.Smtp;

builder.Services.AddValidatedOptions<SmtpEmailOptions>(
    builder.Configuration,
    SmtpEmailOptions.SectionName,
    options => options.Validate());
builder.Services.AddSmtpEmailSender();
```

## EF Core และ database provider

สืบทอด context ของแอปพลิเคชันที่ไม่ใช้ tenant จาก `AuditableDbContext<TUserId, TActor>` โดย `TUserId` คือตัวระบุผู้ใช้ที่ยืนยันตัวตนแล้ว ส่วน `TActor` คือรูปแบบที่เก็บลงในฟิลด์ audit

```csharp
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
    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}
```

ลงทะเบียน implementation ของผู้ใช้ปัจจุบันและตัวส่ง domain event `NullableAuditActorResolver<TUserId>` แปลง user ID ชนิด value type ที่ไม่เป็น null เช่น `Guid` ให้เป็นฟิลด์ audit แบบ nullable เช่น `Guid?`:

```csharp
services.AddSingleton(TimeProvider.System);
services.AddClaimsCurrentUser<Guid>();
services.AddSingleton<
    IAuditActorResolver<Guid, Guid?>,
    NullableAuditActorResolver<Guid>>();
services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();
services.AddPostgreSqlUnitOfWork<ApplicationDbContext>(connectionString);
```

`AddNullableAuditActorResolver<Guid>()`, `AddIdentityAuditActorResolver<Guid>()`, `AddDomainEventDispatcher()` และ `AddMediatorDomainEventDispatcher()` เป็นทางลัดที่เทียบเท่ากันและใช้ semantics แบบ `TryAdd`

options เฉพาะของ Npgsql สำหรับ PostgreSQL อยู่ที่ขอบของ provider:

```csharp
services.AddPostgreSqlUnitOfWork<ApplicationDbContext>(
    connectionString,
    postgreSql => postgreSql.MigrationsAssembly("MyApp.Migrations.PostgreSQL"));
```

หากต้องการใช้ provider อื่นก่อนที่ Whimclean จะมี adapter เฉพาะ ให้อ้างอิง `Athenex.Whimclean.Infrastructure.EntityFrameworkCore` บวกกับ provider นั้น แล้วตั้งค่าผ่านเมธอดลงทะเบียนร่วม:

```csharp
services.AddEntityFrameworkCoreUnitOfWork<ApplicationDbContext>((_, options) =>
    options.UseSqlServer(connectionString));
```

วิธีนี้ยังคง `AuditableDbContext`, query filter ของ soft delete, ขอบเขต concurrency, การส่ง domain event และการลงทะเบียน `IUnitOfWork` ชุดเดิมไว้ เพิ่มแพ็กเกจ EF multitenancy แบบเลือกใช้ได้เมื่อต้องแยกข้อมูลตาม tenant สิ่งที่เปลี่ยนมีเพียงการตั้งค่า provider

image ของแอปพลิเคชันสามารถ apply migration ของตัวเองได้ `DatabaseMigration.IsRequested(args)` ตรวจจับสวิตช์ `--migrate` และ `MigrateDatabaseAsync<TContext>` apply migration ที่ค้างอยู่ พร้อม log ชื่อของมันและคืนค่าจำนวนที่รัน:

```csharp
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Operations;

var app = builder.Build();

if (DatabaseMigration.IsRequested(args))
{
    await app.Services.MigrateDatabaseAsync<ApplicationDbContext>();
    return;
}
```

### การยืนยันตัวตนและผู้ใช้ปัจจุบัน

Whimclean ไม่เก็บผู้ใช้ credential หรือ token ให้ยืนยันตัวตนกับ identity provider ภายนอก (Keycloak, Microsoft Entra ID, Auth0) หรือต่อ ASP.NET Core Identity โดยตรงกับ API ของ Microsoft เอง ไม่ว่าทางใด ไลบรารีรับเฉพาะ principal ที่ยืนยันตัวตนแล้วผ่าน `ICurrentUser<TUserId>`

สำหรับ scheme ใดก็ตามที่เติมค่า `HttpContext.User` `Api.AspNetCore` มี implementation ที่อิง claim ให้:

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://keycloak.example.com/realms/whimclean";
        options.Audience = "whimclean-api";
    });

builder.Services.AddClaimsCurrentUser<Guid>();
```

ชนิด claim เริ่มต้นคือ `ClaimTypes.NameIdentifier` ส่ง `options => options.ClaimType = "sub"` เมื่อปิด inbound claim mapping การลงทะเบียนใช้ `TryAddScoped` ดังนั้น `ICurrentUser<TUserId>` ที่แอปพลิเคชันจัดหาเองจะชนะ

`Identity.Keycloak` แทนที่การตั้งค่า JWT bearer ที่เขียนเองด้วย `AddKeycloakAuthentication` ซึ่ง bind จาก section `Keycloak` (`Authority`, `Audience`, `MetadataAddress` แบบเลือกใส่, `RequireHttpsMetadata`, `NameClaimType`, `RoleClaimType`) มันปิด inbound claim mapping เพื่อให้ `sub` ยังคงเป็น `sub` ตรึง issuer ที่ถูกต้องไว้ที่ `Authority` และลงทะเบียนบริการ authorization ให้ด้วย ส่วน `AddKeycloakAdministration` เพิ่ม `IKeycloakUserAdministration` ซึ่งเป็น typed client บน admin API ที่ใช้ service account จาก section `KeycloakAdmin`:

```csharp
using Athenex.Whimclean.Api.AspNetCore;
using Athenex.Whimclean.Identity.Keycloak;

builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddKeycloakAdministration(builder.Configuration);
builder.Services.AddClaimsCurrentUser<Guid>(options => options.ClaimType = "sub");
```

เมื่อแอปพลิเคชันมีตารางผู้ใช้ของตัวเองและ token มีเพียง subject ของ identity provider ให้ใช้ `AddLocalUserResolution<TUserId>` แทนผู้ใช้ปัจจุบันแบบอิง claim middleware จะ map claim ของ subject ไปยัง id ภายในผ่าน `ILocalUserLookup<TUserId>` ของแอปพลิเคชัน แล้วบันทึกลงใน `RequestCurrentUser<TUserId>` แบบ scoped ส่วน subject ที่ไม่มีผู้ใช้ภายในจะปล่อยให้ผู้ใช้ปัจจุบันว่างไว้ วาง middleware หลังการยืนยันตัวตนและก่อนสิ่งใดก็ตามที่อ่านผู้ใช้ปัจจุบัน:

```csharp
using Athenex.Whimclean.Api.AspNetCore;
using Athenex.Whimclean.Api.AspNetCore.Context;

builder.Services.AddLocalUserResolution<Guid>(options => options.ClaimType = "sub");
builder.Services.AddScoped<ILocalUserLookup<Guid>, LocalUserLookup>();

app.UseAuthentication();
app.UseLocalUserResolution<Guid>();
app.UseAuthorization();
```

ทุกรายการที่ถูกเพิ่ม แก้ไข หรือลบ ต้องให้ `ICurrentUser<TUserId>` ระบุผู้ใช้ได้ รวมถึงรายการที่ไม่ได้ implement interface ด้าน audit งานเบื้องหลังควรลงทะเบียน implementation ของผู้ใช้ระบบอย่างชัดเจน เช่น `StaticCurrentUser<TUserId>` แทนการข้ามขอบเขตการเขียนไปเงียบ ๆ ส่วน `ICurrentUserInitializer<TUserId>` ให้โค้ด bootstrap กำหนดผู้ใช้ของ `RequestCurrentUser<TUserId>` แบบ scoped ได้ `IdentityAuditActorResolver<TActor>` ใช้ได้เมื่อ ID ของผู้ใช้ปัจจุบันและ actor ที่เก็บใช้ชนิดที่ไม่เป็น null ชนิดเดียวกัน

`AuditableDbContext<TUserId, TActor>` รองรับการบันทึกแบบ asynchronous เท่านั้น เพื่อไม่ให้ domain event ถูกบังคับผ่านการรันแบบ sync-over-async

การ map ของแอปพลิเคชันใน `ConfigureModel` ทำงานก่อน query filter ของ infrastructure ทำให้ context ฐานค้นพบ entity ที่ลงทะเบียนผ่าน `ApplyConfigurationsFromAssembly` เท่านั้นได้ แล้วจึงใส่ `SoftDeleteFilter` ให้ entity ราก (root) ทุกตัวที่ implement `ISoftDelete<TActor>` filter นี้ซ่อนแถวที่ถูกลบ ไม่ได้แปลง `Remove` จริงให้เป็น soft delete แต่การ remove entity ที่รองรับ soft delete จะถูกปฏิเสธด้วย `InvalidOperationException` จึงต้องใช้การ soft delete ของ entity นั้นเอง `IncludeSoftDeleted()` ปิดเฉพาะ filter นี้สำหรับ query เดียว

ความล้มเหลวของ optimistic concurrency ใน EF Core ถูกเปิดเผยเป็น `PersistenceConcurrencyException` ทำให้ exception เฉพาะของ EF อยู่ภายใน infrastructure ขณะที่ชั้น API ยังตอบ HTTP 409 ได้

### Multitenancy แบบเลือกใช้ได้

ติดตั้ง adapter เสริมของ EF และ API เฉพาะในแอปพลิเคชันที่รองรับ tenant ผู้ใช้งานเป็นเจ้าของ entity `Tenant` และ `TenantMembership` วงจรชีวิตของมัน และ query สำหรับ membership ส่วน Whimclean ให้ขอบเขตที่เชื่อมการตัดสินใจเหล่านั้นเข้ากับการอนุญาตและการแยกข้อมูลใน EF หัวข้อนี้ครอบคลุมโมเดลการแยกเชิงตรรกะ (logical isolation) ที่มาพร้อมแพ็กเกจ ส่วน[คู่มือตั้งค่า multitenancy](docs/multitenancy-setup-TH.md) อธิบายโมเดลการแยกทั้งสามแบบ — เชิงตรรกะ (schema ร่วม), schema ต่อ tenant และฐานข้อมูลต่อ tenant

context ธุรกิจของ tenant สืบทอดจาก `MultiTenantDbContext<TUserId, TActor, TTenantId>`:

```csharp
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy.Persistence;
using Athenex.Whimclean.Multitenancy;

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
    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}
```

implement access validator ในโปรเจกต์ infrastructure ของผู้ใช้งาน ที่นี่คือจุดที่แอปพลิเคชันใช้กฎของตัวเองเรื่องสถานะ membership สถานะ tenant วันหมดอายุ บทบาท หรือการระงับ:

```csharp
public sealed class TenantMembershipAccessValidator(
    AuthenticationDbContext dbContext)
    : ITenantAccessValidator<Guid, Guid>
{
    public Task<bool> HasAccessAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        dbContext.TenantMemberships.AnyAsync(
            membership =>
                membership.UserId == userId &&
                membership.TenantId == tenantId &&
                membership.IsActive,
            cancellationToken);
}
```

ลงทะเบียน adapter เสริมใน composition root ของ API:

```csharp
builder.Services.AddFastEndpointsApi();
builder.Services.AddAspNetCoreMultitenancy<Guid, Guid>();
builder.Services.AddHeaderTenantResolution<Guid>(options =>
    options.HeaderName = "X-Tenant-Id");
builder.Services.AddScoped<
    ITenantAccessValidator<Guid, Guid>,
    TenantMembershipAccessValidator>();
```

แปลง tenant ที่ถูกร้องขอหลังการยืนยันตัวตนและก่อนการอนุญาต แล้วป้องกัน endpoint ของ tenant ด้วย policy ที่ให้มา:

```csharp
app.UseAuthentication();
app.UseTenantResolution<Guid>();
app.UseAuthorization();
app.UseFastEndpointsApi();

public sealed class ListOrdersEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("orders");
        Policies(MultitenancyPolicies.TenantMember);
    }
}
```

resolver แบบ header ถือค่าของมันเป็นเพียง tenant ที่ถูกร้องขอ และต้องมี header นั้นเพียงครั้งเดียว: header ที่หายไปหรือซ้ำกันจะไม่ได้ tenant ใดเลย `ICurrentTenant<TTenantId>` ยังคงว่างจนกว่าผู้ใช้ที่ยืนยันตัวตนแล้วจะผ่าน `ITenantAccessValidator` ผู้เรียกจึงเลือก tenant ที่ไม่ได้รับอนุญาตเพียงแค่เปลี่ยน header ไม่ได้ resolver แบบ host, route หรือ claim สามารถแทนที่ `IRequestedTenantResolver<TTenantId>` ได้โดยไม่ต้องแก้ชั้น EF

การ deploy ที่ให้บริการแต่ละ tenant จาก path หรือ hostname จะส่งต่อ slug มาใน header `AddSlugTenantResolution()` (ไม่มี generic) ตั้งค่า header ของ slug ซึ่งมีค่าเริ่มต้นเป็น `X-Tenant-Slug` และ `UseSlugTenantResolution<TTenantId>()` map slug ที่ trim และแปลงเป็นตัวพิมพ์เล็กแล้วไปยัง id ของ tenant ผ่าน `ITenantSlugLookup<TTenantId>` ของแอปพลิเคชัน middleware ของ slug จะไม่บันทึกอะไรเมื่อมี tenant ที่ถูกร้องขออยู่แล้ว ดังนั้น header ของ id มีลำดับความสำคัญสูงกว่าเมื่อตั้งค่าทั้งสองแบบ:

```csharp
builder.Services.AddSlugTenantResolution();
builder.Services.AddScoped<ITenantSlugLookup<Guid>, TenantSlugLookup>();

app.UseAuthentication();
app.UseTenantResolution<Guid>();
app.UseSlugTenantResolution<Guid>();
app.UseAuthorization();
```

host ที่ไม่มีการแปลง tenant จาก HTTP (worker, seeder) ลงทะเบียน `AmbientCurrentTenant<TTenantId>` แล้วห่องานที่ผูกกับ tenant ไว้ใน `AmbientCurrentTenant<TTenantId>.Begin(tenantId)`

entity ที่ implement `IMultiTenant<TTenantId>` จะได้รับ `TenantFilter` แบบมีชื่อที่ fail-closed หากไม่มี current tenant ที่ได้รับอนุญาต query จะไม่คืนรายการของ tenant ใดเลย รายการของ tenant ที่ถูกเพิ่ม แก้ไข หรือลบต้องตรงกับ current tenant และห้ามเปลี่ยน `TenantId` context ตรวจสอบแต่ไม่เขียนทับค่า query ฝั่งผู้ดูแลต้องได้รับอนุญาตแยกต่างหาก และปิดได้เฉพาะ filter ที่จำเป็นบน EF Core 10:

```csharp
var includingDeleted = dbContext.Orders
    .IgnoreQueryFilters(["SoftDeleteFilter"]);

var acrossTenants = dbContext.Orders
    .IgnoreQueryFilters(["TenantFilter"]);
```

filter แบบมีชื่ออีกตัวยังคงทำงานในแต่ละ query `QueryFilterNames.SoftDelete` และ `QueryFilterNames.Tenant` เก็บชื่อของ filter ทั้งสอง

## API แบบ FastEndpoints

ลงทะเบียน FastEndpoints, OpenAPI, exception handler ของ Whimclean และบริการ problem-details ของ ASP.NET Core:

```csharp
builder.Services.AddFastEndpointsApi();
```

ใส่ route prefix เริ่มต้น `/api/v1`, exception middleware, การจัดการ response ที่ว่างเปล่าตาม status, การ bind filter ของ list query แบบทั้งค่า และสัญญา error response ของ Whimclean:

```csharp
app.UseFastEndpointsApi();
```

แอปพลิเคชันเปลี่ยน prefix และเพิ่มการตั้งค่า FastEndpoints เฉพาะผลิตภัณฑ์ได้:

```csharp
app.UseFastEndpointsApi(
    routePrefix: "api/v2",
    configure: options =>
    {
        options.Security.PermissionsClaimType = "permission";
    });
```

map error ของแอปพลิเคชันภายใน endpoint ด้วย `result.Error.ToHttpResult()`

exception ที่ขอบเขต infrastructure, ความล้มเหลวของ validation และ response ที่ว่างเปล่าจากการอนุญาต/routing ใช้รูปแบบ `ApiErrorResponse` เดียวกัน:

| เงื่อนไข | HTTP | รหัส error |
| --- | ---: | --- |
| `CurrentUserRequiredException` | 401 | `Authentication.CurrentUserRequired` |
| `PersistenceConcurrencyException` | 409 | `Persistence.ConcurrentUpdate` |
| validation ของ FastEndpoints ล้มเหลว | 400 | `Validation.Failed` |
| response 401 ที่ว่างเปล่า | 401 | `Authentication.Unauthorized` |
| response 403 ที่ว่างเปล่า | 403 | `Authorization.Forbidden` |
| response 404 ที่ว่างเปล่า | 404 | `Resource.NotFound` |
| exception ที่ไม่คาดคิด | 500 | `Server.UnexpectedError` |

การติดตั้งและลงทะเบียน `Api.AspNetCore.Multitenancy` เพิ่ม `CurrentTenantRequiredException` → 403 `Tenancy.CurrentTenantRequired` และ `TenantAccessViolationException` → 403 `Tenancy.AccessDenied` โดยไม่ผูกแพ็กเกจ API พื้นฐานเข้ากับ multitenancy

รายละเอียดของ exception ที่ไม่คาดคิดถูก log ไว้แต่ไม่ส่งกลับให้ client

`Api.AspNetCore` ยังมีตัวช่วยด้าน operations ที่ host ของ API ต้องใช้: `MapHealthEndpoints` map endpoint แบบไม่ต้องยืนยันตัวตน `/health/live` และ `/health/ready` (readiness รันเฉพาะ health check ที่ติด tag `HealthCheckTags.Ready` และทั้งคู่ได้รับยกเว้นจาก rate limit), `AddRequestRateLimiting` เพิ่ม fixed window ทั่วทั้งระบบต่อผู้เรียกจาก section `RateLimiting` (`PermitLimit` ค่าเริ่มต้น 300; `WindowSeconds` ค่าเริ่มต้น 60; ตรวจสอบผ่าน `RequestRateLimitOptions.Validate()`) ใช้คู่กับ `app.UseRateLimiter()`, `AddValidatedOptions` bind คลาส options และตรวจสอบตอนเริ่มระบบ และ `ConfigurationPlaceholder.IsUnset` ตรวจจับเครื่องหมาย `<set-via-user-secrets>` ที่ยังค้างอยู่ในการตั้งค่าที่ commit ไว้ (`AddDbContextCheck` มาจาก `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`):

```csharp
using Athenex.Whimclean.Api.AspNetCore.Operations;

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(tags: [HealthCheckTags.Ready]);
builder.Services.AddRequestRateLimiting(builder.Configuration);

app.UseAuthentication();
app.UseRateLimiter();
app.MapHealthEndpoints();
```
