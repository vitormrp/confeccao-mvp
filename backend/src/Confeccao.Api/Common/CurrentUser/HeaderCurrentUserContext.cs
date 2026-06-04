namespace Confeccao.Api.Common.CurrentUser;

public class HeaderCurrentUserContext : ICurrentUserContext
{
    public const string HeaderName = "X-User-Id";

    private readonly IHttpContextAccessor _accessor;

    public HeaderCurrentUserContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public Guid? UserId
    {
        get
        {
            var header = _accessor.HttpContext?.Request.Headers[HeaderName].ToString();
            return Guid.TryParse(header, out var id) ? id : null;
        }
    }

    public Guid RequireUserId() =>
        UserId ?? throw new InvalidOperationException(
            $"Request requires '{HeaderName}' header with a valid user id.");
}
