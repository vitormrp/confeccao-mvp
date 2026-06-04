using Npgsql;

namespace Confeccao.Api.Infrastructure;

/// <summary>
/// Adapts the URI-style connection strings handed out by Postgres-as-a-service
/// providers (Neon, Render, Heroku, Supabase, ...) into the key/value form
/// Npgsql expects. If the input is already in key/value form it's returned
/// unchanged.
///
/// Accepts both <c>postgres://</c> and <c>postgresql://</c> schemes. Forces
/// SSL on URI inputs because every managed provider requires it.
///
/// Uses <see cref="NpgsqlConnectionStringBuilder"/> for the final assembly so
/// special characters in the password (semicolons, equals signs, quotes —
/// common in auto-generated provider passwords) get escaped correctly. The
/// previous string-interpolated version broke on Neon's real passwords.
/// </summary>
public static class ConnectionStringHelper
{
    public static string Normalize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;
        if (!raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return raw;
        }

        var uri = new Uri(raw);
        var userInfo = uri.UserInfo.Split(':', 2);
        var user = Uri.UnescapeDataString(userInfo[0]);
        var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var host = uri.Host;
        var port = uri.IsDefaultPort ? 5432 : uri.Port;
        var db = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(db))
            throw new ArgumentException(
                "Connection URI must include a host and a database name.", nameof(raw));

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Database = db,
            Username = user,
            Password = pass,
            SslMode = SslMode.Require,
        };
        return builder.ConnectionString;
    }
}
