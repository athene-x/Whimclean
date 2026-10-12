# Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL

[English](README.md)

adapter สำหรับ provider PostgreSQL/Npgsql ของโครงสร้างพื้นฐาน EF Core ใน Whimclean

## การติดตั้ง

```shell
dotnet add package Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL
```

package นี้จัดหา `Npgsql.EntityFrameworkCore.PostgreSQL` ให้ และขึ้นกับ package EF Core แบบไม่ผูกกับ provider

## ลงทะเบียน business context

ใช้ `AddPostgreSqlUnitOfWork` เมื่อ context implement `IUnitOfWork` เช่น context ที่สืบทอดจาก `AuditableDbContext` หรือ `MultiTenantDbContext`:

```csharp
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL;

services.AddPostgreSqlUnitOfWork<ApplicationDbContext>(
    connectionString,
    postgreSql => postgreSql.MigrationsAssembly(
        "MyApp.Migrations.PostgreSQL"));
```

คำสั่งนี้เรียก `UseNpgsql` ด้วย connection string และลงทะเบียนทั้ง `ApplicationDbContext` และ `IUnitOfWork` แบบ scoped อาร์กิวเมนต์ตัวที่สองเป็น `Action<NpgsqlDbContextOptionsBuilder>` ที่ระบุหรือไม่ก็ได้ connection string ที่เป็น null หรือช่องว่างจะ throw `ArgumentException`

## ลงทะเบียน EF context ทั่วไป

ใช้ `AddPostgreSqlContext` สำหรับ context ที่ framework เป็นเจ้าของและต้องไม่กลายเป็น unit of work ของแอปพลิเคชัน เช่น context สำหรับรายงานหรือการเชื่อมต่อระบบภายนอกที่แยกออกมา:

```csharp
services.AddPostgreSqlContext<ReportingDbContext>(
    reportingConnectionString,
    postgreSql => postgreSql.MigrationsAssembly(
        "MyApp.Reporting.Migrations.PostgreSQL"));
```

ลงทะเบียนเฉพาะ DbContext เท่านั้น ไม่มีอย่างอื่น

## การแยก context

หลาย context อาจใช้ฐานข้อมูล PostgreSQL เดียวกันได้ แต่ต้องแยกประวัติ migration และคำสั่ง migration ให้ระบุได้ชัดเจนตาม context Whimclean ไม่รัน migration เองโดยอัตโนมัติ package EF Core จะนำ migration ไปใช้เฉพาะเมื่อแอปพลิเคชันถูกเริ่มด้วย switch `--migrate` ซึ่งจัดการโดย `DatabaseMigration`

multitenancy ไม่ผูกกับ provider context ที่รองรับ tenant ยังคงใช้ `AddPostgreSqlUnitOfWork` ให้ติดตั้ง package EF multitenancy แยกต่างหากเพื่อสืบทอด context นั้นจาก `MultiTenantDbContext`

package นี้ไม่มีโมเดล tenant, การจัดการสมาชิก หรือพฤติกรรมของ API

## การค้นหาแบบไม่สนใจตัวพิมพ์

`SearchILike` จำกัด query ให้เหลือแถวที่สมาชิกชนิด string ตัวใดตัวหนึ่งที่ระบุมีคำค้นอยู่ โดยใช้ `EF.Functions.ILike` และ escape `%`, `_` และ `\` ผ่าน `LikePattern` คำค้นที่เป็น null จะไม่เปลี่ยน query pattern ถูกส่งเป็น query parameter และรายการฟิลด์ที่ว่างจะ throw `ArgumentException`:

```csharp
using Athenex.Whimclean.Infrastructure.EntityFrameworkCore.PostgreSQL.Queries;

query = query.SearchILike(options.SearchTerm, user => user.Email, user => user.DisplayName);
```

Repository และคู่มือสถาปัตยกรรมฉบับเต็ม: https://github.com/athene-x/Whimclean
