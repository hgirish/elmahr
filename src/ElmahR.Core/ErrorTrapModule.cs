namespace ElmahR.Core
{
    #region Imports

    using System;
    using System.Web;
    using Microsoft.AspNet.SignalR;
    using Elmah;

    #endregion

    public class ErrorTrapModule : IHttpModule
    {
        private volatile string _selfSourceId;

        public void Init(HttpApplication application)
        {
            application.Error += (_, args) => TrapError(application.Server.GetLastError(), application.Context);
        }

        private void TrapError(Exception error, HttpContext context)
        {
            if (_selfSourceId == null)
            {
                lock (this)
                {
                    if (_selfSourceId == null)
                    {
                        _selfSourceId = GlobalHost.DependencyResolver.Resolve<IApplications>().SelfSourceId ?? Error.ElmahRSourceId;
                    }
                }
            }

            var jsonified = error.AsJson(context);

            Hub.Log(string.Format("An error occurred in ElmahR: {0}", error), Hub.Severity.Critical);

            Error.ProcessAndAppendError(
                _selfSourceId,
                Guid.NewGuid().ToString(),
                jsonified,
                string.Empty, 
                ex =>
                {
                    Hub.Log(string.Format("An error occurred while processing an internal error in ElmahR: {0}", error), 
                        Hub.Severity.Critical);

                    Error.ProcessError(
                        Error.ElmahRSourceId,
                        ex,
                        String.Empty,
                        _ => { },
                        true);
                });
        }

        public void Dispose()
        {
        }
    }
}
