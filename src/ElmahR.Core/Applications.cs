namespace ElmahR.Core
{
    #region Imports

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using Config;
    using Persistors;

    #endregion

    // Implementing an errors store, where things
    // like errors persistence and sources activation
    // are handled. 

    public class Applications : IApplications
    {
        private readonly Dictionary<string, Application> _sources = new Dictionary<string, Application>();

        public void Add(string key, Application application)
        {
            _sources.Add(key, application);
        }

        private readonly IApplicationsPersistor _persistor;

        private int _counter;

        public Applications(IApplicationsPersistorFactory persistor)
        {
            _persistor = persistor.Build();

            var context = HttpContext.Current;

            if (!(context.GetSection("elmahr") is RootSection))
                return;

            var section = (RootSection)context.GetSection("elmahr");

            _persistor.Init(section);

            SelfSourceId = section.SelfSourceId ?? Error.ElmahRSourceId;

            foreach (var app in section.Applications)
                AddSource(app.ApplicationName, app.SourceId, app.TestExceptionUrl, app);
        }

        public void Banner(Action<string> func)
        {
            func("Used persistor:");
            _persistor.Banner(func);
        }

        public Applications AddSource(string applicationName, string sourceId, string testExceptionUrl, SourceSection config)
        {
            if (this[sourceId] != null)
                throw new ArgumentException("This application is already registered.", sourceId);

            var source = new Application(_persistor, applicationName, sourceId, testExceptionUrl, config, ++_counter);
            _sources.Add(source.SourceId, source);
            return this;
        }

        public string SelfSourceId { get; private set; }

        public Application this[string key]
        {
            get { return _sources.ContainsKey(key) ? _sources[key] : null; }
        }

        public IEnumerator<Application> GetEnumerator()
        {
            return _sources.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerable<Error> GetErrors(int count, string[] activeApps)
        {
            return _persistor.GetErrors(count, activeApps, this);
        }

        public IEnumerable<Error> GetErrors(int count, string beforeId, string[] activeApps)
        {
            return _persistor.GetErrors(count, beforeId, activeApps, this);
        }

        public bool HasSource(string sourceId)
        {
            return this[sourceId] != null;
        }

        public Error GetError(string id)
        {
            return _sources.Values.Select(source => source.GetError(id, this))
                                  .FirstOrDefault(error => error != null);
        }

        public ErrorsResume GetErrorsResume(string sourceId)
        {
            return _persistor.GetErrorsResume(sourceId);
        }


        public void ClearErrorsBefore(string sourceId, DateTime when)
        {
            _persistor.ClearErrorsBefore(sourceId, when);
        }

        public void ClearErrorsAfter(string sourceId, DateTime when)
        {
            _persistor.ClearErrorsAfter(sourceId, when);
        }

        public IEnumerable<TK> GetErrorsStats<TK>(string[] applicationIds, Func<string, string, DateTime, int, TK> selector)
        {
            return _persistor.GetErrorsStats(applicationIds, selector);
        }
    }
}