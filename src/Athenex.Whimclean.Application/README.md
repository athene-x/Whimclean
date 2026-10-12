# Athenex.Whimclean.Application

[ภาษาไทย](README-TH.md)

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

The interface exposes one member, `TryGetUserId(out TUserId userId)`; `GetRequiredUserId()` is an extension method in `CurrentUserExtensions`. The API or host implements this interface from claims, a system-user identity, or another trusted execution context. `StaticCurrentUser<TUserId>` is a ready-made implementation that always answers with one fixed user id, for background jobs and tests.

## Unit of work

Use `IUnitOfWork` at application write boundaries:

```csharp
await unitOfWork.CommitAsync(cancellationToken);
```

The EF Core infrastructure package registers its business context as this contract. When the database reports a concurrency conflict, that package throws `PersistenceConcurrencyException` (in `Athenex.Whimclean.Application.Persistence`) so application code never depends on EF Core exception types.

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

The limits are constants on `ListQueryOptions`: the page defaults to 1 and the size to 10, the size may not exceed 100, the search term is trimmed and capped at 200 characters, and a query carries at most 5 sort terms and 10 filters. `Offset` is precomputed from page and size.

Infrastructure should translate only application-approved field names and operators into database expressions. Do not dynamically execute arbitrary client field names.

`FacetQueryOptions` provides the equivalent bounded input for distinct-value or facet queries; its `limit` caps how many values a facet may return (100 by default, 1000 at most). `FilterTerm` also supports `NotEqual`, `Contains`, `StartsWith`, `EndsWith`, and multi-value `In` comparisons (up to 25 values); filter values are passed through verbatim so whitespace-significant data stays matchable.

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

A message needs at least one recipient and either a `textBody` or an `htmlBody`; addresses and the subject are trimmed. `EmailAttachment` takes a file name, the content as `ReadOnlyMemory<byte>`, and a content type that defaults to `application/octet-stream`.

The consuming infrastructure project owns SMTP or provider-specific delivery.

This package does not depend on EF Core, ASP.NET Core Identity, a database provider, or multitenancy.

## Parse list queries from a query string

`ListQueryParser` turns the raw query-string values of a list endpoint into `ListQueryOptions`, so every API shares one syntax:

| Parameter | Format | Example |
| --- | --- | --- |
| `searchTerm` | free text | `searchTerm=acme` |
| `sort` | comma-separated fields, `-` prefix for descending | `sort=name,-createdAt` |
| `filters` | `field:operator:value`, one per repeated parameter | `filters=status:in:Active,Suspended` |
| `page`, `size` | one-based page and page size | `page=2&size=20` |

Operators are `eq`, `ne`, `gt`, `ge`, `lt`, `le`, `cn` (contains), `sw`, `ew` and `in` (comma-separated values); operator codes are case-insensitive. `Parse`, `ParseSort` and `ParseFilters` throw `FormatException` on bad input, while `TryParseFilter` and `TryParseSort` report a readable error without throwing, which the API package uses for validation.

```csharp
var options = ListQueryParser.Parse(searchTerm, sort, filters, page, size);
```

`EnumFilter` (in `Athenex.Whimclean.Application.Queries`) parses filter values into enum members case-insensitively and rejects undefined values: `TryParse<TEnum>` handles one value and `Parse<TEnum>` keeps the valid members of a list. The API validator (`AllowEnumFilterField<TEnum>` in `Athenex.Whimclean.Api.AspNetCore`) and the persistence filter therefore agree on which names are valid.

`WhereActive()` in `ActivatableQueryExtensions` filters an `IQueryable<T>` or `IEnumerable<T>` of `IActivatable` entities down to the active ones.

The `Map` extension in `OffsetPagedResultExtensions` projects a page's items while keeping its page number, size and totals, for turning persistence projections into response models:

```csharp
using Athenex.Whimclean.Application.Pagination;

OffsetPagedResult<OrderResponse> response = page.Map(order => new OrderResponse(order.Id));
```

## Set the current user outside a request

`ICurrentUserInitializer<TUserId>` lets startup bootstrap and seeders name the user their writes are attributed to through `SetUserId`. The ASP.NET Core package implements it together with a request-scoped `ICurrentUser<TUserId>` (`RequestCurrentUser<TUserId>`, registered by `AddLocalUserResolution<TUserId>`).

Repository and full architecture guide: https://github.com/athene-x/Whimclean
