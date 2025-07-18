using System.Text;
using HtmlAgilityPack;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace VK_File_Downloader;

public class MediaDownloader
{
    private static readonly string[] ImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".svg" };
    private static readonly string[] VideoExtensions = { ".mp4", ".webm" };
    private static readonly string[] AudioExtensions = { ".ogg", ".mp3", ".wav" };

    private readonly HttpClient _client = new HttpClient();
    //private readonly YoutubeDL _yt;
    private readonly Encoding _htmlEncoding = Encoding.GetEncoding("windows-1251");
    
    public MediaDownloader()
    {
        //_yt = new YoutubeDL();
        //_yt.YoutubeDLPath = YoutubeDLSharp.Utils.YtDlpBinaryName;
        //_yt.FFmpegPath = Utils.FfmpegBinaryName;
    }
    
    public async Task<int> ProcessPageAsync(string pagePath)
    {
        var doc = new HtmlAgilityPack.HtmlDocument();
        doc.Load(pagePath, _htmlEncoding);
        
        var attachmentLinks = doc.DocumentNode
            .SelectNodes("//a[contains(@class,'attachment__link')][@href]")?
            .ToList();
        if (attachmentLinks == null || attachmentLinks.Count == 0)
            return 0;

        int downloadedCount = 0;
        string pageFolder      = Path.GetDirectoryName(pagePath)!;
        string mediaRootFolder = Path.Combine(pageFolder, "Media");

        foreach (var linkNode in attachmentLinks)
        {
            string url = linkNode.GetAttributeValue("href", "");
            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                continue;

            string fileName = Path.GetFileName(uri.LocalPath);
            string ext      = Path.GetExtension(fileName).ToLowerInvariant();
            
            string subDir;
            if (ImageExtensions.Contains(ext))  subDir = "Photo";
            else if (VideoExtensions.Contains(ext)) subDir = "Video";
            else if (AudioExtensions.Contains(ext)) subDir = "Audio";
            else                                  subDir = "Other";

            string targetFolder = Path.Combine(mediaRootFolder, subDir);
            Directory.CreateDirectory(targetFolder);

            string localPath    = Path.Combine(targetFolder, fileName);
            string relativePath = Path.Combine("Media", subDir, fileName).Replace('\\','/');
            
            if (!File.Exists(localPath))
            {
                try
                {
                    var data = await _client.GetByteArrayAsync(uri);
                    await File.WriteAllBytesAsync(localPath, data);
                    downloadedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Download error {url}: {ex.Message}");
                    continue;
                }
            }
            
            HtmlAgilityPack.HtmlNode newNode;
            if (ImageExtensions.Contains(ext))
            {
                newNode = HtmlAgilityPack.HtmlNode.CreateNode($@"<img src=""{relativePath}"" alt=""{fileName}"" />");
            }
            else if (VideoExtensions.Contains(ext))
            {
                if (ext.Equals(".mp4") || url.Contains("vk.com/video"))
                {
                    string outName   = Path.ChangeExtension(fileName, ".mp4");
                    string relPath   = Path.Combine("Media", subDir, outName).Replace('\\','/');

                    if (!File.Exists(localPath))
                    {
                        bool ok = await DownloadVideoAsync(url, localPath);
                        if (!ok) continue;
                        downloadedCount++;
                    }

                    // создаём тег video
                    var videoNode = HtmlNode.CreateNode(
                        $@"<video controls src=""{relPath}"">Your browser does not support video.</video>");
                    linkNode.ParentNode.ReplaceChild(videoNode, linkNode);
                }
                newNode = HtmlAgilityPack.HtmlNode.CreateNode(
                    $@"<video controls src=""{relativePath}"">Your browser does not support video.</video>");
            }
            else if (AudioExtensions.Contains(ext))
            {
                newNode = HtmlAgilityPack.HtmlNode.CreateNode(
                    $@"<audio controls src=""{relativePath}"">Your browser does not support audio</audio>");
            }
            else
            {
                linkNode.SetAttributeValue("href", relativePath);
                continue;
            }
            
            linkNode.ParentNode.ReplaceChild(newNode, linkNode);
        }
        
        doc.Save(pagePath, _htmlEncoding);
        return downloadedCount;
    }
    
    private async Task<bool> DownloadVideoAsync(string url, string outputPath)
    {
        return false;
        // var opts = new OptionSet()
        // {
        //     Output        = outputPath,
        //     Format        = "bestvideo+bestaudio",
        //     MergeOutputFormat = DownloadMergeFormat.Mp4
        // };
        //
        // var result = await _yt.RunVideoDownload(url,overrideOptions:opts);
        // if (!result.Success)
        //     Console.WriteLine($"Error yt-dlp: {result.ErrorOutput}");
        // return result.Success;
    }
}