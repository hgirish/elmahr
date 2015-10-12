[assembly: WebActivator.PreApplicationStartMethod( typeof($rootnamespace$.App_Start.ElmahR.Core), "PreStart")]
[assembly: WebActivator.PostApplicationStartMethod(typeof($rootnamespace$.App_Start.ElmahR.Core), "PostStart")]

namespace $rootnamespace$.App_Start.ElmahR {
    public static class Core {
        private static bool preBootstrapped = false;
    
        public static void PreStart() {
            global::ElmahR.Core.Modules.Bootstrapper.PreBootstrap();
            preBootstrapped = true;
        }
        
        public static void PostStart() {
            if (!preBootstrapped)
                global::ElmahR.Core.Modules.Bootstrapper.PreBootstrap();
            global::ElmahR.Core.Modules.Bootstrapper.Bootstrap();
        }
    }
}