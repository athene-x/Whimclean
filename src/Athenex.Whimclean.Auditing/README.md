# Athenex.Whimclean.Auditing

[ภาษาไทย](README-TH.md)

An audit log for Whimclean applications: every command marked as audited is recorded with its actor, target, outcome and the field changes its domain events reported, whether it succeeded, failed or threw. Entries are written outside the command's unit of work, so a failed attempt still leaves a trace while its own changes roll back.

## Install

```shell
dotnet add package Athenex.Whimclean.Auditing
dotnet add package Athenex.Whimclean.Auditing.AspNetCore   # in the API host
```

The package depends on `Athenex.Whimclean.Application`, `Athenex.Whimclean.Domain` and `Mediator.Abstractions`; the application supplies the Mediator source generator as it does for `Athenex.Whimclean.Mediator`.

## Mark commands as audited

```csharp
using Athenex.Whimclean.Auditing;
using Mediator;

public sealed record SuspendTenantCommand(Guid TenantId, string Reason)
    : ICommand<Result>, IAuditedCommand
{
    public AuditDescriptor Describe() => new(
        Action: "Tenant.Suspend",
        EntityType: "Tenant",
        EntityId: TenantId.ToString(),
        TenantId: TenantId,
        Reason: Reason);
}
```

`AuditDescriptor` takes `Action` and `EntityType`, then optional `EntityId`, `EntityReference`, `TenantId`, `Reason` and `Metadata` (a dictionary serialized as a JSON object). A command that creates an entity leaves `EntityId` null; the behavior fills it from a successful `Result<T>` whose value is a `Guid`, a string or a primitive such as a number. `TenantId` must be of the tenant id type registered with `AddAuditing`; a value of another type is ignored.

## Report changes from domain events

Handlers derived from `DomainEventHandler<TEvent>` (package `Athenex.Whimclean.Mediator`) add what they know to the scoped collector; the entry picks it up after the command commits:

```csharp
using Athenex.Whimclean.Auditing;
using Athenex.Whimclean.Mediator;

public sealed class TenantRenamedAuditHandler(IAuditChangeCollector collector)
    : DomainEventHandler<TenantRenamed>
{
    protected override ValueTask HandleAsync(TenantRenamed domainEvent, CancellationToken cancellationToken)
    {
        collector.Add("name", domainEvent.OldName, domainEvent.NewName);
        collector.SetEntityReference(domainEvent.Slug);
        return ValueTask.CompletedTask;
    }
}
```

`IAuditChangeCollector` offers `Add(field, oldValue, newValue)`, `SetEntityReference(reference)` and `SetTenantId(tenantId)`. Values the descriptor already names win over the collector's.

## Implement the writer

The entry must commit on its own connection. A `DbContext` factory does that; the table is mapped and migrated by the application's main context:

```csharp
using Athenex.Whimclean.Auditing;
using Microsoft.EntityFrameworkCore;

public sealed class AuditLogWriter(IDbContextFactory<AuditLogDbContext> factory)
    : IAuditLogWriter<Guid, Guid>
{
    public async ValueTask WriteAsync(AuditLog<Guid, Guid> entry, CancellationToken cancellationToken = default)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        context.Add(entry);
        await context.SaveChangesAsync(cancellationToken);
    }
}
```

Map `Changes` and `Metadata` to a JSON column and give the text columns the `AuditLog<,>.Maximum…Length` sizes; the entity trims and truncates to those, so writes never fail on length:

| Constant | Length |
| --- | --- |
| `MaximumActorNameLength` | 200 |
| `MaximumActionLength` | 100 |
| `MaximumEntityTypeLength` | 100 |
| `MaximumEntityIdLength` | 100 |
| `MaximumEntityReferenceLength` | 300 |
| `MaximumReasonLength` | 1000 |
| `MaximumErrorCodeLength` | 200 |
| `MaximumCorrelationIdLength` | 100 |
| `MaximumIpAddressLength` | 45 |
| `MaximumUserAgentLength` | 500 |

The JSON documents are produced by `AuditJson` with web defaults (camelCase) and nulls kept, so a change to `null` stays visible.

## Register

```csharp
using Athenex.Whimclean.Auditing;
using Athenex.Whimclean.Auditing.AspNetCore;
using Athenex.Whimclean.Mediator;

services
    .AddAuditing<Guid, Guid>()          // before AddUnitOfWorkBehavior
    .AddMediatorDomainEventDispatcher()
    .AddUnitOfWorkBehavior();
services.AddScoped<IAuditLogWriter<Guid, Guid>, AuditLogWriter>();
services.AddHttpAuditRequestContext();  // Athenex.Whimclean.Auditing.AspNetCore
```

`AddAuditing` must come before `AddUnitOfWorkBehavior`: Mediator runs behaviors in registration order, and the audit behavior has to wrap the unit of work to see the committed outcome. The audit write itself is not cancellable: an outcome that happened is always recorded, and an exception from the handler is recorded and then rethrown.

`AddAuditing<TUserId, TTenantId>` registers the scoped `AuditCommandBehavior<,>`, `AuditChangeCollector` and `AuditLogRecorder<TUserId, TTenantId>`, plus `TimeProvider.System` and a `NullAuditRequestContext` unless one of each is already registered. The type arguments are the local user id and the tenant id, both value types. The `IAuditLogWriter<TUserId, TTenantId>` is registered separately. Outside an HTTP request the `NullAuditRequestContext` stays in place and an entry whose caller is unknown is attributed to `system`.

## What an entry holds

| Field | Source |
| --- | --- |
| `Id`, `OccurredAt` | a version 7 `Guid` and the `TimeProvider` clock, normalized to UTC |
| `ActorId`, `ActorName`, `ActorType` | `ICurrentUser<TUserId>` and the request context's actor name, falling back to the user id as text; `system` when neither knows the caller |
| `Action`, `EntityType`, `EntityId`, `EntityReference`, `TenantId`, `Reason`, `Metadata` | the descriptor, completed by the collector |
| `Result`, `ErrorCode` | `AuditResult.Success`, or `AuditResult.Failed` with the failed `Result`'s error code or the exception type name |
| `Changes` | the collector's changes as a JSON array, on success only |
| `CorrelationId`, `IpAddress`, `UserAgent` | the request context |

Repository and full architecture guide: https://github.com/athene-x/Whimclean
