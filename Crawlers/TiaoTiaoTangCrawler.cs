using PuppeteerSharp;

namespace Crawlers;

public class TiaoTiaoTangCrawler : AbstractCrawler
{
    private readonly PageSaver _pageSaver;

    public TiaoTiaoTangCrawler(PageSaver pageSaver)
    {
        _pageSaver = pageSaver;
    }
    
    public override string Name => "跳跳堂";
    public override async Task<IPage> StartCrawl(IBrowser browser)
    {
        var page = await browser.NewPageAsync();
        await page.GoToAsync(@"https://tttang.com/");
        return page;
    }

    public override async Task<List<CrawlTarget>> GetTargets(IPage page)
    {
        var pageNum = 1;
        var targets = new List<CrawlTarget>();
        targets.AddRange(await _pageSaver.GetMarkedTargetsByCrawler("tttang"));
        do
        {
            await Task.Delay(3000);
            await page.GoToAsync($"https://tttang.com/?page={pageNum}");
            var elements = await page.QuerySelectorAllAsync("div.media-body.mx-md-3.mx-2");
            foreach (var element in elements)
            {
                var link =  await element.QuerySelectorAsync("h3 > a.title");
                var name = await link.EvaluateFunctionAsync<string>("(element) => element.innerText");
                var url = await link.EvaluateFunctionAsync<string>("(element) => element.href");
                var author = await element.QuerySelectorAsync("span.author > a").EvaluateFunctionAsync<string>("(element) => element.innerText");
                var target = new XianZhiCrawlTarget(name, url, author, "tttang");
                if (targets.Exists(t=>t.Url == target.Url))
                {
                    goto returnResult;
                }
                await _pageSaver.MarkTarget(target);
                targets.Add(target);
            }
            pageNum++;
            var nextPage = await page.QuerySelectorAsync("ul.pagination > li.last");
            // get class list
            var disabled = await nextPage.EvaluateFunctionAsync<bool>("element => element.classList.contains('disabled')");
            if (disabled)
            {
                break;
            }
        } while (true);
        returnResult:
        return targets;
    }

    public override async Task<IPage?> ParseTarget(CrawlTarget crawlTarget, IPage page)
    {
        try
        {
            await page.GoToAsync(crawlTarget.Url);
            await Task.Delay(3000);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return page;
    }
}