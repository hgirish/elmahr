namespace ElmahR.Core.Dependencies
{
    #region Imports

    using System;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using Microsoft.AspNet.SignalR;
    using Persistors;
    using Config;

    #endregion

    public class DependencyResolverBuilderException : ApplicationException
    {
        public DependencyResolverBuilderException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public DependencyResolverBuilderException(string message)
            : base(message)
        {
        } 
    }

    public class DependencyResolverBuilder : IDependencyResolverBuilder
    {
        class PersistorTypeFinderHolder
        {
            [Import(typeof(IPersistorTypeFinder), AllowDefault = true)]
#pragma warning disable 649
            public IPersistorTypeFinder PersistorTypeFinder { get; set; }
#pragma warning restore 649
        }

        [Import(typeof(IDependencyKernel), AllowDefault=true)]
#pragma warning disable 649
        private IDependencyKernel _dependencyKernel;
#pragma warning restore 649

        public Func<Type> PersistorTypeBuilder { get; private set; }

        public DependencyResolverBuilder()
        {
            var catalog = new AggregateCatalog();

            catalog.Catalogs.Add(new DirectoryCatalog(AppDomain.CurrentDomain.SetupInformation.PrivateBinPath));

            var container = new CompositionContainer(catalog);

            //dependency kernel

            try
            {
                container.ComposeParts(this);
            }
            catch (ChangeRejectedException)
            {
                throw new DependencyResolverBuilderException(
                    @"Problem encountered while trying to setup a dependency resolver. 
Please check if you have correctly deployed your ElmahR.IoC.* package of choice (only one allowed).
Please remove all ElmahR.IoC.* packages or assemblies to use the default embedded resolver.");
            }

            if (_dependencyKernel == null)
                _dependencyKernel = new DependencyKernel();

            //persistor type
            PersistorTypeBuilder = () =>
            {
                var context = HttpContext.Current;
                var section = context.GetSection("elmahr") as RootSection;
                var persistorType = section != null ? section.PersistorType : null;
                if (persistorType == null)
                {
                    var finder = new PersistorTypeFinderHolder();

                    try
                    {
                        container.ComposeParts(finder);
                    }
                    catch (ChangeRejectedException)
                    {
                        throw new DependencyResolverBuilderException(
                            @"Problem encountered while trying to setup a persistor. 
Please check if you have correctly deployed your ElmahR.Persistence.* package of choice (only one allowed).
Please remove all ElmahR.Persistence.* assemblies to use the default 'in memory' persistor.");
                    }

                    return finder.PersistorTypeFinder == null
                         ? typeof(InMemoryPersistor)
                         : finder.PersistorTypeFinder.Find();
                }

                try
                {
                    return Type.GetType(persistorType);
                }
                catch (Exception)
                {
                    throw new DependencyResolverBuilderException(
                        @"Problem encountered while trying to setup a persistor. 
Please check the type name specified in the 'persistorType' attribute of 'elmahr' configuration section.");
                }
            };
            
        }

        public IDependencyResolverBuilder Register<T>(Type concreteType)
        {
            _dependencyKernel.Register<T>(concreteType);

            return this;
        }

        public IDependencyResolverBuilder RegisterInstance<T>(T concreteType) where T: class
        {
            _dependencyKernel.RegisterInstance(concreteType);

            return this;
        }

        public IDependencyResolver Build()
        {
            return new DependencyResolverImpl(_dependencyKernel);
        }

        class DependencyResolverImpl : DefaultDependencyResolver
        {
            private readonly IDependencyKernel _kernel;

            public DependencyResolverImpl(IDependencyKernel kernel)
            {
                if (kernel == null)
                    throw new ArgumentNullException("kernel");

                _kernel = kernel;
            }

            public override object GetService(Type serviceType)
            {
                try
                {
                    return _kernel.TryGet(serviceType) ?? base.GetService(serviceType);
                }
                catch (Exception ex)
                {
                    var type = ex.GetType();
                    if (typeof(OutOfMemoryException) == type)
                        throw;
                    throw new DependencyResolverBuilderException("Unable to resolve dependency. Please check inner exception for details.", ex);
                }
            }

            public override IEnumerable<object> GetServices(Type serviceType)
            {
                try
                {
                    return _kernel.GetAll(serviceType).Concat(base.GetServices(serviceType));
                }
                catch (Exception ex)
                {
                    var type = ex.GetType();
                    if (typeof(OutOfMemoryException) == type)
                        throw;
                    throw new DependencyResolverBuilderException("Unable to resolve dependency. Please check inner exception for details.", ex);
                }
            }
        }
    }
}
