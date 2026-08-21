namespace Athenex.Whimclean.Application.Context;

public sealed class CurrentUserRequiredException()
    : Exception("A current user is required to persist changes.");
