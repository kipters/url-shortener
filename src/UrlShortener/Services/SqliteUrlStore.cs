using Microsoft.Extensions.Options;
using UrlShortener.ConfigModel;

using static SQLitePCL.raw;
using static SQLitePCL.Ugly.ugly;

namespace UrlShortener.Services
{
    public class SqliteUrlStore : IUrlStore
    {
        private readonly string _filename;

        public SqliteUrlStore(IOptions<SqliteConfig> config)
        {
            _filename = config.Value.FilePath;
        }

        public void Init()
        {
            using var db = open_v2(_filename, SQLITE_OPEN_CREATE | SQLITE_OPEN_READWRITE, null);
            db.exec("CREATE TABLE IF NOT EXISTS Urls (key TEXT NOT NULL UNIQUE, url TEXT NOT NULL)");
        }

        public void StoreUrl(string key, Uri url)
        {
            using var db = open_v2(_filename, SQLITE_OPEN_READWRITE, null);
            using var stmt = db.prepare("INSERT INTO Urls (key, url) VALUES (?, ?)");
            stmt.bind_text(1, key);
            stmt.bind_text(2, url.ToString());

            stmt.step_done();
        }

        public bool TryGetUrl(string key, out Uri? url)
        {
            using var db = open_v2(_filename, SQLITE_OPEN_READONLY, null);
            using var stmt = db.prepare("SELECT url FROM Urls WHERE key = ?");
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
}
