# Athenex.Whimclean.Mediator

[English](README.md)

integration แบบเลือกใช้ได้กับ [Mediator](https://github.com/martinothamar/Mediator) (`martinothamar/Mediator` แบบ source-generated ไม่ใช่ MediatR): pipeline behavior ที่ commit unit of work หลังแต่ละ command และ dispatcher ที่ส่ง domain event เป็น notification ของ mediator แพ็กเกจนี้ขึ้นกับ `Mediator.Abstractions` เท่านั้น (บวก `Athenex.Whimclean.Application` และ `Athenex.Whimclean.Infrastructure`); แอปพลิเคชันที่นำไปใช้เป็นผู้จัดหา source generator เอง

## การติดตั้ง

```shell
dotnet add package Athenex.Whimclean.Mediator
dotnet add package Mediator.SourceGenerator
```

`Mediator.SourceGenerator` ต้องถูกอ้างอิงจากโปรเจกต์แอปพลิเคชัน (หรือโปรเจกต์ที่ compilation ของมันมี handler อยู่) — generator จะสร้าง `AddMediator` และโค้ด dispatch ไว้ที่นั่น

## การลงทะเบียน

```csharp
using Athenex.Whimclean.Mediator;

builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
});

builder.Services
    .AddMediatorDomainEventDispatcher() // before any default-dispatcher registration
    .AddUnitOfWorkBehavior();

// e.g. AddPostgreSqlUnitOfWork<AppDbContext>(...) afterwards; its
// TryAdd of the default dispatcher then yields to the mediator one.
```

`AddMediatorDomainEventDispatcher` ลงทะเบียน `MediatorDomainEventDispatcher` เป็น `IDomainEventDispatcher` แบบ scoped ด้วย TryAdd ดังนั้นให้เรียกมัน **ก่อน** สิ่งที่ลงทะเบียน `ServiceProviderDomainEventDispatcher` ตัว default (โดยทั่วไปคือการลงทะเบียน unit of work) `AddUnitOfWorkBehavior` เพิ่ม open generic `UnitOfWorkBehavior<,>` เป็น `IPipelineBehavior<,>` แบบ scoped ด้วย TryAddEnumerable การเรียกสองครั้งจึงลงทะเบียน behavior เพียงครั้งเดียว

## command เป็นผู้ commit unit of work

`UnitOfWorkBehavior<TMessage, TResponse>` ผูกกับทุก message ที่ implement `ICommand<TResponse>` — query และ notification ไม่ถูกแตะต้อง handler แก้ไข aggregate แล้ว return; behavior เรียก `IUnitOfWork.CommitAsync` หลังจากนั้น หนึ่ง command จึงครอบคลุมหนึ่ง commit พอดี และ handler ไม่ต้องบันทึกเอง:

```csharp
using Athenex.Whimclean.Domain.Results;
using Mediator;

public sealed record PlaceOrder(Guid CustomerId) : ICommand<Result<Guid>>;

public sealed class PlaceOrderHandler(AppDbContext context)
    : ICommandHandler<PlaceOrder, Result<Guid>>
{
    public async ValueTask<Result<Guid>> Handle(
        PlaceOrder command, CancellationToken cancellationToken)
    {
        var order = Order.Place(command.CustomerId);
        context.Orders.Add(order);
        return order.Id; // no SaveChanges/Commit here
    }
}
```

การ commit จะถูกข้ามเมื่อ handler คืน `Result` (หรือ `Result<T>`) ที่ล้มเหลว หรือ throw ทำให้การเปลี่ยนแปลงที่ค้างอยู่ไม่ถูกบันทึก

## domain event ในรูป notification

domain event implement `INotification` เองไม่ได้ — ชั้น domain ไม่มี dependency กับ mediator — ดังนั้น `MediatorDomainEventDispatcher` จึงห่อแต่ละ event ไว้ใน `DomainEventNotification` แล้ว publish ผ่าน `IPublisher` handler สืบทอดจาก `DomainEventHandler<TEvent>` ซึ่งรับทุก notification และส่งต่อเฉพาะ event ที่เป็นชนิด `TEvent` ของมัน:

```csharp
using Athenex.Whimclean.Application.Email;
using Athenex.Whimclean.Mediator;

public sealed class OrderPlacedHandler(IEmailSender emailSender)
    : DomainEventHandler<OrderPlaced>
{
    protected override async ValueTask HandleAsync(
        OrderPlaced domainEvent,
        CancellationToken cancellationToken)
    {
        await emailSender.SendAsync(
            new EmailMessage(
                "member@example.com",
                "Order confirmed",
                $"Order {domainEvent.OrderId} has been placed."),
            cancellationToken);
    }
}
```

source generator ค้นพบ subclass ของ `DomainEventHandler<TEvent>` เหมือน `INotificationHandler` อื่น ๆ และลงทะเบียนให้ event ถูก dispatch หลังจาก commit สำเร็จ เหมือนกับ pipeline ในตัวทุกประการ (ดู [README ของ `Athenex.Whimclean.Infrastructure`](../Athenex.Whimclean.Infrastructure/README-TH.md)) event ที่ไม่มี handler ตรงกันจะไม่ทำอะไร (no-op): dispatcher กลืน `MissingMessageHandlerException` ของ mediator ไว้ ลำดับของ handler และการรวบรวม error เป็นไปตาม notification publisher ที่ตั้งค่าไว้ใน mediator; ด้วย publisher ตัว default handler จะทำงานตามลำดับ และความล้มเหลวแรกจะถูกส่งต่อออกมา

### ทำไม notification จึงไม่เป็น generic

source generator ของ Mediator ไม่รองรับ message แบบ generic และมันสแกนทุก assembly ที่ถูกอ้างอิง เวอร์ชันก่อน 1.2.0 มาพร้อม `DomainEventNotification<TEvent>` ซึ่งทำให้ `Mediator.g.cs` ที่ generate ออกมา compile ไม่ผ่าน (`CS0246: The type or namespace name 'TEvent' could not be found`) ในทุกโปรเจกต์ที่อ้างอิงแพ็กเกจนี้และรัน generator notification แบบไม่ใช่ generic บวกกับ base `DomainEventHandler<TEvent>` ที่ทำหน้าที่กรอง ทำให้ handler ยังมี type ชัดเจนโดยไม่ต้องเปิดเผย open generic message แต่ละ handler ถูกเรียกหนึ่งครั้งต่อ event ที่ dispatch และ return แบบ synchronous เมื่อชนิดไม่ตรง; ต้นทุนคือการตรวจสอบชนิดหนึ่งครั้ง

การย้ายจาก 1.1.0: แทนที่ `INotificationHandler<DomainEventNotification<TEvent>>` ด้วย `DomainEventHandler<TEvent>`, เปลี่ยนชื่อ `Handle` เป็น `HandleAsync`, ทำให้เป็น `protected override` และรับ `TEvent` โดยตรงแทน `notification.DomainEvent`

Repository และคู่มือสถาปัตยกรรมฉบับเต็ม: https://github.com/athene-x/Whimclean
