[assembly: WebActivator.PreApplicationStartMethod( typeof(ElmahR.Demo.Appharbor.App_Start.ElmahR.Core), "PreStart")]
[assembly: WebActivator.PostApplicationStartMethod(typeof(ElmahR.Demo.Appharbor.App_Start.ElmahR.Core), "PostStart")]

namespace ElmahR.Demo.Appharbor.App_Start.ElmahR {
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