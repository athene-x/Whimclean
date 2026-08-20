namespace Athenex.Whimclean.Application.Persistence;

public sealed class PersistenceConcurrencyException(
    string message,
    Exception innerException) : Exception(message, innerException);
