using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Text;

namespace UntapEdoConverter
{
    internal class Program
    {
        private static readonly string
            UpdateUrl = @"https://db.ygoprodeck.com/api/v7/checkDBVer.php",
            UpdateDir = @"checkDBVer.php.json",
            CardUrl = @"https://db.ygoprodeck.com/api/v7/cardinfo.php",
            CardDir = @"cardinfo.php.json";

        /// <summary>
        /// Retrieve JSON from online
        /// </summary>
        /// <param name="url">Url to retrieve JSON from</param>
        /// <returns>JSON String</returns>
        private static string WebGetJSON(string url)
        {
            string webString = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                webString = client.GetStringAsync(url).Result;
            }
            return webString;
        }

        static void Main(string[] args)
        {
            Dictionary<string, string> cards = new Dictionary<string, string>();
            string json = string.Empty;

            // Check if up to date 
            string onlineVersion = WebGetJSON(UpdateUrl);
            if (File.Exists(UpdateDir) && File.Exists(CardDir))
            {
                // Check local is up to date
                string localVersion = File.ReadAllText(UpdateDir);
                if (localVersion.Equals(onlineVersion))
                    json = File.ReadAllText(CardDir);
            }
            // Download from online host
            if (json.Equals(string.Empty))
            {
                File.WriteAllText(UpdateDir, onlineVersion);
                json = WebGetJSON(CardUrl);
                File.WriteAllText(CardDir, json);
            }

            // Find Card IDs
            foreach(JObject card in (JArray)JObject.Parse(json)["data"])
            {
                if(card.ContainsKey("name") && card.ContainsKey("id"))
                {
                    string name = card.SelectToken("name").ToString().ToLower();
                    string id = card.SelectToken("id").ToString().ToLower();
                    cards.Add(name, id);
                }
            }

            // Convert .txts to .ydks
            Console.WriteLine("Reading decks from " + Assembly.GetExecutingAssembly().Location);
            foreach(string file in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.txt"))
            {
                StringBuilder sb = new StringBuilder();
                string[] splits = file.Split("\\");
                string newFile = splits[splits.Length - 1];
                newFile = newFile.Remove(newFile.Length - 4);
                newFile = newFile + ".ydk";
                List<string> missing = new List<string>();
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        while (!sr.EndOfStream)
                        {
                            string line = sr.ReadLine();
                            if (!line.StartsWith("//") && !line.Equals(string.Empty))
                            {
                                int copies = 0;
                                if(int.TryParse(string.Empty + line[0], out copies))
                                {
                                    string name = line.Remove(0, 2).ToLower();
                                    if (cards.ContainsKey(name))
                                    {
                                        for (int i = 0; i < copies; i++)
                                        {
                                            sb.AppendLine(cards[name]);
                                        }
                                    }
                                    else
                                    {
                                        missing.Add(copies + " " + name);
                                    }
                                }
                            }
                            else if (line.Contains("sideboard"))
                            {
                                sb.AppendLine("!side");
                            }
                        }
                    }
                }
                if(sb.Length > 0)
                {
                    File.WriteAllText(newFile, sb.ToString());
                    if (File.Exists(newFile))
                    {
                        Console.Write("\nFile {0} created", newFile);
                        if (missing.Count > 0)
                        {
                            Console.WriteLine(" and is missing cards:");
                            foreach(string card in missing)
                            {
                                Console.WriteLine("\t{0}", card);
                            }
                        }
                    }
                }
            }

            Console.WriteLine("\n\nPress Enter to close");
            while (Console.ReadKey().Key != ConsoleKey.Enter) ;
        }
    }
}