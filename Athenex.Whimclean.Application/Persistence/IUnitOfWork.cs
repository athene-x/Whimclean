namespace Athenex.Whimclean.Application.Persistence;

public interface IUnitOfWork
{
    Task CommitAsync(
        CancellationToken cancellationToken = default);
}
