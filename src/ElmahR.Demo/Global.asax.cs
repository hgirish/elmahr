using ElmahR;
[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(Starter), "PreStart")]
[assembly: WebActivatorEx.PostApplicationStartMethod(typeof(Starter), "PostStart")]

namespace ElmahR
{
    #region Imports

    using System;
    using System.Web;
    using Core.Modules;

    #endregion

    public static class Starter
    {
        private static bool preBootstrapped = false;

        public static void PreStart()
        {
            Bootstrapper.PreBootstrap();
            preBootstrapped = true;
        }

        public static void PostStart()
        {
            if (!preBootstrapped)
                Bootstrapper.PreBootstrap();
            Bootstrapper.Bootstrap();
        }
    }

    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            ElmahR.Modules.Dashboard.Modules.Bootstrapper.Bootstrap();
        }
    }
}