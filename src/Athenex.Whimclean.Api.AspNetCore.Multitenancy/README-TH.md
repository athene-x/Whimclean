# Athenex.Whimclean.Api.AspNetCore.Multitenancy

[English](README.md)

การ resolve tenant และการอนุญาตตามสมาชิกภาพสำหรับ ASP.NET Core (ตัวเลือกเสริม) สำหรับ Whimclean API

## ติดตั้ง

```shell
dotnet add package Athenex.Whimclean.Api.AspNetCore.Multitenancy
```

package นี้พึ่งพา `Athenex.Whimclean.Api.AspNetCore` สำหรับ error response ของ tenant ที่สม่ำเสมอ และพึ่งพา contract ของ multitenancy ที่ไม่มี dependency ใน `Athenex.Whimclean.Multitenancy.Abstractions` ไม่มีโค้ดของ FastEndpoints และใช้ได้ทั้งกับ FastEndpoints และ Minimal APIs

## implement การตรวจสอบสมาชิกภาพ

แอปพลิเคชันผู้ใช้งานเป็นเจ้าของ tenant และสมาชิกภาพ ให้ implement กฎการอนุญาตผ่าน `ITenantAccessValidator<TUserId, TTenantId>`:

```csharp
using Athenex.Whimclean.Multitenancy;
using Microsoft.EntityFrameworkCore;

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

ใส่การตรวจสอบสถานะ tenant, วันหมดอายุของสมาชิกภาพ, การระงับ หรือสิทธิ์การใช้งานไว้ที่นี่เมื่อจำเป็น implementation ควร fail closed: คืน `false` สำหรับผู้ใช้หรือ tenant ที่ไม่รู้จัก

## ลงทะเบียนการเชื่อมต่อกับ API

```csharp
using Athenex.Whimclean.Api.AspNetCore.Multitenancy;
using Athenex.Whimclean.Api.FastEndpoints;
using Athenex.Whimclean.Multitenancy;

builder.Services.AddFastEndpointsApi();
builder.Services.AddAspNetCoreMultitenancy<Guid, Guid>();
builder.Services.AddHeaderTenantResolution<Guid>(options =>
    options.HeaderName = "X-Tenant-Id");
builder.Services.AddScoped<
    ITenantAccessValidator<Guid, Guid>,
    TenantMembershipAccessValidator>();
```

generic argument ตัวแรกคือชนิด ID ของ current user ตัวที่สองคือชนิด ID ของ tenant

`AddAspNetCoreMultitenancy` ลงทะเบียน:

- `TenantContext<TTenantId>` แบบ scoped ซึ่งเปิดให้ใช้ทั้งในฐานะ `IRequestedTenant<TTenantId>` และ `ICurrentTenant<TTenantId>`
- authorization policy `TenantMember` (เปลี่ยนชื่อได้ผ่านพารามิเตอร์ `policyName`; `MultitenancyPolicies.TenantMember` เก็บค่าเริ่มต้น)
- authorization handler สำหรับสมาชิกภาพ
- การ map exception ของ tenant ให้เป็นรูปแบบ error มาตรฐานของ API

`AddHeaderTenantResolution<TTenantId>` ลงทะเบียน `HeaderRequestedTenantResolver<TTenantId>` เป็น `IRequestedTenantResolver<TTenantId>` (ผ่าน `TryAddScoped` ดังนั้น resolver ที่มีอยู่แล้วจะชนะ) ชื่อ header มีค่าเริ่มต้นเป็น `X-Tenant-Id` (`HeaderTenantResolutionOptions.HeaderName`) และค่าถูก parse ด้วย invariant culture ต้องมีค่า header เพียงหนึ่งค่าเท่านั้น: header ที่หายไปจะ resolve ไม่ได้อะไร และ header ที่ซ้ำถือว่ากำกวมและ resolve ไม่ได้อะไรเช่นกัน

## ลำดับ middleware

authentication ต้องระบุผู้ใช้ก่อนที่การอนุญาตตามสมาชิกภาพของ tenant จะทำงาน:

```csharp
app.UseAuthentication();
app.UseTenantResolution<Guid>();
app.UseAuthorization();
app.UseFastEndpointsApi();
```

resolver เพียงบันทึก tenant ที่ถูกร้องขอ `ICurrentTenant<Guid>` ยังว่างอยู่จนกว่าผู้ใช้ที่ยืนยันตัวตนแล้วจะผ่าน access validator

## ป้องกัน endpoint ของ tenant

```csharp
using Athenex.Whimclean.Api.AspNetCore.Multitenancy;
using FastEndpoints;

public sealed class ListOrdersEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("orders");
        Policies(MultitenancyPolicies.TenantMember);
    }

    public override Task HandleAsync(CancellationToken cancellationToken) =>
        HttpContext.Response.SendOkAsync(cancellationToken);
}
```

EF context ที่รองรับ tenant ซึ่งถูก resolve ระหว่าง endpoint จะได้รับ `ICurrentTenant<TTenantId>` ที่ผ่านการอนุญาตแล้วจาก request scope เดียวกัน

## ใช้ resolver จาก route, host หรือ claim

แทนที่ header resolver โดย implement `IRequestedTenantResolver<TTenantId>`:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Multitenancy;
using Microsoft.AspNetCore.Http;

public sealed class RouteTenantResolver
    : IRequestedTenantResolver<Guid>
{
    public bool TryResolve(HttpContext context, out Guid tenantId) =>
        Guid.TryParse(
            context.Request.RouteValues["tenantId"]?.ToString(),
            out tenantId);
}

services.AddScoped<IRequestedTenantResolver<Guid>, RouteTenantResolver>();
```

อย่าเรียก `AddHeaderTenantResolution` เมื่อต้องการให้ resolver ที่กำหนดเองเป็นแหล่งเดียว

## error response

- `CurrentTenantRequiredException` → 403 `Tenancy.CurrentTenantRequired`
- `TenantAccessViolationException` → 403 `Tenancy.AccessDenied`
- tenant ที่ถูกร้องขอซึ่งหายไปหรือถูกปฏิเสธ จะทำให้ policy `TenantMember` ไม่ผ่าน และได้ body 403 มาตรฐาน (`Authorization.Forbidden`)

package นี้ไม่สร้างตาราง tenant, ไม่จัดการสมาชิกภาพ และไม่ตัดสินว่าผู้ใช้ Identity เป็นแบบ global หรือเฉพาะ tenant ค่าเริ่มต้นที่แนะนำคือผู้ใช้ authentication แบบ global ร่วมกับสมาชิกภาพของ tenant ที่ผู้ใช้งาน package เป็นเจ้าของ

## resolve tenant จาก slug

deployment ที่ให้บริการแต่ละ tenant จาก path (`/acme/...`) หรือ hostname จะส่งต่อ slug ใน header; API จะ map slug นั้นเป็น id ของ tenant:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Multitenancy;
using Microsoft.EntityFrameworkCore;

public sealed class TenantSlugLookup(ApplicationDbContext dbContext) : ITenantSlugLookup<Guid>
{
    public async ValueTask<Guid> FindBySlugAsync(string slug, CancellationToken ct = default) =>
        await dbContext.Tenants
            .Where(tenant => tenant.Slug == slug)
            .Select(tenant => tenant.Id)
            .SingleOrDefaultAsync(ct);
}

builder.Services.AddAspNetCoreMultitenancy<Guid, Guid>();
builder.Services.AddHeaderTenantResolution<Guid>();          // optional: X-Tenant-Id still works
builder.Services.AddSlugTenantResolution();                  // X-Tenant-Slug
builder.Services.AddScoped<ITenantSlugLookup<Guid>, TenantSlugLookup>();

app.UseAuthentication();
app.UseTenantResolution<Guid>();
app.UseSlugTenantResolution<Guid>();
app.UseAuthorization();
```

`AddSlugTenantResolution` ไม่เป็น generic: ลงทะเบียนเพียง `SlugTenantResolutionOptions` (ชื่อ header ค่าเริ่มต้น `X-Tenant-Slug`); ชนิด id ของ tenant ระบุให้กับ `UseSlugTenantResolution<TTenantId>` และ `ITenantSlugLookup<TTenantId>` slug มาถึงในสภาพตัดช่องว่างและเป็นตัวพิมพ์เล็กแล้ว slug middleware จะไม่บันทึกอะไรเมื่อมี tenant ถูก resolve ไว้แล้ว ดังนั้น header ที่ระบุ id อย่างชัดเจนจะชนะ และ identifier ที่เป็นค่า default (ว่าง) หมายถึงไม่มี tenant นั้น เช่นเดียวกับ header ของ id ต้องมีค่า header ของ slug เพียงหนึ่งค่า; header ที่ซ้ำจะ resolve ไม่ได้อะไร เช่นเดียวกับ header resolver มันเพียงบันทึกคำกล่าวอ้าง; สมาชิกภาพยังถูกตรวจสอบโดย policy `TenantMember`

Repository และคู่มือสถาปัตยกรรมฉบับเต็ม: https://github.com/athene-x/Whimclean
