namespace ElmahR.Elmah
{
    #region Imports

    using System;
    using System.Web;

    #endregion

    public static class ExceptionExtensions
    {
        public static string AsJson(this Exception source, HttpContext context)
        {
            var error = new global::Elmah.Error(source, context);

            return global::Elmah.ErrorJson.EncodeString(error);
        }

        public static string AsJson(this Exception source)
        {
            return AsJson(source, null);
        }
    }
}
