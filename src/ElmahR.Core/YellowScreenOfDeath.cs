namespace ElmahR.Core
{
    #region Imports

    using System.Web;
    using Microsoft.AspNet.SignalR;

    #endregion

    /// <summary>
    /// Summary description for YellowScreenOfDeath
    /// </summary>
    public class YellowScreenOfDeath : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var errorId = context.Request.QueryString["id"] ?? string.Empty;

            if (errorId.Length == 0)
                return;

            var response = context.Response;

            var apps = GlobalHost.DependencyResolver.Resolve<IApplications>();

            var error = apps.GetError(errorId);
            if (error == null)
            {
                // TODO: Send error response entity
                response.Status = "404 Not Found";
                return;
            }

            if (string.IsNullOrEmpty(error.WebHostHtmlMessage))
                return;

            response.Write(error.WebHostHtmlMessage);
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}