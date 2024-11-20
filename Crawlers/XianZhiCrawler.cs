using System.Text.Json;
using Newtonsoft.Json;
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
        await page.SetCookieAsync(JsonConvert.DeserializeObject<List<EditCookie>>(await File.ReadAllTextAsync("cookies/xz.json")).Select(t=>t.ToCookieParam()).ToArray());
        await page.EvaluateExpressionOnNewDocumentAsync("""
            const newProto = navigator.__proto__;
        delete newProto.webdriver;  //删除navigator.webdriver字段
        navigator.__proto__ = newProto;
        window.chrome = {};  //添加window.chrome字段，为增加真实性还需向内部填充一些值
        window.chrome.app = {"InstallState":"hehe", "RunningState":"haha", "getDetails":"xixi", "getIsInstalled":"ohno"};
        window.chrome.csi = function(){};
        window.chrome.loadTimes = function(){};
        window.chrome.runtime = function(){};
        Object.defineProperty(navigator, 'userAgent', {  //userAgent在无头模式下有headless字样，所以需覆写
            get: () => "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_3) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/81.0.4044.113 Safari/537.36",
        });
        Object.defineProperty(navigator, 'plugins', {  //伪装真实的插件信息
            get: () => [{"description": "Portable Document Format",
                        "filename": "internal-pdf-viewer",
                        "length": 1,
                        "name": "Chrome PDF Plugin"}]
        });
        Object.defineProperty(navigator, 'languages', { //添加语言
            get: () => ["zh-CN", "zh", "en"],
        });
        const originalQuery = window.navigator.permissions.query; //notification伪装
        window.navigator.permissions.query = (parameters) => (
        parameters.name === 'notifications' ?
          Promise.resolve({ state: Notification.permission }) :
          originalQuery(parameters)
        """);
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
            if (await page.GetTitleAsync() == "滑动验证页面")
            {
                await Task.Delay(TimeSpan.FromHours(1));
            }
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
    public override string Name { get; set; } = name;
    public override string Url { get; } = url;
    public override string Author { get; } = author;
    public override string Crawler { get; } = crawler;
}