# การอัปเกรดเป็น 1.3.0

[English](upgrading-to-1.3.md)

ทุกการเปลี่ยนแปลงเป็นแบบเพิ่มเติม การลงทะเบียนเดิมยังทำงานได้ ขั้นตอนเหล่านี้ใช้แทนโค้ดที่แอปพลิเคชันผู้ใช้ library เคยเขียนเอง

## List query

ลบ `ListQueryParser`, `ListQueryRequest`, `ListQueryRequestValidator` และ filter binding ของแอปพลิเคชัน จากนั้น:

- import `Athenex.Whimclean.Application.Queries` สำหรับ parser และ `Athenex.Whimclean.Api.FastEndpoints.Queries` สำหรับ request และ validator
- แทนที่ `AllowFilterFields(dictionary, isValidValue)` ด้วยการเรียก `AllowFilterField` หรือ `AllowEnumFilterField<TEnum>` หนึ่งครั้งต่อฟิลด์
- ลบการเรียก `KeepListQueryFiltersWhole` ออกจาก `UseFastEndpointsApi` เพราะ adapter เรียกให้แล้ว

ในโปรเจกต์ persistence ให้ลบ `SortFields`, `LikePattern`, `EnumFilter` และ extension `Search`, `Page` และ `ToPage` แล้ว import `Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Queries` (และ `Athenex.Whimclean.Application.Queries` สำหรับ `EnumFilter`) และแทนที่ predicate `EF.Functions.ILike` ที่เขียนเองด้วย `SearchILike(options.SearchTerm, x => x.Field, ...)` จากแพ็กเกจ PostgreSQL

## ผู้ใช้ปัจจุบันและการ resolve tenant

แทนที่ middleware สำหรับผู้ใช้ภายในของแอปพลิเคชันด้วย `AddLocalUserResolution<Guid>` ร่วมกับ `ILocalUserLookup<Guid>` ที่ค้นจากตารางผู้ใช้ และ `UseLocalUserResolution<Guid>()` หลังการยืนยันตัวตน แทนที่ middleware และ resolver สำหรับ slug ด้วย `AddSlugTenantResolution`, `ITenantSlugLookup<Guid>` และ `UseSlugTenantResolution<Guid>()` หลัง `UseTenantResolution<Guid>()` และก่อน `UseAuthorization()`

## Keycloak

แทนที่การตั้งค่า JWT bearer ด้วย `AddKeycloakAuthentication(configuration)` section `Keycloak` ยังคงมี `Authority`, `Audience`, `MetadataAddress` และ `RequireHttpsMetadata` แทนที่ admin client ด้วย `AddKeycloakAdministration(configuration)` และพึ่งพา `IKeycloakUserAdministration` section `KeycloakAdmin` ไม่เปลี่ยนแปลง

## Audit log

แทนที่ type สำหรับ auditing ของแอปพลิเคชันด้วยแพ็กเกจ: command implement `IAuditedCommand` และคืน `AuditDescriptor`; event handler สืบทอดจาก `DomainEventHandler<TEvent>` (จาก `Athenex.Whimclean.Mediator`) และใช้ `IAuditChangeCollector`; แอปพลิเคชันเก็บไว้เพียง `IAuditLogWriter<Guid, Guid>` ของตัวเองและ EF mapping ของ `AuditLog<Guid, Guid>` ลงทะเบียนด้วย `AddAuditing<Guid, Guid>()` ก่อน `AddUnitOfWorkBehavior()` และ `AddHttpAuditRequestContext()` ใน host คอลัมน์ของ entity ตรงกับรูปแบบเดิม ตารางเดิมจึงไม่ต้องทำ migration นอกจากเปลี่ยนชื่อ CLR type ในโมเดล

## การปฏิบัติการ

แทนที่ helper สำหรับ health, rate limiting, สวิตช์ migration, การตรวจสอบ options และ configuration placeholder ด้วย `MapHealthEndpoints`, `AddRequestRateLimiting`, `DatabaseMigration.IsRequested` ร่วมกับ `MigrateDatabaseAsync<TContext>`, `AddValidatedOptions` และ `ConfigurationPlaceholder` จาก `Athenex.Whimclean.Api.AspNetCore` (namespace `Operations` และ `Configuration`) และแพ็กเกจ EF Core (`Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Operations`) ลงทะเบียน readiness check ของฐานข้อมูลด้วย `AddDbContextCheck<TContext>(tags: [HealthCheckTags.Ready])` จาก `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`

## Email

ลงทะเบียน `AddSmtpEmailSender` และส่งผ่าน `IEmailSender` notifier ของแอปพลิเคชันประกอบ `EmailMessage` แทนการคุยกับ `SmtpClient` โดยตรง
