# Athenex.Whimclean.Auditing.AspNetCore

[English](README.md)

จัดหาสิ่งที่ HTTP request ปัจจุบันรู้เกี่ยวกับผู้เรียกให้กับ `Athenex.Whimclean.Auditing`

## ติดตั้ง

```shell
dotnet add package Athenex.Whimclean.Auditing.AspNetCore
```

## ลงทะเบียน

```csharp
using Athenex.Whimclean.Auditing;
using Athenex.Whimclean.Auditing.AspNetCore;

services.AddAuditing<Guid, Guid>();
services.AddHttpAuditRequestContext();
```

`AddHttpAuditRequestContext` ลงทะเบียน HTTP context accessor และแทนที่การลงทะเบียน `IAuditRequestContext` จึงเรียกก่อนหรือหลัง `AddAuditing` ก็ได้ `HttpAuditRequestContext` แทนที่ null context ที่ `AddAuditing` ลงทะเบียนไว้ และอ่าน:

- ชื่อผู้กระทำ เมื่อ principal ผ่านการยืนยันตัวตนแล้ว จาก claim `name` ตามด้วย `email` แล้วจึงชื่อของ identity;
- correlation id จาก `X-Correlation-Id` (`HttpAuditRequestContext.CorrelationHeader`) ตามด้วย `X-Request-Id` (`HttpAuditRequestContext.RequestIdHeader`) แล้วจึง trace identifier ของ request;
- remote address และ header `User-Agent`

claim และ header ที่ว่างถือว่าไม่มี ทุกค่าเป็น null นอก request ดังนั้นงาน background ยังทำงานได้ตามปกติ

Repository และคู่มือสถาปัตยกรรมฉบับเต็ม: https://github.com/athene-x/Whimclean
