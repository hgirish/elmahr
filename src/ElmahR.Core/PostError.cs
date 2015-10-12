namespace ElmahR.Core
{
    // PostError is a handler which expects a base64-encoded
    // json representation of an error. 

    #region Imports

    using System;
    using System.Web;
    using System.Text;
    using System.Linq;
    using Config;

    #endregion

    public class PostError : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            var @params = context.Request.Params;

            var errorText = Decode(@params["error"]);
            var errorId = @params["errorId"];
            var sourceId = @params["sourceId"];
            var infoUrl = @params["infoUrl"];

            var infoUrlPath = string.IsNullOrWhiteSpace(infoUrl)
                    ? string.Empty
                    : new Uri(infoUrl).AbsoluteUri;

            var section = context.GetSection("elmahr") as RootSection;
            var hasSection = section != null;
            if (!hasSection)
                return;

            var error = (
                from _ in new[] { errorText }
                let hasSource = section.Applications.Any(a => a.SourceId == sourceId)
                let secret = hasSource
                    ? section.Applications.Single(a => a.SourceId == sourceId).Secret
                    : string.Empty
                let hasSecret = !string.IsNullOrWhiteSpace(secret)
                select hasSecret
                    ? Crypto.DecryptStringAes(errorText, secret)
                    : errorText)
                .FirstOrDefault();

            var payload = Error.ProcessAndAppendError(
                sourceId,
                errorId,
                error,
                infoUrlPath,
                ex =>
                {
                    Hub.Log(string.Format("An error occurred in ElmahR: {0}", ex), Hub.Severity.Critical);

                    Error.ProcessError(
                        Error.ElmahRSourceId,
                        ex,
                        infoUrlPath,
                        _ => { },
                        true);
                },
                true);

            if (payload != null)
                Hub.Log(string.Format("Received error from {0}: {1}",
                    payload.GetApplication().ApplicationName,
                    payload.Message));
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        static string Decode(string str)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(str));
        }
    }
}