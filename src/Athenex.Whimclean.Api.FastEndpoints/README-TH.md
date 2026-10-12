# Athenex.Whimclean.Api.FastEndpoints

[English](README.md)

adapter ของ FastEndpoints สำหรับข้อตกลง API ของ Whimclean: การลงทะเบียน endpoint, OpenAPI และ response ของการตรวจสอบข้อมูล error contract, การจัดการ exception, response ตาม status code และการ map result อยู่ใน `Athenex.Whimclean.Api.AspNetCore` (ซึ่ง package นี้อ้างอิง) จึงยังเหมือนเดิมหากแอปพลิเคชันย้ายไปใช้ Minimal APIs ในภายหลัง

## ติดตั้ง

```shell
dotnet add package Athenex.Whimclean.Api.FastEndpoints
```

## ลงทะเบียน service และ middleware

```csharp
using Athenex.Whimclean.Api.FastEndpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFastEndpointsApi();

var app = builder.Build();

app.UseAuthentication();       // when authentication is configured
app.UseAuthorization();        // when authorization is configured
app.UseFastEndpointsApi();

app.Run();
```

`AddFastEndpointsApi` ลงทะเบียน FastEndpoints, การจัดการ error ร่วมจาก `AddApiErrorHandling` และเอกสาร OpenAPI `UseFastEndpointsApi` ใช้ route prefix เริ่มต้น `api/v1`, การจัดการ exception, response 401/403/404 ที่สม่ำเสมอสำหรับ endpoint ที่จบโดยไม่มี body, การ map การตรวจสอบข้อมูลของ FastEndpoints, การ bind filter ของ list query แบบทั้งค่า และการค้นหา endpoint

ปรับ route prefix และการตั้งค่า FastEndpoints เมื่อจำเป็น:

```csharp
app.UseFastEndpointsApi(
    routePrefix: "api/v2",
    configure: options =>
    {
        options.Security.PermissionsClaimType = "permission";
    });
```

ตั้งค่าเอกสาร OpenAPI ที่สร้างขึ้นระหว่างลงทะเบียน service:

```csharp
builder.Services.AddFastEndpointsApi(document =>
{
    document.Title = "Orders API";
});
```

## map error ของแอปพลิเคชัน

แปลง `Error` ของ Whimclean ที่ล้มเหลวเป็น HTTP response มาตรฐาน:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Errors;

var result = await handler.HandleAsync(request, cancellationToken);

if (result.IsFailure)
{
    await Send.ResultAsync(result.Error.ToHttpResult());
    return;
}
```

การ map คือ validation → 400, unauthorized → 401, forbidden → 403, not found → 404, conflict → 409 และความล้มเหลวทั่วไป → 500

response ใช้รูปร่างนี้:

```json
{
  "code": "Order.NotFound",
  "description": "The order was not found.",
  "errors": null
}
```

ความล้มเหลวจากการตรวจสอบข้อมูลของ FastEndpoints ตอบ 400 ด้วย code `Validation.Failed` และเติม `errors` ด้วย code, description และชื่อ field แบบ camelCase ของแต่ละความล้มเหลว

## การ map exception ในตัว

- `CurrentUserRequiredException` → 401 `Authentication.CurrentUserRequired`
- `PersistenceConcurrencyException` → 409 `Persistence.ConcurrentUpdate`
- exception ที่ไม่คาดคิด → 500 `Server.UnexpectedError`

รายละเอียดของ exception ที่ไม่คาดคิดถูกบันทึก log แต่ไม่ส่งกลับให้ client

## เพิ่มการ map exception เฉพาะผลิตภัณฑ์

```csharp
using Athenex.Whimclean.Api.AspNetCore.Errors;
using Microsoft.AspNetCore.Http;

public sealed class OrderApiExceptionMapper : IApiExceptionMapper
{
    public bool TryMap(
        Exception exception,
        out ApiExceptionMapping mapping)
    {
        if (exception is OrderLockedException)
        {
            mapping = new ApiExceptionMapping(
                StatusCodes.Status409Conflict,
                new ApiErrorResponse(
                    "Order.Locked",
                    "The order is currently locked."));
            return true;
        }

        mapping = default;
        return false;
    }
}

services.AddSingleton<IApiExceptionMapper, OrderApiExceptionMapper>();
```

mapper ถูกตรวจตามลำดับการลงทะเบียน และตัวแรกที่ตรงจะถูกใช้ package multitenancy สำหรับ ASP.NET Core (ตัวเลือกเสริม) เพิ่มการ map ของ tenant ผ่านจุดขยายเดียวกันนี้

## endpoint แบบรายการ

สืบทอด request จาก `ListQueryRequest` เพื่อรับ `searchTerm`, `sort`, `filters`, `page` และ `size` ที่ bind จาก query string และสืบทอด validator จาก `ListQueryRequestValidator<TRequest>` เพื่อประกาศสิ่งที่ endpoint รองรับ:

```csharp
using Athenex.Whimclean.Api.FastEndpoints.Queries;
using Athenex.Whimclean.Application.Pagination;
using Athenex.Whimclean.Application.Queries;
using FastEndpoints;

public sealed class ListTenantsRequest : ListQueryRequest;

public sealed class ListTenantsRequestValidator : ListQueryRequestValidator<ListTenantsRequest>
{
    public ListTenantsRequestValidator()
    {
        AllowSortFields("slug", "name", "status");
        AllowEnumFilterField<TenantStatus>("status");
        AllowFilterField("createdAt", FilterOperator.GreaterThanOrEqual, FilterOperator.LessThan);
    }
}

public sealed class ListTenantsEndpoint(ITenantQueries queries)
    : Endpoint<ListTenantsRequest, OffsetPagedResult<TenantResponse>>
{
    public override void Configure() => Get("tenants");

    public override async Task HandleAsync(ListTenantsRequest request, CancellationToken ct) =>
        await Send.OkAsync(await queries.ListAsync(request.ToListQueryOptions(), ct), ct);
}
```

validator พื้นฐานปฏิเสธค่า sort และ filter ที่ผิดรูปแบบ, page และ size นอกช่วง (`page` ≥ 1, `size` ระหว่าง 1 ถึง `ListQueryOptions.MaximumSize`), filter มากกว่า `ListQueryOptions.MaximumFilters` รายการ และคำค้นหาที่ยาวเกินไป; allow-list ปฏิเสธ field, operator และค่าที่ไม่รู้จักพร้อมข้อความระบุชื่อสิ่งนั้น `AllowFilterField` และ `AllowEnumFilterField` ใช้ operator `Equal` และ `In` เป็นค่าเริ่มต้นเมื่อไม่ระบุ; `AllowEnumFilterField<TEnum>` รับชื่อสมาชิกของ enum โดยไม่สนตัวพิมพ์ใหญ่เล็ก หากไม่มี allow-list ทุก field จะผ่าน และ query จะละเลยสิ่งที่ไม่รู้จัก `ToListQueryOptions` แปลง request เป็น `ListQueryOptions`; ให้รัน validator ก่อนเพื่อให้ข้อมูลที่ผิดรูปแบบปรากฏเป็น 400 แทนที่จะเป็น exception `UseFastEndpointsApi` ลงทะเบียน binding (`ListQueryBinding.KeepListQueryFiltersWhole`) ที่เก็บค่า `in` ที่คั่นด้วยจุลภาคไว้ใน filter เดียว

Repository และคู่มือสถาปัตยกรรมฉบับเต็ม: https://github.com/athene-x/Whimclean
