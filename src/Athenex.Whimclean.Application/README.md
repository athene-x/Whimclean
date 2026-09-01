# Athenex.Whimclean.Application

Application-layer contracts and validated request models for .NET 10 clean-architecture applications.

## Install

```shell
dotnet add package Athenex.Whimclean.Application
```

The package depends only on `Athenex.Whimclean.Domain`.

## Current user

Application and infrastructure services can depend on `ICurrentUser<TUserId>` without referencing ASP.NET Core Identity:

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

The API or host implements this interface from claims, a system-user identity, or another trusted execution context.

## Unit of work

Use `IUnitOfWork` at application write boundaries:

```csharp
await unitOfWork.CommitAsync(cancellationToken);
```

The EF Core infrastructure package registers its business context as this contract.

## Validated list queries

`ListQueryOptions` normalizes search text, materializes sort/filter collections, and enforces bounded paging:

```csharp
using Athenex.Whimclean.Application.Queries;

var options = new ListQueryOptions(
    searchTerm: "pending",
    sort: [new SortTerm("createdAt", SortDirection.Descending)],
    filters: [new FilterTerm("status", "Pending")],
    page: 1,
    size: 20);
```

Infrastructure should translate only application-approved field names and operators into database expressions. Do not dynamically execute arbitrary client field names.

`FacetQueryOptions` provides the equivalent bounded input for distinct-value or facet queries; its `limit` caps how many values a facet may return (100 by default). `FilterTerm` also supports `NotEqual`, `Contains`, `StartsWith`, `EndsWith`, and multi-value `In` comparisons; filter values are passed through verbatim so whitespace-significant data stays matchable.

## Pagination results

Use `OffsetPagedResult<T>` for numbered pages and `KeysetPagedResult<T, TKey>` for cursor-based navigation.

## Email boundary

Application code creates `EmailMessage` and depends on `IEmailSender`:

```csharp
await emailSender.SendAsync(
    new EmailMessage(
        "member@example.com",
        "Welcome",
        "Your account is ready."),
    cancellationToken);
```

The full constructor accepts multiple recipients, CC/BCC lists, `from`/`replyTo` overrides, and `EmailAttachment` files:

```csharp
var invoice = new EmailMessage(
    recipients: ["owner@example.com", "billing@example.com"],
    subject: "Invoice 2026-08",
    textBody: "Your invoice is attached.",
    bcc: ["audit@example.com"],
    replyTo: "support@example.com",
    attachments: [new EmailAttachment("invoice.pdf", pdfBytes, "application/pdf")]);
```

The consuming infrastructure project owns SMTP or provider-specific delivery.

This package does not depend on EF Core, ASP.NET Core Identity, a database provider, or multitenancy.

Repository and full architecture guide: https://github.com/athene-x/Whimclean
