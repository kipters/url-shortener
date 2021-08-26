using System.Diagnostics.CodeAnalysis;

namespace UrlShortener.ConfigModel
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class SqliteConfig
    {
        public string FilePath { get; set; } = null!;
    }
}
