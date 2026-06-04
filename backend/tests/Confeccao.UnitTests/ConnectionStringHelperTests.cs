using Confeccao.Api.Infrastructure;
using Npgsql;

namespace Confeccao.UnitTests;

public class ConnectionStringHelperTests
{
    [Fact]
    public void Passes_through_key_value_format_unchanged()
    {
        var raw = "Host=localhost;Port=5433;Database=confeccao;Username=u;Password=p";
        Assert.Equal(raw, ConnectionStringHelper.Normalize(raw));
    }

    [Fact]
    public void Translates_postgres_uri_to_npgsql_format()
    {
        var raw = "postgres://alice:s3cret@db.example.com:5432/mydb";
        var builder = new NpgsqlConnectionStringBuilder(ConnectionStringHelper.Normalize(raw));

        Assert.Equal("db.example.com", builder.Host);
        Assert.Equal(5432, builder.Port);
        Assert.Equal("mydb", builder.Database);
        Assert.Equal("alice", builder.Username);
        Assert.Equal("s3cret", builder.Password);
        Assert.Equal(SslMode.Require, builder.SslMode);
    }

    [Fact]
    public void Accepts_postgresql_scheme()
    {
        var raw = "postgresql://u:p@host/db";
        var builder = new NpgsqlConnectionStringBuilder(ConnectionStringHelper.Normalize(raw));
        Assert.Equal("host", builder.Host);
        Assert.Equal("db", builder.Database);
    }

    [Fact]
    public void Defaults_port_to_5432_when_omitted()
    {
        var raw = "postgres://u:p@host/db";
        var builder = new NpgsqlConnectionStringBuilder(ConnectionStringHelper.Normalize(raw));
        Assert.Equal(5432, builder.Port);
    }

    [Fact]
    public void Url_decodes_user_and_password()
    {
        // Neon's auto-generated passwords often contain special characters that
        // get percent-encoded in the URI.
        var raw = "postgres://my%2Buser:p%40ss%2Fword@host/db";
        var builder = new NpgsqlConnectionStringBuilder(ConnectionStringHelper.Normalize(raw));

        Assert.Equal("my+user", builder.Username);
        Assert.Equal("p@ss/word", builder.Password);
    }

    [Fact]
    public void Handles_neon_style_url_with_sslmode_query()
    {
        // Real Neon URL shape: pooled hostnames include "-pooler", sslmode=require,
        // long random suffixes. We don't need to honor the query string — SSL is
        // forced unconditionally on URI inputs — but parsing shouldn't choke on it.
        var raw = "postgresql://confeccao_owner:abc123XYZ@ep-cool-name-12345.us-east-2.aws.neon.tech/confeccao?sslmode=require";
        var builder = new NpgsqlConnectionStringBuilder(ConnectionStringHelper.Normalize(raw));

        Assert.Equal("ep-cool-name-12345.us-east-2.aws.neon.tech", builder.Host);
        Assert.Equal("confeccao", builder.Database);
        Assert.Equal("confeccao_owner", builder.Username);
        Assert.Equal(SslMode.Require, builder.SslMode);
    }

    [Fact]
    public void Throws_when_uri_has_no_database()
    {
        var raw = "postgres://u:p@host/";
        Assert.Throws<ArgumentException>(() => ConnectionStringHelper.Normalize(raw));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Passes_through_blank_input_unchanged(string raw)
    {
        Assert.Equal(raw, ConnectionStringHelper.Normalize(raw));
    }

    [Theory]
    // Real-world passwords from Postgres-as-a-service providers can contain any
    // of these characters. The naive string-interpolated version of Normalize()
    // crashed Npgsql's parser at runtime — these cases pin that fix.
    [InlineData("abc;def")]
    [InlineData("abc=def")]
    [InlineData("abc'def")]
    [InlineData("abc\"def")]
    [InlineData("abc def")]
    [InlineData("a;b=c'd\"e f")]
    [InlineData("X4ad1y=NhDC8wXyZ+/abc;")]  // Neon-shaped: base64-ish with `;` thrown in
    public void Result_is_parseable_by_npgsql_for_passwords_with_special_chars(string password)
    {
        var encoded = Uri.EscapeDataString(password);
        var raw = $"postgres://owner:{encoded}@db.example.com/mydb";

        var normalized = ConnectionStringHelper.Normalize(raw);

        // Round-trip through Npgsql's parser — this is what crashed on Render.
        var builder = new NpgsqlConnectionStringBuilder(normalized);
        Assert.Equal("db.example.com", builder.Host);
        Assert.Equal("mydb", builder.Database);
        Assert.Equal("owner", builder.Username);
        Assert.Equal(password, builder.Password);
    }
}
