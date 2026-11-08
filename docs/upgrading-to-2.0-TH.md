# การอัปเกรดเป็น 2.0.0

[English](upgrading-to-2.0.md)

เวอร์ชัน 2.0.0 เป็น release ที่มี breaking change โดยมีเป้าหมายเดียว: package `Domain` ต้องไม่มีความกังวลด้าน persistence audit stamp และ soft delete ย้ายจาก interface ของ domain ไปเป็น shadow property ของ EF Core และ parser ของ query string ย้ายไป package API เมธอดลงทะเบียนทุกตัวจาก 1.3.0 ยังใช้ได้เหมือนเดิม สิ่งที่เปลี่ยนอยู่ที่โมเดล domain, การตั้งค่าโมเดล EF และ namespace หนึ่งตัว

## สารบัญ

- [สรุป breaking change](#สรุป-breaking-change)
- [โมเดล domain](#โมเดล-domain)
- [การตั้งค่าโมเดล EF Core](#การตั้งค่าโมเดล-ef-core)
- [Soft delete](#soft-delete)
- [การอ่าน stamp ใน query](#การอ่าน-stamp-ใน-query)
- [Migration](#migration)
- [List query parser](#list-query-parser)
- [Test](#test)
- [Checklist](#checklist)

## สรุป breaking change

| 1.3 | 2.0 |
| --- | --- |
| `AuditableAggregateRoot<TId, TActor>`, `AuditableEntity<TId, TActor>` | ถูกลบ ให้สืบทอด `AggregateRoot<TId>` / `Entity<TId>` และเรียก `HasAuditStamps()` ในการตั้งค่า EF |
| `IAuditable<TActor>`, `ICreateAuditable<TActor>`, `IUpdateAuditable<TActor>` | ถูกลบ การประทับค่าขับเคลื่อนด้วย annotation ของโมเดล ไม่ใช่ interface |
| `ISoftDelete<TActor>` ที่มี `IsDeleted`, `DeletedAt`, `DeletedBy` บน entity | ถูกลบ ให้เรียก `HasSoftDelete()` property เหล่านี้กลายเป็น shadow property |
| entity สลับ `IsDeleted` เอง; `Remove` throw สำหรับ type ที่ soft delete ได้ | `Remove` เป็น soft delete; `HardDelete` ลบแถวจริง; `Restore` กู้คืน |
| `Athenex.Whimclean.Application.Queries.ListQueryParser` | `Athenex.Whimclean.Api.AspNetCore.Queries.ListQueryParser` (API เดิม) |
| `IActivatable` | ไม่เปลี่ยน |

ส่วนอื่นใน `Domain`, `Application`, `Mediator`, `Auditing`, `Multitenancy.Abstractions` และ package API ไม่เปลี่ยนรูปร่าง

## โมเดล domain

แทนที่ base class แบบ auditable และลบสมาชิกด้าน audit กับ soft delete ออกจากทุก entity

```csharp
// 1.3
public sealed class Customer : AuditableAggregateRoot<Guid, Guid?>, ISoftDelete<Guid?>
{
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public void Delete() => IsDeleted = true;
}

// 2.0
public sealed class Customer : AggregateRoot<Guid>
{
}
```

ลบเมธอดของ domain ที่มีหน้าที่เพียงสลับ `IsDeleted` (`Delete()`, `Restore()`) เพราะชั้น persistence เป็นเจ้าของสถานะนี้แล้ว คงเมธอดที่แทนการเปลี่ยนสถานะทางธุรกิจไว้ (เช่น `Archive()` ที่ตั้งค่า `Status`) เพราะการ archive เป็นการตัดสินใจของ domain ส่วน soft delete เป็นการตัดสินใจของการจัดเก็บ

หากกฎธุรกิจต้องใช้เวลาที่สร้างจริง (เช่น "ยกเลิกออเดอร์ไม่ได้หลังสั่งไปแล้ว 24 ชั่วโมง") ให้คง property ของ domain เช่น `PlacedAt` ที่ aggregate ตั้งค่าเองจาก `now` ที่ handler ส่งเข้ามา อย่านำ `CreatedAt` กลับมาใช้เพื่อการนี้ เพราะ stamp ถูกเขียนโดย infrastructure และไม่ใช่ส่วนหนึ่งของโมเดล

test ของ domain ที่ assert `CreatedAt`, `CreatedBy` หรือ `IsDeleted` บน aggregate ให้ลบทิ้ง พฤติกรรมเหล่านี้ทดสอบที่ชั้น EF

## การตั้งค่าโมเดล EF Core

เลือกใช้ (opt in) ทีละ entity type จาก `ConfigureModel` type ที่ไม่ได้ตั้งค่าจะไม่มีอะไรเกิดขึ้น

```csharp
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.Persistence;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(customer => customer.Id);

        builder.HasAuditStamps();   // CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
        builder.HasSoftDelete();    // IsDeleted, DeletedAt, DeletedBy + SoftDeleteFilter
    }
}
```

context เพิ่ม property หลัง `ConfigureModel` ทำงาน โดยใช้ `TActor` ของตัวเอง

| Property | ชนิด | เพิ่มโดย |
| --- | --- | --- |
| `CreatedAt` | `DateTimeOffset` | `HasAuditStamps()` |
| `CreatedBy` | `TActor`; เป็น required เมื่อ `TActor` เป็น value type ที่ไม่ nullable เช่น `Guid` นอกนั้นเป็น nullable (รวมถึง reference type ทุกชนิดเช่น `string` เพราะ nullable reference annotation มองไม่เห็นตอน runtime) | `HasAuditStamps()` |
| `UpdatedAt` | `DateTimeOffset?` | `HasAuditStamps()` |
| `UpdatedBy` | `TActor` แบบ nullable | `HasAuditStamps()` |
| `IsDeleted` | `bool` | `HasSoftDelete()` |
| `DeletedAt` | `DateTimeOffset?` | `HasSoftDelete()` |
| `DeletedBy` | `TActor` แบบ nullable | `HasSoftDelete()` |

ชื่อเป็นค่าคงที่บน `AuditStampNames` และ `SoftDeleteNames` หากต้องการเปลี่ยนชื่อคอลัมน์ ให้ประกาศ property ก่อน แล้ว context จะใช้ซ้ำ

```csharp
builder.HasAuditStamps();
builder.Property<DateTimeOffset>(AuditStampNames.CreatedAt).HasColumnName("created_at");
```

หาก entity ยังประกาศ CLR property ที่ชื่อและชนิดตรงกับ stamp (เช่น `CreatedAt` ที่ตัดสินใจคงไว้) context จะประทับค่าลง property นั้นแทนการเพิ่ม shadow คอลัมน์เดิมจึงยังใช้ได้

## Soft delete

| การกระทำ | 1.3 | 2.0 |
| --- | --- | --- |
| soft delete | `entity.Delete()` แล้วบันทึก | `context.Remove(entity)` (หรือ `repository.Remove`) แล้วบันทึก |
| hard delete | ทำไม่ได้กับ type ที่เป็น `ISoftDelete` | `context.HardDelete(entity)` แล้วบันทึก |
| กู้คืน | `entity.Restore()` แล้วบันทึก | `context.Restore(entity)` แล้วบันทึก (โหลดด้วย `IncludeSoftDeleted()` ก่อน) |
| อ่าน flag | `entity.IsDeleted` | `context.IsSoftDeleted(entity)` หรือ `EF.Property<bool>(e, SoftDeleteNames.IsDeleted)` |

soft delete ผ่าน `Remove` จะประทับ `UpdatedAt`/`UpdatedBy` ด้วยเมื่อ type มี audit stamp เพราะแถวถูกอัปเดต named filter `SoftDeleteFilter`, `IncludeSoftDeleted()` และกฎที่ว่า `IgnoreQueryFilters()` เปล่า ๆ ปิด filter ของ tenant ด้วย ไม่เปลี่ยนแปลง

repository ที่มี `RemoveAsync(aggregate)` ไม่ต้องเปลี่ยน signature เนื้อในยังเป็น `dbContext.Remove(aggregate)`

การ import ที่ต้องเพิ่มแถวในสถานะลบแล้ว ให้ตั้งค่า shadow หลัง `Add`

```csharp
context.Customers.Add(customer);
context.Entry(customer).Property(SoftDeleteNames.IsDeleted).CurrentValue = true;
```

## การอ่าน stamp ใน query

projection ที่อ่าน `customer.CreatedAt` เปลี่ยนเป็นอ่าน shadow property

```csharp
// 1.3
.Select(customer => new CustomerResponse(customer.Id, customer.Name, customer.CreatedAt))
.OrderByDescending(customer => customer.CreatedAt)

// 2.0
.Select(customer => new CustomerResponse(
    customer.Id,
    customer.Name,
    EF.Property<DateTimeOffset>(customer, AuditStampNames.CreatedAt)))
.OrderByDescending(customer => EF.Property<DateTimeOffset>(customer, AuditStampNames.CreatedAt))
```

รายการใน `SortFields<T>` เป็น expression ดังนั้น `createdAt` ที่เรียงได้กลายเป็น

```csharp
{ "createdAt", customer => EF.Property<DateTimeOffset>(customer, AuditStampNames.CreatedAt) }
```

EF Core แปล `EF.Property` ใน `Select`, `Where`, `OrderBy` และ `SortFields` ได้เหมือนกัน SQL ที่สร้างไม่เปลี่ยน

## Migration

ชื่อและชนิดคอลัมน์ไม่เปลี่ยนสำหรับ entity ที่มี property ของ 1.3 อยู่แล้ว `dotnet ef migrations add` หลังอัปเกรดจึงควรได้ migration ว่างสำหรับ entity เหล่านั้น ตรวจ migration ที่สร้างก่อน commit

- migration ที่ไม่ว่างมักหมายความว่ามี property ถูกเปลี่ยนชื่อหรือ nullability เปลี่ยน ให้เทียบกับตารางข้างต้น
- entity ที่เพิ่ง opt in `HasSoftDelete()` ได้คอลัมน์เพิ่มสามคอลัมน์ `IsDeleted` เป็น `NOT NULL` แถวเดิมต้องมีค่า default เป็น `false` (เพิ่ม `.HasDefaultValue(false)` บน property ใน `ConfigureModel` หรือตั้งค่าใน migration)

## List query parser

เปลี่ยน namespace ที่ import

```csharp
// 1.3
using Athenex.Whimclean.Application.Queries;   // ListQueryParser

// 2.0
using Athenex.Whimclean.Api.AspNetCore.Queries; // ListQueryParser
using Athenex.Whimclean.Application.Queries;    // ListQueryOptions, FilterTerm, SortTerm, EnumFilter
```

`ListQueryRequest.ToListQueryOptions()` และ `ListQueryRequestValidator<TRequest>` ไม่ได้รับผลกระทบ โค้ดชั้น application ที่เคย parse query string เอง ให้ย้ายการเรียกนั้นไปโปรเจกต์ API package Application ไม่อ้างถึงไวยากรณ์ของ query string อีกต่อไป

## Test

- test ของ domain: ลบ assertion เกี่ยวกับสมาชิกด้าน audit และ soft delete
- test ของ application ที่ใช้ fake ในหน่วยความจำ: fake ที่เคยติดตาม `IsDeleted` บน aggregate ตัดสถานะนั้นออก `Remove` ของ fake repository แค่เอารายการออกจาก list
- test ของ EF: อ่าน stamp ผ่าน `context.Entry(entity).Property(AuditStampNames.CreatedAt).CurrentValue`, soft delete ผ่าน `context.Remove` + `IsSoftDeleted` และกู้คืนผ่าน `context.Restore`

## Checklist

1. อัปเดตการอ้างอิง package `Athenex.Whimclean.*` ทุกตัวเป็น `2.0.0`
2. แทนที่ `AuditableAggregateRoot`/`AuditableEntity` ด้วย `AggregateRoot`/`Entity` ลบ `IAuditable`, `ISoftDelete` และสมาชิกของมันออกจาก entity
3. เพิ่ม `HasAuditStamps()` / `HasSoftDelete()` ใน `IEntityTypeConfiguration` ที่เกี่ยวข้อง
4. แทนที่จุดเรียก `entity.Delete()`/`entity.Restore()` ด้วย `Remove`/`Restore` บน context หรือ repository
5. แทนที่การอ่าน `.CreatedAt`, `.UpdatedAt`, `.IsDeleted` ใน query ด้วย `EF.Property<T>(…)`
6. เปลี่ยน import ของ `ListQueryParser` เป็น `Athenex.Whimclean.Api.AspNetCore.Queries`
7. build, สร้าง migration เพื่อตรวจ (คาดว่าไม่มีคอลัมน์เปลี่ยน) และรัน test ทุกชุด
