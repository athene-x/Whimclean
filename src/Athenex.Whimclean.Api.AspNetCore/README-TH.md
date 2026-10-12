# Athenex.Whimclean.Api.AspNetCore

[English](README.md)

ข้อตกลงสำหรับ ASP.NET Core API ที่ไม่ผูกกับ framework ใด: error contract ที่สม่ำเสมอ, การจัดการ exception, response ตาม status code และการ map `Result` ใช้ได้กับ FastEndpoints, Minimal APIs หรือรูปแบบ endpoint อื่นของ ASP.NET Core

## ติดตั้ง

```shell
dotnet add package Athenex.Whimclean.Api.AspNetCore
```

ผู้ใช้ FastEndpoints มักติดตั้ง `Athenex.Whimclean.Api.FastEndpoints` แทน ซึ่งต่อยอดจาก package นี้และลงทะเบียนทุกอย่างด้านล่างให้โดยอัตโนมัติ

## ลงทะเบียน service และ middleware

```csharp
using Athenex.Whimclean.Api.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiErrorHandling();

var app = builder.Build();

app.UseApiErrorHandling();

app.MapGet("api/v1/orders", () => Results.Ok());

app.Run();
```

`AddApiErrorHandling` ลงทะเบียน pipeline ของ exception handler, การรองรับ ProblemDetails และการ map exception ขอบเขตระบบชุดเริ่มต้น `UseApiErrorHandling` เพิ่มการจัดการ exception พร้อม body แบบ JSON ที่สม่ำเสมอสำหรับ response 401 (`Authentication.Unauthorized`), 403 (`Authorization.Forbidden`) และ 404 (`Resource.NotFound`) ที่จบโดยไม่มี body

## error response

ความล้มเหลวทุกแบบใช้รูปร่างเดียวกัน:

```json
{
  "code": "Resource.NotFound",
  "description": "The requested resource was not found.",
  "errors": null
}
```

การ map exception ชุดเริ่มต้น:

- `CurrentUserRequiredException` → 401 `Authentication.CurrentUserRequired`
- `PersistenceConcurrencyException` → 409 `Persistence.ConcurrentUpdate`
- exception ที่ไม่ได้ map → 500 `Server.UnexpectedError` โดยซ่อนรายละเอียด

เพิ่มการ map สำหรับ exception ของแอปพลิเคชันได้โดยลงทะเบียน `IApiExceptionMapper` เพิ่มเติม mapper ถูกตรวจตามลำดับการลงทะเบียน และตัวแรกที่ตรงจะถูกใช้

## current user จาก claim

`AddClaimsCurrentUser<TUserId>` ลงทะเบียน `ICurrentUser<TUserId>` ที่อ่านจาก claim เพื่อให้ write boundary ของระบบ audit ทำงานได้กับ authentication scheme ใดก็ตามที่เติม `HttpContext.User` รวมถึง JWT bearer token จาก identity provider ภายนอก เช่น Keycloak, Microsoft Entra ID หรือ Auth0 Whimclean ไม่เก็บผู้ใช้หรือ credential; ให้ยืนยันตัวตนกับ provider ของคุณ แล้ว audit pipeline จะระบุผู้เขียนจาก claim ใน token

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
builder.Services.AddClaimsCurrentUser<Guid>(options =>
    options.ClaimType = "sub");
```

การลงทะเบียนใช้ `TryAddScoped` ดังนั้น `ICurrentUser<TUserId>` ที่แอปพลิเคชันจัดให้เอง (เช่น system user ของ background job) จะชนะ implementation แบบ claim

## map result เป็น HTTP

```csharp
using Athenex.Whimclean.Api.AspNetCore.Errors;

app.MapGet("api/v1/orders/{id:guid}", async (Guid id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetOrderQuery(id));
    return result.ToHttpResult(order => Results.Ok(order));
});
```

`ErrorType` map เป็น status code ดังนี้: `Validation` → 400, `Unauthorized` → 401, `Forbidden` → 403, `NotFound` → 404, `Conflict` → 409, `Failure` → 500

## current user จากตารางภายใน

เมื่อแอปพลิเคชันเก็บแถวผู้ใช้ของตนเอง และ token มีเพียง subject ของ identity provider ให้ resolve id ภายในต่อ request:

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

`AddLocalUserResolution` ลงทะเบียน `RequestCurrentUser<TUserId>` แบบ scoped เป็น `ICurrentUser<TUserId>` (ผ่าน `TryAddScoped` ดังนั้นการลงทะเบียนที่มีอยู่แล้วจะชนะ) และอ่าน subject จาก claim ที่ระบุโดย `ClaimsCurrentUserOptions.ClaimType` identifier ที่เป็นค่า default (ว่าง) หมายถึงไม่มีผู้ใช้ภายใน; current user จะยังไม่ถูกตั้งค่า และ write boundary จะปฏิเสธการเขียน `RequestCurrentUser<TUserId>` แบบ scoped ตัวเดียวกันนี้ implement `ICurrentUserInitializer<TUserId>` ด้วย ดังนั้น bootstrap ตอนเริ่มระบบสามารถระบุผู้เขียนได้หลังจาก resolve ผู้ใช้ของตนเอง

## การปฏิบัติงาน

```csharp
using Athenex.Whimclean.Api.AspNetCore.Operations;

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(tags: [HealthCheckTags.Ready]);
builder.Services.AddRequestRateLimiting(builder.Configuration);

app.UseAuthentication();
app.UseRateLimiter();
app.MapHealthEndpoints();
```

- `MapHealthEndpoints` map `/health/live` ซึ่งตอบทันทีที่ process ให้บริการ request ได้ และ `/health/ready` ซึ่งรัน health check ที่ติด tag `HealthCheckTags.Ready`; ทั้งสองเป็น anonymous และได้รับยกเว้นจาก rate limit path ทั้งสองเปลี่ยนได้ผ่านพารามิเตอร์ `livePath` และ `readyPath`
- `AddRequestRateLimiting` เพิ่ม rate limit แบบ fixed window ระดับ global ต่อผู้เรียก โดยใช้ key จาก claim `sub` (`user:{sub}`) หรือ remote address (`ip:{address}`) อ่านค่าจาก section `RateLimiting` (`PermitLimit` ค่าเริ่มต้น 300; `WindowSeconds` ค่าเริ่มต้น 60) การปฏิเสธตอบ 429 options ถูกตรวจด้วย `RequestRateLimitOptions.Validate()` ตอนลงทะเบียน: permit limit หรือ window ที่ไม่เป็นค่าบวกจะ throw `InvalidOperationException` ที่ระบุชื่อ setting มี overload ที่รับ instance ของ `RequestRateLimitOptions` แทน configuration

## options ที่ผ่านการตรวจสอบ

`AddValidatedOptions` bind คลาส options กับ section และตรวจสอบตอน host เริ่มทำงาน ดังนั้น deployment ที่ตั้งค่าผิดจะล้มเหลวตอน boot พร้อมข้อความระบุชื่อ setting:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Configuration;

builder.Services.AddValidatedOptions<EmailOptions>(
    builder.Configuration,
    "Email",
    options => string.IsNullOrWhiteSpace(options.Host) ? "Email:Host is required." : null);
```

delegate ตรวจสอบคืนข้อความความล้มเหลว หรือ `null` เมื่อ options ถูกต้อง `ConfigurationPlaceholder.IsUnset` รู้จัก marker `<set-via-user-secrets>` (`ConfigurationPlaceholder.Value`) ที่ commit ไว้ใน `appsettings.json` สำหรับค่าที่ต้องมาจาก user secrets หรือ environment; และถือว่าค่าที่หายไปหรือว่างเปล่าเป็นค่าที่ยังไม่ได้ตั้งเช่นกัน

Repository และคู่มือสถาปัตยกรรมฉบับเต็ม: https://github.com/athene-x/Whimclean
