﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web;
using Newtonsoft.Json;

namespace ReflexMaps
{
    class Program
    {
        private static string DefaultSource = "http://reflex.abusing.me/api/";
        private static string TargetPath = Path.Combine(AssemblyDirectory, @"base\internal\maps");

        private class Config
        {
            public Config()
            {
                LastUpdate = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
                SourceUrl = DefaultSource;
                MapPath = TargetPath;
                MapVersions = new Dictionary<string, DateTimeOffset>();
            }

            public DateTimeOffset LastUpdate { get; set; }
            public string SourceUrl { get; set; }
            public string MapPath { get; set; }
            public Dictionary<string, DateTimeOffset> MapVersions { get; set; }
        }

        private class Response
        {
            public class MapInfo 
            {
                public string Url { get; set; }
                public DateTimeOffset LastUpdated { get; set; }
            }

            public DateTimeOffset Now { get; set; }
            public IEnumerable<MapInfo> ToUpdate { get; set; }
        }

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("- To get _ALL_ maps run the following command");
                Console.WriteLine("    ReflexMaps all");
                Console.WriteLine("- To get stared map / stared user's map run the following command");
                Console.WriteLine("    ReflexMaps <id>");
                return;
            }

            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var settingFile = Path.Combine(homeDir, ".reflexMaps");

            var config = new Config();
            if (File.Exists(settingFile))
            {
                try
                {
                    var json = File.ReadAllText(settingFile);
                    config = JsonConvert.DeserializeObject<Config>(json);
                }
                catch
                {
                    config = new Config();
                }
            }

            var url = config.SourceUrl;
            var isAll = args[0] == "all";
            if (!isAll)
            {
                url += args[0];
            }
            else
            {
                url += "?since=" +
                       HttpUtility.UrlEncode(JsonConvert.SerializeObject(config.LastUpdate).Replace("\"", ""));
            }

            var webClient = new WebClient();
            var body = webClient.DownloadString(url);
            var response = JsonConvert.DeserializeObject<Response>(body);
            Console.WriteLine("found {0} updates", response.ToUpdate.Count());

            foreach (var mapInfo in response.ToUpdate)
            {
                var filename = Path.GetFileName(mapInfo.Url);
                var targetPath = Path.Combine(config.MapPath, filename);

                if (config.MapVersions.ContainsKey(mapInfo.Url) &&
                    config.MapVersions[mapInfo.Url] == mapInfo.LastUpdated)
                {
                    Console.WriteLine("already latest version of {0}", mapInfo.Url);
                    continue;
                }

                Console.WriteLine("downloading {0} to {1}", mapInfo.Url, targetPath);

                webClient.DownloadFile(new Uri(mapInfo.Url), targetPath);
                config.MapVersions[mapInfo.Url] = mapInfo.LastUpdated;
            }
            Console.WriteLine("storing update information");

            if (isAll)
            {
                config.LastUpdate = response.Now;
            }

            File.WriteAllText(settingFile, JsonConvert.SerializeObject(config));
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}
