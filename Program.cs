using System.Text;
using HtmlAgilityPack;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace VK_File_Downloader;

class Program
    {
        static async Task Main(string[] args)
        {
            //await YoutubeDLSharp.Utils.DownloadYtDlp();
            //await YoutubeDLSharp.Utils.DownloadFFmpeg();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.Write("Путь к папке с index.html: ");
            var folder = Console.ReadLine()!;
            var indexPath = Path.Combine(folder, "index.html");
            if (!File.Exists(indexPath))
            {
                Console.WriteLine("Файл index.html не найден.");
                return;
            }
            // Собираем все внутренние ссылки на HTML-страницы
            var pages = CollectAllPages(indexPath, folder);
            Console.WriteLine($"Найдено {pages.Count} страниц для обработки.");

            var downloader = new MediaDownloader();
            foreach (var page in pages)
            {
                Console.WriteLine($"Обрабатываю {Path.GetFileName(page)} ...");
                var count = await downloader.ProcessPageAsync(page);
                Console.WriteLine($" → скачано и заменено {count} ресурсов.");
            }

            Console.WriteLine("Готово.");
        }

        // Собираем все *.html, на которые ссылаются через <a href>
        private static HashSet<string> CollectAllPages(string startPage, string rootFolder)
        {
            var toVisit = new Queue<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            toVisit.Enqueue(startPage);
            visited.Add(startPage);

            while (toVisit.Count > 0)
            {
                var page = toVisit.Dequeue();
                var doc = new HtmlDocument();
                doc.Load(page);

                var links = doc.DocumentNode
                    .SelectNodes("//a[@href]")
                    ?.Select(n => n.GetAttributeValue("href", ""))
                    .Where(h => h.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                                && !h.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (links == null) continue;

                foreach (var href in links)
                {
                    var full = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(page)!, href));
                    if (File.Exists(full) && visited.Add(full))
                    {
                        toVisit.Enqueue(full);
                    }
                }
            }

            return visited;
        }

        private static async void TestDownload(string outputPath)
        {
            var _yt = new YoutubeDL();
            _yt.YoutubeDLPath = YoutubeDLSharp.Utils.YtDlpBinaryName;
            _yt.FFmpegPath = Utils.FfmpegBinaryName;
            _yt.OutputFolder = outputPath;
            
            var opts = new OptionSet()
            {
                MergeOutputFormat = DownloadMergeFormat.Mp4
            };

            var result = await _yt.RunVideoDownload("https://vk.com/video526616429_456240161",overrideOptions:opts);
            if (!result.Success)
                Console.WriteLine($"Ошибка yt-dlp: {result.ErrorOutput}");
            Console.WriteLine(result.Data);
        }
    }