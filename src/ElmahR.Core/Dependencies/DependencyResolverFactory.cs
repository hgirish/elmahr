namespace ElmahR.Core.Dependencies
{
    public class DependencyResolverFactory
    {
        DependencyResolverFactory()
        {
            _builder = new DependencyResolverBuilder();
        }

        private static volatile DependencyResolverFactory _factory;
        private static readonly object factoryLocker = new object();

        private readonly IDependencyResolverBuilder _builder;

        public static DependencyResolverFactory Create()
        {
            if (_factory == null)
            {
                lock (factoryLocker)
                {
                    if (_factory == null)
                    {
                        _factory = new DependencyResolverFactory();
                    }
                }
            }

            return _factory;
        }

        public IDependencyResolverBuilder Builder { get { return _builder; } }
    }
}