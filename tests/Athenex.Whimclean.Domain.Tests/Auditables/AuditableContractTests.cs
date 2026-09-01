using Athenex.Whimclean.Domain.Auditables;
using Athenex.Whimclean.Domain.Behaviors;

namespace Athenex.Whimclean.Domain.Tests.Auditables;

public sealed class AuditableContractTests
{
    [Fact]
    public void Contracts_SupportNullableGuidActor()
    {
        IAuditable<Guid?> auditable = new GuidAuditable();
        ISoftDelete<Guid?> softDelete = new GuidAuditable();

        Assert.Null(auditable.CreatedBy);
        Assert.Null(auditable.UpdatedBy);
        Assert.Null(softDelete.DeletedBy);
    }

    [Fact]
    public void Contracts_SupportNullableNameActor()
    {
        IAuditable<string?> auditable = new NameAuditable();
        ISoftDelete<string?> softDelete = new NameAuditable();

        Assert.Null(auditable.CreatedBy);
        Assert.Null(auditable.UpdatedBy);
        Assert.Null(softDelete.DeletedBy);
    }

    private sealed class GuidAuditable : IAuditable<Guid?>, ISoftDelete<Guid?>
    {
        public DateTimeOffset CreatedAt { get; init; }
        public Guid? CreatedBy { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
        public Guid? UpdatedBy { get; init; }
        public bool IsDeleted { get; init; }
        public DateTimeOffset? DeletedAt { get; init; }
        public Guid? DeletedBy { get; init; }
    }

    private sealed class NameAuditable : IAuditable<string?>, ISoftDelete<string?>
    {
        public DateTimeOffset CreatedAt { get; init; }
        public string? CreatedBy { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
        public string? UpdatedBy { get; init; }
        public bool IsDeleted { get; init; }
        public DateTimeOffset? DeletedAt { get; init; }
        public string? DeletedBy { get; init; }
    }
}
