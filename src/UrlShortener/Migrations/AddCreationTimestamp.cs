using SQLitePCL;
using SQLitePCL.Ugly;

namespace UrlShortener.Migrations;

public static class AddCreationTimestamp
{
    public static void Up(sqlite3 db)
    {
        db.exec("ALTER TABLE Urls ADD created_at TEXT NOT NULL DEFAULT \"1970-01-01T00:00:00Z\"");
    }

    public static void Down(sqlite3 db)
    {
        db.exec("ALTER TABLE Urls DROP created_at");
    }
}
