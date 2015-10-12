namespace ElmahR.Core
{
    #region Imports

    using System.Threading.Tasks;
    using System.Configuration;
    using System.Linq;
    using System;
    using System.IO;
    using System.Net;
    using System.Collections.Generic;
    using Microsoft.AspNet.SignalR;
    using Microsoft.AspNet.SignalR.Hubs;
    using Microsoft.AspNet.SignalR.Infrastructure;
    using Services;
    using Elmah;
    using Extensions;

    #endregion

    // ElmahR hub

    [HubName("elmahr")]
    public class Hub : Microsoft.AspNet.SignalR.Hub
    {
        private readonly IApplications _apps;
        private readonly IUserService _userService;
        private const int ChunkSize = 25;
        private const string LogGroup = "###LOG###";

        public Hub(IApplications apps, IUserService userService)
        {
            _apps = apps;
            _userService = userService;
        }
        
        public enum Severity
        {
            Info,
            Warning,
            Critical
        }

        public static Task Log(string message, Severity severity = Severity.Info)
        {
            var connectionManager = GlobalHost.DependencyResolver.Resolve<IConnectionManager>();
            var hubContext = connectionManager.GetHubContext<Hub>();
            return hubContext.Clients.Group(LogGroup).log(new
            {
                Message  = message,
                Date     = string.Format("{0:dd-MMM-yyyy HH:mm:ss}", DateTime.UtcNow),
                Severity = Enum.GetName(typeof(Severity), severity)
            });
        }

        public void Connect()
        {
            Exception.Catch(_apps, () =>
            {
                var apps = GetApps((_, __, sid, ___, active, ____) => new
                {
                    SourceId = sid,
                    Active = active
                }).ToArray();

                foreach (var app in apps.Where(a => a.Active))
                {
                    Clients.All.log(string.Format("Connection {0} subscribing to {1}...", Context.ConnectionId, app.SourceId));
                    Groups.Add(Context.ConnectionId, app.SourceId);
                }
            });
        }

        public void AskForApplications()
        {
            Exception.Catch(_apps, () =>
            {
                var apps = GetApps((an, iu, sid, hteu, active, props) => new
                {
                    ApplicationName = an,
                    InfoUrl = iu,
                    SourceId = sid,
                    Active = active,
                    HasTestExceptionUrl = hteu,
                    Properties = props
                }).ToArray();

                Clients.Caller.advertiseApplications(apps);
            });
        }

        public void RetrieveHistoricalErrors()
        {
            Exception.Catch(_apps, () =>
            {
                var apps = GetApps((_, __, sid, ___, active, ____) => new
                {
                    SourceId = sid,
                    Active = active
                }).ToArray();

                var envs =
                    from error in _apps.GetErrors(ChunkSize, apps.Where(k => k.Active)
                                                                 .Select(k => k.SourceId)
                                                                 .ToArray())
                    let source = error.GetApplication()
                    select error.BuildSynthetic().WrapIt("onReconnect");

                envs = envs.OrderBy(e => e.Error.Time);

                Clients.Caller.notifyErrors(envs.ToArray(), false, true);

                Clients.Caller.notifyRememberMe(_userService.GetRememberMeStatus((string)Clients.Caller.rememberMe));
            });
        }

        public void RetrieveFullErrorsStats()
        {
            Exception.Catch(_apps, () =>
            {
                var apps = from k in GetApps((name, __, sid, ___, active, ____) => new
                {
                    ApplicationName = name,
                    SourceId = sid,
                    Active = active
                })
                where k.Active
                select new { k.SourceId, k.ApplicationName };
                apps = apps.ToArray();

                var appsByName = apps.ToDictionary(v => v.SourceId, v => v.ApplicationName);

                var errorsStats = _apps.GetErrorsStats(apps.Select(a => a.SourceId).ToArray(), (aid, t, r, c) => new
                {
                    SourceId = aid, 
                    Type = t, 
                    ReceivedAt = r, 
                    Count = c
                });

                var grouped = 
                    from stat in errorsStats
                    group stat by new {stat.SourceId, stat.Type}
                    into g
                    select new
                    {
                        g.Key.SourceId,
                        ApplicationName = appsByName[g.Key.SourceId],
                        g.Key.Type,
                        ShortType = Error.GetShortType(g.Key.Type),
                        Count = g.Sum(s => s.Count)
                    };

                grouped = grouped.ToArray();

                Clients.Caller.notifyFullErrorsStats(grouped);
            });
        }

        public void AskForMoreErrors(string fromId)
        {
            Exception.Catch(_apps, () =>
            {
                var apps = GetApps((_, __, sid, ___, active, ____) => new
                {
                    SourceId = sid,
                    Active = active
                }).ToArray();

                var envs =
                    from error in _apps.GetErrors(ChunkSize, fromId, apps.Where(k => k.Active)
                                                                         .Select(k => k.SourceId)
                                                                         .ToArray())
                    let source = error.GetApplication()
                    orderby error.Time descending
                    select error.WrapIt("onReconnect");

                Clients.Caller.notifyErrors(envs.ToArray(), true);
            });
        }

        public void SortApplications(string[] applicationIds)
        {
            Exception.Catch(_apps, () => _userService.SortApplications((string)Clients.Caller.rememberMe, applicationIds));
        }

        public void RaiseTestException(string sourceId)
        {
            Exception.Catch(_apps, () =>
            {
                var app = (from source in _apps
                           where source.SourceId == sourceId
                           select source).FirstOrDefault();
                if (app == null || app.TestExceptionUrl == null)
                    return;

                var wc = new WebClient();
                wc.Headers.Add("User-Agent", string.Format("ELMAHR-{0}", sourceId));

                Task.Factory.StartNew(() => wc.DownloadStringAsync(new Uri(app.TestExceptionUrl)));
            });
        }

        public void GetErrorsResume(string sourceId)
        {
            Exception.Catch(_apps, () =>
            {
                var resume = _apps.GetErrorsResume(sourceId);

                Clients.Caller.notifyErrorsResume(sourceId, resume);
            });
        }

        public void ClearAllErrors(string sourceId)
        {
            if (!CanDelete)
                return;
            Exception.Catch(_apps, () => _apps.ClearErrorsBefore(sourceId, DateTime.UtcNow.AddDays(1)), true);
        }

        public void ClearErrorsBefore(string sourceId, DateTime when)
        {
            if (!CanDelete)
                return;
            Exception.Catch(_apps, () => _apps.ClearErrorsBefore(sourceId, when), true);
        }

        public void ClearErrorsAfter(string sourceId, DateTime when)
        {
            if (!CanDelete)
                return;
            Exception.Catch(_apps, () => _apps.ClearErrorsAfter(sourceId, when), true);
        }

        public void AskFullError(string errorId, string sourceId)
        {
            Exception.Catch(_apps, () =>
            {
                var error = _apps.GetError(errorId);

                if (error != null)
                {
                    Clients.Caller.notifyFullError(error.WrapIt());
                    return;
                }

                var specificSource = (from s in _apps where s.SourceId == sourceId select s).FirstOrDefault();
                if (specificSource == null)
                {
                    Clients.Caller.notifyFullError(null);
                    return;
                }

                try
                {
                    var url = new Uri(string.Format("{0}/json?id={1}", specificSource.InfoUrl, errorId));
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, null)
                        .ContinueWith(response =>
                        {
                            try
                            {
                                using (var stream = response.Result.GetResponseStream())
                                {
                                    if (stream == null)
                                        return;

                                    using (var sr = new StreamReader(stream))
                                    {
                                        error = Error.Build(sr.ReadToEnd(), errorId, specificSource);
                                        Clients.Caller.notifyFullError(error.WrapIt());
                                    }
                                }
                            }
                            catch (WebException)
                            {
                                //just go on with a null envelope
                                Clients.Caller.notifyFullError(null);
                            }
                        });
                }
                catch (WebException)
                {
                    //just go on with a null envelope
                    Clients.Caller.notifyFullError(null);
                }
            });
        }

        public void ToggleApplicationStatus(string sourceId, bool active)
        {
            Exception.Catch(_apps, () =>
            {
                var status = _userService.ToggleApplicationStatus((string)Clients.Caller.rememberMe, sourceId, active);

                if (active)
                    Groups.Add(Context.ConnectionId, sourceId);
                else
                    Groups.Remove(Context.ConnectionId, sourceId);

                Clients.Caller.notifyApplicationStatus(sourceId, status);
            });
        }

        public void ToggleLog()
        {
            Exception.Catch(_apps, () =>
            {
                Groups.Add(Context.ConnectionId, LogGroup);

                _apps.Banner(message => Clients.Caller.log(new
                {
                    Message = message,
                    Date = string.Format("{0:dd-MMM-yyyy HH:mm:ss}", DateTime.UtcNow),
                    Severity = Severity.Info
                }));
            });
        }

        public static bool CanDelete
        {
            get { return string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["EnableDelete"]) || ConfigurationManager.AppSettings["EnableDelete"].IsTruthy(); }
        }

        IEnumerable<T> GetApps<T>(Func<string, string, string, bool, bool, IDictionary<string, string>, T> resultor)
        {
            var appsResume = 
                from source in _apps
                where source.SourceId == _apps.SelfSourceId
                select new
                {
                    source.SourceId,
                    Active = true,
                    Index = 0
                };
            appsResume = appsResume.ToArray();

            try
            {
                var userKey = (string)Clients.Caller.rememberMe;
                var appsResumeWithState = _userService.GetApplications(userKey, (sid, v, i) => new
                {
                    SourceId = sid,
                    Active = "true".Equals(v, StringComparison.OrdinalIgnoreCase),
                    Index = i
                });
                appsResumeWithState = appsResumeWithState.ToArray();

                appsResume = appsResumeWithState;
            }
            catch (System.Exception ex)
            {
                var type = ex.GetType();
                if (typeof(OutOfMemoryException) == type)
                    throw;
            }

            return from source in _apps
                   join s in appsResume
                   on source.SourceId equals s.SourceId into stata
                   from x in stata.DefaultIfEmpty()
                   orderby x == null ? 0 : x.Index
                   select resultor(
                       source.ApplicationName,
                       source.InfoUrl,
                       source.SourceId,
                       source.TestExceptionUrl != null,
                       x == null || x.Active,
                       source.Properties.ToDictionary(k => k, source.GetValue, StringComparer.OrdinalIgnoreCase));
        }

        static class Exception
        {
            public static void Catch(IApplications apps, Action action, bool rethrow = false)
            {
                Catch(apps, (Func<object>)(() =>
                {
                    action();
                    return null;
                }), rethrow);
            }

            public static T Catch<T>(IApplications apps, Func<T> func, bool rethrow = false)
            {
                var r = default(T);

                try
                {
                    r = func();
                }
                catch (System.Exception error)
                {
                    Log(string.Format("An error occurred in ElmahR: {0}", error), Severity.Critical);

                    try
                    {
                        Error.ProcessAndAppendError(
                            apps.SelfSourceId,
                            Guid.NewGuid().ToString(),
                            error.AsJson(),
                            System.String.Empty,
                            _ => { });
                    }
                    catch (System.Exception ex)
                    {
                        var type = ex.GetType();
                        if (typeof(OutOfMemoryException) == type)
                            throw;
                    }

                    if (rethrow)
                        throw;
                }

                return r;
            }
        }

        public override Task OnConnected()
        {
            return Log(string.Format("Welcome to client {0}!", Context.ConnectionId), Severity.Warning);
        }

        public override Task OnDisconnected()
        {
            return Log(string.Format("Client {0} has gone", Context.ConnectionId), Severity.Warning);
        }

        public override Task OnReconnected()
        {
            return Log(string.Format("Client {0} has reconnected", Context.ConnectionId), Severity.Warning);
        }
    }
}
