namespace ElmahR.Elmah
{
    #region Imports

    using System.Collections;
    using System.Diagnostics;
    using System;
    using System.Web;
    using ApplicationException = global::Elmah.ApplicationException;

    #endregion

    public class FakeLoadErrorPostModule : ErrorPostModule
    {
        const string ElmahrPrefix = "ELMAHR-";

        protected override string GetSourceId(HttpContext context)
        {
            // This is for testing purposes only: if the sourceIds
            // contains a comma-separated list of tokens, the module will pick one
            // of them randomly, to simulate different sources generating errors.

            var config = (IDictionary)GetConfig();
            if (config == null)
                return string.Empty;

            var testExceptionHeader = context.Request.Headers["User-Agent"];

            if (testExceptionHeader != null && testExceptionHeader.StartsWith(ElmahrPrefix, StringComparison.InvariantCultureIgnoreCase))
                return testExceptionHeader.Replace(ElmahrPrefix, string.Empty);

            var tokens = GetSetting(config, "sourceIds").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            return tokens[new Random().Next(tokens.Length)];
        }

        private static string GetSetting(IDictionary config, string name)
        {
            Debug.Assert(config != null);
            Debug.Assert(!string.IsNullOrEmpty(name));

            var value = ((string)config[name]) ?? string.Empty;

            if (value.Length == 0)
            {
                throw new ApplicationException(string.Format(
                    "The required configuration setting '{0}' is missing for the error posting module.", name));
            }

            return value;
        }
    }
}
