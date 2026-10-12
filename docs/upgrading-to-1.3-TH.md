# การอัปเกรดเป็น 1.3.0

[English](upgrading-to-1.3.md)

เวอร์ชัน 1.3.0 เป็นการเพิ่มเติม: การลงทะเบียนทุกอย่างจาก 1.2.0 ยังทำงานได้ ขั้นตอนด้านล่างใช้แทนโค้ดที่แอปพลิเคชันผู้ใช้ library เขียนเองด้วยสิ่งที่แพ็กเกจจัดให้

## สารบัญ

- [แพ็กเกจ API](#แพ็กเกจ-api)
- [List query](#list-query)
- [ผู้ใช้ปัจจุบันและการ resolve tenant](#ผู้ใช้ปัจจุบันและการ-resolve-tenant)
- [Keycloak](#keycloak)
- [Audit log](#audit-log)
- [งานปฏิบัติการ](#งานปฏิบัติการ)
- [Email](#email)

## แพ็กเกจ API

`Athenex.Whimclean.Api.AspNetCore` เป็นแพ็กเกจ API หลัก สร้างบน ASP.NET Core Minimal APIs ส่วน `Athenex.Whimclean.Api.FastEndpoints` เป็น adapter บาง ๆ ที่ต่อยอดจากมัน

### แอปพลิเคชันที่ใช้ FastEndpoints

ใช้ `AddFastEndpointsApi` และ `UseFastEndpointsApi` ต่อไป มีพฤติกรรมสองอย่างที่ควรทราบ:

- การค้นหา endpoint ทำงานโดยเปิด `IncludeAbstractValidators` ดังนั้นคลาสที่สืบทอดจาก `AbstractValidator<T>` ทุกตัวใน assembly ที่สแกนจะถือเป็น validator ของ request หาก assembly มี validator ที่ไม่ใช่ validator ของ request ให้จำกัดการสแกน:

  ```csharp
  builder.Services.AddFastEndpointsApi(
      configureOpenApi: null,
      configureDiscovery: discovery => discovery.Assemblies = [typeof(Program).Assembly]);
  ```

- response 400 ที่จบโดยไม่มี body จะได้รับ body `ApiErrorResponse` แบบ `Request.Invalid` เช่นเดียวกับ response 401, 403 และ 404 response ที่เขียน body เอง เช่น ความล้มเหลวของ validation จะไม่ถูกแตะต้อง

### แอปพลิเคชันที่ใช้ Minimal API

ลงทะเบียนและ map API ผ่านแพ็กเกจหลัก:

```csharp
builder.Services.AddMinimalApi();
builder.Services.AddApiValidatorsFromAssemblyContaining<Program>();

app.UseMinimalApi();

var api = app.MapApiGroup();
api.MapGet("orders", async ([AsParameters] ListOrdersRequest request, IOrderQueries queries, CancellationToken ct) =>
    Results.Ok(await queries.ListAsync(request.ToListQueryOptions(), ct)));
```

`MapApiGroup` ใส่การตรวจสอบ request ให้ group `api/v1`; `WithRequestValidation` ใส่ให้ endpoint ที่ map ที่อื่น ความล้มเหลวของ validation ใช้ body `Validation.Failed` แบบเดียวกับ FastEndpoints

## List query

ลบ `ListQueryParser`, `ListQueryRequest`, `ListQueryRequestValidator` และ filter binding ที่แอปพลิเคชันเขียนเอง จากนั้น:

1. import `Athenex.Whimclean.Application.Queries` สำหรับ parser และ `Athenex.Whimclean.Api.AspNetCore.Queries` สำหรับ request และ validator
2. แทนที่ `AllowFilterFields(dictionary, isValidValue)` ด้วยการเรียก `AllowFilterField` หรือ `AllowEnumFilterField<TEnum>` หนึ่งครั้งต่อฟิลด์
3. แทนที่การเรียก `Resolve<T>()` ภายใน validator ด้วยการ inject ผ่าน constructor validator สืบทอดจาก `AbstractValidator<TRequest>` ของ FluentValidation ไม่ใช่ `Validator<TRequest>` ของ FastEndpoints
4. ลบการเรียก `KeepListQueryFiltersWhole` ออกจาก `UseFastEndpointsApi` เพราะ adapter เรียกให้ Minimal APIs ไม่ต้องมี binding: `[AsParameters]` เก็บค่า `filters` ที่ซ้ำกันแต่ละค่าไว้ทั้งค่า

ในโปรเจกต์ persistence ให้ลบ `SortFields`, `LikePattern`, `EnumFilter` และ extension `Search`, `Page` และ `ToPage` import `Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Queries` (และ `Athenex.Whimclean.Application.Queries` สำหรับ `EnumFilter`) และแทนที่ predicate `EF.Functions.ILike` ที่เขียนเองด้วย `SearchILike(options.SearchTerm, x => x.Field, ...)` จากแพ็กเกจ PostgreSQL

## ผู้ใช้ปัจจุบันและการ resolve tenant

- แทนที่ middleware สำหรับผู้ใช้ภายในของแอปพลิเคชันด้วย `AddLocalUserResolution<Guid>` ร่วมกับ `ILocalUserLookup<Guid>` ที่ค้นจากตารางผู้ใช้ และ `UseLocalUserResolution<Guid>()` หลังการยืนยันตัวตน
- แทนที่ middleware และ resolver สำหรับ slug ด้วย `AddSlugTenantResolution`, `ITenantSlugLookup<Guid>` และ `UseSlugTenantResolution<Guid>()` หลัง `UseTenantResolution<Guid>()` และก่อน `UseAuthorization()`

## Keycloak

- แทนที่การตั้งค่า JWT bearer ด้วย `AddKeycloakAuthentication(configuration)` section `Keycloak` มี `Authority`, `Audience`, `MetadataAddress` และ `RequireHttpsMetadata` เหมือนเดิม
- แทนที่ admin client ด้วย `AddKeycloakAdministration(configuration)` และพึ่งพา `IKeycloakUserAdministration` section `KeycloakAdmin` ไม่เปลี่ยนแปลง

## Audit log

แทนที่ type สำหรับ auditing ของแอปพลิเคชันด้วยแพ็กเกจ:

- command implement `IAuditedCommand` และคืน `AuditDescriptor`
- event handler สืบทอดจาก `DomainEventHandler<TEvent>` (จาก `Athenex.Whimclean.Mediator`) และใช้ `IAuditChangeCollector`
- แอปพลิเคชันเก็บไว้เพียง `IAuditLogWriter<Guid, Guid>` ของตัวเองและ EF mapping ของ `AuditLog<Guid, Guid>`

ลงทะเบียนด้วย `AddAuditing<Guid, Guid>()` ก่อน `AddUnitOfWorkBehavior()` และ `AddHttpAuditRequestContext()` ใน host คอลัมน์ของ entity ตรงกับรูปแบบเดิม ตารางเดิมจึงไม่ต้องทำ migration นอกจากเปลี่ยนชื่อ CLR type ในโมเดล

## งานปฏิบัติการ

แทนที่ helper ของแอปพลิเคชันด้วยสิ่งที่แพ็กเกจจัดให้:

| helper ของแอปพลิเคชัน | สิ่งที่ใช้แทน | แพ็กเกจ |
| --- | --- | --- |
| health endpoint | `MapHealthEndpoints` | `Api.AspNetCore` (`Operations`) |
| rate limiting | `AddRequestRateLimiting` | `Api.AspNetCore` (`Operations`) |
| สวิตช์ migration | `DatabaseMigration.IsRequested` ร่วมกับ `MigrateDatabaseAsync<TContext>` | `Infrastructure.EntityFrameworkCore` (`Operations`) |
| การตรวจสอบ options | `AddValidatedOptions` | `Api.AspNetCore` (`Configuration`) |
| configuration placeholder | `ConfigurationPlaceholder` | `Api.AspNetCore` (`Configuration`) |

ลงทะเบียน readiness check ของฐานข้อมูลด้วย `AddDbContextCheck<TContext>(tags: [HealthCheckTags.Ready])` จาก `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`

## Email

ลงทะเบียน `AddSmtpEmailSender` และส่งผ่าน `IEmailSender` notifier ของแอปพลิเคชันประกอบ `EmailMessage` แทนการคุยกับ `SmtpClient` โดยตรง
