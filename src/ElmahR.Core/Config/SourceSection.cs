namespace ElmahR.Core.Config
{
    #region Imports

    using System.Collections.Generic;

    #endregion

    public class SourceSection
    {
        readonly Dictionary<string, string> _properties = new Dictionary<string, string>();

        public SourceSection(string applicationName, string sourceId, string testExceptionUrl, string secret, IEnumerable<KeyValuePair<string, string>> properties)
        {
            ApplicationName = applicationName;
            SourceId = sourceId;
            TestExceptionUrl = testExceptionUrl;
            Secret = secret;

            foreach (var a in properties)
                _properties.Add(a.Key, a.Value);
        }

        public string ApplicationName { get; private set; }
        public string SourceId { get; private set; }
        public string TestExceptionUrl { get; private set; }
        public string Secret { get; private set; }

        public string GetProperty(string name)
        {
            if (_properties.ContainsKey(name))
                return _properties[name];
            return null;
        }

        public IEnumerable<string> Properties
        {
            get { return _properties.Keys; }
        }
    }
}