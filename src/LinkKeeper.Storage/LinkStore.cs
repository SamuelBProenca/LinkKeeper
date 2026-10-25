using LinkKeeper.Core;
using Microsoft.Data.Sqlite;

namespace LinkKeeper.Storage;

public sealed class LinkStore
{
    private readonly string connectionString;
    private readonly TimeProvider clock;
    public LinkStore(string path, TimeProvider clock)
    {
        this.clock = clock;
        connectionString = new SqliteConnectionStringBuilder { DataSource = path, DefaultTimeout = 5 }.ToString();
        using var db = Open();
        using var command = db.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=WAL;";
        command.ExecuteNonQuery();
        var assembly = typeof(LinkStore).Assembly;
        using var stream = assembly.GetManifestResourceStream("LinkKeeper.Storage.Migrations.001-links.sql")!;
        using var reader = new StreamReader(stream);
        using var transaction = db.BeginTransaction();
        command.Transaction = transaction;
        command.CommandText = reader.ReadToEnd();
        command.ExecuteNonQuery();
        transaction.Commit();
    }
    private SqliteConnection Open()
    {
        var db = new SqliteConnection(connectionString);
        db.Open();
        return db;
    }
    private static void Owner(string owner)
    {
        if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("Proprietário obrigatório.");
    }
    public static string Normalize(string original)
    {
        var uri = new UriBuilder(original);
        // Preserve path, query, fragment and original URL; only canonicalize URI authority.
        return uri.Uri.AbsoluteUri;
    }
    public (LinkItem Item, bool Created) Add(string owner, ParsedLink parsed, string description = "")
    {
        Owner(owner);
        if (description.Length > 1000) throw new ArgumentException("Descrição: máximo de 1000 caracteres.");
        var normalized = Normalize(parsed.OriginalUrl);
        using var db = Open();
        using var transaction = db.BeginTransaction();
        using var command = db.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO links VALUES($id,$owner,$original,$normalized,$category,$description,
            'API',$now,$now,$expires,'Active',NULL)
            ON CONFLICT(owner,normalized_url,category) DO NOTHING;
            """;
        command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("N"));
        command.Parameters.AddWithValue("$owner", owner);
        command.Parameters.AddWithValue("$original", parsed.OriginalUrl);
        command.Parameters.AddWithValue("$normalized", normalized);
        command.Parameters.AddWithValue("$category", parsed.Category);
        command.Parameters.AddWithValue("$description", description);
        command.Parameters.AddWithValue("$now", parsed.CreatedAt.ToUnixTimeMilliseconds());
        command.Parameters.AddWithValue("$expires", parsed.ExpiresAt is { } expiry ? expiry.ToUnixTimeMilliseconds() : DBNull.Value);
        var created = command.ExecuteNonQuery() == 1;
        command.Parameters.Clear();
        command.CommandText = "SELECT * FROM links WHERE owner=$owner AND normalized_url=$url AND category=$category";
        command.Parameters.AddWithValue("$owner", owner);
        command.Parameters.AddWithValue("$url", normalized);
        command.Parameters.AddWithValue("$category", parsed.Category);
        LinkItem item;
        using (var reader = command.ExecuteReader()) { reader.Read(); item = Read(reader); }
        transaction.Commit();
        return (item, created);
    }
    public IReadOnlyList<LinkItem> List(string owner, string status = "Active", string? category = null, string? search = null)
    {
        Owner(owner);
        if (!Enum.TryParse<LinkStatus>(status, out var state) || !Enum.IsDefined(state))
            throw new ArgumentException("Estado inválido.");
        Expire();
        using var db = Open();
        using var command = db.CreateCommand();
        command.CommandText = """
            SELECT * FROM links WHERE owner=$owner AND status=$status
            AND ($category IS NULL OR category=$category)
            AND ($search IS NULL OR instr(lower(original_url),lower($search))>0
                OR instr(lower(description),lower($search))>0)
            ORDER BY created_at DESC, id LIMIT 200;
            """;
        command.Parameters.AddWithValue("$owner", owner);
        command.Parameters.AddWithValue("$status", status);
        command.Parameters.AddWithValue("$category", (object?)category ?? DBNull.Value);
        command.Parameters.AddWithValue("$search", (object?)search ?? DBNull.Value);
        using var reader = command.ExecuteReader();
        var result = new List<LinkItem>();
        while (reader.Read()) result.Add(Read(reader));
        return result;
    }
    public LinkItem? Get(string owner, string id)
    {
        Owner(owner);
        Expire();
        using var db = Open();
        using var command = db.CreateCommand();
        command.CommandText = "SELECT * FROM links WHERE owner=$owner AND id=$id";
        command.Parameters.AddWithValue("$owner", owner);
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? Read(reader) : null;
    }
    public bool Archive(string owner, string id)
    {
        Owner(owner);
        using var db = Open();
        using var command = db.CreateCommand();
        command.CommandText = "UPDATE links SET status='Archived', archived_at=$now, updated_at=$now WHERE owner=$owner AND id=$id AND status!='Archived'";
        command.Parameters.AddWithValue("$owner", owner);
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$now", clock.GetUtcNow().ToUnixTimeMilliseconds());
        return command.ExecuteNonQuery() == 1;
    }
    public bool Renew(string owner, string id, int days)
    {
        Owner(owner);
        if (days is < 1 or > 36500) throw new ArgumentException("Renovação: entre 1 e 36500 dias.");
        using var db = Open();
        using var command = db.CreateCommand();
        command.CommandText = """
            UPDATE links SET status='Active', expires_at=$expires, updated_at=$now
            WHERE owner=$owner AND id=$id AND
            (status='Expired' OR (status='Active' AND expires_at <= $now))
            """;
        command.Parameters.AddWithValue("$owner", owner);
        command.Parameters.AddWithValue("$id", id);
        var now = clock.GetUtcNow();
        command.Parameters.AddWithValue("$now", now.ToUnixTimeMilliseconds());
        command.Parameters.AddWithValue("$expires", now.AddDays(days).ToUnixTimeMilliseconds());
        return command.ExecuteNonQuery() == 1;
    }
    public int Expire()
    {
        using var db = Open();
        using var command = db.CreateCommand();
        command.CommandText = "UPDATE links SET status='Expired', updated_at=$now WHERE status='Active' AND expires_at <= $now";
        command.Parameters.AddWithValue("$now", clock.GetUtcNow().ToUnixTimeMilliseconds());
        return command.ExecuteNonQuery();
    }
    private static LinkItem Read(SqliteDataReader reader)
    {
        DateTimeOffset? Date(int i) => reader.IsDBNull(i) ? null : DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(i));
        return new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            reader.GetString(4), reader.GetString(5), reader.GetString(6), Date(7)!.Value,
            Date(8)!.Value, Date(9), Enum.Parse<LinkStatus>(reader.GetString(10)), Date(11));
    }
}
