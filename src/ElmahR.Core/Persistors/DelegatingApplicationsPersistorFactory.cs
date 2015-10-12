namespace ElmahR.Core.Persistors
{
    #region Imports

    using System;

    #endregion

    public class DelegatingApplicationsPersistorFactory : IApplicationsPersistorFactory
    {
        private readonly Func<Type> _persistorTypeBuilder;

        public DelegatingApplicationsPersistorFactory(Func<Type> persistorTypeBuilder)
        {
            _persistorTypeBuilder = persistorTypeBuilder;
        }

        public IApplicationsPersistor Build()
        {
            return (IApplicationsPersistor)Activator.CreateInstance(_persistorTypeBuilder());
        }
    }
}