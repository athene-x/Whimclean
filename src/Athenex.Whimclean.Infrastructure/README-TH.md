# Athenex.Whimclean.Infrastructure

[English](README.md)

ส่วนประกอบ infrastructure ที่ไม่ผูกกับผู้ให้บริการรายใด ซึ่ง EF Core integration ของ Whimclean ใช้งาน: การ resolve actor สำหรับ audit, pipeline ของ domain event แบบ in-process และตัวระบุสำหรับ seed แบบ deterministic

## การติดตั้ง

```shell
dotnet add package Athenex.Whimclean.Infrastructure
```

## การ resolve actor สำหรับ audit

`IAuditActorResolver<TUserId, TActor>` (ใน `Athenex.Whimclean.Infrastructure.Auditing`) แปลงตัวระบุผู้ใช้ที่ผ่านการยืนยันตัวตนแล้วให้เป็นรูปแบบที่คอลัมน์ audit จัดเก็บ

สำหรับ user ID แบบ `Guid` ที่เก็บใน property audit แบบ `Guid?` ที่เป็น nullable:

```csharp
using Athenex.Whimclean.Infrastructure;

services.AddNullableAuditActorResolver<Guid>();
```

เมื่อทั้งสองชนิดเหมือนกันและไม่เป็น nullable:

```csharp
services.AddIdentityAuditActorResolver<Guid>();
```

helper ทั้งสองลงทะเบียน singleton แบบ TryAdd (`NullableAuditActorResolver<TUserId>` ต้องใช้ value type; `IdentityAuditActorResolver<TActor>` คืน id กลับไปโดยไม่เปลี่ยนแปลง) implement interface เองเมื่อ actor ของ audit ต้อง map ไปเป็นชื่อ, service account หรือ value object:

```csharp
using Athenex.Whimclean.Infrastructure.Auditing;

services.AddSingleton<
    IAuditActorResolver<Guid, ActorStamp>,
    ActorStampResolver>();
```

## pipeline ของ domain event

แพ็กเกจนี้มาพร้อม pipeline แบบ in-process ใน `Athenex.Whimclean.Infrastructure.DomainEvents`: `IDomainEventDispatcher` ส่ง event ที่ commit แล้วแต่ละตัวไปยัง `IDomainEventHandler<TEvent>` ทุกตัวที่ลงทะเบียนไว้สำหรับ runtime type ของมัน unit of work เรียก dispatcher หลังจาก commit สำเร็จ ความล้มเหลวของ handler จึงไม่ทำให้การเปลี่ยนแปลงที่บันทึกแล้วถูก roll back

```csharp
using Athenex.Whimclean.Infrastructure;

services.AddDomainEventDispatcher();
services.AddDomainEventHandler<OrderPlaced, SendConfirmationEmail>();
services.AddDomainEventHandler<OrderPlaced, ReserveStock>();
```

`AddDomainEventDispatcher` ลงทะเบียน `ServiceProviderDomainEventDispatcher` เป็น service แบบ scoped ด้วย TryAdd; `AddDomainEventHandler` ลงทะเบียน handler แบบ scoped ด้วย TryAddEnumerable ดังนั้น handler หลายตัวตอบสนองต่อ event เดียวได้ ขณะที่การลงทะเบียนซ้ำจะยุบเหลือหนึ่งรายการ

```csharp
using Athenex.Whimclean.Application.Email;
using Athenex.Whimclean.Infrastructure.DomainEvents;

public sealed class SendConfirmationEmail(IEmailSender emailSender)
    : IDomainEventHandler<OrderPlaced>
{
    public Task HandleAsync(
        OrderPlaced domainEvent,
        CancellationToken cancellationToken = default) =>
        emailSender.SendAsync(
            new EmailMessage(
                "member@example.com",
                "Order confirmed",
                $"Order {domainEvent.OrderId} has been placed."),
            cancellationToken);
}
```

### ความล้มเหลวในการ dispatch

เมื่อ handler throw หลังจาก commit สำเร็จแล้ว การบันทึกจะแสดง `DomainEventDispatchException` ออกมา ห้ามลองบันทึกซ้ำ — ข้อมูลถูกบันทึกไปแล้ว; event ที่ไปไม่ถึง handler อยู่ใน `UndeliveredEvents` (event ที่ล้มเหลวอยู่ตัวแรก ตามด้วย event ที่ยังไม่ได้พยายามส่ง) พร้อมให้นำไปจัดการหรือ replay unit of work ของ EF Core ล้าง event ของแต่ละ aggregate ก่อน dispatch ดังนั้น event ที่ handler raise ระหว่าง dispatch จะคงอยู่สำหรับ commit ครั้งถัดไป และมัน dispatch ด้วย `CancellationToken.None` ดังนั้น request ที่ถูกยกเลิกจะไม่สามารถขัดจังหวะการส่ง event ของการเปลี่ยนแปลงที่บันทึกไปแล้ว

### เชื่อมต่อกับ mediator

dispatcher ตัว default แทนที่ได้: ลงทะเบียน `IDomainEventDispatcher` ของคุณเอง (ก่อนหรือแทน `AddDomainEventDispatcher` — TryAdd จะคงของเดิมไว้) เพื่อเชื่อมต่อไปยัง mediator หรือ message dispatcher ตัวอย่างด้านล่างใช้กับ `IMediator` แบบ MediatR ที่ `Publish` รับ object ใดก็ได้:

```csharp
using Athenex.Whimclean.Domain.Events;
using Athenex.Whimclean.Infrastructure.DomainEvents;

public sealed class MediatorDomainEventDispatcher(IMediator mediator)
    : IDomainEventDispatcher
{
    public Task DispatchAsync(
        IDomainEvent domainEvent,
        CancellationToken cancellationToken = default) =>
        mediator.Publish(domainEvent, cancellationToken);
}
```

```csharp
services.AddScoped<IDomainEventDispatcher, MediatorDomainEventDispatcher>();
```

สำหรับ `martinothamar/Mediator` แบบ source-generated แพ็กเกจ `Athenex.Whimclean.Mediator` มีสะพานเชื่อมนี้ให้แล้วในชื่อ `AddMediatorDomainEventDispatcher()`; ดู [README ของแพ็กเกจนั้น](../Athenex.Whimclean.Mediator/README-TH.md)

แพ็กเกจนี้จงใจไม่เลือก MediatR, EF Core, ผู้ให้บริการฐานข้อมูล, Identity หรือ multitenancy

## ตัวระบุสำหรับ seed แบบ deterministic

`DeterministicGuid.Create(parentId, key)` (ใน `Athenex.Whimclean.Infrastructure.Seeding`) สร้างตัวระบุที่คงที่จาก id ของ parent และ key โดย hash ทั้งสองด้วย SHA-256 แถวที่ seed ไว้ เช่น system role จึงได้ id เดียวกันในทุก environment และ seeder ยังคง idempotent overload `Create(key)` ใช้ `Guid.Empty` เป็น parent สำหรับแถว seed ระดับบนสุด:

```csharp
using Athenex.Whimclean.Infrastructure.Seeding;

var ownerRoleId = DeterministicGuid.Create(tenantId, "owner");
```

Repository และคู่มือสถาปัตยกรรมฉบับเต็ม: https://github.com/athene-x/Whimclean
