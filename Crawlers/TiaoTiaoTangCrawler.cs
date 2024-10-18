using PuppeteerSharp;

namespace Crawlers;

public class TiaoTiaoTangCrawler : AbstractCrawler
{
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
        do
        {
            await page.GoToAsync($"https://tttang.com/?page={pageNum}");
            var elements = await page.QuerySelectorAllAsync("div.media-body.mx-md-3.mx-2");
            foreach (var element in elements)
            {
                var link =  await element.QuerySelectorAsync("h3 > a.title");
                var name = await link.EvaluateFunctionAsync<string>("(element) => element.innerText");
                var url = await link.EvaluateFunctionAsync<string>("(element) => element.href");
                var author = await element.QuerySelectorAsync("span.author > a").EvaluateFunctionAsync<string>("(element) => element.innerText");
                targets.Add(new XianZhiCrawlTarget(name, url, author, "tttang"));
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
        
        return targets;
    }

    public override async Task<IPage?> ParseTarget(CrawlTarget crawlTarget, IPage page)
    {
        try
        {
            await page.GoToAsync(crawlTarget.Url);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return page;
    }
}