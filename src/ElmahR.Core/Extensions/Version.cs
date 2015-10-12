namespace ElmahR.Core.Extensions
{
    #region Imports

    using System;
    using System.Linq;
    using System.Reflection;

    #endregion

    public static class Version
    {
        public static string GetCurrent(Type source = null)
        {
            source = source ?? typeof(Version);

            var assembly = source.Assembly;
            var version =
                (from a in
                     assembly.GetCustomAttributes(typeof (AssemblyInformationalVersionAttribute), false)
                             .Cast<AssemblyInformationalVersionAttribute>()
                 select a.InformationalVersion).FirstOrDefault();
            return version ?? assembly.GetName().Version.ToString();
        }
    }
}