namespace ElmahR.IoC.NInject
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using Core.Dependencies;
    using Ninject;    

    #endregion

    [Export(typeof(IDependencyKernel))]
    class DependencyKernel : IDependencyKernel
    {
        readonly StandardKernel _kernel = new StandardKernel();

        public object TryGet(Type serviceType)
        {
            return _kernel.TryGet(serviceType);
        }

        public IEnumerable<object> GetAll(Type serviceType)
        {
            return _kernel.GetAll(serviceType);
        }

        public void Register<T>(Type concreteType)
        {
            _kernel.Bind<T>()
                   .To(concreteType)
                   .InSingletonScope();
        }

        public void RegisterInstance<T>(T instance) where T : class
        {
            _kernel.Bind<T>()
                   .ToConstant(instance)
                   .InSingletonScope();
        }
    }
}