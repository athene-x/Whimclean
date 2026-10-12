# Athenex.Whimclean.Auditing

[English](README.md)

Audit log สำหรับแอปพลิเคชัน Whimclean: ทุก command ที่ถูกทำเครื่องหมายว่าต้อง audit จะถูกบันทึกพร้อมผู้กระทำ เป้าหมาย ผลลัพธ์ และการเปลี่ยนแปลงของ field ที่ domain event รายงาน ไม่ว่าจะสำเร็จ ล้มเหลว หรือ throw รายการถูกเขียนนอก unit of work ของ command ดังนั้นความพยายามที่ล้มเหลวยังคงทิ้งร่องรอยไว้ ขณะที่การเปลี่ยนแปลงของมันเองถูก roll back

## ติดตั้ง

```shell
dotnet add package Athenex.Whimclean.Auditing
dotnet add package Athenex.Whimclean.Auditing.AspNetCore   # in the API host
```

แพ็กเกจนี้ขึ้นกับ `Athenex.Whimclean.Application`, `Athenex.Whimclean.Domain` และ `Mediator.Abstractions` แอปพลิเคชันเป็นผู้จัดหา Mediator source generator เช่นเดียวกับที่ทำสำหรับ `Athenex.Whimclean.Mediator`

## ทำเครื่องหมาย command ว่าต้อง audit

```csharp
using Athenex.Whimclean.Auditing;
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
```

`AuditDescriptor` รับ `Action` และ `EntityType` ตามด้วยค่าเลือกได้ `EntityId`, `EntityReference`, `TenantId`, `Reason` และ `Metadata` (dictionary ที่ถูก serialize เป็น JSON object) command ที่สร้าง entity ปล่อย `EntityId` เป็น null ไว้ behavior จะเติมให้จาก `Result<T>` ที่สำเร็จซึ่งมีค่าเป็น `Guid`, string หรือชนิด primitive เช่นตัวเลข `TenantId` ต้องเป็นชนิดของ tenant id ที่ลงทะเบียนกับ `AddAuditing` ค่าที่เป็นชนิดอื่นจะถูกละเว้น

## รายงานการเปลี่ยนแปลงจาก domain event

handler ที่สืบทอดจาก `DomainEventHandler<TEvent>` (แพ็กเกจ `Athenex.Whimclean.Mediator`) เพิ่มสิ่งที่รู้ลงใน collector แบบ scoped รายการจะรับค่าเหล่านั้นหลัง command commit:

```csharp
using Athenex.Whimclean.Auditing;
using Athenex.Whimclean.Mediator;

public sealed class TenantRenamedAuditHandler(IAuditChangeCollector collector)
    : DomainEventHandler<TenantRenamed>
{
    protected override ValueTask HandleAsync(TenantRenamed domainEvent, CancellationToken cancellationToken)
    {
        collector.Add("name", domainEvent.OldName, domainEvent.NewName);
        collector.SetEntityReference(domainEvent.Slug);
        return ValueTask.CompletedTask;
    }
}
```

`IAuditChangeCollector` มี `Add(field, oldValue, newValue)`, `SetEntityReference(reference)` และ `SetTenantId(tenantId)` ค่าที่ descriptor ระบุไว้แล้วมีลำดับความสำคัญเหนือค่าจาก collector

## สร้าง writer

รายการต้อง commit บน connection ของตัวเอง `DbContext` factory ทำแบบนั้นได้ ส่วนตารางถูก map และ migrate โดย context หลักของแอปพลิเคชัน:

```csharp
using Athenex.Whimclean.Auditing;
using Microsoft.EntityFrameworkCore;

public sealed class AuditLogWriter(IDbContextFactory<AuditLogDbContext> factory)
    : IAuditLogWriter<Guid, Guid>
{
    public async ValueTask WriteAsync(AuditLog<Guid, Guid> entry, CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        context.Add(entry);
        await context.SaveChangesAsync(cancellationToken);
    }
}
```

map `Changes` และ `Metadata` เป็น JSON column และกำหนดขนาด column ข้อความตามค่า `AuditLog<,>.Maximum…Length` entity จะ trim และตัดข้อความให้พอดีกับขนาดเหล่านั้น การเขียนจึงไม่ล้มเหลวเพราะความยาว:

| ค่าคงที่ | ความยาว |
| --- | --- |
| `MaximumActorNameLength` | 200 |
| `MaximumActionLength` | 100 |
| `MaximumEntityTypeLength` | 100 |
| `MaximumEntityIdLength` | 100 |
| `MaximumEntityReferenceLength` | 300 |
| `MaximumReasonLength` | 1000 |
| `MaximumErrorCodeLength` | 200 |
| `MaximumCorrelationIdLength` | 100 |
| `MaximumIpAddressLength` | 45 |
| `MaximumUserAgentLength` | 500 |

เอกสาร JSON ถูกสร้างโดย `AuditJson` ด้วยค่าเริ่มต้นแบบ web (camelCase) และคง null ไว้ การเปลี่ยนค่าเป็น `null` จึงยังมองเห็นได้

## ลงทะเบียน

```csharp
using Athenex.Whimclean.Auditing;
using Athenex.Whimclean.Auditing.AspNetCore;
using Athenex.Whimclean.Mediator;

services
    .AddAuditing<Guid, Guid>()          // before AddUnitOfWorkBehavior
    .AddMediatorDomainEventDispatcher()
    .AddUnitOfWorkBehavior();
services.AddScoped<IAuditLogWriter<Guid, Guid>, AuditLogWriter>();
services.AddHttpAuditRequestContext();  // Athenex.Whimclean.Auditing.AspNetCore
```

`AddAuditing` ต้องมาก่อน `AddUnitOfWorkBehavior`: Mediator รัน behavior ตามลำดับการลงทะเบียน และ audit behavior ต้องห่อ unit of work ไว้เพื่อเห็นผลลัพธ์ที่ commit แล้ว การเขียน audit เองไม่สามารถยกเลิกได้: ผลลัพธ์ที่เกิดขึ้นแล้วจะถูกบันทึกเสมอ และ exception จาก handler จะถูกบันทึกแล้ว throw ต่อ

`AddAuditing<TUserId, TTenantId>` ลงทะเบียน `AuditCommandBehavior<,>`, `AuditChangeCollector` และ `AuditLogRecorder<TUserId, TTenantId>` แบบ scoped พร้อม `TimeProvider.System` และ `NullAuditRequestContext` เว้นแต่มีการลงทะเบียนแต่ละอย่างไว้แล้ว type argument คือ local user id และ tenant id ซึ่งต้องเป็น value type ทั้งคู่ `IAuditLogWriter<TUserId, TTenantId>` ลงทะเบียนแยกต่างหาก นอก HTTP request `NullAuditRequestContext` ยังคงอยู่ และรายการที่ไม่รู้ผู้เรียกจะถูกระบุเป็น `system`

## รายการหนึ่งเก็บอะไรบ้าง

| Field | ที่มา |
| --- | --- |
| `Id`, `OccurredAt` | `Guid` เวอร์ชัน 7 และนาฬิกาจาก `TimeProvider` ปรับเป็น UTC |
| `ActorId`, `ActorName`, `ActorType` | `ICurrentUser<TUserId>` และชื่อผู้กระทำจาก request context ถ้าไม่มีใช้ user id ในรูปข้อความแทน; `system` เมื่อทั้งคู่ไม่รู้ผู้เรียก |
| `Action`, `EntityType`, `EntityId`, `EntityReference`, `TenantId`, `Reason`, `Metadata` | descriptor เติมเต็มโดย collector |
| `Result`, `ErrorCode` | `AuditResult.Success` หรือ `AuditResult.Failed` พร้อม error code ของ `Result` ที่ล้มเหลว หรือชื่อชนิดของ exception |
| `Changes` | การเปลี่ยนแปลงจาก collector ในรูป JSON array เฉพาะเมื่อสำเร็จ |
| `CorrelationId`, `IpAddress`, `UserAgent` | request context |

Repository และคู่มือสถาปัตยกรรมฉบับเต็ม: https://github.com/athene-x/Whimclean
