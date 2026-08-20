namespace Athenex.Whimclean.Application.Context;

public interface ICurrentRequestContext
{
    string CorrelationId { get; }
    string? IpAddress { get; }
}
