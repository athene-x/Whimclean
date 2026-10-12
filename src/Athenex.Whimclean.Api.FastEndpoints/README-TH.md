# Athenex.Whimclean.Api.FastEndpoints

[English](README.md)

adapter ของ FastEndpoints สำหรับ [`Athenex.Whimclean.Api.AspNetCore`](https://github.com/athene-x/Whimclean/blob/main/src/Athenex.Whimclean.Api.AspNetCore/README-TH.md) ซึ่งเป็นแพ็กเกจ API หลักของ Whimclean ติดตั้งในแอปพลิเคชันที่เขียน endpoint ด้วย FastEndpoints

adapter ดูแลเฉพาะส่วนที่เป็นของ FastEndpoints โดยเฉพาะ:

- การลงทะเบียน FastEndpoints และการค้นหา endpoint รวมถึงการค้นหา validator ธรรมดาของ FluentValidation
- เอกสาร OpenAPI ของ FastEndpoints
- การแสดงผลความล้มเหลวของ validation ของ FastEndpoints เป็น error body ร่วม
- การ bind filter ของ `ListQueryRequest` แบบทั้งค่า

ส่วนที่เหลือมาจากแพ็กเกจหลักและทำงานเหมือนกันทั้งสองรูปแบบ: error body และการ map exception, response ตาม status code, การ map `Result`, สัญญา `ListQueryRequest` และ validator ของมัน, การ resolve ผู้ใช้ปัจจุบัน, health endpoint, rate limit และ options ที่ผ่านการตรวจสอบ

## สารบัญ

- [การติดตั้ง](#การติดตั้ง)
- [เริ่มต้นใช้งาน](#เริ่มต้นใช้งาน)
- [อ้างอิงการลงทะเบียน](#อ้างอิงการลงทะเบียน)
- [OpenAPI](#openapi)
- [การตรวจสอบข้อมูล](#การตรวจสอบข้อมูล)
- [endpoint แบบรายการ](#endpoint-แบบรายการ)
- [error response](#error-response)

## การติดตั้ง

```shell
dotnet add package Athenex.Whimclean.Api.FastEndpoints
```

แพ็กเกจนี้อ้างอิง `Athenex.Whimclean.Api.AspNetCore`, `FastEndpoints` และ `FastEndpoints.OpenApi`

## เริ่มต้นใช้งาน

```csharp
using Athenex.Whimclean.Api.FastEndpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFastEndpointsApi();

var app = builder.Build();

app.UseAuthentication();   // when authentication is configured
app.UseAuthorization();    // when authorization is configured
app.UseFastEndpointsApi();

app.Run();
```

ด้วยการตั้งค่านี้:

- endpoint ทุกตัวอยู่ภายใต้ `/api/v1`
- validator ถูกค้นพบและรันก่อน endpoint เมื่อไม่ผ่านจะตอบ 400 `Validation.Failed`
- ความล้มเหลวทุกแบบ ตั้งแต่ exception ที่ถูก throw ไปจนถึง 404 ที่ไม่มี body ตอบด้วย error body ร่วมตามที่อธิบายใน [error response](#error-response)
- ค่า `filters` ที่ซ้ำกันใน query ของ `ListQueryRequest` ถูก bind ทั้งค่า

## อ้างอิงการลงทะเบียน

| เมธอด | ใช้กับ | หน้าที่ |
| --- | --- | --- |
| `AddFastEndpointsApi(configureOpenApi)` | `IServiceCollection` | ลงทะเบียน FastEndpoints โดยเปิด `IncludeAbstractValidators`, การจัดการ error ร่วมจาก `AddApiErrorHandling` และเอกสาร OpenAPI ของ FastEndpoints delegate (ถ้ามี) ใช้ปรับแต่ง `DocumentOptions` |
| `AddFastEndpointsApi(configureOpenApi, configureDiscovery)` | `IServiceCollection` | เหมือนข้างต้น พร้อม hook ที่ปรับ `EndpointDiscoveryOptions` หลังจาก adapter ตั้งค่าของตนแล้ว |
| `UseFastEndpointsApi(routePrefix, configure)` | `IApplicationBuilder` | `UseApiErrorHandling` บวก `UseFastEndpoints` พร้อม route prefix (ค่าเริ่มต้น `api/v1`), response builder ของ validation และ filter binding delegate (ถ้ามี) ใช้ปรับ `Config` ของ FastEndpoints ต่อจากนั้น |

การปรับแต่ง pipeline:

```csharp
app.UseFastEndpointsApi(
    routePrefix: "api/v2",
    configure: options =>
    {
        options.Security.PermissionsClaimType = "permission";
    });
```

## OpenAPI

ตั้งค่าเอกสารของ FastEndpoints ระหว่างลงทะเบียน:

```csharp
builder.Services.AddFastEndpointsApi(document =>
{
    document.Title = "Orders API";
});
```

## การตรวจสอบข้อมูล

### การค้นหา validator

FastEndpoints จับคู่ request DTO แต่ละตัวกับ validator ของมันตอนเริ่มระบบ adapter เปิด `IncludeAbstractValidators` ดังนั้น validator จะสืบทอดจาก `Validator<TRequest>` ของ FastEndpoints หรือสืบทอดโดยตรงจาก `AbstractValidator<TRequest>` ของ FluentValidation ก็ได้ นี่คือสิ่งที่ทำให้ validator ที่สืบทอดจาก `ListQueryRequestValidator<TRequest>` ของแพ็กเกจหลักถูกค้นพบ

เนื่องจากคลาสที่สืบทอดจาก `AbstractValidator<T>` ทุกตัวใน assembly ที่สแกนจะถูกค้นพบ ให้จำกัดการสแกนเมื่อ assembly มี validator ที่ไม่ใช่ validator ของ request:

```csharp
builder.Services.AddFastEndpointsApi(
    configureOpenApi: null,
    configureDiscovery: discovery => discovery.Assemblies = [typeof(Program).Assembly]);
```

validator ที่สืบทอดจาก `AbstractValidator<TRequest>` ใช้ `Resolve<T>()` ของ FastEndpoints ไม่ได้ ให้ inject dependency ผ่าน constructor แทน

### response ของการตรวจสอบ

ความล้มเหลวถูกแสดงผลผ่าน `ValidationHttpMapper` ของแพ็กเกจหลัก body จึงตรงกับ Minimal API ทุกประการ:

```json
{
  "code": "Validation.Failed",
  "description": "One or more validation errors occurred.",
  "errors": [
    { "code": "NotEmptyValidator", "description": "'Customer Id' must not be empty.", "field": "customerId" }
  ]
}
```

## endpoint แบบรายการ

สืบทอด request จาก `ListQueryRequest` ของแพ็กเกจหลัก และ validator จาก `ListQueryRequestValidator<TRequest>` FastEndpoints bind request จาก query string ค้นพบ validator และรันก่อน endpoint:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Queries;
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

พารามิเตอร์ของ query string, กฎการตรวจสอบ และเมธอด allow-list อธิบายไว้ในหัวข้อ [endpoint แบบรายการ](https://github.com/athene-x/Whimclean/blob/main/src/Athenex.Whimclean.Api.AspNetCore/README-TH.md#endpoint-แบบรายการ) ของแพ็กเกจหลัก

`UseFastEndpointsApi` ลงทะเบียน `ListQueryBinding.KeepListQueryFiltersWhole` ซึ่ง bind ค่า `filters` ที่ซ้ำกันแต่ละค่าตามที่ได้รับ เพื่อให้ค่า `in` ที่คั่นด้วยจุลภาค (`status:in:draft,published`) อยู่ใน filter เดียว เรียกเองเฉพาะเมื่อตั้งค่า `UseFastEndpoints` โดยตรง

## error response

ทุก response ที่ล้มเหลวใช้ body ของแพ็กเกจหลัก:

```json
{
  "code": "Order.NotFound",
  "description": "The order was not found.",
  "errors": null
}
```

| เงื่อนไข | HTTP | code |
| --- | ---: | --- |
| `CurrentUserRequiredException` | 401 | `Authentication.CurrentUserRequired` |
| `PersistenceConcurrencyException` | 409 | `Persistence.ConcurrentUpdate` |
| validation ของ FastEndpoints ล้มเหลว | 400 | `Validation.Failed` |
| response จบด้วย 400 โดยไม่มี body | 400 | `Request.Invalid` |
| response จบด้วย 401 โดยไม่มี body | 401 | `Authentication.Unauthorized` |
| response จบด้วย 403 โดยไม่มี body | 403 | `Authorization.Forbidden` |
| response จบด้วย 404 โดยไม่มี body | 404 | `Resource.NotFound` |
| exception อื่นใด | 500 | `Server.UnexpectedError` |

รายละเอียดของ exception ที่ไม่คาดคิดถูกบันทึก log และไม่ส่งกลับให้ client

### การคืน error ของ domain จาก endpoint

```csharp
using Athenex.Whimclean.Api.AspNetCore.Errors;

var result = await handler.HandleAsync(request, ct);

if (result.IsFailure)
{
    await Send.ResultAsync(result.Error.ToHttpResult());
    return;
}
```

`ErrorType` map เป็น status code เช่นเดียวกับแพ็กเกจหลัก: `Validation` 400, `Unauthorized` 401, `Forbidden` 403, `NotFound` 404, `Conflict` 409, `Failure` 500

### การเพิ่มการ map exception

ลงทะเบียน `IApiExceptionMapper`; ดู [การเพิ่มการ map exception](https://github.com/athene-x/Whimclean/blob/main/src/Athenex.Whimclean.Api.AspNetCore/README-TH.md#การเพิ่มการ-map-exception) ในแพ็กเกจหลัก mapper ถูกตรวจตามลำดับการลงทะเบียนและตัวแรกที่ตรงจะถูกใช้

---

Repository และคู่มือสถาปัตยกรรม: https://github.com/athene-x/Whimclean
