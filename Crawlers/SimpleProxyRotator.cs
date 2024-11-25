namespace Crawlers;

public class SimpleProxyRotator : IProxyRotator
{
    private readonly HttpClient _httpClient = new();
    private List<string> _ips;
    private int _pointer = 0;
    
    public async Task Initialize()
    {
        await RefreshProxies();
    }

    private async Task RefreshProxies()
    {
        var res = await _httpClient.GetStringAsync("https://share.proxy.qg.net/get?key=BE0E7894&num=1&area=&isp=0&format=txt&seq=\\n&distinct=true");
        _ips = res.Split('\n').ToList();
        _pointer = 0;
    }

    public string GetCurrentProxy()
    {
        return _ips[_pointer];
    }

    public async Task RotateProxy()
    {
        _pointer++;
        if (_pointer >= _ips.Count)
        {
            await RefreshProxies();
        }
    }
}