# Athenex.Whimclean.Application

[English](README.md)

contract ของชั้น application และโมเดลคำขอที่ผ่านการตรวจสอบแล้ว สำหรับแอปพลิเคชัน clean architecture บน .NET 10

## การติดตั้ง

```shell
dotnet add package Athenex.Whimclean.Application
```

แพ็กเกจนี้ขึ้นกับ `Athenex.Whimclean.Domain` เท่านั้น

## ผู้ใช้ปัจจุบัน

service ของ application และ infrastructure พึ่งพา `ICurrentUser<TUserId>` ได้โดยไม่ต้องอ้างอิง ASP.NET Core Identity:

```csharp
using Athenex.Whimclean.Application.Context;

public sealed class CreateOrderHandler(ICurrentUser<Guid> currentUser)
{
    public void Handle()
    {
        // Throws CurrentUserRequiredException when no principal is available.
        var userId = currentUser.GetRequiredUserId();
    }
}
```

interface นี้มีสมาชิกเดียวคือ `TryGetUserId(out TUserId userId)`; `GetRequiredUserId()` เป็น extension method ใน `CurrentUserExtensions` API หรือ host เป็นผู้ implement interface นี้จาก claim, identity ของ system user หรือบริบทการทำงานอื่นที่เชื่อถือได้ `StaticCurrentUser<TUserId>` เป็น implementation สำเร็จรูปที่ตอบ user id คงที่ค่าเดียวเสมอ สำหรับ background job และการทดสอบ

## Unit of work

ใช้ `IUnitOfWork` ที่ขอบเขตการเขียนของ application:

```csharp
await unitOfWork.CommitAsync(cancellationToken);
```

แพ็กเกจ EF Core infrastructure ลงทะเบียน business context ของตนเป็น contract นี้ เมื่อฐานข้อมูลรายงานความขัดแย้งแบบ concurrency แพ็กเกจนั้นจะ throw `PersistenceConcurrencyException` (ใน `Athenex.Whimclean.Application.Persistence`) โค้ด application จึงไม่ต้องพึ่งพาชนิด exception ของ EF Core

## list query ที่ผ่านการตรวจสอบ

`ListQueryOptions` normalize ข้อความค้นหา, materialize คอลเลกชันของ sort/filter และบังคับขอบเขตของการแบ่งหน้า:

```csharp
using Athenex.Whimclean.Application.Queries;

var options = new ListQueryOptions(
    searchTerm: "pending",
    sort: [new SortTerm("createdAt", SortDirection.Descending)],
    filters: [new FilterTerm("status", "Pending")],
    page: 1,
    size: 20);
```

ขีดจำกัดเป็นค่าคงที่บน `ListQueryOptions`: page มีค่า default 1 และ size มีค่า default 10, size ต้องไม่เกิน 100, ข้อความค้นหาถูกตัดช่องว่างหัวท้ายและจำกัดที่ 200 ตัวอักษร และหนึ่ง query มี sort term ได้ไม่เกิน 5 รายการและ filter ไม่เกิน 10 รายการ `Offset` ถูกคำนวณไว้ล่วงหน้าจาก page และ size

infrastructure ควรแปลงเฉพาะชื่อฟิลด์และ operator ที่ application อนุมัติแล้วให้เป็น expression ของฐานข้อมูล ห้ามรันชื่อฟิลด์ที่ client ส่งมาตามอำเภอใจแบบ dynamic

`FacetQueryOptions` ให้ input ที่มีขอบเขตแบบเดียวกันสำหรับ query ค่าที่ไม่ซ้ำหรือ facet; `limit` ของมันจำกัดจำนวนค่าที่ facet หนึ่งคืนได้ (default 100 สูงสุด 1000) `FilterTerm` ยังรองรับการเปรียบเทียบ `NotEqual`, `Contains`, `StartsWith`, `EndsWith` และ `In` แบบหลายค่า (สูงสุด 25 ค่า); ค่าของ filter ถูกส่งผ่านตามเดิมทุกตัวอักษร ข้อมูลที่ช่องว่างมีความหมายจึงยังจับคู่ได้

## ผลลัพธ์การแบ่งหน้า

ใช้ `OffsetPagedResult<T>` สำหรับหน้าแบบมีหมายเลข และ `KeysetPagedResult<T, TKey>` สำหรับการเลื่อนหน้าแบบ cursor

## ขอบเขตของอีเมล

โค้ด application สร้าง `EmailMessage` และพึ่งพา `IEmailSender`:

```csharp
await emailSender.SendAsync(
    new EmailMessage(
        "member@example.com",
        "Welcome",
        "Your account is ready."),
    cancellationToken);
```

constructor ฉบับเต็มรับผู้รับหลายคน, รายการ CC/BCC, ค่า `from`/`replyTo` ที่กำหนดเอง และไฟล์ `EmailAttachment`:

```csharp
var invoice = new EmailMessage(
    recipients: ["owner@example.com", "billing@example.com"],
    subject: "Invoice 2026-08",
    textBody: "Your invoice is attached.",
    bcc: ["audit@example.com"],
    replyTo: "support@example.com",
    attachments: [new EmailAttachment("invoice.pdf", pdfBytes, "application/pdf")]);
```

ข้อความต้องมีผู้รับอย่างน้อยหนึ่งคน และมี `textBody` หรือ `htmlBody` อย่างใดอย่างหนึ่ง; ที่อยู่อีเมลและหัวเรื่องถูกตัดช่องว่างหัวท้าย `EmailAttachment` รับชื่อไฟล์, เนื้อหาเป็น `ReadOnlyMemory<byte>` และ content type ที่มีค่า default เป็น `application/octet-stream`

โปรเจกต์ infrastructure ที่นำไปใช้เป็นผู้รับผิดชอบการส่งผ่าน SMTP หรือผู้ให้บริการเฉพาะราย

แพ็กเกจนี้ไม่ขึ้นกับ EF Core, ASP.NET Core Identity, ผู้ให้บริการฐานข้อมูล หรือ multitenancy

## แปลง list query จาก query string

`ListQueryParser` แปลงค่าดิบจาก query string ของ list endpoint ให้เป็น `ListQueryOptions` ทุก API จึงใช้ syntax เดียวกัน:

| พารามิเตอร์ | รูปแบบ | ตัวอย่าง |
| --- | --- | --- |
| `searchTerm` | ข้อความอิสระ | `searchTerm=acme` |
| `sort` | ชื่อฟิลด์คั่นด้วยจุลภาค ใส่ `-` นำหน้าเพื่อเรียงจากมากไปน้อย | `sort=name,-createdAt` |
| `filters` | `field:operator:value` หนึ่งรายการต่อพารามิเตอร์ที่ส่งซ้ำ | `filters=status:in:Active,Suspended` |
| `page`, `size` | หมายเลขหน้าเริ่มที่หนึ่งและขนาดหน้า | `page=2&size=20` |

operator ได้แก่ `eq`, `ne`, `gt`, `ge`, `lt`, `le`, `cn` (contains), `sw`, `ew` และ `in` (ค่าคั่นด้วยจุลภาค); รหัส operator ไม่สนใจตัวพิมพ์เล็กใหญ่ `Parse`, `ParseSort` และ `ParseFilters` จะ throw `FormatException` เมื่อ input ไม่ถูกต้อง ส่วน `TryParseFilter` และ `TryParseSort` รายงานข้อผิดพลาดที่อ่านเข้าใจได้โดยไม่ throw ซึ่งแพ็กเกจ API ใช้สำหรับการตรวจสอบ

```csharp
var options = ListQueryParser.Parse(searchTerm, sort, filters, page, size);
```

`EnumFilter` (ใน `Athenex.Whimclean.Application.Queries`) แปลงค่าของ filter เป็นสมาชิกของ enum โดยไม่สนใจตัวพิมพ์เล็กใหญ่ และปฏิเสธค่าที่ไม่ได้นิยามไว้: `TryParse<TEnum>` จัดการค่าเดียว และ `Parse<TEnum>` เก็บเฉพาะสมาชิกที่ถูกต้องจากรายการ validator ของ API (`AllowEnumFilterField<TEnum>` ใน `Athenex.Whimclean.Api.AspNetCore`) และ filter ฝั่ง persistence จึงเห็นตรงกันว่าชื่อใดถูกต้อง

`WhereActive()` ใน `ActivatableQueryExtensions` กรอง `IQueryable<T>` หรือ `IEnumerable<T>` ของ entity ที่เป็น `IActivatable` ให้เหลือเฉพาะรายการที่ active

extension `Map` ใน `OffsetPagedResultExtensions` แปลงรายการในหน้าโดยคงหมายเลขหน้า ขนาด และยอดรวมไว้ สำหรับเปลี่ยน projection ฝั่ง persistence ให้เป็นโมเดลตอบกลับ:

```csharp
using Athenex.Whimclean.Application.Pagination;

OffsetPagedResult<OrderResponse> response = page.Map(order => new OrderResponse(order.Id));
```

## กำหนดผู้ใช้ปัจจุบันนอก request

`ICurrentUserInitializer<TUserId>` ให้โค้ด bootstrap ตอนเริ่มระบบและ seeder ระบุผู้ใช้ที่การเขียนของตนจะถูกบันทึกเป็นผู้กระทำผ่าน `SetUserId` แพ็กเกจ ASP.NET Core implement มันพร้อมกับ `ICurrentUser<TUserId>` แบบ request-scoped (`RequestCurrentUser<TUserId>` ซึ่งลงทะเบียนโดย `AddLocalUserResolution<TUserId>`)

Repository และคู่มือสถาปัตยกรรมฉบับเต็ม: https://github.com/athene-x/Whimclean
