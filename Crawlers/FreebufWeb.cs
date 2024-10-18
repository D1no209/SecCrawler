using PuppeteerSharp;

namespace Crawlers;

public class FreebufWeb : AbstractCrawler
{
    private readonly PageSaver _pageSaver;
    public override string Name => "Freebuf Web 安全";

    public FreebufWeb(PageSaver pageSaver)
    {
        _pageSaver = pageSaver;
    }
    
    public override async Task<IPage> StartCrawl(IBrowser browser)
    {
        var page = await browser.NewPageAsync();
        await page.GoToAsync("https://www.freebuf.com/articles/web");
        return page;
    }

    public override async Task<List<CrawlTarget>> GetTargets(IPage page)
    {
        // get page count
        var targets = new List<CrawlTarget>();
        targets.AddRange(await _pageSaver.GetMarkedTargetsByCrawler("freebuf"));
        var nextPageClicker = await page.QuerySelectorAsync("li[title='下一页']");
        do
        {
            var items = await page.QuerySelectorAllAsync("div.content-view > div.container-center > div.article-item");
            foreach (var item in items)
            {
                var title = await item.QuerySelectorAsync("div.title-left > a");
                var url = await title.EvaluateFunctionAsync<string>("(element) => element.href");
                var name = await title.EvaluateFunctionAsync<string>("(element) => element.innerText");
                name = name.Trim();
                var author = await item.QuerySelectorAsync("div.item-bottom > p > a");
                var authorName = await author.EvaluateFunctionAsync<string>("(element) => element.innerText");
                if (targets.Exists(t=>t.Url == url))
                {
                    goto returnResult;
                }

                var target = new XianZhiCrawlTarget(name, url, authorName.Trim(), "freebuf");
                await _pageSaver.MarkTarget(target);
                targets.Add(target);
            }
            var goNext = await nextPageClicker.EvaluateFunctionAsync<bool>("element => element.classList.contains('ant-pagination-disabled')");
            if (goNext)
            {
                break;
            }

            await Task.Delay(1000);
            await nextPageClicker.ClickAsync();
        } while (true);
returnResult:
        return targets;

    }

    public override async Task<IPage?> ParseTarget(CrawlTarget crawlTarget, IPage page)
    {
        try
        {
            await page.GoToAsync(crawlTarget.Url);
            // hide unrelated elements
            List<string> elementsSelectors =
            [
                "header.articles-layout-header",
                "div.page-header",
                "div.floating-view",
                "div.aside-left",
                "div.aside-right",
                "footer.ant-layout-footer",
                "div.remix-module",
                "div.introduce"
            ];

            foreach (var selector in elementsSelectors)
            {
                var element = await page.QuerySelectorAsync(selector);
                await page.EvaluateFunctionAsync("element => element.style.display = 'none'", element);
            }
            
            // use the large photo
            var photos = await page.QuerySelectorAllAsync("div.main-warpper > img");
            foreach (var photo in photos)
            {
                // set src to the large attribute
                await photo.EvaluateFunctionAsync("element => element.src = element.getAttribute('large')", photo);
            }
            
            var contents = await page.QuerySelectorAsync("div.content-detail");
            await page.EvaluateFunctionAsync("element => element.style.maxWidth = '100%'", contents);
            await Task.Delay(5000);
            return page;

        }
        catch
        {
            return null;
        }
    }
}