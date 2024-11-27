namespace Crawlers;

public class SimpleProxyRotator : IProxyRotator
{
    private readonly HttpClient _httpClient = new();
    private List<string> _ips;
    private int _pointer = 0;
    private string _key;
    
    public async Task Initialize()
    {
        _key = Environment.GetEnvironmentVariable("QG_AUTH_KEY") ?? "";
        await RefreshProxies();
    }

    private async Task RefreshProxies()
    {
        var res = await _httpClient.GetStringAsync($"https://share.proxy.qg.net/get?key={_key}&num=1&area=&isp=0&format=txt&seq=,&distinct=true");
        _ips = res.Split(',').ToList();
        _pointer = 0;
    }

    public string GetCurrentProxy()
    {
        return _ips[_pointer];
    }

    private DateTime _lastRotate = DateTime.MinValue;
    
    public async Task RotateProxy()
    {
        if ((DateTime.Now - _lastRotate).TotalSeconds < 10)
        {
            return;
        }
        _lastRotate = DateTime.Now;

        _pointer++;
        if (_pointer >= _ips.Count)
        {
            await RefreshProxies();
        }
    }
}