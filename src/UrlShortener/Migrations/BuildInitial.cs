using SQLitePCL;
using SQLitePCL.Ugly;

namespace UrlShortener.Migrations;

public static class BuildInitial
{
    public static void Up(sqlite3 db)
    {
        // This one needs to be idempotent for backwards compatibility
        db.exec("CREATE TABLE IF NOT EXISTS Urls (key TEXT NOT NULL UNIQUE, url TEXT NOT NULL)");
    }

    public static void Down(sqlite3 db)
    {
        db.exec("DROP TABLE IF EXISTS Urls");
    }
}
