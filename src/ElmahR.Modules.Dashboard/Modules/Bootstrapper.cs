namespace ElmahR.Modules.Dashboard.Modules
{
    public static class Bootstrapper
    {
        public static void Bootstrap()
        {
            Core.Modules.Bootstrapper.RegisterRoute("ElmahR/Dashboard", "~/elmahr/dashboard.cshtml");
        }
    }
}
