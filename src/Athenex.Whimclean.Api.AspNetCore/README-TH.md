# Athenex.Whimclean.Api.AspNetCore

[English](README.md)

แพ็กเกจ API หลักของแอปพลิเคชัน Whimclean ออกแบบสำหรับ ASP.NET Core Minimal APIs และให้สิ่งต่อไปนี้:

- การลงทะเบียนการจัดการ error และเอกสาร OpenAPI ในครั้งเดียว
- การตรวจสอบ request ด้วย FluentValidation ที่ใช้ได้ทั้งกับ route group และ endpoint รายตัว
- สัญญา `ListQueryRequest` สำหรับ endpoint แบบรายการที่แบ่งหน้า เรียงลำดับ และกรองได้
- รูปร่าง error response เดียวสำหรับ exception, response ตาม status code ที่ไม่มี body, ความล้มเหลวของ validation และ `Result` ของ domain ที่ล้มเหลว
- การ resolve ผู้ใช้ปัจจุบันจาก claim หรือจากตารางผู้ใช้ภายใน
- health endpoint, rate limit ต่อผู้เรียก และ options ที่ผ่านการตรวจสอบสำหรับ host

แอปพลิเคชันที่เขียนด้วย FastEndpoints ให้ติดตั้ง [`Athenex.Whimclean.Api.FastEndpoints`](https://github.com/athene-x/Whimclean/blob/main/src/Athenex.Whimclean.Api.FastEndpoints/README-TH.md) แทน adapter นั้นต่อยอดจากแพ็กเกจนี้ ทั้งสองรูปแบบจึงใช้สัญญาและ response ชุดเดียวกัน

## สารบัญ

- [การติดตั้ง](#การติดตั้ง)
- [เริ่มต้นใช้งาน](#เริ่มต้นใช้งาน)
- [อ้างอิงการลงทะเบียน](#อ้างอิงการลงทะเบียน)
- [OpenAPI](#openapi)
- [การตรวจสอบ request](#การตรวจสอบ-request)
- [endpoint แบบรายการ](#endpoint-แบบรายการ)
- [error response](#error-response)
- [การ map result เป็น HTTP](#การ-map-result-เป็น-http)
- [ผู้ใช้ปัจจุบัน](#ผู้ใช้ปัจจุบัน)
- [งานปฏิบัติการ](#งานปฏิบัติการ)
- [options ที่ผ่านการตรวจสอบ](#options-ที่ผ่านการตรวจสอบ)

## การติดตั้ง

```shell
dotnet add package Athenex.Whimclean.Api.AspNetCore
```

แพ็กเกจนี้ขึ้นกับ `Athenex.Whimclean.Application`, `FluentValidation` และ `Microsoft.AspNetCore.OpenApi`

## เริ่มต้นใช้งาน

```csharp
using Athenex.Whimclean.Api.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalApi();
builder.Services.AddApiValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

app.UseAuthentication();   // when authentication is configured
app.UseAuthorization();    // when authorization is configured
app.UseMinimalApi();

var api = app.MapApiGroup();
api.MapGet("orders", () => Results.Ok());

app.Run();
```

ด้วยการตั้งค่านี้:

- endpoint ทุกตัวที่ map บน `api` อยู่ภายใต้ `/api/v1`
- อาร์กิวเมนต์ของ handler ที่ชนิดมี validator ลงทะเบียนไว้จะถูกตรวจสอบก่อน handler ทำงาน
- ความล้มเหลวทุกแบบ ตั้งแต่ exception ที่ถูก throw ไปจนถึง 404 ที่ไม่มี body ตอบด้วย error body ร่วมตามที่อธิบายใน [error response](#error-response)
- เอกสาร OpenAPI ให้บริการที่ `/openapi/v1.json`

## อ้างอิงการลงทะเบียน

| เมธอด | ใช้กับ | หน้าที่ |
| --- | --- | --- |
| `AddMinimalApi(configureOpenApi)` | `IServiceCollection` | ลงทะเบียน `AddApiErrorHandling` และเอกสาร OpenAPI delegate (ถ้ามี) ใช้ปรับแต่ง `OpenApiOptions` |
| `AddApiValidatorsFromAssembly(assembly, lifetime)` | `IServiceCollection` | ลงทะเบียน `IValidator<T>` ที่เป็น concrete ทุกตัวใน assembly ภายใต้ interface ของมัน ค่าเริ่มต้นเป็น scoped และใช้ `TryAdd` ดังนั้นการลงทะเบียนที่ระบุเองจะชนะ validator ที่เป็น abstract หรือ open generic จะถูกข้าม |
| `AddApiValidatorsFromAssemblyContaining<T>(lifetime)` | `IServiceCollection` | เหมือนข้างต้น สำหรับ assembly ที่มี `T` |
| `AddApiErrorHandling()` | `IServiceCollection` | เฉพาะการจัดการ error: สายของ `IApiExceptionMapper`, `ApiExceptionHandler` และการรองรับ ProblemDetails |
| `UseMinimalApi(openApiRoutePattern)` | `WebApplication` | `UseApiErrorHandling` บวก endpoint ของ OpenAPI ส่ง `null` เพื่อไม่ map เอกสาร |
| `UseApiErrorHandling()` | `IApplicationBuilder` | การจัดการ exception และ body สำหรับ response 400, 401, 403 และ 404 ที่จบโดยไม่มี body |
| `MapApiGroup(routePrefix)` | `IEndpointRouteBuilder` | สร้าง route group (ค่าเริ่มต้น `api/v1`) พร้อมการตรวจสอบ request |
| `WithRequestValidation()` | endpoint หรือ group | ใส่การตรวจสอบ request ให้ endpoint ที่ map นอก group |

## OpenAPI

`AddMinimalApi` ลงทะเบียนเอกสาร; `UseMinimalApi` map เอกสารไว้ที่ `/openapi/{documentName}.json`

ปรับแต่งเอกสารผ่าน `OpenApiOptions`:

```csharp
builder.Services.AddMinimalApi(openApi =>
    openApi.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Orders API";
        return Task.CompletedTask;
    }));
```

หากต้องการเปิดเอกสารเฉพาะใน development ให้ไม่ map เอกสารแล้ว map เองตามเงื่อนไข:

```csharp
app.UseMinimalApi(openApiRoutePattern: null);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
```

## การตรวจสอบ request

### หลักการทำงาน

1. `MapApiGroup` (หรือ `WithRequestValidation`) ผูก endpoint filter factory เข้ากับ group หรือ endpoint
2. ตอนเริ่มระบบ factory ตรวจ handler แต่ละตัวครั้งเดียว และจดจำพารามิเตอร์ที่ชนิดมี `IValidator<T>` ลงทะเบียนไว้ endpoint ที่ไม่มีพารามิเตอร์เช่นนั้นทำงานตามปกติโดยไม่มีค่าใช้จ่ายเพิ่ม
3. ในแต่ละ request อาร์กิวเมนต์ที่จดจำไว้ถูกตรวจสอบตามลำดับพารามิเตอร์ ความล้มเหลวแรกจะหยุด handler และตอบ 400 `Validation.Failed`

### การเขียน validator

validator คือ `AbstractValidator<T>` ธรรมดาของ FluentValidation; `AddApiValidatorsFromAssembly` ลงทะเบียนให้ dependency รับผ่าน constructor

```csharp
using FluentValidation;

public sealed record CreateOrderRequest(string CustomerId, decimal Total);

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(request => request.CustomerId).NotEmpty();
        RuleFor(request => request.Total).GreaterThan(0);
    }
}

api.MapPost("orders", (CreateOrderRequest request) => Results.Created());
```

### response ของการตรวจสอบ

```json
{
  "code": "Validation.Failed",
  "description": "One or more validation errors occurred.",
  "errors": [
    { "code": "NotEmptyValidator", "description": "'Customer Id' must not be empty.", "field": "customerId" },
    { "code": "GreaterThanValidator", "description": "'Total' must be greater than '0'.", "field": "total" }
  ]
}
```

แต่ละรายการมีรหัส error ของ FluentValidation, ข้อความ และชื่อ property แบบ camelCase ความล้มเหลวที่ไม่ผูกกับ property ใดจะมี `field: null`

### การตรวจสอบด้วยตนเอง

`ValidationHttpMapper` สร้าง response แบบเดียวกันจากผลของ FluentValidation ใดก็ได้:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Errors;

var result = await validator.ValidateAsync(request, cancellationToken);
if (!result.IsValid)
{
    return result.ToHttpResult();   // 400 พร้อม body ข้างต้น
}
```

`failures.ToApiErrorResponse()` คืนเฉพาะ body สำหรับกรณีที่ตั้ง status code เองที่อื่น

## endpoint แบบรายการ

`ListQueryRequest` คือสัญญา query string ที่ endpoint แบบรายการซึ่งแบ่งหน้าแบบ offset ใช้ร่วมกัน:

| พารามิเตอร์ | ความหมาย | ค่าเริ่มต้น |
| --- | --- | --- |
| `searchTerm` | ข้อความค้นหาที่จับคู่กับฟิลด์ค้นหาของ endpoint | ไม่มี |
| `sort` | ชื่อฟิลด์คั่นด้วยจุลภาค นำหน้าด้วย `-` เพื่อเรียงจากมากไปน้อย | ไม่มี |
| `filters` | รายการ `field:operator:value` ซ้ำได้หลายค่า | ไม่มี |
| `page` | หมายเลขหน้าเริ่มที่ 1 | `1` |
| `size` | จำนวนรายการต่อหน้า | `10` |

`ListQueryParser` ใน `Athenex.Whimclean.Api.AspNetCore.Queries` แปลงค่าเหล่านี้เป็น `ListQueryOptions` ของ `Athenex.Whimclean.Application` (`Parse`, `ParseSort`, `ParseFilters` throw `FormatException` เมื่อ input ผิด ส่วน `TryParseSort` และ `TryParseFilter` คืนข้อความข้อผิดพลาดที่ validator ใช้) ไวยากรณ์ของ `sort` และ `filters` อธิบายไว้ในหัวข้อ "ไวยากรณ์ของ list query" ของ README แพ็กเกจ Application

### การนิยาม request และ validator

สืบทอด request หนึ่งตัวต่อ endpoint สืบทอด validator ของมันจาก `ListQueryRequestValidator<TRequest>` และประกาศฟิลด์ที่ endpoint รองรับ:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Queries;
using Athenex.Whimclean.Application.Queries;

public sealed class ListOrdersRequest : ListQueryRequest;

public sealed class ListOrdersRequestValidator : ListQueryRequestValidator<ListOrdersRequest>
{
    public ListOrdersRequestValidator()
    {
        AllowSortFields("createdAt", "total");
        AllowEnumFilterField<OrderStatus>("status");
        AllowFilterField("createdAt", FilterOperator.GreaterThanOrEqual, FilterOperator.LessThan);
    }
}
```

### การ map endpoint

bind request ด้วย `[AsParameters]` การตรวจสอบ request ของ group รัน validator ก่อน handler ดังนั้น `ToListQueryOptions()` จะได้รับเฉพาะข้อมูลที่ถูกต้องเสมอ:

```csharp
api.MapGet("orders", async (
    [AsParameters] ListOrdersRequest request,
    IOrderQueries queries,
    CancellationToken cancellationToken) =>
    Results.Ok(await queries.ListAsync(request.ToListQueryOptions(), cancellationToken)));
```

การ bind ของ Minimal API เก็บค่า `filters` ที่ซ้ำกันแต่ละค่าไว้ทั้งค่า ดังนั้นค่า `in` ที่คั่นด้วยจุลภาค เช่น `status:in:draft,published` จะอยู่ใน filter เดียว

### กฎการตรวจสอบ

validator พื้นฐานปฏิเสธ:

- ค่า `sort` และ `filters` ที่ผิดรูปแบบ
- `page` ที่น้อยกว่า 1 และ `size` ที่อยู่นอกช่วง 1 ถึง `ListQueryOptions.MaximumSize`
- filter มากกว่า `ListQueryOptions.MaximumFilters` รายการ
- `searchTerm` ที่ยาวเกิน `ListQueryOptions.MaximumSearchLength`

allow-list เพิ่มกฎเฉพาะของแต่ละ endpoint:

| เมธอด | ผล |
| --- | --- |
| `AllowSortFields(params string[] fields)` | เฉพาะฟิลด์ที่ระบุเท่านั้นที่ปรากฏใน `sort` ได้ |
| `AllowFilterField(field, params operators)` | ฟิลด์นี้กรองได้ด้วย operator ที่ระบุ (`Equal` และ `In` เมื่อไม่ระบุ) และรับค่าใดก็ได้ |
| `AllowFilterField(field, isValidValue, params operators)` | เหมือนข้างต้น แต่รับเฉพาะค่าที่ predicate คืน `true` |
| `AllowEnumFilterField<TEnum>(field, params operators)` | เหมือนข้างต้น แต่รับเฉพาะชื่อสมาชิกของ `TEnum` โดยไม่สนตัวพิมพ์ใหญ่เล็ก |

เมื่ออนุญาตฟิลด์ filter ใดแล้ว filter ที่ระบุฟิลด์อื่นจะถูกปฏิเสธ หากไม่มี allow-list ทุกฟิลด์จะผ่าน และ query จะละเลยสิ่งที่ไม่รู้จัก ข้อความปฏิเสธระบุชื่อฟิลด์ operator หรือค่าที่ผิด เช่น `Sort field 'email' is not supported.`

## error response

ทุก response ที่ล้มเหลวใช้ body เดียวกัน:

```json
{
  "code": "Resource.NotFound",
  "description": "The requested resource was not found.",
  "errors": null
}
```

| เงื่อนไข | HTTP | code |
| --- | ---: | --- |
| `CurrentUserRequiredException` | 401 | `Authentication.CurrentUserRequired` |
| `PersistenceConcurrencyException` | 409 | `Persistence.ConcurrentUpdate` |
| การตรวจสอบ request ล้มเหลว | 400 | `Validation.Failed` |
| response จบด้วย 400 โดยไม่มี body (เช่น ค่าใน query ที่ bind ไม่ได้) | 400 | `Request.Invalid` |
| response จบด้วย 401 โดยไม่มี body | 401 | `Authentication.Unauthorized` |
| response จบด้วย 403 โดยไม่มี body | 403 | `Authorization.Forbidden` |
| response จบด้วย 404 โดยไม่มี body | 404 | `Resource.NotFound` |
| exception อื่นใด | 500 | `Server.UnexpectedError` |

รายละเอียดของ exception ที่ไม่คาดคิดถูกบันทึก log และไม่ส่งกลับให้ client request ที่ client ยกเลิกเองจะไม่มี body และถูก log ที่ระดับ debug

### การเพิ่มการ map exception

implement `IApiExceptionMapper` แล้วลงทะเบียน mapper ถูกตรวจตามลำดับการลงทะเบียนและตัวแรกที่ตรงจะถูกใช้; `DefaultApiExceptionMapper` ในตัวลงทะเบียนด้วย `TryAddEnumerable` ดังนั้น mapper ที่เพิ่มเข้ามาจะไม่แทนที่มัน

```csharp
using Athenex.Whimclean.Api.AspNetCore.Errors;
using Microsoft.AspNetCore.Http;

public sealed class OrderApiExceptionMapper : IApiExceptionMapper
{
    public bool TryMap(Exception exception, out ApiExceptionMapping mapping)
    {
        if (exception is OrderLockedException)
        {
            mapping = new ApiExceptionMapping(
                StatusCodes.Status409Conflict,
                new ApiErrorResponse("Order.Locked", "The order is currently locked."));
            return true;
        }

        mapping = default;
        return false;
    }
}

builder.Services.AddSingleton<IApiExceptionMapper, OrderApiExceptionMapper>();
```

`Athenex.Whimclean.Api.AspNetCore.Multitenancy` เพิ่มการ map ของ tenant ผ่านจุดขยายเดียวกันนี้

## การ map result เป็น HTTP

`ToHttpResult` แปลง `Result` หรือ `Error` ของ domain เป็น `IResult` พร้อม body ร่วม:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Errors;

api.MapGet("orders/{id:guid}", async (Guid id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetOrderQuery(id));
    return result.ToHttpResult(order => Results.Ok(order));
});
```

| `ErrorType` | HTTP |
| --- | ---: |
| `Validation` | 400 |
| `Unauthorized` | 401 |
| `Forbidden` | 403 |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `Failure` | 500 |

## ผู้ใช้ปัจจุบัน

Whimclean ไม่เก็บผู้ใช้หรือ credential ให้ยืนยันตัวตนกับ provider ใดก็ได้ที่เติม `HttpContext.User` (Keycloak, Microsoft Entra ID, Auth0, ASP.NET Core Identity) แล้วเลือกวิธีแปลง principal ที่ยืนยันตัวตนแล้วให้เป็น `ICurrentUser<TUserId>` ที่ write boundary ของระบบ audit ใช้

### จาก claim

`AddClaimsCurrentUser<TUserId>` อ่าน id ของผู้ใช้จาก claim บน principal:

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

ชนิด claim เริ่มต้นคือ `ClaimTypes.NameIdentifier` ซึ่ง JWT bearer authentication map claim `sub` มาให้ เมื่อปิด inbound claim mapping (`options.MapInboundClaims = false`) ให้อ่าน claim ดิบแทน:

```csharp
builder.Services.AddClaimsCurrentUser<Guid>(options => options.ClaimType = "sub");
```

`TUserId` ต้อง implement `IParsable<TUserId>` การลงทะเบียนใช้ `TryAddScoped` ดังนั้น `ICurrentUser<TUserId>` ที่แอปพลิเคชันจัดให้เอง (เช่น system user ของ background job) จะชนะ

### จากตารางผู้ใช้ภายใน

เมื่อแอปพลิเคชันเก็บแถวผู้ใช้ของตนเอง และ token มีเพียง subject ของ provider ให้ resolve id ภายในต่อ request:

```csharp
using Athenex.Whimclean.Api.AspNetCore;
using Athenex.Whimclean.Api.AspNetCore.Context;
using Microsoft.EntityFrameworkCore;

public sealed class LocalUserLookup(ApplicationDbContext dbContext) : ILocalUserLookup<Guid>
{
    public async ValueTask<Guid> FindByExternalSubjectAsync(string externalSubject, CancellationToken ct = default) =>
        await dbContext.Users
            .Where(user => user.ExternalSubject == externalSubject)
            .Select(user => user.Id)
            .SingleOrDefaultAsync(ct);
}

builder.Services.AddLocalUserResolution<Guid>(options => options.ClaimType = "sub");
builder.Services.AddScoped<ILocalUserLookup<Guid>, LocalUserLookup>();

app.UseAuthentication();
app.UseLocalUserResolution<Guid>();
```

`AddLocalUserResolution` ลงทะเบียน `RequestCurrentUser<TUserId>` แบบ scoped เป็น `ICurrentUser<TUserId>` (ผ่าน `TryAddScoped`) และเป็น `ICurrentUserInitializer<TUserId>` ดังนั้น bootstrap ตอนเริ่มระบบสามารถระบุผู้เขียนได้หลังจาก resolve ผู้ใช้ของตนเอง middleware อ่าน subject จาก claim ที่ระบุโดย `ClaimsCurrentUserOptions.ClaimType` แล้วเรียก lookup identifier ที่เป็นค่า default (ว่าง) หมายถึงไม่มีผู้ใช้ภายใน: ผู้ใช้ปัจจุบันจะยังไม่ถูกตั้งค่า และ write boundary จะปฏิเสธการเขียน วาง middleware หลังการยืนยันตัวตนและก่อนสิ่งใดก็ตามที่อ่านผู้ใช้ปัจจุบัน เช่น การ resolve tenant และการอนุญาต

## งานปฏิบัติการ

```csharp
using Athenex.Whimclean.Api.AspNetCore.Operations;

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(tags: [HealthCheckTags.Ready]);
builder.Services.AddRequestRateLimiting(builder.Configuration);

app.UseAuthentication();
app.UseRateLimiter();
app.MapHealthEndpoints();
```

### health endpoint

`MapHealthEndpoints` map endpoint แบบ anonymous สองตัวที่ได้รับยกเว้นจาก rate limit:

| path | พฤติกรรม |
| --- | --- |
| `/health/live` | ตอบทันทีที่ process ให้บริการ request ได้ ไม่รัน check ใด |
| `/health/ready` | รัน health check ที่ติด tag `HealthCheckTags.Ready` |

path ทั้งสองเปลี่ยนได้ผ่านพารามิเตอร์ `livePath` และ `readyPath` `AddDbContextCheck` มาจาก `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`

### rate limit

`AddRequestRateLimiting` เพิ่ม rate limit แบบ fixed window ระดับ global ต่อผู้เรียก โดยใช้ key จาก claim `sub` (`user:{sub}`) หรือสำหรับ request แบบ anonymous ใช้ remote address (`ip:{address}`) การปฏิเสธตอบ 429

| การตั้งค่า (section `RateLimiting`) | ค่าเริ่มต้น |
| --- | ---: |
| `PermitLimit` | 300 |
| `WindowSeconds` | 60 |

options ถูกตรวจด้วย `RequestRateLimitOptions.Validate()` ตอนลงทะเบียน: permit limit หรือ window ที่ไม่เป็นค่าบวกจะ throw `InvalidOperationException` ที่ระบุชื่อ setting มี overload ที่รับ instance ของ `RequestRateLimitOptions` แทน configuration

## options ที่ผ่านการตรวจสอบ

`AddValidatedOptions` bind คลาส options กับ section ของ configuration และตรวจสอบตอน host เริ่มทำงาน ดังนั้น deployment ที่ตั้งค่าผิดจะล้มเหลวตอน boot พร้อมข้อความระบุชื่อ setting:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Configuration;

builder.Services.AddValidatedOptions<EmailOptions>(
    builder.Configuration,
    "Email",
    options => string.IsNullOrWhiteSpace(options.Host) ? "Email:Host is required." : null);
```

delegate ตรวจสอบคืนข้อความความล้มเหลว หรือ `null` เมื่อ options ถูกต้อง

`ConfigurationPlaceholder.IsUnset` รู้จัก marker `<set-via-user-secrets>` (`ConfigurationPlaceholder.Value`) ที่ commit ไว้ใน `appsettings.json` สำหรับค่าที่ต้องมาจาก user secrets หรือ environment ค่าที่หายไปหรือว่างเปล่าถือว่ายังไม่ได้ตั้งเช่นกัน

---

Repository และคู่มือสถาปัตยกรรม: https://github.com/athene-x/Whimclean
