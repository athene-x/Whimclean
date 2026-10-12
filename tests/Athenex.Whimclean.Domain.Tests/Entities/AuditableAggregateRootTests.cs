using Athenex.Whimclean.Domain.Auditables;
using Athenex.Whimclean.Domain.Entities;

namespace Athenex.Whimclean.Domain.Tests.Entities;

public sealed class AuditableAggregateRootTests
{
    private sealed class Order(Guid id) : AuditableAggregateRoot<Guid, Guid?>(id);

    private sealed class OrderLine(int id) : AuditableEntity<int, Guid?>(id);

    [Fact]
    public void AuditableAggregateRoot_ImplementsAuditableWithUnsetFields()
    {
        var order = new Order(Guid.NewGuid());

        Assert.IsAssignableFrom<IAuditable<Guid?>>(order);
        Assert.Equal(default, order.CreatedAt);
        Assert.Null(order.CreatedBy);
        Assert.Null(order.UpdatedAt);
        Assert.Null(order.UpdatedBy);
        Assert.Empty(order.DomainEvents);
    }

    [Fact]
    public void AuditableEntity_KeepsIdentityEquality()
    {
        Assert.Equal(new OrderLine(1), new OrderLine(1));
        Assert.NotEqual(new OrderLine(1), new OrderLine(2));
        Assert.Throws<ArgumentException>(() => new OrderLine(0));
    }
}
