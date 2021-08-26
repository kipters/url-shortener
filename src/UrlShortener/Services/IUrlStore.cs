namespace UrlShortener.Services
{
    public interface IUrlStore
    {
        void Init();

        void StoreUrl(string key, Uri url);
        bool TryGetUrl(string key, out Uri? url);
    }
}
