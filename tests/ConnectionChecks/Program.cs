using Npgsql;

var checks = 0;

void Check(bool condition)
{
    if (!condition) throw new Exception("Connection conversion check failed.");
    checks++;
}

void Reject(string? value)
{
    try
    {
        DatabaseConnection.Normalize(value);
    }
    catch (InvalidOperationException)
    {
        checks++;
        return;
    }
    throw new Exception("Invalid configuration was accepted.");
}

const string local = "Host=localhost;Port=5433;Database=tires;Username=postgres;Password=postgres";
Check(DatabaseConnection.Normalize(local) == local);

var internalUrl = new NpgsqlConnectionStringBuilder(
    DatabaseConnection.Normalize("postgres://user:password@render-internal/tires"));
Check(internalUrl.Host == "render-internal" && internalUrl.Port == 5432);
Check(internalUrl.Username == "user" && internalUrl.Password == "password" && internalUrl.Database == "tires");
Check(internalUrl.SslMode == SslMode.Prefer);

var externalUrl = new NpgsqlConnectionStringBuilder(DatabaseConnection.Normalize(
    "postgresql://user%40example:p%40ss%3Aword%3B%22%25%2B@db.example.com:6432/tire%20db?sslmode=require"));
Check(externalUrl.Host == "db.example.com" && externalUrl.Port == 6432);
Check(externalUrl.Username == "user@example" && externalUrl.Password == "p@ss:word;\"%+");
Check(externalUrl.Database == "tire db" && externalUrl.SslMode == SslMode.Require);

var verifiedUrl = new NpgsqlConnectionStringBuilder(DatabaseConnection.Normalize(
    "postgres://user:password@host/tires?sslmode=verify-full"));
Check(verifiedUrl.SslMode == SslMode.VerifyFull);

Reject(null);
Reject(" ");
Reject("postgres://user@host/tires");
Reject("postgres://user:password@host/");
Reject("postgres://user:password@host/tires?sslmode=invalid");
Reject("postgres://user:password@host/tires?unexpected=value");
Reject("postgres://user:password@host/tires#fragment");

Console.WriteLine($"Passed {checks} connection conversion checks.");
