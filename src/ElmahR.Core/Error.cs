namespace ElmahR.Core
{
    #region Imports

    using System;
    using System.Web;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Microsoft.AspNet.SignalR;
    using Microsoft.AspNet.SignalR.Infrastructure;
    using Newtonsoft.Json;
    using Elmah;
    using Config;

    #endregion

    public class Error
    {
        public const string ElmahRSourceId     = "###ElmahR###";

        const string BrowserSupportUrlTemplate = "{0}.gif";

        private Application _application;

        private string _jsonError;

        public string Id { get; set; }

        public string Host { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public string Detail { get; set; }
        public string User { get; set; }
        public string StatusCode { get; set; }
        public DateTime Time { get; set; }
        public string Url { get; set; }

        public string WebHostHtmlMessage { get; set; }
        public string Source { get; set; }
        public IDictionary<string, string> ServerVariables { get; set; }
        public IDictionary<string, string> Form { get; set; }
        public IDictionary<string, object> Cookies { get; set; }

        public string ShortType { get; set; }
        public string BrowserSupportUrl { get; set; }
        public string IsoTime { get; set; }
        public bool HasYsod { get; set; }

        public bool IsFull { get; set; }

        public static Error Build(string jsonError, string errorId, Application application)
        {
            if (application == null) throw new ArgumentNullException("application");

            var e = JsonConvert.DeserializeObject<Error>(jsonError);

            e.Id = errorId;
            e._jsonError = jsonError;
            e._application = application;

            SetShortType(e);

            if (e.ServerVariables != null && e.ServerVariables.ContainsKey("HTTP_USER_AGENT"))
            {
                var userAgent = e.ServerVariables["HTTP_USER_AGENT"];

                e.BrowserSupportUrl = userAgent.IndexOf("ELMAHR", StringComparison.OrdinalIgnoreCase) >= 0
                                    ? string.Format(BrowserSupportUrlTemplate, "skull")
                                    : userAgent.IndexOf("MSIE", StringComparison.OrdinalIgnoreCase) >= 0
                                    ? string.Format(BrowserSupportUrlTemplate, "compatible_ie")
                                    : userAgent.IndexOf("Chrome", StringComparison.OrdinalIgnoreCase) >= 0
                                    ? string.Format(BrowserSupportUrlTemplate, "compatible_chrome")
                                    : userAgent.IndexOf("Firefox", StringComparison.OrdinalIgnoreCase) >= 0
                                    ? string.Format(BrowserSupportUrlTemplate, "compatible_firefox")
                                    : userAgent.IndexOf("Safari", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      userAgent.IndexOf("AppleWebKit", StringComparison.OrdinalIgnoreCase) >= 0
                                    ? string.Format(BrowserSupportUrlTemplate, "compatible_safari")
                                    : userAgent.IndexOf("Opera", StringComparison.OrdinalIgnoreCase) >= 0
                                    ? string.Format(BrowserSupportUrlTemplate, "compatible_opera")
                                    : string.Format(BrowserSupportUrlTemplate, "questionmark");
            }
            else
                e.BrowserSupportUrl = string.Format(BrowserSupportUrlTemplate, "questionmark");

            e.IsoTime = e.Time.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

            e.IsFull = true;

            if (!string.IsNullOrWhiteSpace(e.WebHostHtmlMessage))
            {
                var section = HttpContext.Current.GetSection("elmahr") as RootSection;

                e.Url = string.Format("{0}?id={1}",
                                      section != null && !string.IsNullOrWhiteSpace(section.Ysod)
                                      ? section.Ysod
                                      : "YellowScreenOfDeath.axd", 
                                      errorId);
                e.HasYsod = true;
            }

            return e;
        }

        public Error BuildSynthetic()
        {
            var e = this;
            return new Error
            {
                _application = e._application,
                Id = e.Id,
                Host = e.Host,
                Type = e.Type,
                Message = e.Message,
                User = e.User,
                StatusCode = e.StatusCode,
                Time = e.Time,
                HasYsod = e.HasYsod,
                Url = e.Url,
                BrowserSupportUrl = e.BrowserSupportUrl,
                IsoTime = e.IsoTime,
                ShortType = e.ShortType
            };
        }

        public Application GetApplication()
        {
            return _application;
        }

        public string GetJsonError()
        {
            return _jsonError;
        }

        public static Error ProcessError(string sourceId, Exception ex, string infoUrlPath, Action<Exception> errorCatcher, bool synthetic = false)
        {
            return Process(sourceId, 
                infoUrlPath,
                source =>
                {
                    var jsonError = ex.AsJson(null);
                    var errorId = Guid.NewGuid().ToString();
                    return Build(jsonError, errorId, source);        
                }, 
                synthetic);

        }

        public static Error ProcessAndAppendError(string sourceId, Exception ex, string infoUrlPath, Action<Exception> errorCatcher, bool synthetic = false)
        {
            return Process(sourceId, 
                infoUrlPath,
                source =>
                {
                    var jsonError = ex.AsJson(null);
                    var errorId = Guid.NewGuid().ToString();
                    return source.AppendError(jsonError, errorId, errorCatcher);        
                }, 
                synthetic);
        }

        public static Error ProcessError(string sourceId, string errorId, string jsonError, string infoUrlPath, Action<Exception> errorCatcher, bool synthetic = false)
        {
            return Process(sourceId,
                infoUrlPath,
                source => Build(jsonError, errorId, source),
                synthetic);
        }

        public static Error ProcessAndAppendError(string sourceId, string errorId, string jsonError, string infoUrlPath, Action<Exception> errorCatcher, bool synthetic = false)
        {
            return Process(sourceId,
                infoUrlPath,
                source => source.AppendError(jsonError, errorId, errorCatcher),
                synthetic);
        }

        public Envelope WrapIt()
        {
            return WrapIt(string.Empty);
        }

        public Envelope WrapIt(string className)
        {
            var source = GetApplication();

            return new Envelope
            {
                Id = source.Id,
                ApplicationName = source.ApplicationName,
                Error = this,
                InfoUrl = source.InfoUrl,
                SourceId = source.SourceId,
                Class = className
            };
        }

        static Error Process(string sourceId, string infoUrlPath, Func<Application, Error> builder, bool synthetic = false)
        {
            var apps = GlobalHost.DependencyResolver.Resolve<IApplications>();

            var source = apps[sourceId];
            if (source == null)
                return null;

            var connectionManager = GlobalHost.DependencyResolver.Resolve<IConnectionManager>();
            var hubContext = connectionManager.GetHubContext<Hub>();

            if (!string.IsNullOrWhiteSpace(infoUrlPath))
                source.SetInfoUrl(infoUrlPath);

            var payload = builder(source);

            if (synthetic)
                payload = payload.BuildSynthetic();
            var envelope = payload.WrapIt();

            hubContext.Clients
                      .Group(envelope.SourceId)
                      .notifyErrors(new[] { envelope });

            return payload;
        }

        static void SetShortType(Error e)
        {
            e.ShortType = GetShortType(e.Type);
        }

        public static string GetShortType(string type)
        {
            var match = Regex.Match(type, @"(\w+\.)+(?'type'\w+)Exception");
            return match.Success
                 ? match.Groups["type"].Value
                 : type;
        }
    }
}