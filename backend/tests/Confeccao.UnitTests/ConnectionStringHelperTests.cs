using Confeccao.Api.Infrastructure;

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
        var result = ConnectionStringHelper.Normalize(raw);

        Assert.Contains("Host=db.example.com", result);
        Assert.Contains("Port=5432", result);
        Assert.Contains("Database=mydb", result);
        Assert.Contains("Username=alice", result);
        Assert.Contains("Password=s3cret", result);
        Assert.Contains("Ssl Mode=Require", result);
        Assert.Contains("Trust Server Certificate=true", result);
    }

    [Fact]
    public void Accepts_postgresql_scheme()
    {
        var raw = "postgresql://u:p@host/db";
        var result = ConnectionStringHelper.Normalize(raw);
        Assert.Contains("Host=host", result);
        Assert.Contains("Database=db", result);
    }

    [Fact]
    public void Defaults_port_to_5432_when_omitted()
    {
        var raw = "postgres://u:p@host/db";
        Assert.Contains("Port=5432", ConnectionStringHelper.Normalize(raw));
    }

    [Fact]
    public void Url_decodes_user_and_password()
    {
        // Neon's auto-generated passwords often contain special characters that
        // get percent-encoded in the URI.
        var raw = "postgres://my%2Buser:p%40ss%2Fword@host/db";
        var result = ConnectionStringHelper.Normalize(raw);

        Assert.Contains("Username=my+user", result);
        Assert.Contains("Password=p@ss/word", result);
    }

    [Fact]
    public void Handles_neon_style_url_with_sslmode_query()
    {
        // Real Neon URL shape: pooled hostnames include "-pooler", sslmode=require,
        // long random suffixes. We don't need to honor the query string — SSL is
        // forced unconditionally on URI inputs — but parsing shouldn't choke on it.
        var raw = "postgresql://confeccao_owner:abc123XYZ@ep-cool-name-12345.us-east-2.aws.neon.tech/confeccao?sslmode=require";
        var result = ConnectionStringHelper.Normalize(raw);

        Assert.Contains("Host=ep-cool-name-12345.us-east-2.aws.neon.tech", result);
        Assert.Contains("Database=confeccao", result);
        Assert.Contains("Username=confeccao_owner", result);
        Assert.Contains("Ssl Mode=Require", result);
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
}
