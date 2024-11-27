using System.Net.Http.Json;
using System.Text.Json.Serialization;
using PuppeteerSharp;
using Spectre.Console;

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
        // https://www.freebuf.com/fapi/frontend/category/list?name=web&tag=category&limit=20&page=0&select=0&order=0
        var targets = (await _pageSaver.GetMarkedTargetsByCrawler("freebuf")).DistinctBy(t=>t.Url).ToList();
        var httpClient = new HttpClient();
        var pageId = 0;
        while (true)
        {
            await Task.Delay(500);
            AnsiConsole.MarkupLine("Fetching page [yellow]{0}[/]", pageId);
            var res = await httpClient.GetFromJsonAsync<FreebufResult>($"https://www.freebuf.com/fapi/frontend/category/list?name=web&tag=category&limit=20&page={pageId}&select=0&order=0");
            if (res?.Data.Articles.Count is 0)
            {
                break;
            }

            pageId++;
            var ars = res.Data.Articles
                .Select(t => new XianZhiCrawlTarget(
                    t.Title,
                    $"https://www.freebuf.com{t.ArticleUrl}",
                    t.Nickname,
                    "freebuf"
                ));
            foreach (var xianZhiCrawlTarget in ars)
            {
                if (await _pageSaver.CheckSaved(xianZhiCrawlTarget.Url))
                    goto returnResult;
                targets.Add(xianZhiCrawlTarget);
                await _pageSaver.MarkTarget(xianZhiCrawlTarget);
            }
        }
        returnResult:
        return targets;
    }

    class FreebufResult
    {
        [JsonPropertyName("data")] public FreebufData Data { get; set; }

        internal class FreebufData
        {
            [JsonPropertyName("data_list")] public List<FreebufTargetData> Articles { get; set; }

            public class FreebufTargetData
            {
                [JsonPropertyName("id")] public string Id { get; set; }
                [JsonPropertyName("post_title")] public string Title { get; set; }
                [JsonPropertyName("url")] public string ArticleUrl { get; set; }
                [JsonPropertyName("nickname")] public string Nickname { get; set; }
            }
        }
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
            var photos = await page.QuerySelectorAllAsync("p > img");
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