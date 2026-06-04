namespace Confeccao.Api.Common.CurrentUser;

/// <summary>
/// Resolves the current user. The MVP implementation reads <c>X-User-Id</c> from
/// the request headers — anonymous-friendly: callers send the id of whichever user
/// they're acting as. When auth is added, this interface gets a session-backed
/// implementation and call sites stay the same.
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>The current user id, or null if unset.</summary>
    Guid? UserId { get; }

    /// <summary>The current user id, throwing if unset. Use when an endpoint requires identity.</summary>
    Guid RequireUserId();
}
