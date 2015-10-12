namespace ElmahR.Core.Dependencies
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using TinyIoC;
    
    #endregion

    class DependencyKernel : IDependencyKernel
    {
        public object TryGet(Type serviceType)
        {
            return TinyIoCContainer.Current.CanResolve(serviceType) 
                   ? TinyIoCContainer.Current.Resolve(serviceType) 
                   : null;
        }

        public IEnumerable<object> GetAll(Type serviceType)
        {
            return TinyIoCContainer.Current.ResolveAll(serviceType);
        }

        public void Register<T>(Type concreteType)
        {
            TinyIoCContainer.Current.Register(typeof(T), concreteType).AsSingleton();
        }

        public void RegisterInstance<T>(T instance) where T: class
        {
            TinyIoCContainer.Current.Register(instance);
        }
    }
}
