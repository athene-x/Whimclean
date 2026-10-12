# Athenex.Whimclean.Auditing.AspNetCore

[ภาษาไทย](README-TH.md)

Supplies `Athenex.Whimclean.Auditing` with what the current HTTP request knows about the caller.

## Install

```shell
dotnet add package Athenex.Whimclean.Auditing.AspNetCore
```

## Register

```csharp
using Athenex.Whimclean.Auditing;
using Athenex.Whimclean.Auditing.AspNetCore;

services.AddAuditing<Guid, Guid>();
services.AddHttpAuditRequestContext();
```

`AddHttpAuditRequestContext` registers the HTTP context accessor and replaces the `IAuditRequestContext` registration, so it works before or after `AddAuditing`. `HttpAuditRequestContext` replaces the null context registered by `AddAuditing` and reads:

- the actor name, when the principal is authenticated, from the `name` claim, then `email`, then the identity name;
- the correlation id from `X-Correlation-Id` (`HttpAuditRequestContext.CorrelationHeader`), then `X-Request-Id` (`HttpAuditRequestContext.RequestIdHeader`), then the request trace identifier;
- the remote address and the `User-Agent` header.

Blank claims and headers count as absent. Every value is null outside a request, so background work keeps working.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
