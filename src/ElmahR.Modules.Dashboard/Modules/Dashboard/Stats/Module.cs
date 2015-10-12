namespace ElmahR.Modules.Dashboard.Modules.Dashboard.Stats
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
            yield return resultor("~/Scripts/" + version + "/stats", new[]
            {
                fileDescriptor("~/Scripts/raphael.js", null),
                fileDescriptor("~/Scripts/ElmahR/elmahr.stats.js", null),
                fileDescriptor("~/Content/Themes/elmahr/bootstrap/js/bootstrap-collapse.js", null)
            });
        }

        public IEnumerable<TR> DefineStyles<TF, TR>(string version, Func<string, string, string> cndRenamer, Func<string, string, TF> fileDescriptor, Func<string, IEnumerable<TF>, TR> resultor)
        {
            yield break;
        }

        public string Name { get { return null; } }
    }
}