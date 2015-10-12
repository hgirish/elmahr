[assembly: WebActivator.PostApplicationStartMethod( typeof($rootnamespace$.App_Start.ElmahR.Modules.Dashboard), "PostStart")]

namespace $rootnamespace$.App_Start.ElmahR.Modules {
    public static class Dashboard {
        public static void PostStart() {
            global::ElmahR.Modules.Dashboard.Modules.Bootstrapper.Bootstrap();
        }
    }
}