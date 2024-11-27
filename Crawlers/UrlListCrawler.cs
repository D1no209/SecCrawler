using PuppeteerSharp;

namespace Crawlers;

public class UrlListCrawler : AbstractCrawler
{
    public override string Name => "微信文章";
    public override Task StartCrawl()
    {
        return Task.CompletedTask;
    }
    
    public override async Task<IPage> NewPage(IBrowser browser)
    {
        var page = await browser.NewPageAsync();
        return page;
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
            var ele = await page.QuerySelectorAsync("#img-content > h1");
            if (ele == null)
            {
                var followBtn = (await page.QuerySelectorAsync("#js_access_msg"));
                if (followBtn is null)
                    return null;
                // get href
                var newUrl  = await followBtn.EvaluateFunctionAsync<string>("(e) => e.href");
                await page.GoToAsync(newUrl);
            }
            var title = (await ele.EvaluateFunctionAsync<string>("(e) => e.innerText")).Trim();
            crawlTarget.Name = title;
            var authorName = await page.QuerySelectorAsync("#js_name");
            if (authorName == null)
            {
                return null;
            }

            var author = (await authorName.EvaluateFunctionAsync<string>("(ele) => ele.innerText")).Trim();
            crawlTarget.Author = author;
            
            // load all images
            var images = await page.QuerySelectorAllAsync("img");
            foreach (var image in images)
            {
                // check if image have `data-src` attr
                var src = await image.EvaluateFunctionAsync<string>("(e) => e.getAttribute('data-src')");
                if (src == null)
                {
                    continue;
                }
                await image.EvaluateFunctionAsync("e => e.src = e.getAttribute('data-src')");
            }

            await Task.Delay(2000);
            return page;
        }
        catch (Exception e)
        {
            return null;
        }
    }
}