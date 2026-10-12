# Athenex.Whimclean.Identity.Keycloak

[English](README.md)

การเชื่อมต่อ Keycloak สำหรับ Whimclean API: JWT bearer authentication ตามแบบแผน claim ที่ token ของ Keycloak ใช้ และ admin API client สำหรับจัดเตรียมผู้ใช้ด้วย service account

## ติดตั้ง

```shell
dotnet add package Athenex.Whimclean.Identity.Keycloak
```

## ยืนยันตัวตนการเรียก API

```csharp
using Athenex.Whimclean.Api.AspNetCore;
using Athenex.Whimclean.Identity.Keycloak;

builder.Services.AddKeycloakAuthentication(builder.Configuration);
builder.Services.AddClaimsCurrentUser<Guid>(options => options.ClaimType = "sub");
```

```json
{
  "Keycloak": {
    "Authority": "https://auth.example.com/realms/app",
    "Audience": "app-api",
    "MetadataAddress": "http://keycloak:8080/realms/app/.well-known/openid-configuration",
    "RequireHttpsMetadata": true
  }
}
```

`AddKeycloakAuthentication(configuration)` bind `KeycloakOptions` จาก section `Keycloak` (ส่งชื่อ section อื่นได้) และ throw `InvalidOperationException` เมื่อ section หายไปหรือไม่ถูกต้อง overload อีกตัวรับ instance ของ `KeycloakOptions` `Authority` ต้องเป็น URL แบบ absolute และ `Audience` เป็นค่าบังคับ `MetadataAddress` เป็นค่าเลือกได้ ค่าเริ่มต้นคือ `{Authority}/.well-known/openid-configuration` ส่วน `RequireHttpsMetadata` มีค่าเริ่มต้นเป็น `true`

การลงทะเบียนปิด inbound claim mapping เพื่อให้ `sub` ยังคงเป็น `sub` ตรึง issuer ที่ถูกต้องไว้ที่ `Authority` แม้ discovery จะทำงานกับ `MetadataAddress` ภายใน ตรวจสอบ audience นำชื่อจาก `preferred_username` และ role จาก `roles` (ปรับได้ทั้งคู่ผ่าน `NameClaimType` และ `RoleClaimType`) และลงทะเบียน authorization service เมื่อ local user id อยู่ในตารางของคุณเอง ให้ใช้ `AddLocalUserResolution<Guid>` จาก `Athenex.Whimclean.Api.AspNetCore` แทน claims current user

## จัดเตรียมผู้ใช้

```csharp
builder.Services.AddKeycloakAdministration(builder.Configuration);
```

```json
{
  "KeycloakAdmin": {
    "BaseUrl": "http://keycloak:8080",
    "Realm": "app",
    "ClientId": "app-api",
    "ClientSecret": "<set-via-user-secrets>",
    "PasswordSetupClientId": "app-web",
    "PasswordSetupRedirectUrl": "https://app.example.com/"
  }
}
```

`AddKeycloakAdministration(configuration)` bind `KeycloakAdminOptions` จาก section `KeycloakAdmin` (ส่งชื่อ section อื่นได้) overload อีกตัวรับ `Action<KeycloakAdminOptions>` client ต้องมี service-account role `manage-users` และ `view-users` options ถูกตรวจสอบตอนเริ่มระบบ: `BaseUrl` ต้องเป็น URL แบบ absolute, `Realm`, `ClientId` และ `ClientSecret` เป็นค่าบังคับ และเมื่อกำหนด `PasswordSetupRedirectUrl` ค่านั้นต้องเป็น URL แบบ absolute และต้องกำหนด `PasswordSetupClientId` ด้วย จากนั้น `IKeycloakUserAdministration` มีให้ใช้:

| Method | ทำอะไร |
| --- | --- |
| `FindUserByEmailAsync` | ค้นหา email ที่ตรงกันทุกตัวอักษร คืน `Result<KeycloakUserLookup>` โดยเป็น `KeycloakUserLookup.NotFound` เมื่อไม่พบ |
| `CreateUserAsync` | สร้างผู้ใช้ที่เปิดใช้งานโดยใช้ email เป็นชื่อ พร้อม required action `VERIFY_EMAIL` และ `UPDATE_PASSWORD`; คืน `KeycloakUser` หรือ `UserAlreadyExists` เมื่อซ้ำ |
| `RequiresPasswordSetupAsync` | `UPDATE_PASSWORD` ยังค้างอยู่หรือไม่ |
| `SendPasswordSetupEmailAsync` | ส่ง actions email โดย redirect ไปยัง `PasswordSetupRedirectUrl` ต่อด้วย path ที่เลือกได้ เมื่อกำหนด URL นั้นไว้ |
| `UpdateUserNameAsync`, `SetUserEnabledAsync` | อ่าน แก้ไข แล้วเขียน user representation กลับ |
| `DeleteUserAsync` | สำเร็จแม้ผู้ใช้ถูกลบไปแล้ว |

`KeycloakUser` มี `Subject` (ค่า `sub` ใน token ของผู้ใช้), `Email`, `FirstName` และ `LastName` ผู้ใช้ที่ Keycloak ไม่มีชื่อให้จะได้ user name เป็น `FirstName`

ทุกการเรียกคืน `Result` พร้อมหนึ่งใน `KeycloakErrors` แทนการ throw: `Unavailable` (API ล้มเหลวหรือเข้าถึงไม่ได้), `UserAlreadyExists`, `UserNotFound` (ผู้ใช้ไม่มีอยู่แล้ว) และ `PasswordSetupEmailNotSent` response body จาก Keycloak ถูกบันทึกลง log เท่านั้น ไม่ส่งกลับให้ผู้เรียก token ของ service account ได้มาด้วย client credentials grant ถูก cache โดย `KeycloakAdminTokenProvider` แบบ singleton และต่ออายุก่อนหมดอายุสามสิบวินาที

Repository และคู่มือสถาปัตยกรรมฉบับเต็ม: https://github.com/athene-x/Whimclean
