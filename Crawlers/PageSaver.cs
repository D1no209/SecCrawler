﻿using LiteDB;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using Spectre.Console;

namespace Crawlers;

public class PageSaver
{
    private readonly string _root;
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<CrawledPage> _crawlerTargets;
    private readonly ILiteCollection<CrawlTarget> _crawlerMarkedTarget;

    public Task ClearCrawler(string crawler)
    {
        _crawlerMarkedTarget.DeleteMany(t => t.Crawler == crawler);
        _crawlerTargets.DeleteMany(t => t.Crawler == (crawler));
        return Task.CompletedTask;
    } 
    
    public PageSaver(string root)
    {
        _root = root;
        _db = new LiteDatabase("crawl.db");
        _crawlerTargets = _db.GetCollection<CrawledPage>();
        _crawlerMarkedTarget = _db.GetCollection<CrawlTarget>();
    }
    public Task MarkTarget(CrawlTarget target)
    {
        _crawlerMarkedTarget.Insert(target);
        return Task.CompletedTask;
    }
    
    public  Task<List<CrawlTarget>> GetMarkedTargetsByCrawler(string crawler)
    {
        return Task.FromResult(_crawlerMarkedTarget.Query().Where(t => t.Crawler == crawler).ToList());
    }
    
    public Task<bool> CheckSaved(string url)
    {
        return Task.FromResult(_crawlerTargets.Exists(t => t.Url == url));
    }

    public void CheckBroken()
    {
        /*
         // Recover from wrong removed broken path
        // merge _crawlerTargets and _crawlerMarkedTarget
        var urls = _crawlerTargets.FindAll().Select(t => t.Url).ToList();
        foreach (var crawlMarkedTarget in _crawlerMarkedTarget.FindAll())
        {
            if (urls.Contains(crawlMarkedTarget.Url))
                continue;
            var filepath = Path.Combine(_root,
                $"{crawlMarkedTarget.Crawler}/{NormalizeFileName(crawlMarkedTarget.Name)} by {NormalizeFileName(crawlMarkedTarget.Author)}.mhtml");
            _crawlerTargets.Insert(new CrawledPage(crawlMarkedTarget.Name, crawlMarkedTarget.Url, crawlMarkedTarget.Author, filepath, crawlMarkedTarget.Crawler));
        }
        */
        var path = _crawlerTargets.Query().Select(t => t.Path.Replace("/","\\")).ToList();
        var home = path.GroupBy(Path.GetDirectoryName).ToList();
        foreach (var grouping in home)
        {
            if (string.IsNullOrWhiteSpace(grouping.Key)) continue;
            if (!Directory.Exists(grouping.Key))
            {
                AnsiConsole.MarkupLine("[yellow]Broken Path: {0}[/]", grouping.Key.EscapeMarkup());
                path.Remove(grouping.Key);
                continue;
            }
            var actualFiles = Directory.EnumerateFiles(grouping.Key);
            foreach (var actualFile in actualFiles)
            {
                path.Remove(actualFile);
            }
        }
        
        _crawlerTargets.DeleteMany(t => path.Contains(t.Path.Replace("/", "\\")));
        foreach (var brokenPath in path)
        {
            AnsiConsole.MarkupLine("[yellow]Broken Path: {0}[/]", brokenPath.EscapeMarkup());
        }
    }
    
    public async Task SavePage(IPage? page, CrawlTarget crawlTarget)
    {
        if (page == null)
            return;
        Directory.CreateDirectory(Path.Combine(_root, $"{crawlTarget.Crawler}"));
        Directory.CreateDirectory(Path.Combine(_root, $"{crawlTarget.Crawler}-pdf"));
        var name = crawlTarget.Name;
        // replace invalid characters with fullwidth characters
        name = NormalizeFileName(name);
        var author = NormalizeFileName(crawlTarget.Author);
        var saveto = Path.Combine(_root, $"{crawlTarget.Crawler}/{name} by {author}.mhtml");
        var cdpSession = await page.CreateCDPSessionAsync();
        var pageContent = await cdpSession.SendAsync<JObject>("Page.captureSnapshot");
        await File.WriteAllTextAsync(saveto, pageContent.Value<string>("data"));
        
        var pdfPath = Path.Combine(_root, $"{crawlTarget.Crawler}-pdf/{name} by {author}.pdf");
        await page.PdfAsync(pdfPath);
        _crawlerTargets.Insert(new CrawledPage(crawlTarget.Name, crawlTarget.Url, crawlTarget.Author, saveto, crawlTarget.Crawler));
    }
    
    public static string NormalizeFileName(string name)
    {
        return name.Replace(":", "：")
            .Replace("/", "／")
            .Replace("\\", "＼")
            .Replace("*", "＊")
            .Replace("?", "？")
            .Replace("\"", "＂")
            .Replace("<", "＜")
            .Replace(">", "＞")
            .Replace("|", "｜")
            .Replace("\n", " ")
            .Replace("\r", " ")
            .Replace("\t", " ");
    }
}

public record CrawledPage(string Name, string Url,string Author, string Path, string Crawler);