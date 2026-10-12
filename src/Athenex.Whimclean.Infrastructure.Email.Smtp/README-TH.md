# Athenex.Whimclean.Infrastructure.Email.Smtp

[English](README.md)

การ implement `IEmailSender` จาก `Athenex.Whimclean.Application` แบบ SMTP ผ่าน `System.Net.Mail`

## ติดตั้ง

```shell
dotnet add package Athenex.Whimclean.Infrastructure.Email.Smtp
```

## ลงทะเบียน

```csharp
using Athenex.Whimclean.Infrastructure.Email.Smtp;

services.AddSmtpEmailSender(options =>
{
    options.Host = "smtp.example.com";
    options.Port = 587;
    options.From = "noreply@example.com";
    options.FromName = "Example";
    options.User = "smtp-user";
    options.Password = configuration["Email:Password"];
});
```

ใน ASP.NET Core host ให้ bind และตรวจสอบ options จาก section `Email` แทน แล้วเรียกการลงทะเบียนโดยไม่ส่ง delegate:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Configuration;
using Athenex.Whimclean.Infrastructure.Email.Smtp;

services.AddValidatedOptions<SmtpEmailOptions>(configuration, SmtpEmailOptions.SectionName, options => options.Validate());
services.AddSmtpEmailSender();
```

`SmtpEmailOptions` มี `Host`, `Port` (ค่าเริ่มต้น 587), `From`, `FromName`, `User`, `Password`, `EnableSsl` (ค่าเริ่มต้น `true`) และ `TimeoutSeconds` (ค่าเริ่มต้น 30) เมื่อใช้ delegate options จะถูกตรวจสอบตอนใช้งานครั้งแรก เมื่อใช้ `AddValidatedOptions` จะถูกตรวจสอบตอนเริ่มระบบ `Validate()` ล้มเหลวพร้อมข้อความระบุชื่อ setting เมื่อ `Host` หรือ `From` หายไป `Port` ไม่ใช่ TCP port ที่ถูกต้อง หรือ `TimeoutSeconds` ไม่เป็นค่าบวก

## พฤติกรรม

- ข้อความที่มี body ทั้งสองแบบถูกส่งเป็น plain text พร้อม HTML alternate view ข้อความที่มี body เดียวถูกส่งตามที่เป็น
- `From` บนข้อความมีผลเหนือผู้ส่งที่กำหนดไว้ `ReplyTo`, CC, BCC และไฟล์แนบ map ตรงตัว โดยไฟล์แนบคง content type ที่ประกาศไว้
- `EnableSsl` เจรจา STARTTLS และเปิดอยู่โดยค่าเริ่มต้น credential ถูกส่งเฉพาะเมื่อกำหนด `User` `SmtpClient` ใหม่พร้อม timeout ที่กำหนดถูกสร้างต่อการส่งหนึ่งครั้ง
- sender ถูกลงทะเบียนด้วย `TryAddScoped` ดังนั้น `IEmailSender` ที่แอปพลิเคชันจัดหาเองจะมีผลแทน

Repository และคู่มือสถาปัตยกรรมฉบับเต็ม: https://github.com/athene-x/Whimclean
