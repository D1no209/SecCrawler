using System.Net;
using System.Text;
using PuppeteerSharp;
using Spectre.Console;

namespace Crawlers;

public class XianZhiCrawler : AbstractCrawler
{
    private readonly PageSaver _pageSaver;
    // private readonly IProxyRotator _proxyRotator;

    public XianZhiCrawler(PageSaver pageSaver, IProxyRotator proxyRotator)
    {
        _pageSaver = pageSaver;
        // _proxyRotator = proxyRotator;
    }

    public override string Name => "先知社区";
    
    public override async Task StartCrawl()
    {
        // await _proxyRotator.Initialize();
    }

    public override async Task<IPage> NewPage(IBrowser browser)
    {
        var page = await browser.NewPageAsync();
        await page.GoToAsync(@"https://xz.aliyun.com/");
        // await page.SetRequestInterceptionAsync(true);
        // page.AddRequestInterceptor(RequestInterceptor);
        return page;
    }

    public override async Task<List<CrawlTarget>> GetTargets(IPage page)
    {
        var targets = await GetAreaTargets(page, "9", "技术文章"); // 技术文章
        targets.AddRange(await GetAreaTargets(page, "24", "渗透测试")); // 渗透测试
        targets.AddRange(await GetAreaTargets(page, "2", "漏洞分析")); // 漏洞分析
        targets.AddRange(await GetAreaTargets(page, "14", "Web安全")); // web安全
        targets.AddRange(await GetAreaTargets(page, "20","二进制安全")); // 二进制安全
        targets.AddRange(await GetAreaTargets(page, "15","移动安全")); // 移动安全
        targets.AddRange(await GetAreaTargets(page, "16","IOT 安全")); // IOT 安全
        targets.AddRange(await GetAreaTargets(page, "10","企业安全")); // 企业安全
        targets.AddRange(await GetAreaTargets(page, "21","区块链安全")); // 区块链安全
        targets.AddRange(await GetAreaTargets(page, "11","CTF")); // CTF
        targets.AddRange(await GetAreaTargets(page, "3","安全工具")); // 安全工具
        return targets;
    }

    private async Task<List<CrawlTarget>> GetAreaTargets(IPage page, string area, string areaName)
    {
        var targets = new List<CrawlTarget>();
        targets.AddRange((await _pageSaver.GetMarkedTargetsByCrawler("xianzhi")).DistinctBy(t=>t.Url));
        await page.GoToAsync(@"https://xz.aliyun.com/news?cate_id=" + area);
        var i = 0;
        while (true)
        {
            var pageUrl = $"https://xz.aliyun.com/news?cate_id={area}&page={i + 1}";
            await page.GoToAsync(pageUrl);
            var elements = await page.QuerySelectorAllAsync("div.news_item");
            foreach (var element in elements)
            {
                var name =
                    await (await element.QuerySelectorAsync("a.news_title")).EvaluateFunctionAsync<string>(
                        "(element) => element.innerText");
                var url =
                    await (await element.QuerySelectorAsync("a.news_title")).EvaluateFunctionAsync<string>(
                        "(element) => element.href");
                var author =
                    await (await element.QuerySelectorAsync("a.user-info > span")).EvaluateFunctionAsync<string>(
                        "(element) => element.innerText");
                // if (targets.Exists(t => t.Url == url))
                // {
                //     goto returnResult;
                // }

                var target = new XianZhiCrawlTarget(name, url, author, "xianzhi",areaName);
                targets.Add(target);
                await _pageSaver.MarkTarget(target);
            }

            var nextPage = await page.QuerySelectorAsync("li.page-item-last");
            if (nextPage == null)
            {
                break;
            }
            var classList = await nextPage.EvaluateFunctionAsync<string>("(element) => element.className");
            if (classList.Contains("disabled"))
            {
                break;
            }
            i++;
            await Task.Delay(1000);
        }

        returnResult:
        return targets;
    }

    public override async Task<IPage?> ParseTarget(CrawlTarget crawlTarget, IPage page)
    {
        navigateToPage:
        try
        {
            try
            {
                await page.GoToAsync(crawlTarget.Url);
            }
            catch
            {
                // await _proxyRotator.RotateProxy();
                // goto navigateToPage;
            }

            // 隐藏无关元素
            if (await page.GetTitleAsync() == "滑动验证页面")
            {
                AnsiConsole.MarkupLine("[yellow] WAF HITTED, ROTATING PROXY[/]");
                // await _proxyRotator.RotateProxy();
                goto navigateToPage;
            }

            List<string> selectors = ["div.nav_border", "div.right_container", "div.detail_share", "div.comment_textarea"];
            foreach (var selector in selectors)
            {
                var element = await page.QuerySelectorAsync(selector);
                await page.EvaluateFunctionAsync("element => element.style.display = 'none'", element);
            }
            return page;
        }
        catch
        {
            return null;
        }
    }


    private async Task RequestInterceptor(IRequest request)
    {
        var url = request.Url;
        if (url.StartsWith("https://storage.tttang.com/"))
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(10000);
            url = $"https://web.archive.org/web/0if_/{url}";
            var backupHttpHandler = new HttpClientHandler();
            backupHttpHandler.Proxy = new WebProxy("http://127.0.0.1:7897");
            var backupHttpClient = new HttpClient(backupHttpHandler);
            try
            {
                var res = await backupHttpClient.GetByteArrayAsync(url, cts.Token);
                await request.RespondAsync(new ResponseData
                {
                    BodyData = res,
                    ContentType = "image/jpeg",
                    Status = HttpStatusCode.OK
                });
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLine($"[yellow]image from tttang error[/], {url}");
                await request.AbortAsync();
            }
        }
        
        if (!url.StartsWith("https://xz.aliyun.com/t/"))
        {
            await request.ContinueAsync();
            return;
        }

        // AnsiConsole.MarkupLine("Current Proxy: [bold]{0}[/] , Requesting: {1}", _proxyRotator.GetCurrentProxy(), request.Url);
        var httpClientHandler = new HttpClientHandler();
        // httpClientHandler.Proxy = new WebProxy(_proxyRotator.GetCurrentProxy());
        var httpClient = new HttpClient(httpClientHandler);
        var method = request.Method.Method switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "DELETE" => HttpMethod.Delete,
            _ => HttpMethod.Get
        };
        var msg = new HttpRequestMessage(method, url);
        foreach (var (key, value) in request.Headers)
        {
            msg.Headers.TryAddWithoutValidation(key, value);
        }

        if (request.HasPostData)
        {
            var postData = request.PostData;
            msg.Content = new StringContent(postData.ToString() ?? string.Empty);
        }

        HttpResponseMessage resp;
        try
        {
            var ctkSource = new CancellationTokenSource();
            ctkSource.CancelAfter(10000);
            resp = await httpClient.SendAsync(msg, ctkSource.Token);
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine("[yellow]Request Error, rotating proxy[/]");
            // await _proxyRotator.RotateProxy();
            await RequestInterceptor(request);
            return;
        }

        var content = await resp.Content.ReadAsByteArrayAsync();
        var body = Encoding.UTF8.GetString(content);

        var respHeader = resp.Headers.ToDictionary(t => t.Key, t => (object)t.Value.First());

        await request.RespondAsync(new ResponseData
        {
            Body = body,
            BodyData = content,
            Headers = respHeader,
            ContentType = resp.Content.Headers.ContentType?.ToString(),
            Status = resp.StatusCode
        });
    }
}

public class XianZhiCrawlTarget(string name, string url, string author, string crawler, string category ) : CrawlTarget
{
    public override string Name { get; set; } = name;
    public override string Url { get; } = url;
    public override string Author { get; set; } = author;
    public override string Category { get; set; } = category;
    public override string Crawler { get; } = crawler;
}