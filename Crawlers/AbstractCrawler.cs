using PuppeteerSharp;

namespace Crawlers;

public abstract class AbstractCrawler
{
    public abstract string Name { get; }
    public abstract Task<IPage> StartCrawl(IBrowser browser);

    public abstract Task<List<CrawlTarget>> GetTargets(IPage page);

    public abstract Task<IPage?> ParseTarget(CrawlTarget crawlTarget, IPage page);
}

public abstract class CrawlTarget
{
    public abstract string Name { get; }
    public abstract string Url { get; }
    public abstract string Author { get; }
    public abstract string Crawler { get; }
}