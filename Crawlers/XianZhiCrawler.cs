using PuppeteerSharp;

namespace Crawlers;

public class XianZhiCrawler : AbstractCrawler
{
    private readonly PageSaver _pageSaver;

    public XianZhiCrawler(PageSaver pageSaver)
    {
        _pageSaver = pageSaver;
    }
    public override string Name => "先知社区 - 技术文章";

    public override async Task<IPage> StartCrawl(IBrowser browser)
    {
        var page = await browser.NewPageAsync();
        await page.GoToAsync(@"https://xz.aliyun.com/");
        return page;
    }

    public override async Task<List<CrawlTarget>> GetTargets(IPage page)
    {
        var targets = await GetAreaTargets(page, "1");
        targets.AddRange(await GetAreaTargets(page, "4"));
        return targets;
    }

    private async Task<List<CrawlTarget>> GetAreaTargets(IPage page, string area)
    {
        var targets = new List<CrawlTarget>();
        targets.AddRange(await _pageSaver.GetMarkedTargetsByCrawler("xianzhi"));
        await page.GoToAsync(@"https://xz.aliyun.com/tab/" + area);

        var pageSpan = await page.QuerySelectorAsync("ul.pull-right > li > a.active");
        var content = await pageSpan.EvaluateFunctionAsync<string>("(element) => element.innerText");
        var res = content[2..];
        var totalPage = int.Parse(res);
        for (var i = 0; i < totalPage; i++)
        {
            var pageUrl = $"https://xz.aliyun.com/tab/{area}?page={i + 1}";
            await page.GoToAsync(pageUrl);
            var elements = await page.QuerySelectorAllAsync("table.topic-list > tbody > tr > td");
            foreach (var element in elements)
            {
                var name = await (await element.QuerySelectorAsync("a.topic-title")).EvaluateFunctionAsync<string>("(element) => element.innerText");
                var url = await (await element.QuerySelectorAsync("a.topic-title")).EvaluateFunctionAsync<string>("(element) => element.href");
                var author = await (await element.QuerySelectorAsync("p.topic-info > a")).EvaluateFunctionAsync<string>("(element) => element.innerText");
                if (targets.Exists(t => t.Url == url))
                {
                    goto returnResult;
                }

                var target = new XianZhiCrawlTarget(name, url, author, "xianzhi");
                targets.Add(target);
                await _pageSaver.MarkTarget(target);
            }

            await Task.Delay(3000);
        } 
returnResult:
        return targets;
    }
    
    public override async Task<IPage?> ParseTarget(CrawlTarget crawlTarget, IPage page)
    {
        try
        {
            try
            {
                await page.GoToAsync(crawlTarget.Url);
            }
            catch
            {
                // ignore
            }
            // 隐藏无关元素

            List<string> selectors = [".navbar", ".sidebar", "#reply-box", ".bs-docs-footer"];
            foreach (var selector in selectors)
            {
                var element = await page.QuerySelectorAsync(selector);
                await page.EvaluateFunctionAsync("element => element.style.display = 'none'", element);
            }

            await Task.Delay(15000);
            return page;
        }
        catch
        {
            return null;
        }
    }
}

public class XianZhiCrawlTarget(string name, string url, string author, string crawler) : CrawlTarget
{
    public override string Name { get; } = name;
    public override string Url { get; } = url;
    public override string Author { get; } = author;
    public override string Crawler { get; } = crawler;
}