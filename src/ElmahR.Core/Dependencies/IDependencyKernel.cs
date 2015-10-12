namespace ElmahR.Core.Dependencies
{
    #region Imports

    using System;
    using System.Collections.Generic;
    
    #endregion

    public interface IDependencyKernel
    {
        object TryGet(Type serviceType);
        IEnumerable<object> GetAll(Type serviceType);
        void Register<T>(Type concreteType);
        void RegisterInstance<T>(T instance) where T : class;
    }
}