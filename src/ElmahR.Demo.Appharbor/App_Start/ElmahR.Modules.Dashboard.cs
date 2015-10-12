[assembly: WebActivator.PostApplicationStartMethod( typeof(ElmahR.Demo.Appharbor.App_Start.ElmahR.Modules.Dashboard), "PostStart")]

namespace ElmahR.Demo.Appharbor.App_Start.ElmahR.Modules {
    public static class Dashboard {
        public static void PostStart() {
            global::ElmahR.Modules.Dashboard.Modules.Bootstrapper.Bootstrap();
        }
    }
}