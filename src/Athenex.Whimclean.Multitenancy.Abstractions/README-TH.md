# Athenex.Whimclean.Multitenancy.Abstractions

[English](README.md)

contract แบบไม่มี dependency สำหรับ multitenancy ที่เลือกใช้ได้ แอปพลิเคชันฝั่งผู้ใช้เป็นเจ้าของการจัดการ tenant package นี้กำหนดเฉพาะขอบเขตการเชื่อมต่อเท่านั้น type ทั้งหมดอยู่ใน namespace `Athenex.Whimclean.Multitenancy`

## การติดตั้ง

```shell
dotnet add package Athenex.Whimclean.Multitenancy.Abstractions
```

แอปพลิเคชันส่วนใหญ่ติดตั้ง adapter multitenancy สำหรับ EF หรือ ASP.NET Core แทน และได้รับ package นี้มาโดยอ้อม

## ทำเครื่องหมาย entity ที่ tenant เป็นเจ้าของ

```csharp
using Athenex.Whimclean.Multitenancy;

public sealed class Order : IMultiTenant<Guid>
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
}
```

`IMultiTenant<TTenantId>` ระบุความเป็นเจ้าของข้อมูลผ่าน `TenantId` แบบอ่านอย่างเดียว adapter EF ที่เลือกใช้ได้นำไปใช้สำหรับ query filter และการตรวจสอบการเขียน

## tenant ที่ร้องขอ กับ tenant ปัจจุบัน

- `IRequestedTenant<TTenantId>` แทน tenant ที่ยังไม่เชื่อถือ ซึ่งถูกเลือกจาก header, route, host หรือข้อมูลนำเข้าของ request ในลักษณะเดียวกัน
- `ICurrentTenant<TTenantId>` แทน tenant ที่ถูกกำหนดขึ้นหลังผ่านการอนุญาตแล้วเท่านั้น

ทั้งสองเปิดเผย `TryGetTenantId(out TTenantId tenantId)` ซึ่งคืนค่า `false` เมื่อไม่มี tenant การแยก contract ทั้งสองนี้ป้องกันไม่ให้ข้อมูลนำเข้าของ request กลายเป็นขอบเขตข้อมูลที่ได้รับอนุญาตโดยอัตโนมัติ

`AmbientCurrentTenant<TTenantId>` คือ `ICurrentTenant<TTenantId>` ที่ทำงานบน scope แบบ async-local สำหรับ host ที่ไม่มีการ resolve tenant ผ่าน HTTP เช่น background worker หรือ seeder ให้ห่องานที่ผูกกับ tenant ด้วย `using (AmbientCurrentTenant<Guid>.Begin(tenantId))` scope ซ้อนกันได้ การ dispose จะคืนค่า tenant ก่อนหน้า และผู้เรียกเป็นผู้รับรอง tenant เองเพราะไม่มีการตรวจสอบสิทธิ์เข้าถึงในจุดนี้

## การตรวจสอบสิทธิ์เข้าถึงที่ผู้ใช้เป็นเจ้าของ

implement `ITenantAccessValidator<TUserId, TTenantId>` ให้ทำงานกับที่เก็บข้อมูลสมาชิกภาพและกฎของแอปพลิเคชันเอง:

```csharp
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

implementation ควรเป็นแบบ fail closed และคืนค่า `false` สำหรับผู้ใช้หรือ tenant ที่ไม่รู้จัก ผู้ใช้ package เป็นเจ้าของ:

- entity `Tenant` และ `TenantMembership`
- การสร้าง ระงับ และลบ tenant
- การเชิญสมาชิก บทบาท สถานะ และวันหมดอายุของสมาชิกภาพ
- กฎการสมัครสมาชิกหรือสิทธิ์การใช้งาน
- การอนุญาตข้าม tenant สำหรับผู้ดูแลระบบ

package นี้ยังกำหนด `CurrentTenantRequiredException` (กำลังจะบันทึกการเปลี่ยนแปลงที่ tenant เป็นเจ้าของโดยไม่มี tenant ปัจจุบันที่ถูกกำหนดไว้) และ `TenantAccessViolationException` (การบันทึกจะเปลี่ยน record ที่ tenant อื่นเป็นเจ้าของ) สำหรับขอบเขต persistence แบบ fail closed

package นี้ไม่มี dependency ต่อ Domain, Application, EF Core, ASP.NET Core, Identity หรือ provider ฐานข้อมูล

Repository และคู่มือสถาปัตยกรรมฉบับเต็ม: https://github.com/athene-x/Whimclean
