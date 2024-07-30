using LiteDB;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;

namespace Crawlers;

public class PageSaver
{
    private readonly string _root;
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<CrawledPage> _crawlerTargets;
    private readonly ILiteCollection<CrawlTarget> _crawlerMarkedTarget;

    public PageSaver(string root)
    {
        _root = root;
        _db = new LiteDatabase("crawl.db");
        _crawlerTargets = _db.GetCollection<CrawledPage>();
        _crawlerMarkedTarget = _db.GetCollection<CrawlTarget>();
    }
    public Task MarkTarget(CrawlTarget target)
    {
        _crawlerMarkedTarget.Insert(target);
        return Task.CompletedTask;
    }
    
    public  Task<List<CrawlTarget>> GetMarkedTargetsByCrawler(string crawler)
    {
        return Task.FromResult(_crawlerMarkedTarget.Query().Where(t => t.Crawler == crawler).ToList());
    }
    
    public Task<bool> CheckSaved(string url)
    {
        return Task.FromResult(false);
        return Task.FromResult(_crawlerTargets.Exists(t => t.Url == url));
    }
    
    public async Task SavePage(IPage? page, CrawlTarget crawlTarget)
    {
        if (page == null)
            return;
        Directory.CreateDirectory(Path.Combine(_root, $"{crawlTarget.Crawler}"));
        var name = crawlTarget.Name;
        // replace invalid characters with fullwidth characters
        name = NormalizeFileName(name);
        var author = NormalizeFileName(crawlTarget.Author);
        var saveto = Path.Combine(_root, $"{crawlTarget.Crawler}/{name} by {author}.mhtml");
        var cdpSession = await page.CreateCDPSessionAsync();
        var pageContent = await cdpSession.SendAsync<JObject>("Page.captureSnapshot");
        await File.WriteAllTextAsync(saveto, pageContent.Value<string>("data"));
        _crawlerTargets.Insert(new CrawledPage(crawlTarget.Name, crawlTarget.Url, crawlTarget.Author, saveto));
    }
    
    public static string NormalizeFileName(string name)
    {
        return name.Replace(":", "：")
            .Replace("/", "／")
            .Replace("\\", "＼")
            .Replace("*", "＊")
            .Replace("?", "？")
            .Replace("\"", "＂")
            .Replace("<", "＜")
            .Replace(">", "＞")
            .Replace("|", "｜")
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("\t", " ");
    }
}

public record CrawledPage(string Name, string Url,string Author, string Path);