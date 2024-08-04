using Crawlers;
using Depository.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PuppeteerSharp;
using Spectre.Console;

var builder = Host.CreateApplicationBuilder(args);
builder.ConfigureContainer(new DepositoryServiceProviderFactory());
builder.Services.AddSingleton(new PageSaver(@"D:\安全文章存档"));
builder.Services.AddSingleton<AbstractCrawler, XianZhiCrawler>();
builder.Services.AddSingleton<AbstractCrawler, FreebufWeb>();
var app = builder.Build();

var browser = await new Launcher().LaunchAsync(new LaunchOptions()
{
    Headless = false,
    ExecutablePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
});

var pageSaver = app.Services.GetRequiredService<PageSaver>();
AnsiConsole.MarkupLine("Checking Broken Path");
pageSaver.CheckBroken();
var crawlers = app.Services.GetServices<AbstractCrawler>();
List<Task> tasks = [];
await AnsiConsole.Progress()
    .StartAsync(async ctx =>
    {
        foreach (var crawler in crawlers)
        {
            var task = Task.Run(async () =>
            {
                var task = ctx.AddTask(crawler.Name);
                task.IsIndeterminate(true);
                var page = await crawler.StartCrawl(browser);
                var target = await crawler.GetTargets(page);
                task.IsIndeterminate(false);
                task.MaxValue(target.Count);
                for (var index = 0; index < target.Count; index++)
                {
                    task.Increment(1);
                    var crawlTarget = target[index];
                    if (await pageSaver.CheckSaved(crawlTarget.Url))
                        continue;
                    var pg = await crawler.ParseTarget(crawlTarget, page);
                    await pageSaver.SavePage(pg, crawlTarget);
                    AnsiConsole.WriteLine($"[{index}/{target.Count}] {crawlTarget.Name}");
                }
            });
            tasks.Add(task);
        }
        await Task.WhenAll(tasks.ToArray());
    });
    