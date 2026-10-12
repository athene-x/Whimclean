# คู่มือตั้งค่า Multitenancy

[English](multitenancy-setup.md)

วิธีตั้งค่าแอปพลิเคชัน Whimclean สำหรับรูปแบบการแยกข้อมูล tenant ทั้งสามแบบ:

| | กรณีที่ 1: แยกเชิงตรรกะ (Logical isolation) | กรณีที่ 2: แยกด้วย schema | กรณีที่ 3: แยกด้วยฐานข้อมูล |
| --- | --- | --- | --- |
| การจัดวางข้อมูล | ฐานข้อมูลเดียว schema เดียว มีคอลัมน์ `TenantId` ในทุกแถวที่ tenant เป็นเจ้าของ | ฐานข้อมูลเดียว หนึ่ง PostgreSQL schema ต่อ tenant | หนึ่งฐานข้อมูลต่อ tenant |
| บังคับใช้โดย | query filter ของ Whimclean + การตรวจสอบการเขียน | ตั้งค่า `search_path` ของ PostgreSQL ต่อ connection | เลือก connection string ต่อ request |
| การรองรับจาก library | ส่งมอบครบถ้วนแล้ว | ปัจจุบันผู้ใช้ library ต้องเขียนเอง (รูปแบบ interceptor ด้านล่าง; มีแผนจะทำ helper เป็นแพ็กเกจ) | ปัจจุบันผู้ใช้ library ต้องเขียนเอง (รูปแบบ connection-resolver ด้านล่าง; มีแผนจะทำ helper `ITenantStore` เป็นแพ็กเกจ) |
| ระดับความเข้มของการแยก | ระดับแอปพลิเคชัน | ระดับ database object | ระดับกายภาพ |
| ความพยายามในการทำ migration | รัน migration หนึ่งครั้ง | รันหนึ่งครั้งต่อ schema | รันหนึ่งครั้งต่อฐานข้อมูล |
| ต้นทุน / ความหนาแน่น | หนาแน่นสูงสุด ถูกที่สุด | ปานกลาง | หนึ่ง connection pool และพื้นที่จัดเก็บต่อ tenant |
| เลือกเมื่อ | ค่าเริ่มต้นของ SaaS; tenant ขนาดเล็กจำนวนมาก | ข้อกำหนดด้าน compliance ต้องการแยก schema; จำนวน tenant ปานกลาง | tenant ขนาดใหญ่จำนวนน้อย; ต้องการ backup/restore ต่อ tenant, ตรึงภูมิภาค หรือการแยกตามสัญญา |

ส่วน HTTP ของการตั้งค่า — การ resolve tenant ที่ถูกร้องขอ การตรวจสอบสมาชิกภาพ และการกำหนด tenant ปัจจุบัน — **เหมือนกันทั้งสามกรณี** ต่างกันเฉพาะส่วน persistence เท่านั้น

---

## การตั้งค่าร่วม (ทั้งสามกรณี)

### 1. ติดตั้งแพ็กเกจ

```shell
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Multitenancy # case 1 only
dotnet add package Athenex.Whimclean.Api.AspNetCore.Multitenancy
dotnet add package Athenex.Whimclean.Api.FastEndpoints # or use Minimal APIs with Api.AspNetCore
```

แพ็กเกจชั้นล่าง (`Domain`, `Application`, `Infrastructure`, `Multitenancy.Abstractions`) จะถูกติดตั้งมาโดยอ้อม

### 2. ยืนยันตัวตนและ resolve ผู้ใช้ปัจจุบัน

Whimclean ไม่เก็บข้อมูลผู้ใช้ ให้ยืนยันตัวตนกับ identity provider ของคุณ แล้วให้ claims adapter จัดหา `ICurrentUser<Guid>`:

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://keycloak.example.com/realms/myapp";
        options.Audience = "myapp-api";
    });

builder.Services.AddClaimsCurrentUser<Guid>();
```

เมื่อ user id เป็นแถวในตารางภายในแทนที่จะเป็น subject ของ token ให้ใช้ `AddLocalUserResolution<Guid>()` ร่วมกับ `ILocalUserLookup<Guid>` ที่ค้นจากตารางผู้ใช้แทน และเพิ่ม `app.UseLocalUserResolution<Guid>()` หลัง `UseAuthentication()`

### 3. เป็นเจ้าของโมเดล tenant และการตัดสินใจเรื่องสมาชิกภาพ

entity `Tenant` และ `TenantMembership` วงจรชีวิตของมัน และ query สมาชิกภาพเป็นของคุณ Whimclean ถามเพียงคำถามเดียวผ่าน `ITenantAccessValidator<TUserId, TTenantId>`:

```csharp
public sealed class TenantMembershipAccessValidator(CatalogDbContext catalog)
    : ITenantAccessValidator<Guid, Guid>
{
    public Task<bool> HasAccessAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        catalog.TenantMemberships.AnyAsync(
            membership =>
                membership.UserId == userId &&
                membership.TenantId == tenantId &&
                membership.IsActive,
            cancellationToken);
}
```

ในกรณีที่ 2 และ 3 ให้เก็บข้อมูลสมาชิกภาพนี้ในฐานข้อมูล/schema **catalog** ที่ใช้ร่วมกัน — ต้องอ่านได้ก่อนที่ connection ที่ผูกกับ tenant ใด ๆ จะมีอยู่

### 4. ลงทะเบียน HTTP tenant pipeline

```csharp
builder.Services.AddAspNetCoreMultitenancy<Guid, Guid>();
builder.Services.AddHeaderTenantResolution<Guid>(); // default header X-Tenant-Id
builder.Services.AddScoped<
    ITenantAccessValidator<Guid, Guid>,
    TenantMembershipAccessValidator>();
```

```csharp
app.UseAuthentication();
app.UseTenantResolution<Guid>(); // records the *requested* tenant — no authorization yet
app.UseAuthorization();          // TenantMember policy validates and establishes the *current* tenant
```

เมื่อ web tier ส่งต่อ slug ของ tenant แทน id ให้เรียก `builder.Services.AddSlugTenantResolution()` เพิ่ม (header เริ่มต้น `X-Tenant-Slug`) ลงทะเบียน `ITenantSlugLookup<Guid>` ที่ค้นจากตาราง tenant และเพิ่ม `app.UseSlugTenantResolution<Guid>()` หลัง `UseTenantResolution<Guid>()` slug จะถูกใช้เฉพาะเมื่อยังไม่มี requested tenant ถูกบันทึกไว้ ดังนั้น `X-Tenant-Id` ที่ระบุชัดเจนจะชนะ

ป้องกัน endpoint ของ tenant ด้วย policy ที่ให้มา:

```csharp
Policies(MultitenancyPolicies.TenantMember); // FastEndpoints
// or .RequireAuthorization(MultitenancyPolicies.TenantMember) on a Minimal API group
```

requested tenant (claim จาก transport ที่ยังไม่ได้ตรวจสอบ) และ current tenant (กำหนดขึ้นหลังจาก `ITenantAccessValidator` อนุมัติแล้วเท่านั้น) ถูกแยกเป็นคนละ interface โดยตั้งใจ — โค้ดที่ใช้ `ICurrentTenant<Guid>` จะไม่มีวันเห็นค่าที่ยังไม่ผ่านการตรวจสอบ resolver แบบ host, route หรือ claim สามารถแทนที่ `IRequestedTenantResolver<Guid>` ได้โดยไม่ต้องแตะต้องส่วนที่อยู่ด้านล่าง

### 5. host ที่ไม่ใช่ HTTP

background worker, queue consumer และ seeder ไม่มีการ resolve ผ่าน HTTP ให้ลงทะเบียน implementation แบบ ambient และครอบงานที่ผูกกับ tenant อย่างชัดเจน:

```csharp
services.AddScoped<ICurrentTenant<Guid>, AmbientCurrentTenant<Guid>>();
services.AddScoped<ICurrentUser<Guid>>(_ => new StaticCurrentUser<Guid>(SystemUserId));
```

```csharp
using (AmbientCurrentTenant<Guid>.Begin(tenantId))
{
    // reads are filtered and writes attributed to tenantId here
}
```

ผู้เรียกเป็นผู้รับรอง tenant เอง — `Begin` ไม่ตรวจสอบสิทธิ์การเข้าถึง

---

## กรณีที่ 1: แยกเชิงตรรกะ (schema ร่วมกัน คอลัมน์ `TenantId`)

รองรับเต็มรูปแบบโดยแพ็กเกจที่ส่งมอบแล้ว entity ที่ tenant เป็นเจ้าของ implement `IMultiTenant<Guid>` และ context สืบทอดจาก `MultiTenantDbContext`:

```csharp
public sealed class Order : AggregateRoot<Guid>, IAuditable<Guid?>, IMultiTenant<Guid>
{
    public Guid TenantId { get; private set; }
    // ...
}

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    TimeProvider timeProvider,
    ICurrentUser<Guid> currentUser,
    IAuditActorResolver<Guid, Guid?> auditActorResolver,
    IDomainEventDispatcher domainEventDispatcher,
    ICurrentTenant<Guid> currentTenant)
    : MultiTenantDbContext<Guid, Guid?, Guid>(
        options, timeProvider, currentUser, auditActorResolver,
        domainEventDispatcher, currentTenant)
{
    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
}
```

```csharp
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddNullableAuditActorResolver<Guid>();
builder.Services.AddDomainEventDispatcher();
builder.Services.AddPostgreSqlUnitOfWork<ApplicationDbContext>(connectionString);
```

สิ่งที่ context บังคับใช้:

- entity `IMultiTenant<Guid>` ได้รับ query filter ชื่อ `TenantFilter` แบบ fail-closed: เมื่อไม่มี current tenant query จะไม่คืนอะไรเลย
- ทุกแถวที่ tenant เป็นเจ้าของซึ่งถูกเพิ่ม แก้ไข หรือลบ ต้องเป็นของ current tenant และ `TenantId` ที่ถูกแก้ไขจะถูกปฏิเสธทันที (`TenantAccessViolationException`) — แถวไม่สามารถถูกย้ายระหว่าง tenant ได้ แม้แต่ย้าย *เข้า* tenant ปัจจุบันก็ตาม
- query สำหรับงานดูแลระบบสามารถปิด filter ทีละตัวได้โดยไม่ทิ้งอีกตัว:

```csharp
var acrossTenants = dbContext.Orders.IgnoreQueryFilters([QueryFilterNames.Tenant]);
var includingDeleted = dbContext.Orders.IncludeSoftDeleted();
```

migration: รัน `dotnet ef database update` ครั้งเดียว — schema เดียวให้บริการทุก tenant

---

## กรณีที่ 2: แยกด้วย schema (หนึ่ง PostgreSQL schema ต่อ tenant)

ยังไม่มีเป็นแพ็กเกจ รูปแบบที่ตกลงกันคือ **connection interceptor ที่ตั้งค่า `search_path`** ในทุก connection ที่เปิด **ห้าม** ใช้ `HasDefaultSchema` ต่อ tenant — นั่นจะสร้าง EF model cache หนึ่งรายการต่อ tenant

เนื่องจาก schema เป็นตัวแยกข้อมูล entity จึงไม่ต้อง implement `IMultiTenant<Guid>` และ context สืบทอดจาก `AuditableDbContext<Guid, Guid?>` ธรรมดา (auditing, soft delete และการ dispatch domain event ยังคงทำงาน)

### interceptor

```csharp
public sealed class TenantSearchPathInterceptor(ICurrentTenant<Guid> currentTenant)
    : DbConnectionInterceptor
{
    public override void ConnectionOpened(
        DbConnection connection, ConnectionEndEventData eventData) =>
        SetSearchPath(connection);

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        using var command = CreateSearchPathCommand(connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private void SetSearchPath(DbConnection connection)
    {
        using var command = CreateSearchPathCommand(connection);
        command.ExecuteNonQuery();
    }

    private DbCommand CreateSearchPathCommand(DbConnection connection)
    {
        if (!currentTenant.TryGetTenantId(out var tenantId))
        {
            // Fail closed: without a tenant there is no schema to query.
            throw new CurrentTenantRequiredException();
        }

        // SET does not accept parameters; set_config does, which keeps
        // the schema name out of SQL string concatenation. The name is
        // derived from a Guid, so its charset is safe by construction.
        var command = connection.CreateCommand();
        command.CommandText =
            "SELECT set_config('search_path', $1, false)";
        var parameter = command.CreateParameter();
        parameter.Value = $"tenant_{tenantId:N},public";
        command.Parameters.Add(parameter);
        return command;
    }
}
```

### ลงทะเบียน interceptor

interceptor ต้องการ `ICurrentTenant` แบบ scoped จึงต้องลงทะเบียนผ่านเมธอดที่ไม่ผูกกับ provider ทางลัด `AddPostgreSqlUnitOfWork` เปิดให้เพียง action ของ `NpgsqlDbContextOptionsBuilder` ที่เป็นตัวเลือก ไม่ได้ให้ service provider หรือ `DbContextOptionsBuilder` ซึ่งเป็นที่อยู่ของ `AddInterceptors`:

```csharp
builder.Services.AddScoped<TenantSearchPathInterceptor>();
builder.Services.AddEntityFrameworkCoreUnitOfWork<ApplicationDbContext>(
    (serviceProvider, options) => options
        .UseNpgsql(connectionString)
        .AddInterceptors(
            serviceProvider.GetRequiredService<TenantSearchPathInterceptor>()));
```

`DbContextOptions` เป็น scoped ดังนั้นแต่ละ request จะสร้าง options ด้วย interceptor instance ของ request นั้นเอง

### กฎและข้อควรระวัง

- **ห้ามนำค่าที่ไม่น่าเชื่อถือมาต่อเป็นชื่อ schema** ให้สร้างชื่อจาก current tenant id ที่ผ่านการตรวจสอบแล้วเท่านั้น (ตามแบบแผน `tenant_{guid:N}` ด้านบน) ไม่ใช่จาก header หรือค่าใน route
- connection pooling ปลอดภัย: interceptor ทำงานทุกครั้งที่มีการ *open* จริง และ Npgsql จะรีเซ็ตสถานะ session (รวมถึง `search_path`) เมื่อนำ connection ใน pool กลับมาใช้
- เก็บตารางอ้างอิงที่ใช้ร่วมกันไว้ใน `public` ส่วนต่อท้าย `,public` ใน search path ทำให้มองเห็นตารางเหล่านั้นแบบอ่านผ่านได้
- การจัดเตรียม tenant = `CREATE SCHEMA tenant_x` + รัน migration กับ schema นั้น

### Migration (รันหนึ่งครั้งต่อ schema)

มีแผนจะทำ `ITenantMigrationRunner` เป็นแพ็กเกจ ระหว่างนี้ให้วนรอบ schema เอง แต่ละ schema จะมี `__EFMigrationsHistory` ของตัวเอง เพราะตาราง history เดินตาม `search_path`:

```csharp
foreach (var tenantId in await catalog.GetTenantIdsAsync())
{
    using (AmbientCurrentTenant<Guid>.Begin(tenantId))
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        // ExecuteSqlRawAsync, not ExecuteSqlAsync: DDL cannot take the
        // schema name as a parameter. Safe here because the name is
        // derived from the Guid, never from user input.
        await dbContext.Database.ExecuteSqlRawAsync(
            $"CREATE SCHEMA IF NOT EXISTS tenant_{tenantId:N}");
        await dbContext.Database.MigrateAsync();
    }
}
```

รันสิ่งนี้จาก maintenance host ที่ลงทะเบียน `AmbientCurrentTenant<Guid>` และ `StaticCurrentUser<Guid>` (หัวข้อ “host ที่ไม่ใช่ HTTP”)

---

## กรณีที่ 3: แยกด้วยฐานข้อมูล (หนึ่งฐานข้อมูลต่อ tenant)

ยังไม่มีเป็นแพ็กเกจ รูปแบบที่ตกลงกันคือ **connection resolver ต่อ request** ที่อิง tenant store ซึ่งผู้ใช้ library เป็นเจ้าของ มีแผนจะทำ abstraction `ITenantStore` เป็นแพ็กเกจ

เช่นเดียวกับกรณีที่ 2 entity ไม่ต้อง implement `IMultiTenant<Guid>` และ context สืบทอดจาก `AuditableDbContext<Guid, Guid?>`

### tenant connection store

แมป tenant → connection string จากฐานข้อมูล catalog ที่ใช้ร่วมกัน options action ด้านล่างเป็นแบบ synchronous ดังนั้นให้ตอบการค้นหาจาก snapshot ในหน่วยความจำ (รีเฟรชเมื่อจัดเตรียม tenant ใหม่):

```csharp
public interface ITenantConnectionStore
{
    string GetConnectionString(Guid tenantId);
}
```

### ลงทะเบียน connection resolver

```csharp
builder.Services.AddSingleton<ITenantConnectionStore, CatalogTenantConnectionStore>();
builder.Services.AddEntityFrameworkCoreUnitOfWork<ApplicationDbContext>(
    (serviceProvider, options) =>
    {
        var currentTenant = serviceProvider
            .GetRequiredService<ICurrentTenant<Guid>>();

        if (!currentTenant.TryGetTenantId(out var tenantId))
        {
            // Fail closed: resolving the context without a tenant is a bug,
            // and the API layer maps this to 403 Tenancy.CurrentTenantRequired.
            throw new CurrentTenantRequiredException();
        }

        var store = serviceProvider
            .GetRequiredService<ITenantConnectionStore>();
        options.UseNpgsql(store.GetConnectionString(tenantId));
    });
```

`DbContextOptions` เป็น scoped ดังนั้น connection string จะถูกเลือกต่อ request — หลังจาก policy `TenantMember` กำหนด current tenant แล้ว

### ข้อควรระวังด้านปฏิบัติการ

- catalog (tenant, สมาชิกภาพ, connection string) อยู่ในฐานข้อมูลของตัวเองพร้อม context ธรรมดาของตัวเอง — ลงทะเบียนผ่าน `AddPostgreSqlContext<CatalogDbContext>` เพื่อไม่ให้กลายเป็น `IUnitOfWork` ของแอปพลิเคชัน
- แต่ละฐานข้อมูล tenant มี Npgsql connection pool ของตัวเอง เมื่อมี tenant จำนวนมาก ให้จำกัด `Maximum Pool Size` ต่อ tenant มิฉะนั้น pool จะดึงทรัพยากรของเซิร์ฟเวอร์จนหมด
- ห้ามสร้าง connection string จากข้อมูลใน request — ให้ใช้เฉพาะแถวใน catalog ที่ระบุด้วย current tenant ที่*ผ่านการตรวจสอบแล้ว*
- การจัดเตรียม tenant = สร้างฐานข้อมูล, เพิ่มแถวใน catalog, รัน migration, รีเฟรช snapshot ของ store

### Migration (รันหนึ่งครั้งต่อฐานข้อมูล)

connection resolver ด้านบนเลือกฐานข้อมูลจาก current tenant อยู่แล้ว ดังนั้นให้วนรอบ catalog ภายใต้ ambient tenant scope และ resolve context จาก container:

```csharp
foreach (var tenantId in await catalog.GetTenantIdsAsync())
{
    using (AmbientCurrentTenant<Guid>.Begin(tenantId))
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
```

เช่นเดียวกับกรณีที่ 2 ให้รันสิ่งนี้จาก maintenance host ที่ลงทะเบียน `AmbientCurrentTenant<Guid>` และ `StaticCurrentUser<Guid>` (หัวข้อ “host ที่ไม่ใช่ HTTP”) migration ไม่ dispatch domain event ใด ๆ

---

## การผสมรูปแบบ

กรณีต่าง ๆ ประกอบกันได้ มีสองแบบผสมที่ควรรู้:

- **Defense in depth**: คง `IMultiTenant<Guid>` + `MultiTenantDbContext` ไว้*ซ้อนบน*การแยกด้วย schema หรือฐานข้อมูล row filter จะช่วยป้องกันกรณี connection ถูกส่งผิดที่ โดยแลกกับคอลัมน์ `TenantId` ที่ซ้ำซ้อน
- **tenant หลายระดับ**: tenant ขนาดใหญ่ใช้ฐานข้อมูลเฉพาะ tenant ขนาดเล็กใช้ฐานข้อมูลแบบแยกเชิงตรรกะร่วมกัน HTTP pipeline เหมือนกันทั้งสองแบบ มีเพียง options action ที่แยกสาขาตามโหมดการแยกของ tenant ใน catalog

เริ่มจากกรณีที่ 1 เว้นแต่มีข้อกำหนดระบุเป็นอย่างอื่น — เป็นแบบที่ถูกที่สุดทั้งในการรันและการทำ migration และ filter แบบ fail-closed บวกการตรวจสอบการเขียน (รวมถึงการปฏิเสธการย้าย tenant) ให้การรับประกันระดับแอปพลิเคชันที่แข็งแรงอยู่แล้ว การย้าย tenant ออกไปยัง schema หรือฐานข้อมูลของตัวเองในภายหลังเป็นการย้ายข้อมูล ไม่ใช่การเขียนโค้ดใหม่: domain model และ HTTP pipeline ยังคงเดิม
