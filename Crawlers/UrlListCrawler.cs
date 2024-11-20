using PuppeteerSharp;

namespace Crawlers;

public class UrlListCrawler : AbstractCrawler
{
    public override string Name => "微信文章";
    public override async Task<IPage> StartCrawl(IBrowser browser)
    {
        return await browser.NewPageAsync();
    }

    public override async Task<List<CrawlTarget>> GetTargets(IPage page)
    {
        var urls = await File.ReadAllLinesAsync("url.txt");
        return urls.Select(CrawlTarget (url) => new XianZhiCrawlTarget
            (url, url, "", "url")).ToList();
    }

    public override async Task<IPage?> ParseTarget(CrawlTarget crawlTarget, IPage page)
    {
        try{
            await page.GoToAsync(crawlTarget.Url);
            // await Task.Delay(3000);
            var ele = await page.QuerySelectorAsync("#img-content >h1");
            if (ele == null)
            {
                return null;
            }
            var title = (await ele.EvaluateFunctionAsync<string>("(e) => e.innerText")).Trim();
            crawlTarget.Name = title;
            return page;
        }
        catch (Exception e)
        {
            return null;
        }
    }
}