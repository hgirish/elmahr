namespace ElmahR.Elmah
{
    #region Imports

    using System.Linq;
    using System.Web;

    #endregion

    static class HttpModuleCollectionExtensions
    {
        public static T GetSingleModule<T>(this HttpModuleCollection modules)
        {
            return Enumerable.Range(0, modules.Count)
                             .Select(i => modules[i])
                             .OfType<T>()
                             .SingleOrDefault();
        }
    }
}
