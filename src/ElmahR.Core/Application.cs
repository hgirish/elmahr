namespace ElmahR.Core
{
    #region Imports

    using System;
    using System.Threading.Tasks;
    using Persistors;
    using System.Collections.Generic;
    using Config;

    #endregion

    public class Application
    {
        private readonly IApplicationsPersistor _persistor;
        private readonly string _applicationName;
        private readonly string _sourceId;
        private readonly string _testExceptionUrl;
        private readonly int _id;

        private string _infoUrl;
        private readonly SourceSection _config;

        public Application(IApplicationsPersistor persistor, string applicationName, string sourceId, string testExceptionUrl, SourceSection config, int id)
        {
            _persistor = persistor;
            _applicationName = applicationName;
            _sourceId = sourceId;
            _testExceptionUrl = testExceptionUrl;
            _config = config;
            _id = id;
        }

        public int Id
        {
            get { return _id; }
        }

        public string SourceId
        {
            get { return _sourceId; }
        }

        public string ApplicationName
        {
            get { return _applicationName; }
        }

        public string InfoUrl
        {
            get { return _infoUrl; }
        }

        public string TestExceptionUrl
        {
            get { return _testExceptionUrl; }
        }

        public Error AppendError(string jsonError, string errorId, Action<Exception> errorCatcher)
        {
            var fullError = Error.Build(jsonError, errorId, this);

            Task.Factory
                .StartNew(() => _persistor.AddError(fullError, errorId))
                .ContinueWith(t =>
                {
                    if (t.Exception != null && errorCatcher != null)
                         errorCatcher(t.Exception);
                });

            return fullError;
        }

        public Error GetError(string id, IApplications applications)
        {
            return _persistor.GetError(id, applications);
        }

        public void SetInfoUrl(string infoUrl)
        {
            _infoUrl = infoUrl;
        }

        public string GetValue(string name)
        {
            return _config.GetProperty(name);
        }

        public IEnumerable<string> Properties
        {
            get { return _config.Properties; }
        }
    }
}