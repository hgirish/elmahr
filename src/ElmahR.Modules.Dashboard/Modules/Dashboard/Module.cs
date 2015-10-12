namespace ElmahR.Modules.Dashboard.Modules.Dashboard
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using Core.Modules;    

    #endregion

    [Export(typeof(IModule))]
    public class Module : IModule
    {
        public IEnumerable<TR> DefineScripts<TF, TR>(string version, Func<string, string, string> cndRenamer, Func<string, string, TF> fileDescriptor, Func<string, IEnumerable<TF>, TR> resultor)
        {
            yield return resultor("~/Scripts/" + version + "/dashboard", new[]
            {
                fileDescriptor("~/Scripts/jquery-ui-{version}.js", cndRenamer("jquery-ui", "http://ajax.googleapis.com/ajax/libs/jqueryui/1.10.0/jquery-ui.min.js")),
                fileDescriptor("~/Scripts/jquery.timeago.js", null),
                fileDescriptor("~/Scripts/knockout-{version}.js", cndRenamer("knockout", "http://ajax.aspnetcdn.com/ajax/knockout/knockout-2.2.1.js")),
                fileDescriptor("~/Scripts/ElmahR/elmahr.dashboard.js", null),
                fileDescriptor("~/Content/Themes/elmahr/bootstrap/js/bootstrap-collapse.js", null)
            });
        }

        public IEnumerable<TR> DefineStyles<TF, TR>(string version, Func<string, string, string> cndRenamer, Func<string, string, TF> fileDescriptor, Func<string, IEnumerable<TF>, TR> resultor)
        {
            yield return resultor("~/Content/Themes/base/jquery-ui-css", new[]
            {
                fileDescriptor("~/Content/Themes/base/jquery-ui.css", cndRenamer("jquery-ui-css", "http://ajax.googleapis.com/ajax/libs/jqueryui/1.10.0/themes/base/jquery-ui.css"))
            });
            yield return resultor("~/Content/Themes/elmahr/bootstrap/" + version + "/css", new[]
            {
                fileDescriptor("~/Content/Themes/elmahr/bootstrap/css/bootstrap.css", null),
                fileDescriptor("~/Content/Themes/elmahr/bootstrap/css/bootstrap-responsive.css", null),
                fileDescriptor("~/Content/Themes/elmahr/bootstrap/css/bootstrap-applied.css", null)
            });
            yield return resultor("~/Content/Themes/elmahr/dashboard/" + version + "/css", new[]
            {
                fileDescriptor("~/Content/Themes/elmahr/bootstrap/css/elmahr.css", null),
            });
        }

        public string Name { get { return null; } }
    }
}