namespace ElmahR.Core
{
    #region Imports

    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Web;
    using System.Web.Caching;
    using RazorEngine.Templating;
    using RazorEngine.Configuration;

    #endregion

    public abstract class PluginTemplate<T> : TemplateBase<T>
    {
        public string Href(string path)
        {
            return VirtualPathUtility.ToAbsolute(path);
        }
    }

    public static class Plugins
    {
        const string AppDataPlugins = "~/App_Data/Plugins";

        static Plugins()
        {
            RazorEngine.Razor.SetTemplateService(new TemplateService(new TemplateServiceConfiguration { BaseTemplateType = typeof(PluginTemplate<>) }));
        }

        public static IEnumerable<string> InitPlugins()
        {
            var folder = MapPath(AppDataPlugins);
            return
                from f in (Directory.Exists(folder) ? new DirectoryInfo(folder).GetFiles() : Enumerable.Empty<FileInfo>())
                let content = Parse(f.FullName)
                select content;
        }

        public static IEnumerable<KeyValuePair<string, KeyValuePair<string, string>>> BuildPlugins()
        {
            var folder = MapPath(AppDataPlugins);
            return
                from d in (Directory.Exists(folder) ? new DirectoryInfo(folder).EnumerateDirectories() : Enumerable.Empty<DirectoryInfo>())
                from f in d.GetFiles()
                let content = Parse(f.FullName)
                select new KeyValuePair<string, KeyValuePair<string, string>>(
                    d.Name,
                    new KeyValuePair<string, string>(f.Name.Replace(f.Extension, string.Empty), content));
        }

        private static string MapPath(string path)
        {
            return HttpContext.Current.Server.MapPath(path);
        }

        private static string Parse(string path)
        {
            var cache = HttpContext.Current.Cache;

            if (cache[path] == null)
            {
                cache.Insert(path,
                    RazorEngine.Razor.Parse(File.ReadAllText(path)), 
                    new CacheDependency(path));
            }

            return cache[path].ToString();
        }
    }
}