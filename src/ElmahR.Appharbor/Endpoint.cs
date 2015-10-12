namespace ElmahR.Appharbor
{
    #region Imports

    using System;
    using System.IO;
    using System.Linq;
    using System.Web;
    using System.Web.Script.Serialization;
    using Microsoft.AspNet.SignalR;
    using Microsoft.AspNet.SignalR.Infrastructure;
    using Core;

    #endregion

    class Info
    {
        public Application Application { get; set; }
        public Build Build { get; set; }
        public string Raw { get; set; }
        public DateTime Time { get; set; }
        public string IsoTime { get; set; }
    }

    class Application
    {
        public string Name { get; set; }
        public string Id { get; set; }
    }

    class Build
    {
        public string Status { get; set; }
        public Commit Commit { get; set; }
    }

    class Commit
    {
        public string Id { get; set; }
        public string Message { get; set; }
    }

    public class Endpoint : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            //Appharbor posts build infos with this format:

            /*
            {
              "application": {
                "name": "foo"
              }, 
              "build": {
                "commit": {
                  "id": "77d991fe61187d205f329ddf9387d118a09fadcd", 
                  "message": "Implement foo"
                }, 
                "status": "succeeded"
              }
            }
            */

            using (var r = new StreamReader(context.Request.InputStream))
            {
                var message = r.ReadToEnd();

                var buildInfo = new JavaScriptSerializer().Deserialize<Info>(message);
                buildInfo.Raw = message;

                var apps = GlobalHost.DependencyResolver.Resolve<IApplications>();
                var target = apps.FirstOrDefault(a => a.GetValue("appharborId") == buildInfo.Application.Name);
                if (target == null)
                    return;

                buildInfo.Application.Id = target.SourceId;
                buildInfo.Time = DateTime.Now;
                buildInfo.IsoTime = buildInfo.Time.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

                var connectionManager = GlobalHost.DependencyResolver.Resolve<IConnectionManager>();
                connectionManager.GetHubContext<Hub>()
                                 .Clients.All
                                 .notifyBuildInfo(buildInfo);
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
