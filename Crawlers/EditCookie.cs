using PuppeteerSharp;

namespace Crawlers;

public class EditCookie
{
    public string Domain { get; set; }
    public string Name { get; set; }
    public string Path { get; set; }
    public string Value { get; set; }
    
    public CookieParam ToCookieParam()
    {
        return new CookieParam
        {
            Domain = Domain,
            Name = Name,
            Path = Path,
            Value = Value
        };
    }
}