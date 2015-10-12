namespace ElmahR.Core.Modules
{
    #region Imports

    using System;
    using System.Text;
    using System.Web;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Linq;
    using System.Web.Optimization;
    using System.Web.Routing;
    using Routing;
    using MoreLinq;
    using Microsoft.Ajax.Utilities;
    using Microsoft.AspNet.SignalR;
    using Extensions;
    using Dependencies;
    using Config;

    #endregion

    public static class Bootstrapper
    {
        class ConfigurableScriptBundle : Bundle
        {
            public ConfigurableScriptBundle(string virtualPath, string cdnPath) :
                this(virtualPath, new CodeSettings
                {
                    EvalTreatment = EvalTreatment.MakeImmediateSafe,
                    PreserveImportantComments = true
                }, cdnPath)
            {
                ConcatenationToken = ";\r\n";
            }

            ConfigurableScriptBundle(string virtualPath, CodeSettings codeSettings, string cdnPath) :
                base(virtualPath, cdnPath, new IBundleTransform[] { new ConfigurableJsTransform(codeSettings) })
            {
                ConcatenationToken = ";\r\n";
            }
        }

        class ConfigurableJsTransform : IBundleTransform
        {
            private readonly CodeSettings _codeSettings;

            public ConfigurableJsTransform(CodeSettings codeSettings)
            {
                _codeSettings = codeSettings;
            }

            public void Process(BundleContext context, BundleResponse response)
            {
                if (context == null)
                {
                    throw new ArgumentNullException("context");
                }
                if (response == null)
                {
                    throw new ArgumentNullException("response");
                }
                if (!context.EnableInstrumentation)
                {
                    var minifier = new Minifier();
                    var content = minifier.MinifyJavaScript(response.Content, _codeSettings);
                    if (minifier.ErrorList.Count > 0)
                    {
                        GenerateErrorResponse(response, minifier.ErrorList);
                    }
                    else
                    {
                        response.Content = content;
                    }
                }
                response.ContentType = "text/javascript";
            }

            private static void GenerateErrorResponse(BundleResponse response, IEnumerable<object> errors)
            {
                var content = new StringBuilder();
                content.Append("/* ");
                content.Append("MinifyError").Append("\r\n");
                foreach (var current in errors)
                {
                    content.Append(current).Append("\r\n");
                }
                content.Append(" */\r\n");
                content.Append(response.Content);
                response.Content = content.ToString();
            }
        }

        class ModulesResolver
        {
            [ImportMany(typeof(IModule))]
#pragma warning disable 649
            private IEnumerable<IModule> _modules;
#pragma warning restore 649

            public ModulesResolver()
            {
                var catalog = new AggregateCatalog();

                catalog.Catalogs.Add(new DirectoryCatalog(AppDomain.CurrentDomain.SetupInformation.PrivateBinPath));

                var container = new CompositionContainer(catalog);

                try
                {
                    container.ComposeParts(this);   //here we load _modules!
                }
                catch (CompositionException compositionException)
                {
                    Console.WriteLine(compositionException.ToString());
                }                
            }

            public IEnumerable<IModule> Modules { get { return _modules; } }
        }

        private static readonly ModulesResolver resolver = new ModulesResolver();
        private static Func<string, string, string> _cndRenamer;
        private static bool _skipSignalrRebase = false;

        class CoreModule : IModule
        {
            public IEnumerable<TR> DefineScripts<TF, TR>(string version, Func<string, string, string> cndRenamer, Func<string, string, TF> fileDescriptor, Func<string, IEnumerable<TF>, TR> resultor)
            {
                yield return resultor("~/Scripts/" + version + "/core", new[]
                {
                    fileDescriptor("~/Scripts/jquery-{version}.js", cndRenamer("jquery", "http://code.jquery.com/jquery-1.10.2.min.js")),
                    fileDescriptor("~/Scripts/jquery-migrate-{version}.js", cndRenamer("jquery-migrate", "http://code.jquery.com/jquery-migrate-1.0.0.min.js")),
                    fileDescriptor("~/Scripts/jquery.cookie.js", null),
                    fileDescriptor("~/Scripts/json2.js", cndRenamer("json2", "http://cdnjs.cloudflare.com/ajax/libs/json2/20121008/json2.js")),
                    fileDescriptor("~/Scripts/jquery.signalR-{version}.js", null),
                    fileDescriptor("~/Scripts/underscore.js", cndRenamer("underscore", "http://cdnjs.cloudflare.com/ajax/libs/underscore.js/1.5.1/underscore-min.js")),
                    fileDescriptor("~/Scripts/ElmahR/elmahr.core.js", null)
                });
            }

            public IEnumerable<TR> DefineStyles<TF, TR>(string version, Func<string, string, string> cndRenamer, Func<string, string, TF> fileDescriptor, Func<string, IEnumerable<TF>, TR> resultor)
            {
                yield break;
            }

            public string Name { get { return "core"; } }
        }

        class InitModule : IModule
        {
            public IEnumerable<TR> DefineScripts<TF, TR>(string version, Func<string, string, string> cndRenamer, Func<string, string, TF> fileDescriptor, Func<string, IEnumerable<TF>, TR> resultor)
            {
                yield return resultor("~/Scripts/" + version + "/init", new[]
                {
                    fileDescriptor("~/Scripts/ElmahR/elmahr.init.js", null)
                });
            }

            public IEnumerable<TR> DefineStyles<TF, TR>(string version, Func<string, string, string> cndRenamer, Func<string, string, TF> fileDescriptor, Func<string, IEnumerable<TF>, TR> resultor)
            {
                yield break;
            }

            public string Name { get { return "core"; } }
        }

        static string Mangle(string name, string suffix, Type sourceType)
        {
            return string.Format("{0}_{1}", name, string.IsNullOrWhiteSpace(suffix) ? sourceType.FullName.Replace('.', '_') : suffix);
        }

        static IEnumerable<IHtmlString> Render(string version, Func<string, string, string> cndRenamer, Func<IEnumerable<IModule>> modules, bool useCdn, Func<string, bool> modulesFilter = null)
        {
            var styles =  from r in BuildForStyle(version, cndRenamer, modules, useCdn)
                          let name = r.Path
                          where modulesFilter == null || modulesFilter(name)
                          select Styles.Render(name);
            var scripts = from r in BuildForScript(version, cndRenamer, modules, useCdn)
                          let name = r.Path
                          where modulesFilter == null || modulesFilter(name)
                          select Scripts.Render(name);

            return styles.Concat(scripts);
        }

        static IEnumerable<Bundle> BuildForScript(string version, Func<string, string, string> cndRenamer, Func<IEnumerable<IModule>> modules, bool useCdn)
        {
            var cdns =
                from m in modules()
                from d in m.DefineScripts(version, cndRenamer, (p, cdn) => new { Path = p, CdnPath = cdn }, (k, fs) => new { Key = k, Files = fs })
                from s in d.Files
                select new
                {
                    Script = s,
                    Name = Mangle(d.Key, m.Name, m.GetType())
                } into p
                group p by p.Script into g
                select new
                {
                    Script = g.Key,
                    g.First().Name
                } into g
                group g by g.Name
                into bs
                from b in bs.GroupAdjacent(arg => useCdn ? arg.Script.CdnPath : null).Index()
                let i = b.Key
                let v = b.Value
                select new
                {
                    Name = bs.Key + i,
                    CdnPath = useCdn ? v.Key : null,
                    Blocks = v
                } into f
                select new ConfigurableScriptBundle(f.Name, f.CdnPath).Include(f.Blocks.Select(x => x.Script.Path).ToArray());

            return cdns.ToArray();
        }

        static IEnumerable<Bundle> BuildForStyle(string version, Func<string, string, string> cndRenamer, Func<IEnumerable<IModule>> modules, bool useCdn)
        {
            var grouped =
                from m in modules()
                from d in m.DefineStyles(version, cndRenamer, (p, cdn) => new { Path = p, CdnPath = cdn }, (k, fs) => new { Key = k, Files = fs })
                from s in d.Files
                select new
                {
                    Script = s,
                    Name = Mangle(d.Key, null, m.GetType())
                } into p
                group p by p.Script into g
                select new
                {
                    Script = g.Key,
                    g.First().Name
                } into g
                group g by g.Name
                into bs
                    from b in bs.GroupAdjacent(arg => useCdn ? arg.Script.CdnPath : null).Index()
                let i = b.Key
                let v = b.Value
                select new
                {
                    Name = bs.Key + i,
                    CdnPath = useCdn ? v.Key : null,
                    Blocks = v
                } into f
                select new StyleBundle(f.Name, f.CdnPath).Include(f.Blocks.Select(x => x.Script.Path).ToArray());

            return grouped.ToArray();
        }

        public static IHtmlString RenderBundles(string imagesPath, string title = null, Func<string, bool> modulesFilter = null)
        {
            var version = Extensions.Version.GetCurrent();
            var sb = new StringBuilder();
            var httpRequest = HttpContext.Current.Request;

            var log = httpRequest.QueryString["log"].IsTruthy() ? "1" : "0";
            var enableDelete = Core.Hub.CanDelete ? "1" : "0";

            //core
            foreach (var htmlString in Render(version, _cndRenamer, () => Enumerable.Repeat(new CoreModule(), 1), BundleTable.Bundles.UseCdn))
                sb.Append(htmlString.ToHtmlString());

            //plugins
            foreach (var s in Plugins.InitPlugins())
                sb.Append(s);

            //startup
            sb.Append(string.Format(@"
    <script type='text/javascript'>
        elmahr.config.root               = '{0}';
        elmahr.config.signalrRoot        = '{1}';
        elmahr.config.imagesPath         = '{2}';
        elmahr.config.title              = '{3}';
        elmahr.config.log                = {4};
        elmahr.config.enableDelete       = {5};
    </script>",
                 VirtualPathUtility.ToAbsolute("~/"), VirtualPathUtility.ToAbsolute("~" + GetSignalrRoot()), imagesPath, title ?? "ElmahR", log, enableDelete));

            foreach (var htmlString in Render(version, _cndRenamer, () => resolver.Modules, BundleTable.Bundles.UseCdn, modulesFilter))
                sb.Append(htmlString.ToHtmlString());

            foreach (var htmlString in Render(version, _cndRenamer, () => Enumerable.Repeat(new InitModule(), 1), BundleTable.Bundles.UseCdn))
                sb.Append(htmlString.ToHtmlString());

            return new HtmlString(sb.ToString());
        }

        static string GetSignalrRoot()
        {
            return string.Format("/signalr{0}", _skipSignalrRebase 
                                                ? string.Empty 
                                                : Extensions.Version.GetCurrent(typeof(Hub)).Replace(".", ""));
        }

        public static void BuildBundles()
        {
            var section = HttpContext.Current.GetSection("elmahr") as RootSection;

            //BundleTable.EnableOptimizations = true;
            BundleTable.Bundles.UseCdn = section != null && section.UseCdn;

            var bundles = BundleTable.Bundles;
            var version = Extensions.Version.GetCurrent();

            foreach (var bundle in BuildForScript(version, _cndRenamer, () => Enumerable.Repeat(new CoreModule(), 1), BundleTable.Bundles.UseCdn))
                bundles.Add(bundle);

            foreach (var bundle in BuildForScript(version, _cndRenamer, () => resolver.Modules, BundleTable.Bundles.UseCdn))
                bundles.Add(bundle);

            foreach (var bundle in BuildForScript(version, _cndRenamer, () => Enumerable.Repeat(new InitModule(), 1), BundleTable.Bundles.UseCdn))
                bundles.Add(bundle);

            foreach (var bundle in BuildForStyle(version, _cndRenamer, () => resolver.Modules, BundleTable.Bundles.UseCdn))
                bundles.Add(bundle);
        }

        static void Wire(Action<IDependencyResolverBuilder> dependenciesDefiner = null)
        {
            var factory = DependencyResolverFactory.Create();
            var builder = factory.Builder;

            if (dependenciesDefiner != null)
                dependenciesDefiner(builder);
            else
                DependenciesDefiner.Define(builder);

            GlobalHost.DependencyResolver = builder.Build();
        }

        public static void PreBootstrap(
            bool enableCrossDomain = false, 
            Action<IDependencyResolverBuilder> dependenciesDefiner = null,
            bool skipSignalrRebase = false)
        {
            _skipSignalrRebase = skipSignalrRebase;

            Wire(dependenciesDefiner);

            RouteTable.Routes.MapConnection<StartupConnection>("commands", "elmahr/commands", new ConnectionConfiguration
            {
                EnableCrossDomain       = enableCrossDomain
            });

            RouteTable.Routes.MapHubs(GetSignalrRoot(), new HubConfiguration
            {
                EnableJavaScriptProxies = false,
                EnableCrossDomain       = enableCrossDomain
            });
        }

        public static void Bootstrap(Func<string, string, string> cndRenamer = null)
        {
            _cndRenamer = cndRenamer ?? ((name, path) => path);

            RegisterRoute("ElmahR/Basic", "~/elmahr/basic.cshtml");

            BuildBundles();
        }

        public static bool RegisterRoute(string routeUrl, string virtualPath)
        {
            if (RouteTable.Routes[routeUrl] != null)
                return false;

            RouteTable.Routes.MapWebPageRoute(routeUrl, virtualPath);

            return true;
        }
    }
}