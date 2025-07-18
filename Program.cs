using System.Text;
using HtmlAgilityPack;

namespace VK_File_Downloader;

class Program
    {
        static async Task Main(string[] args)
        {
            
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.Write("Path to folder with index.html: ");
            var folder = Console.ReadLine()!;
            var indexPath = Path.Combine(folder, "index.html");
            if (!File.Exists(indexPath))
            {
                Console.WriteLine("File index.html not found.");
                return;
            }
            var pages = CollectAllPages(indexPath, folder);
            Console.WriteLine($"Found {pages.Count} page for proceed.");

            var downloader = new MediaDownloader();
            foreach (var page in pages)
            {
                Console.WriteLine($"Processed {Path.GetFileName(page)} ...");
                var count = await downloader.ProcessPageAsync(page);
                Console.WriteLine($" → Downloaded and replaced {{count}} resources.");
            }

            Console.WriteLine("Done.");
        }
        
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
    }