# Athenex.Whimclean

[English](README.md)

Whimclean คือชุด building block สำหรับ .NET 10 สำหรับแอปพลิเคชันธุรกิจ: โมเดล domain ที่มี aggregate, domain event และฟิลด์ audit; สัญญาในชั้น application สำหรับ query, pagination และอีเมล; persistence ด้วย EF Core แบบไม่ผูกกับ provider พร้อม adapter สำหรับ PostgreSQL; multitenancy แบบเลือกใช้ได้; และ host แบบ ASP.NET Core Minimal API พร้อม adapter สำหรับ FastEndpoints

การตัดสินใจสามข้อที่กำหนดรูปร่างของ library:

- **การยืนยันตัวตนเป็นแบบนำมาเอง** แอปพลิเคชันยืนยันตัวตนกับ identity provider ภายนอก (Keycloak, Microsoft Entra ID, Auth0) หรือกับ ASP.NET Core Identity และ Whimclean รับเฉพาะ principal ที่ได้ผ่าน `ICurrentUser<TUserId>` ไม่เก็บผู้ใช้ credential หรือ token
- **dependency ชี้ทางเดียว** แพ็กเกจของ provider, API และ tenant ขึ้นกับแพ็กเกจกลาง ไม่มีทางกลับกัน จึงเปลี่ยน provider หรือรูปแบบ endpoint ได้โดยไม่แตะชั้น domain และ application
- **สัญญา API เดียว** `Athenex.Whimclean.Api.AspNetCore` เป็นแพ็กเกจ API หลัก สร้างบน Minimal APIs ส่วน `Athenex.Whimclean.Api.FastEndpoints` นำ error body, response ของ validation และสัญญา list query ชุดเดียวกันไปใช้กับ FastEndpoints

## สารบัญ

- [แพ็กเกจ](#แพ็กเกจ)
- [การติดตั้ง](#การติดตั้ง)
- [การประกอบแอปพลิเคชัน](#การประกอบแอปพลิเคชัน)
- [โมเดล domain](#โมเดล-domain)
- [สัญญาในชั้น application](#สัญญาในชั้น-application)
- [persistence ด้วย EF Core](#persistence-ด้วย-ef-core)
- [การยืนยันตัวตนและผู้ใช้ปัจจุบัน](#การยืนยันตัวตนและผู้ใช้ปัจจุบัน)
- [command, event และ audit log](#command-event-และ-audit-log)
- [multitenancy](#multitenancy)
- [API host](#api-host)
- [อ่านเพิ่มเติม](#อ่านเพิ่มเติม)

## แพ็กเกจ

แต่ละแพ็กเกจมี README ของตัวเอง พร้อมการติดตั้ง ขอบเขต และตัวอย่าง

### พื้นฐาน

| แพ็กเกจ | ให้อะไร | ขึ้นกับ |
| --- | --- | --- |
| [`Domain`](src/Athenex.Whimclean.Domain/README-TH.md) | entity, aggregate root, domain event, ฟิลด์ audit, soft delete, `Result` และ `Error`, การ normalize ข้อความ | ไม่มี |
| [`Application`](src/Athenex.Whimclean.Application/README-TH.md) | สัญญาของผู้ใช้ปัจจุบันและ context ปัจจุบัน, abstraction สำหรับอีเมล, โมเดล pagination, สัญญาด้าน persistence, list query options ที่ผ่านการตรวจสอบ และไวยากรณ์ของ `ListQueryParser` | `Domain` |
| [`Infrastructure`](src/Athenex.Whimclean.Infrastructure/README-TH.md) | การ resolve audit actor แบบไม่ผูกกับ provider, pipeline ของ domain event ภายในโปรเซส, ตัวระบุสำหรับ seed แบบกำหนดผลได้ | `Domain` |
| [`Multitenancy.Abstractions`](src/Athenex.Whimclean.Multitenancy.Abstractions/README-TH.md) | สัญญาของ tenant marker และ current tenant, การตรวจสอบสิทธิ์เข้าถึง, current tenant แบบ ambient สำหรับ host ที่ไม่ใช่ HTTP, exception ของขอบเขต tenant | ไม่มี |

### persistence

| แพ็กเกจ | ให้อะไร | ขึ้นกับ |
| --- | --- | --- |
| [`Infrastructure.EntityFrameworkCore`](src/Athenex.Whimclean.Infrastructure.EntityFrameworkCore/README-TH.md) | `AuditableDbContext`, การเขียนที่บังคับต้องมีผู้ใช้ปัจจุบัน, การประทับค่า audit, query filter สำหรับ soft delete แบบมีชื่อ, การแปลง concurrency exception, การส่ง domain event แบบ asynchronous, ตัวช่วยสำหรับ list query, สวิตช์ `--migrate`, การลงทะเบียน `DbContext`/`IUnitOfWork` | `Application`, `Infrastructure`, EF Core (ไม่มี provider) |
| [`Infrastructure.EntityFrameworkCore.PostgreSQL`](src/Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL/README-TH.md) | Npgsql, การลงทะเบียน PostgreSQL, ตัวช่วยค้นหาแบบไม่สนตัวพิมพ์ `SearchILike` | `Infrastructure.EntityFrameworkCore` |
| [`Infrastructure.EntityFrameworkCore.Multitenancy`](src/Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy/README-TH.md) | `MultiTenantDbContext`, query filter ของ tenant แบบ fail-closed และการตรวจสอบการเขียน | `Infrastructure.EntityFrameworkCore`, `Multitenancy.Abstractions` |

### API

| แพ็กเกจ | ให้อะไร | ขึ้นกับ |
| --- | --- | --- |
| [`Api.AspNetCore`](src/Athenex.Whimclean.Api.AspNetCore/README-TH.md) | แพ็กเกจ API หลัก: การลงทะเบียน Minimal API พร้อม OpenAPI, การตรวจสอบ request ด้วย FluentValidation บน group `api/v1`, สัญญา `ListQueryRequest`, สัญญา error พร้อมการ map exception และ `Result`, การ resolve ผู้ใช้ปัจจุบัน, health endpoint, rate limit, options ที่ผ่านการตรวจสอบ | `Application` |
| [`Api.FastEndpoints`](src/Athenex.Whimclean.Api.FastEndpoints/README-TH.md) | adapter ของ FastEndpoints: การลงทะเบียน FastEndpoints และ OpenAPI ของมัน, response ของ validation บนสัญญา error ร่วม, การ bind filter ของ list query แบบทั้งค่า | `Api.AspNetCore` |
| [`Api.AspNetCore.Multitenancy`](src/Athenex.Whimclean.Api.AspNetCore.Multitenancy/README-TH.md) | การ resolve tenant ที่ถูกร้องขอจาก header ของ id หรือ slug, การอนุญาตตาม membership, tenant context ที่ผ่านการอนุญาต, การ map error ของ tenant ใช้ได้ทั้งกับ Minimal APIs และ FastEndpoints | `Api.AspNetCore`, `Multitenancy.Abstractions` |

### cross-cutting

| แพ็กเกจ | ให้อะไร | ขึ้นกับ |
| --- | --- | --- |
| [`Mediator`](src/Athenex.Whimclean.Mediator/README-TH.md) | การผสานกับ [Mediator](https://github.com/martinothamar/Mediator): pipeline behavior ที่ commit unit of work หลังแต่ละ command และการส่ง domain event เป็น notification แอปพลิเคชันเป็นผู้จัดหา `Mediator.SourceGenerator` | `Application`, `Infrastructure`, `Mediator.Abstractions` |
| [`Auditing`](src/Athenex.Whimclean.Auditing/README-TH.md) | audit log ที่บันทึกผลลัพธ์ actor และการเปลี่ยนแปลงของฟิลด์ของทุก command ที่ถูก audit โดยเขียนนอก unit of work | `Application`, `Domain`, `Mediator.Abstractions` |
| [`Auditing.AspNetCore`](src/Athenex.Whimclean.Auditing.AspNetCore/README-TH.md) | ชื่อผู้เรียก, correlation id, ที่อยู่ และ user agent สำหรับ audit log โดยอ่านจาก request | `Auditing` |
| [`Infrastructure.Email.Smtp`](src/Athenex.Whimclean.Infrastructure.Email.Smtp/README-TH.md) | `IEmailSender` บน SMTP พร้อม options ที่ผ่านการตรวจสอบ | `Application` |
| [`Identity.Keycloak`](src/Athenex.Whimclean.Identity.Keycloak/README-TH.md) | การยืนยันตัวตนแบบ JWT bearer ตามธรรมเนียม claim ของ Keycloak และ client ของ admin API สำหรับสร้างผู้ใช้ | `Domain` |

### แผนภาพ dependency

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

แพ็กเกจ PostgreSQL ไม่มีโค้ดของ auditing, tenant, soft delete, concurrency หรือ domain event; พฤติกรรมเหล่านั้นอยู่ในแพ็กเกจ EF Core ที่ไม่ผูกกับ provider และพฤติกรรมของ tenant เป็นแพ็กเกจ decorator แยกต่างหาก แพ็กเกจ provider ใหม่จึงต้องการเพียง dependency ของ provider นั้นและเมธอดลงทะเบียนที่ส่งต่อไปยัง `AddEntityFrameworkCoreUnitOfWork<TContext>`

### การเลือกแพ็กเกจ

อ้างอิงเฉพาะแพ็กเกจระดับบนสุดที่แอปพลิเคชันใช้ NuGet จะดึง dependency มาให้แบบ transitive

| ความต้องการของแอปพลิเคชัน | แพ็กเกจที่อ้างอิงโดยตรง |
| --- | --- |
| ใช้เฉพาะสัญญาของ domain และ application | `Domain`, `Application` |
| API ธุรกิจบน PostgreSQL (Minimal APIs) | `Infrastructure.EntityFrameworkCore.PostgreSQL`, `Api.AspNetCore` |
| ใช้ FastEndpoints แทน Minimal APIs | `Infrastructure.EntityFrameworkCore.PostgreSQL`, `Api.FastEndpoints` |
| เพิ่มการแยกข้อมูลตาม tenant ใน EF | แพ็กเกจก่อนหน้า บวก `Infrastructure.EntityFrameworkCore.Multitenancy` |
| เพิ่มการ resolve tenant จาก HTTP และการอนุญาตตาม membership | แพ็กเกจก่อนหน้า บวก `Api.AspNetCore.Multitenancy` |
| commit แต่ละ command ผ่าน Mediator | แพ็กเกจก่อนหน้า บวก `Mediator` (และ `Mediator.SourceGenerator` จากฝั่งแอปพลิเคชัน) |
| audit ทุก command | แพ็กเกจก่อนหน้า บวก `Auditing` และ `Auditing.AspNetCore` |
| ยืนยันตัวตนกับ Keycloak และสร้างผู้ใช้ของมัน | แพ็กเกจก่อนหน้า บวก `Identity.Keycloak` |
| ส่งอีเมลผ่าน SMTP | แพ็กเกจก่อนหน้า บวก `Infrastructure.Email.Smtp` |

## การติดตั้ง

### feed ของแพ็กเกจ

แพ็กเกจเผยแพร่บน NuGet feed ของ athene-x ใน [GitHub Packages](https://github.com/orgs/athene-x/packages) ไม่ใช่ nuget.org การใช้งานต้องเป็นสมาชิกขององค์กร athene-x

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

3. หาก repository ที่ใช้งานมี `packageSourceMapping` ใน `nuget.config` การ restore จะพิจารณาเฉพาะ source ที่ map ไว้ ให้ map prefix ของแพ็กเกจด้วย:

   ```xml
   <packageSourceMapping>
     <packageSource key="github">
       <package pattern="Athenex.Whimclean.*" />
     </packageSource>
   </packageSourceMapping>
   ```

### การเพิ่มแพ็กเกจ

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

## การประกอบแอปพลิเคชัน

API ธุรกิจบน PostgreSQL ที่ยืนยันตัวตนด้วย Keycloak ประกอบแพ็กเกจเข้าด้วยกันดังนี้ แต่ละส่วนอธิบายในหัวข้อถัดไป

```csharp
using Athenex.Whimclean.Api.AspNetCore;
using Athenex.Whimclean.Api.AspNetCore.Operations;
using Athenex.Whimclean.Identity.Keycloak;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Operations;
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL;
using Athenex.Whimclean.Mediator;

var builder = WebApplication.CreateBuilder(args);

// Authentication and the current user
builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddClaimsCurrentUser<Guid>(options => options.ClaimType = "sub");

// Persistence
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddNullableAuditActorResolver<Guid>();
builder.Services.AddMediatorDomainEventDispatcher();
builder.Services.AddPostgreSqlUnitOfWork<ApplicationDbContext>(
    builder.Configuration.GetConnectionString("Default")!);

// Commands
builder.Services.AddMediator();
builder.Services.AddUnitOfWorkBehavior();

// API host
builder.Services.AddMinimalApi();
builder.Services.AddApiValidatorsFromAssemblyContaining<Program>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(tags: [HealthCheckTags.Ready]);
builder.Services.AddRequestRateLimiting(builder.Configuration);

var app = builder.Build();

if (DatabaseMigration.IsRequested(args))
{
    await app.Services.MigrateDatabaseAsync<ApplicationDbContext>();
    return;
}

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseMinimalApi();
app.MapHealthEndpoints();

var api = app.MapApiGroup();
api.MapGet("orders/{id:guid}", async (Guid id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetOrderQuery(id));
    return result.ToHttpResult(order => Results.Ok(order));
});

app.Run();
```

## โมเดล domain

### entity และ aggregate root

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

event ที่ raise แล้วเข้าถึงได้ผ่าน `DomainEvents` การล้าง event เป็นเรื่องของ infrastructure: `ClearDomainEvents()` เข้าถึงได้ผ่าน `IHasDomainEvents` เท่านั้น และ EF Core context เรียกมันหลังบันทึกสำเร็จ ก่อนส่ง event ออกไป

### ฟิลด์ audit

`IAuditable<TActor>` ประกาศฟิลด์ audit สี่ฟิลด์ ชนิดของ actor กำหนด nullability ของตัวเอง แอปพลิเคชันจึงเก็บเป็น identifier ชื่อ หรือชนิด actor ที่กำหนดเองก็ได้:

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

`AuditableEntity<TId, TActor>` และ `AuditableAggregateRoot<TId, TActor>` ประกาศฟิลด์เหล่านี้ไว้ครั้งเดียว entity ที่สืบทอดจึงไม่ต้องประกาศซ้ำ EF Core context ประทับค่าตอนบันทึก

### result

```csharp
using Athenex.Whimclean.Domain.Results;

Result<Guid> CreateOrder(bool isValid) => isValid
    ? Result<Guid>.Success(Guid.NewGuid())
    : Result<Guid>.Failure(
        Error.Validation("Order.Invalid", "The order is invalid."));
```

การอ่าน `Value` จาก result ที่ล้มเหลวจะ throw `InvalidOperationException` ให้ตรวจ `IsSuccess` หรือ `IsFailure` ก่อน แพ็กเกจ API map แต่ละ `ErrorType` เป็น HTTP status code (ดู [สัญญา error](#สัญญา-error))

## สัญญาในชั้น application

### list query

`ListQueryOptions` คือ input ที่ผ่านการตรวจสอบและ normalize แล้วของ list query แบบแบ่งหน้าด้วย offset มันตรวจขีดจำกัดของตัวเองและเปิดเผย collection แบบอ่านอย่างเดียวที่ materialize แล้ว โค้ดฝั่ง infrastructure จึงแปลงเฉพาะฟิลด์และ operator ที่อนุมัติแล้วไปเป็น query ของฐานข้อมูล:

```csharp
using Athenex.Whimclean.Application.Queries;

var options = new ListQueryOptions(
    searchTerm: "pending",
    sort: [new SortTerm("createdAt", SortDirection.Descending)],
    filters: [new FilterTerm("status", "Pending")],
    page: 1,
    size: 20);
```

`ListQueryParser` สร้าง options ชุดเดียวกันจากค่าดิบของ query string:

```csharp
var parsed = ListQueryParser.Parse(
    searchTerm: "pending",
    sort: "-createdAt",
    filters: ["status:in:Pending,Paid"],
    page: 1,
    size: 20);
```

| input | ไวยากรณ์ |
| --- | --- |
| `sort` | ชื่อฟิลด์คั่นด้วยจุลภาค นำหน้าด้วย `-` เพื่อเรียงจากมากไปน้อย |
| `filters` | หนึ่ง `field:operator:value` ต่อรายการ operator ได้แก่ `eq`, `ne`, `gt`, `ge`, `lt`, `le`, `cn` (contains), `sw`, `ew`, `in` (ค่าคั่นด้วยจุลภาค) |

`EnumFilter` ใน namespace เดียวกัน แปลงค่าของ filter เป็นสมาชิกของ enum สำหรับการแปลงเป็น query

**ใน API** ให้สืบทอด request จาก `ListQueryRequest` และ validator จาก `ListQueryRequestValidator<TRequest>` (ทั้งคู่อยู่ใน `Athenex.Whimclean.Api.AspNetCore.Queries`) เพื่อกำหนดรายการฟิลด์ sort และ filter ที่อนุญาตของแต่ละ endpoint `ToListQueryOptions()` จะ parse request ที่ผ่านการตรวจสอบแล้ว:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Queries;
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

api.MapGet("orders", async (
    [AsParameters] ListOrdersRequest request,
    IOrderQueries queries,
    CancellationToken cancellationToken) =>
    Results.Ok(await queries.ListAsync(request.ToListQueryOptions(), cancellationToken)));
```

Minimal API bind request ด้วย `[AsParameters]` และ filter ตรวจสอบของ `MapApiGroup` รัน validator ที่ลงทะเบียนโดย `AddApiValidatorsFromAssembly` endpoint ของ FastEndpoints ใช้ request เดียวกันเป็น DTO และ FastEndpoints ค้นพบ validator เอง

**ใน persistence** แพ็กเกจ EF Core แปลง options: `SortFields<T>` map ชื่อ sort ที่อนุญาตไปยัง key selector, `SearchLike` (หรือ `SearchILike` จากแพ็กเกจ PostgreSQL) ค้นหาหลายคอลัมน์โดย escape wildcard ให้ และ `Page`/`ToPage` ใส่ offset และห่อผลลัพธ์:

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

### pagination

```csharp
using Athenex.Whimclean.Application.Pagination;

var result = new OffsetPagedResult<string>(
    items: ["first", "second"],
    page: 1,
    size: 20,
    totalItems: 2);
```

`Map` แปลงรายการของหน้าโดยคงหมายเลขหน้า ขนาด และยอดรวมไว้ `KeysetPagedResult<T, TKey>` คือแบบ cursor ที่คู่กัน

### อีเมล

use case ในชั้น application ส่งผ่าน `IEmailSender` ให้ implement ในชั้น infrastructure หรือลงทะเบียน implementation แบบ SMTP ที่มาพร้อมแพ็กเกจ:

```csharp
var message = new EmailMessage(
    recipient: "member@example.com",
    subject: "Welcome",
    textBody: "Your account is ready.");

await emailSender.SendAsync(message, cancellationToken);
```

`Infrastructure.Email.Smtp` ส่งผ่าน `System.Net.Mail` พร้อม options สำหรับ host, port, credential, TLS และผู้ส่งเริ่มต้น bind `SmtpEmailOptions` จาก section `Email` และตรวจสอบตอนเริ่มระบบ:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Configuration;
using Athenex.Whimclean.Infrastructure.Email.Smtp;

builder.Services.AddValidatedOptions<SmtpEmailOptions>(
    builder.Configuration,
    SmtpEmailOptions.SectionName,
    options => options.Validate());
builder.Services.AddSmtpEmailSender();
```

## persistence ด้วย EF Core

### context ของแอปพลิเคชัน

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

context ทำงานดังนี้:

- **การเขียนทุกครั้งต้องมีผู้ใช้** record ที่เพิ่ม แก้ไข หรือลบต้องมี `ICurrentUser<TUserId>` ที่ให้ผู้ใช้ได้ รวมถึง record ที่ไม่มี audit interface background job ลงทะเบียน system user อย่างชัดเจน เช่น `StaticCurrentUser<TUserId>` แทนการข้ามขอบเขตนี้
- **ฟิลด์ audit ถูกประทับตอนบันทึก** ผ่าน `IAuditActorResolver<TUserId, TActor>`
- **แถวที่ soft delete ถูกซ่อน** การ map ของแอปพลิเคชันใน `ConfigureModel` ทำงานก่อน filter ของ infrastructure context ฐานจึงค้นพบ entity ราก (root) ทุกตัวที่ implement `ISoftDelete<TActor>` และใส่ `SoftDeleteFilter` แบบมีชื่อให้ การ `Remove` จริงของ entity เช่นนี้ถูกปฏิเสธด้วย `InvalidOperationException` ให้ใช้การ soft delete ของ entity นั้นเอง `IncludeSoftDeleted()` ปิด filter นี้สำหรับ query เดียว
- **domain event ถูกส่งหลังบันทึกสำเร็จ** แล้วจึงล้าง การบันทึกรองรับเฉพาะแบบ asynchronous event จึงไม่ถูกบังคับผ่าน sync-over-async
- **concurrency failure ถูกแปลง** `DbUpdateConcurrencyException` ปรากฏเป็น `PersistenceConcurrencyException` ซึ่งชั้น API map เป็น HTTP 409

### การลงทะเบียน

```csharp
services.AddSingleton(TimeProvider.System);
services.AddClaimsCurrentUser<Guid>();
services.AddSingleton<
    IAuditActorResolver<Guid, Guid?>,
    NullableAuditActorResolver<Guid>>();
services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();
services.AddPostgreSqlUnitOfWork<ApplicationDbContext>(connectionString);
```

`NullableAuditActorResolver<TUserId>` แปลง id ผู้ใช้ชนิด value type ที่ไม่เป็น null เช่น `Guid` ไปเป็นฟิลด์ audit แบบ nullable เช่น `Guid?`; `IdentityAuditActorResolver<TActor>` ใช้เมื่อ id ผู้ใช้และ actor ที่เก็บใช้ชนิดเดียวกันที่ไม่เป็น null ทางลัด `AddNullableAuditActorResolver<Guid>()`, `AddIdentityAuditActorResolver<Guid>()`, `AddDomainEventDispatcher()` และ `AddMediatorDomainEventDispatcher()` ลงทะเบียน service ชุดเดียวกันด้วยพฤติกรรม `TryAdd`

options ของ Npgsql อยู่ที่ขอบของ provider:

```csharp
services.AddPostgreSqlUnitOfWork<ApplicationDbContext>(
    connectionString,
    postgreSql => postgreSql.MigrationsAssembly("MyApp.Migrations.PostgreSQL"));
```

### provider อื่น

อ้างอิง `Athenex.Whimclean.Infrastructure.EntityFrameworkCore` บวกกับ provider นั้น แล้วตั้งค่าผ่านเมธอดลงทะเบียนร่วม:

```csharp
services.AddEntityFrameworkCoreUnitOfWork<ApplicationDbContext>((_, options) =>
    options.UseSqlServer(connectionString));
```

`AuditableDbContext`, query filter ของ soft delete, ขอบเขต concurrency, การส่ง domain event และการลงทะเบียน `IUnitOfWork` เป็นชุดเดียวกัน ต่างกันเพียงการตั้งค่า provider เพิ่มแพ็กเกจ EF multitenancy เมื่อต้องแยกข้อมูลตาม tenant

### migration

image ของแอปพลิเคชันรัน migration ของตัวเอง `DatabaseMigration.IsRequested(args)` ตรวจจับสวิตช์ `--migrate` และ `MigrateDatabaseAsync<TContext>` รัน migration ที่ค้างอยู่ โดย log ชื่อของแต่ละตัวและคืนจำนวนที่รัน:

```csharp
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Operations;

var app = builder.Build();

if (DatabaseMigration.IsRequested(args))
{
    await app.Services.MigrateDatabaseAsync<ApplicationDbContext>();
    return;
}
```

## การยืนยันตัวตนและผู้ใช้ปัจจุบัน

Whimclean ไม่เก็บผู้ใช้ credential หรือ token ให้ยืนยันตัวตนกับ identity provider ภายนอก (Keycloak, Microsoft Entra ID, Auth0) หรือต่อ ASP.NET Core Identity โดยตรงกับ API ของ Microsoft เอง ไม่ว่าทางใด library รับเฉพาะ principal ที่ยืนยันตัวตนแล้วผ่าน `ICurrentUser<TUserId>`

### ผู้ใช้ปัจจุบันจาก claim

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

ชนิด claim เริ่มต้นคือ `ClaimTypes.NameIdentifier` ส่ง `options => options.ClaimType = "sub"` เมื่อปิด inbound claim mapping การลงทะเบียนใช้ `TryAddScoped` ดังนั้น `ICurrentUser<TUserId>` ที่แอปพลิเคชันจัดให้เองจะชนะ

### Keycloak

`Identity.Keycloak` แทนที่การตั้งค่า JWT bearer ที่เขียนเองด้วย `AddKeycloakAuthentication` ซึ่ง bind จาก section `Keycloak` (`Authority`, `Audience`, `MetadataAddress` แบบเลือกใส่, `RequireHttpsMetadata`, `NameClaimType`, `RoleClaimType`) มันปิด inbound claim mapping เพื่อให้ `sub` ยังคงเป็น `sub` ตรึง issuer ที่ถูกต้องไว้ที่ `Authority` และลงทะเบียนบริการ authorization ให้ด้วย `AddKeycloakAdministration` เพิ่ม `IKeycloakUserAdministration` ซึ่งเป็น typed client บน admin API ที่ใช้ service account จาก section `KeycloakAdmin`:

```csharp
using Athenex.Whimclean.Api.AspNetCore;
using Athenex.Whimclean.Identity.Keycloak;

builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddKeycloakAdministration(builder.Configuration);
builder.Services.AddClaimsCurrentUser<Guid>(options => options.ClaimType = "sub");
```

### ผู้ใช้ปัจจุบันจากตารางผู้ใช้ภายใน

เมื่อแอปพลิเคชันมีตารางผู้ใช้ของตัวเองและ token มีเพียง subject ของ identity provider ให้ใช้ `AddLocalUserResolution<TUserId>` แทนผู้ใช้ปัจจุบันแบบอิง claim middleware จะ map claim ของ subject ไปยัง id ภายในผ่าน `ILocalUserLookup<TUserId>` ของแอปพลิเคชัน แล้วบันทึกลงใน `RequestCurrentUser<TUserId>` แบบ scoped subject ที่ไม่มีผู้ใช้ภายในจะปล่อยให้ผู้ใช้ปัจจุบันว่างไว้ วาง middleware หลังการยืนยันตัวตนและก่อนสิ่งใดก็ตามที่อ่านผู้ใช้ปัจจุบัน:

```csharp
using Athenex.Whimclean.Api.AspNetCore;
using Athenex.Whimclean.Api.AspNetCore.Context;

builder.Services.AddLocalUserResolution<Guid>(options => options.ClaimType = "sub");
builder.Services.AddScoped<ILocalUserLookup<Guid>, LocalUserLookup>();

app.UseAuthentication();
app.UseLocalUserResolution<Guid>();
app.UseAuthorization();
```

`ICurrentUserInitializer<TUserId>` ให้โค้ด bootstrap ตั้งผู้ใช้ของ `RequestCurrentUser<TUserId>` แบบ scoped ได้หลังจาก resolve ผู้ใช้ของตนเอง

## command, event และ audit log

### Mediator

`Athenex.Whimclean.Mediator` ผสานกับ [Mediator](https://github.com/martinothamar/Mediator) `AddUnitOfWorkBehavior` ลงทะเบียน pipeline behavior ที่ commit unit of work หลังแต่ละ command ที่สำเร็จ และ `MediatorDomainEventDispatcher` (ลงทะเบียนโดย `AddMediatorDomainEventDispatcher`) ส่ง domain event เป็น notification ของ Mediator แอปพลิเคชันเป็นผู้จัดหา `Mediator.SourceGenerator`

### audit log ของ command

แพ็กเกจเสริม `Auditing` บันทึกผลลัพธ์ของ command ไม่ใช่ฟิลด์ของแถว command implement `IAuditedCommand`; `AuditCommandBehavior` เขียนรายการ `AuditLog<TUserId, TTenantId>` ผ่าน `IAuditLogWriter<TUserId, TTenantId>` ของแอปพลิเคชันนอก unit of work ของ command นั้น; handler ของ domain event เพิ่มการเปลี่ยนแปลงของฟิลด์ผ่าน `IAuditChangeCollector`:

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

## multitenancy

ติดตั้งแพ็กเกจ multitenancy ของ EF และ API เฉพาะในแอปพลิเคชันที่ใช้ tenant ผู้ใช้งานเป็นเจ้าของ entity `Tenant` และ `TenantMembership` วงจรชีวิตของมัน และ query สำหรับสมาชิกภาพ ส่วน Whimclean ให้ขอบเขตที่เชื่อมการตัดสินใจเหล่านั้นเข้ากับการอนุญาตและการแยกข้อมูลใน EF หัวข้อนี้ครอบคลุมรูปแบบการแยกข้อมูลเชิงตรรกะที่มาพร้อมแพ็กเกจ [คู่มือตั้งค่า multitenancy](docs/multitenancy-setup-TH.md) อธิบายครบทั้งสามรูปแบบ: เชิงตรรกะ (schema ร่วม), schema ต่อ tenant และฐานข้อมูลต่อ tenant

### context ของ tenant

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

entity ที่ implement `IMultiTenant<TTenantId>` ได้รับ `TenantFilter` แบบมีชื่อที่ fail-closed: หากไม่มี current tenant ที่ผ่านการอนุญาต query จะไม่คืน record ของ tenant ใดเลย record ของ tenant ที่เพิ่ม แก้ไข หรือลบต้องตรงกับ current tenant และเปลี่ยน `TenantId` ไม่ได้ context ตรวจสอบแต่ไม่เขียนทับ

query ของผู้ดูแลระบบต้องมีการอนุญาตแยกต่างหาก และปิดเฉพาะ filter ที่จำเป็น:

```csharp
var includingDeleted = dbContext.Orders
    .IgnoreQueryFilters(["SoftDeleteFilter"]);

var acrossTenants = dbContext.Orders
    .IgnoreQueryFilters(["TenantFilter"]);
```

filter แบบมีชื่ออีกตัวยังทำงานในแต่ละ query `QueryFilterNames.SoftDelete` และ `QueryFilterNames.Tenant` เก็บชื่อของ filter ทั้งสอง

### การตรวจสอบสิทธิ์เข้าถึง

implement access validator ในโปรเจกต์ infrastructure ของผู้ใช้งาน ที่นี่คือจุดที่แอปพลิเคชันใส่กฎของตัวเองเรื่องสถานะสมาชิกภาพ สถานะ tenant วันหมดอายุ บทบาท หรือการระงับ:

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

### การ resolve tenant ใน API

ลงทะเบียนการเชื่อมต่อกับ API ใน composition root:

```csharp
builder.Services.AddMinimalApi();
builder.Services.AddAspNetCoreMultitenancy<Guid, Guid>();
builder.Services.AddHeaderTenantResolution<Guid>(options =>
    options.HeaderName = "X-Tenant-Id");
builder.Services.AddScoped<
    ITenantAccessValidator<Guid, Guid>,
    TenantMembershipAccessValidator>();
```

resolve tenant ที่ถูกร้องขอหลังการยืนยันตัวตนและก่อนการอนุญาต แล้วป้องกัน endpoint ของ tenant ด้วย policy ที่ให้มา:

```csharp
app.UseAuthentication();
app.UseTenantResolution<Guid>();
app.UseAuthorization();
app.UseMinimalApi();

app.MapApiGroup()
    .MapGet("orders", (IOrderQueries queries) => Results.Ok(queries.List()))
    .RequireAuthorization(MultitenancyPolicies.TenantMember);
```

endpoint ของ FastEndpoints ใช้ policy เดียวกันด้วย `Policies(MultitenancyPolicies.TenantMember)` ในเมธอด `Configure`

resolver แบบ header ถือค่าของมันเป็นเพียง tenant *ที่ถูกร้องขอ* และต้องมี header นั้นเพียงครั้งเดียว: header ที่หายไปหรือซ้ำกันจะไม่ได้ tenant ใดเลย `ICurrentTenant<TTenantId>` ยังว่างจนกว่าผู้ใช้ที่ยืนยันตัวตนแล้วจะผ่าน `ITenantAccessValidator` ผู้เรียกจึงเลือก tenant ที่ไม่ได้รับอนุญาตเพียงแค่เปลี่ยน header ไม่ได้ resolver แบบ host, route หรือ claim สามารถแทนที่ `IRequestedTenantResolver<TTenantId>` ได้โดยไม่ต้องแก้ชั้น EF

### การ resolve จาก slug

การ deploy ที่ให้บริการแต่ละ tenant จาก path หรือ hostname ส่งต่อ slug มาใน header `AddSlugTenantResolution()` ตั้งค่า header ของ slug (ค่าเริ่มต้น `X-Tenant-Slug`) และ `UseSlugTenantResolution<TTenantId>()` map slug ที่ trim และแปลงเป็นตัวพิมพ์เล็กแล้วไปยัง id ของ tenant ผ่าน `ITenantSlugLookup<TTenantId>` ของแอปพลิเคชัน middleware ของ slug จะไม่บันทึกอะไรเมื่อมี tenant ที่ถูกร้องขออยู่แล้ว ดังนั้น header ของ id มีลำดับความสำคัญสูงกว่าเมื่อตั้งค่าทั้งสองแบบ:

```csharp
builder.Services.AddSlugTenantResolution();
builder.Services.AddScoped<ITenantSlugLookup<Guid>, TenantSlugLookup>();

app.UseAuthentication();
app.UseTenantResolution<Guid>();
app.UseSlugTenantResolution<Guid>();
app.UseAuthorization();
```

### host ที่ไม่ใช่ HTTP

worker และ seeder ลงทะเบียน `AmbientCurrentTenant<TTenantId>` แล้วห่องานที่ผูกกับ tenant ไว้ใน `AmbientCurrentTenant<TTenantId>.Begin(tenantId)`

## API host

### Minimal API

`Api.AspNetCore` ลงทะเบียนการจัดการ error ร่วม เอกสาร OpenAPI และ validator ของ FluentValidation ของแอปพลิเคชัน แล้ว map endpoint เข้า group `/api/v1` ซึ่งตรวจสอบอาร์กิวเมนต์ของ handler ทุกตัวที่มี validator ลงทะเบียนไว้:

```csharp
builder.Services.AddMinimalApi();
builder.Services.AddApiValidatorsFromAssemblyContaining<Program>();

app.UseMinimalApi();

var api = app.MapApiGroup();
api.MapGet("orders/{id:guid}", async (Guid id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetOrderQuery(id));
    return result.ToHttpResult(order => Results.Ok(order));
});
```

| ความต้องการ | เมธอด |
| --- | --- |
| route prefix อื่น | `MapApiGroup("api/v2")` |
| ไม่เปิด endpoint ของ OpenAPI | `UseMinimalApi(openApiRoutePattern: null)` |
| ปรับแต่งเอกสาร | `AddMinimalApi(openApi => ...)` |
| ตรวจสอบ endpoint ที่ map นอก group | `.WithRequestValidation()` |

### adapter ของ FastEndpoints

แอปพลิเคชันที่เขียนด้วย FastEndpoints ลงทะเบียน adapter แทน adapter ต่อยอดจากแพ็กเกจหลัก ดังนั้นสัญญา error, สัญญา list query และ response ของ validation เหมือนกันทุกประการ:

```csharp
builder.Services.AddFastEndpointsApi();

app.UseFastEndpointsApi();
```

`UseFastEndpointsApi` ใส่ route prefix เริ่มต้น `/api/v1`, การจัดการ error ร่วม, การ bind filter ของ list query แบบทั้งค่า และ response ของ validation แบบ Whimclean prefix และการตั้งค่า FastEndpoints ปรับได้:

```csharp
app.UseFastEndpointsApi(
    routePrefix: "api/v2",
    configure: options =>
    {
        options.Security.PermissionsClaimType = "permission";
    });
```

### สัญญา error

error ของแอปพลิเคชันกลายเป็น HTTP response ด้วย `result.ToHttpResult(...)` หรือ `error.ToHttpResult()`: `Validation` 400, `Unauthorized` 401, `Forbidden` 403, `NotFound` 404, `Conflict` 409, `Failure` 500

exception ที่ขอบเขต infrastructure, ความล้มเหลวของ validation และ response ที่ว่างเปล่าจากการอนุญาตหรือ routing ใช้ body `ApiErrorResponse` เดียวกัน:

```json
{
  "code": "Resource.NotFound",
  "description": "The requested resource was not found.",
  "errors": null
}
```

| เงื่อนไข | HTTP | รหัส error |
| --- | ---: | --- |
| `CurrentUserRequiredException` | 401 | `Authentication.CurrentUserRequired` |
| `PersistenceConcurrencyException` | 409 | `Persistence.ConcurrentUpdate` |
| validation ล้มเหลว (filter ของ Minimal API หรือ FastEndpoints) | 400 | `Validation.Failed` |
| response 400 ที่ว่างเปล่า | 400 | `Request.Invalid` |
| response 401 ที่ว่างเปล่า | 401 | `Authentication.Unauthorized` |
| response 403 ที่ว่างเปล่า | 403 | `Authorization.Forbidden` |
| response 404 ที่ว่างเปล่า | 404 | `Resource.NotFound` |
| exception ที่ไม่คาดคิด | 500 | `Server.UnexpectedError` |

`Api.AspNetCore.Multitenancy` เพิ่ม `CurrentTenantRequiredException` → 403 `Tenancy.CurrentTenantRequired` และ `TenantAccessViolationException` → 403 `Tenancy.AccessDenied` โดยไม่ผูกแพ็กเกจ API พื้นฐานเข้ากับ multitenancy รายละเอียดของ exception ที่ไม่คาดคิดถูก log ไว้ ไม่ส่งกลับให้ client

### งานปฏิบัติการ

`Api.AspNetCore` ยังมีตัวช่วยที่ host ของ API ต้องใช้:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Operations;

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(tags: [HealthCheckTags.Ready]);
builder.Services.AddRequestRateLimiting(builder.Configuration);

app.UseAuthentication();
app.UseRateLimiter();
app.MapHealthEndpoints();
```

| ตัวช่วย | พฤติกรรม |
| --- | --- |
| `MapHealthEndpoints` | map endpoint แบบไม่ต้องยืนยันตัวตน `/health/live` และ `/health/ready`; readiness รันเฉพาะ health check ที่ติด tag `HealthCheckTags.Ready`; ทั้งคู่ได้รับยกเว้นจาก rate limit `AddDbContextCheck` มาจาก `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` |
| `AddRequestRateLimiting` | fixed window ทั่วทั้งระบบต่อผู้เรียกจาก section `RateLimiting` (`PermitLimit` ค่าเริ่มต้น 300; `WindowSeconds` ค่าเริ่มต้น 60) ตรวจสอบผ่าน `RequestRateLimitOptions.Validate()` ใช้คู่กับ `app.UseRateLimiter()` |
| `AddValidatedOptions` | bind คลาส options กับ section และตรวจสอบตอนเริ่มระบบ |
| `ConfigurationPlaceholder.IsUnset` | ตรวจจับเครื่องหมาย `<set-via-user-secrets>` ที่ยังค้างอยู่ในการตั้งค่าที่ commit ไว้ |

## อ่านเพิ่มเติม

- README ของแต่ละแพ็กเกจ ลิงก์อยู่ใน [แพ็กเกจ](#แพ็กเกจ)
- [คู่มือตั้งค่า multitenancy](docs/multitenancy-setup-TH.md): การแยกข้อมูลเชิงตรรกะ, schema ต่อ tenant และฐานข้อมูลต่อ tenant
- [การอัปเกรดเป็น 1.3.0](docs/upgrading-to-1.3-TH.md): การแทนที่ helper ที่แอปพลิเคชันเขียนเองด้วยแพ็กเกจ
