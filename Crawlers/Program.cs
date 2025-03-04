using Crawlers;
using Depository.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PuppeteerSharp;
using Spectre.Console;

var builder = Host.CreateApplicationBuilder(args);
builder.ConfigureContainer(new DepositoryServiceProviderFactory());
builder.Services.AddSingleton(new PageSaver(@"/articles"));
builder.Services.AddSingleton<IProxyRotator, SimpleProxyRotator>();
// builder.Services.AddSingleton<AbstractCrawler, XianZhiCrawler>();
// builder.Services.AddSingleton<AbstractCrawler, FreebufWeb>();
// builder.Services.AddSingleton<AbstractCrawler, TiaoTiaoTangCrawler>();
builder.Services.AddSingleton<AbstractCrawler, UrlListCrawler>();
var app = builder.Build();

var browser = await new Launcher().LaunchAsync(new LaunchOptions()
{
    Headless = false,
    ExecutablePath = Environment.GetEnvironmentVariable("CHROME_PATH") ?? @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    Args =
    [
        "--no-sandbox",
        "--disable-dev-shm-usage",
        "--disable-gpu",
        "--disable-extensions",
        "--disable-software-rasterizer",
        "--disable-setuid-sandbox",
        "--ignore-certificate-errors",
        "--media-cache-size=1",
        "--disk-cache-size=1",
        "--disable-features=HttpsUpgrades"
    ]
});

var pageSaver = app.Services.GetRequiredService<PageSaver>();
AnsiConsole.MarkupLine("Checking Broken Path");
// pageSaver.CheckBroken();
var crawlers = app.Services.GetServices<AbstractCrawler>();
List<Task> tasks = [];
var parallel = 10;
await AnsiConsole.Progress()
    .StartAsync(async ctx =>
    {
        await Parallel.ForEachAsync(crawlers, async (crawler, token) =>
        {
            {
                var task = ctx.AddTask(crawler.Name);
                task.IsIndeterminate();

                await crawler.StartCrawl();
                var target = await crawler.GetTargets(await crawler.NewPage(browser));
                task.IsIndeterminate(false);
                task.MaxValue(target.Count);

                await Parallel.ForAsync(0, parallel, async (i, token) =>
                {
                    var page = await crawler.NewPage(browser);
                    for (var index = i; index < target.Count; index += parallel)
                    {
                        task.Increment(1);
                        var crawlTarget = target[index];
                        if (await pageSaver.CheckSaved(crawlTarget.Url))
                            continue;
                        var pg = await crawler.ParseTarget(crawlTarget, page);
                        await pageSaver.SavePage(pg, crawlTarget);
                        AnsiConsole.WriteLine($"[{crawlTarget.Crawler}][{index}/{target.Count}] {crawlTarget.Name}");
                    }
                });
            }
        });
    });