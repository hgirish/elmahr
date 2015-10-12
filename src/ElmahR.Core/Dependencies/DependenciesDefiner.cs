namespace ElmahR.Core.Dependencies
{
    #region Imports

    using System;
    using System.Web;
    using Config;
    using Persistors;
    using Services;

    #endregion

    public static class DependenciesDefiner
    {
        public static void Define(IDependencyResolverBuilder builder)
        {
            builder.Register<IApplications>(typeof(Applications));
            builder.RegisterInstance<IApplicationsPersistorFactory>(new DelegatingApplicationsPersistorFactory(builder.PersistorTypeBuilder));
            builder.Register<IUserService>(typeof(UserService));
        }
    }
}