namespace ElmahR.Core.Dependencies
{
    #region Imports

    using System;
    using Microsoft.AspNet.SignalR;

    #endregion

    public interface IDependencyResolverBuilder
    {
        IDependencyResolverBuilder Register<T>(Type concreteType);
        IDependencyResolverBuilder RegisterInstance<T>(T concreteType) where T : class;
        IDependencyResolver Build();
        Func<Type> PersistorTypeBuilder { get; }
    }
}