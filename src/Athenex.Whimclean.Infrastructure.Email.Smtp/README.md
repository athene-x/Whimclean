# Athenex.Whimclean.Infrastructure.Email.Smtp

[ภาษาไทย](README-TH.md)

SMTP implementation of `IEmailSender` from `Athenex.Whimclean.Application`, over `System.Net.Mail`.

## Install

```shell
dotnet add package Athenex.Whimclean.Infrastructure.Email.Smtp
```

## Register

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

In an ASP.NET Core host, bind and validate the options from the `Email` section instead and call the registration without a delegate:

```csharp
using Athenex.Whimclean.Api.AspNetCore.Configuration;
using Athenex.Whimclean.Infrastructure.Email.Smtp;

services.AddValidatedOptions<SmtpEmailOptions>(configuration, SmtpEmailOptions.SectionName, options => options.Validate());
services.AddSmtpEmailSender();
```

`SmtpEmailOptions` holds `Host`, `Port` (default 587), `From`, `FromName`, `User`, `Password`, `EnableSsl` (default `true`) and `TimeoutSeconds` (default 30). With the delegate the options are validated when first used; with `AddValidatedOptions` they are validated at startup. `Validate()` fails with a message naming the setting when `Host` or `From` is missing, `Port` is not a valid TCP port, or `TimeoutSeconds` is not positive.

## Behavior

- A message with both bodies is sent as plain text with an HTML alternate view; a single body is sent as is.
- `From` on the message overrides the configured sender; `ReplyTo`, CC, BCC and attachments map directly, attachments keeping their declared content type.
- `EnableSsl` negotiates STARTTLS and is on by default; credentials are sent only when `User` is set. A new `SmtpClient` with the configured timeout is created per send.
- The sender is registered with `TryAddScoped`, so an application-supplied `IEmailSender` wins.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
