# Athenex.Whimclean.Domain

[English](README.md)

ส่วนประกอบพื้นฐานของ domain ขนาดเล็กที่ไม่มี dependency สำหรับแอปพลิเคชัน .NET 10

## การติดตั้ง

```shell
dotnet add package Athenex.Whimclean.Domain
```

## Aggregate root และ domain event

สืบทอด aggregate root จาก `AggregateRoot<TId>` และ raise event ภายใน behavior ของ domain:

```csharp
using Athenex.Whimclean.Domain.Entities;
using Athenex.Whimclean.Domain.Events;

public sealed record OrderSubmitted(Guid OrderId) : DomainEvent;

public sealed class Order : AggregateRoot<Guid>
{
    public Order(Guid id) : base(id)
    {
    }

    public void Submit()
    {
        RaiseDomainEvent(new OrderSubmitted(Id));
    }
}
```

`Entity<TId>` ปฏิเสธ id ที่เป็นค่า default ใน constructor และเปรียบเทียบ entity ด้วย runtime type กับ `Id`; entity สองตัวที่ id ยังเป็นค่า default จะไม่เท่ากันเสมอ `DomainEvent` เป็น abstract record ที่ให้ `EventId` แบบ version 7 กับทุก event

Infrastructure อ่าน `DomainEvents` และเรียก `ClearDomainEvents()` หลังจาก dispatch สำเร็จ `ClearDomainEvents()` ถูก implement แบบ explicit บน `IHasDomainEvents` จึงเข้าถึงได้ผ่าน interface นั้นเท่านั้น และไม่ปรากฏบน public surface ของ aggregate

## ผลลัพธ์ของการดำเนินการ

ใช้ `Result` หรือ `Result<TValue>` สำหรับความล้มเหลวที่คาดไว้ของ domain และ application:

```csharp
using Athenex.Whimclean.Domain.Results;

Result<Guid> CreateOrder(bool valid) => valid
    ? Result<Guid>.Success(Guid.NewGuid())
    : Result<Guid>.Failure(
        Error.Validation("Order.Invalid", "The order is invalid."));
```

factory ของ error ที่มีให้คือ `Error.Failure`, `Error.Validation`, `Error.NotFound`, `Error.Conflict`, `Error.Unauthorized` และ `Error.Forbidden`; แต่ละตัวกำหนด `ErrorType` ที่ตรงกัน ตรวจสอบ `IsSuccess` หรือ `IsFailure` ก่อนอ่าน `Result<TValue>.Value` ซึ่งจะ throw เมื่อผลลัพธ์ล้มเหลว ค่าและ error แปลงเป็น result ได้โดยนัย (implicit) และ `Match`, `Map` กับ `Bind` ใช้ต่อ result เป็นลูกโซ่ได้โดยไม่ต้องแกะค่าออก

## Auditing และ behavior

implement เฉพาะ contract ที่ entity ต้องใช้:

```csharp
using Athenex.Whimclean.Domain.Auditables;
using Athenex.Whimclean.Domain.Behaviors;

public sealed class OrderAudit : IAuditable<Guid?>, ISoftDelete<Guid?>
{
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
```

contract เหล่านี้เป็นแบบอ่านอย่างเดียว; EF infrastructure เป็นผู้ประทับค่าตอนบันทึก `IAuditable<TActor>` รวม `ICreateAuditable<TActor>` กับ `IUpdateAuditable<TActor>` เข้าด้วยกัน ซึ่ง implement แยกกันได้ ชนิดของ actor กำหนด nullability ของตัวเอง แอปพลิเคชันจึงเก็บเป็น `Guid?`, `string?` หรือค่า actor แบบกำหนดเองได้ `IActivatable` (flag `IsActive`) เป็น behavior ตัวที่สาม; แถวที่ไม่ active ยังมองเห็นได้ตามปกติ เว้นแต่ query จะเลือกใช้ `WhereActive()` จากแพ็กเกจ Application

แพ็กเกจนี้ไม่มี dependency กับ EF Core, ASP.NET Core, Identity, PostgreSQL หรือ multitenancy

## base class สำหรับ auditable

`AuditableEntity<TId, TActor>` และ `AuditableAggregateRoot<TId, TActor>` ประกาศ property `CreatedAt`, `CreatedBy`, `UpdatedAt` และ `UpdatedBy` ที่ EF infrastructure ประทับค่าให้ ชนิด entity จึงไม่ต้องประกาศซ้ำ:

```csharp
using Athenex.Whimclean.Domain.Entities;

public sealed class Tenant : AuditableAggregateRoot<Guid, Guid?>
{
    private Tenant(Guid id) : base(id) { }
}
```

## การ normalize ข้อความ

`TextNormalizer` ใน `Athenex.Whimclean.Domain.Text` เป็น static helper `NormalizeWhitespace` ตัดช่องว่างหัวท้ายและยุบช่องว่างที่ติดกันให้เหลือช่องว่างเดียว; `NormalizeSlug` ทำเพิ่มโดยแปลงเป็นตัวพิมพ์เล็ก และเปลี่ยนช่องว่าง ขีดกลาง และขีดล่างให้เป็นขีดกลางเดียว สำหรับค่าที่จะไปอยู่ใน URL หรือ DNS label

Repository และคู่มือสถาปัตยกรรมฉบับเต็ม: https://github.com/athene-x/Whimclean
