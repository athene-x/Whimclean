namespace Athenex.Whimclean.Domain.Auditables;

/// <summary>
/// Combines <see cref="ICreateAuditable{TActor}" /> and
/// <see cref="IUpdateAuditable{TActor}" /> for records whose full write
/// history (creation and last modification) must be attributable. The
/// contract is read-only: the EF infrastructure package stamps the values
/// through the change tracker at save time.
/// </summary>
/// <typeparam name="TActor">
/// The stored actor type (e.g. a user id). May itself be a nullable type
/// when system-initiated writes leave the actor columns unset.
/// </typeparam>
public interface IAuditable<TActor> : ICreateAuditable<TActor>, IUpdateAuditable<TActor>
{
}
