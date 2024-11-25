namespace Crawlers;

public interface IProxyRotator
{
    public Task Initialize();
    public string GetCurrentProxy();
    public Task RotateProxy();
}