using Microsoft.Extensions.Options;
using SQLitePCL;
using UrlShortener.ConfigModel;
using UrlShortener.Migrations;
using UrlShortener.Services;

using static SQLitePCL.raw;
using static SQLitePCL.Ugly.ugly;

namespace UrlShortener.HostedServices;

public class SqliteManager : IHostedService, IUrlStore
{
    private readonly ILogger<SqliteManager> _logger;
    private readonly string _filename;
    private readonly Action<sqlite3>[] _migrations;

    public SqliteManager(ILogger<SqliteManager> logger, IOptions<SqliteConfig> options)
    {
        _logger = logger;
        _filename = options.Value.FilePath;
        _migrations = new Action<sqlite3>[]
        {
            BuildInitial.Up,
            AddCreationTimestamp.Up,
        };
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Batteries_V2.Init();
        _logger.LogInformation("SQLite initialized");

        using var db = open_v2(_filename, SQLITE_OPEN_CREATE | SQLITE_OPEN_READWRITE, null);
        db.exec("PRAGMA journal_mode=WAL");
        db.exec("CREATE TABLE IF NOT EXISTS Migrations (version INTEGER NOT NULL UNIQUE, date TEXT NOT NULL)");

        var latestVersion = db.query_scalar<int>("SELECT MAX(version) FROM Migrations");
        _logger.LogInformation("Latest DB version: {latestVersion}", latestVersion);

        foreach (var migration in _migrations.Skip(latestVersion))
        {
            var newVersion = latestVersion + 1;;
            _logger.LogInformation("Running migration to version {nextVersion}", newVersion);
            migration(db);
            db.exec("INSERT INTO Migrations (version, date) VALUES (?, ?)", newVersion, DateTime.UtcNow.ToString("O"));
            _logger.LogInformation("Migration to version {newVersion}", newVersion);
        }

        _logger.LogInformation("DB migration complete");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        using var db = open_v2(_filename, SQLITE_OPEN_CREATE | SQLITE_OPEN_READWRITE, null);
        _logger.LogInformation("Running WAL checkpoint");
        db.wal_checkpoint(null!, SQLITE_CHECKPOINT_FULL, out var logSize, out var framesCheckPointed);
        _logger.LogInformation("WAL checkpoint complete, log size {logSize}, {checkpointedFrames} frames",
            logSize, framesCheckPointed);

        return Task.CompletedTask;
    }

    public void StoreUrl(string key, Uri url)
    {
        using var db = open_v2(_filename, SQLITE_OPEN_READWRITE, null);
        using var stmt = db.prepare("INSERT INTO Urls (key, url, created_at) VALUES (?, ?, ?)");
        stmt.bind_text(1, key);
        stmt.bind_text(2, url.ToString());
        stmt.bind_text(3, DateTime.UtcNow.ToString("O"));

        stmt.step_done();
    }

    public bool TryGetUrl(string key, out Uri? url)
    {
        using var db = open_v2(_filename, SQLITE_OPEN_READONLY, null);
        using var stmt = db.prepare("Select url from Urls WHERE key = ?");
        stmt.bind_text(1, key);

        var result = stmt.step();

        if (result == SQLITE_ROW)
        {
            var storedUrl = stmt.column_text(0);
            url = new Uri(storedUrl);
            return true;
        }

        url = null;
        return false;
    }
}
