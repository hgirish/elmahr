namespace ElmahR.Core.Config
{
    #region Imports

    using System.Collections.Generic;
    using System.Linq;

    #endregion

    public class RootSection
    {
        private readonly IDictionary<string, string> _properties;

        public string PersistorType { get; private set; }
        public string SelfSourceId { get; private set; }
        public string Ysod { get; private set; }
        public bool UseCdn { get; private set; }
        public bool EnableCrossDomain { get; private set; }

        public SourceSection[] Applications { get; private set; }

        public RootSection(string type, string selfSourceId, string ysod, bool useCdn, bool enableCrossDomain, IEnumerable<KeyValuePair<string, string>> properties, SourceSection[] applications)
        {
            _properties = properties.ToDictionary(k => k.Key, v => v.Value);

            PersistorType = type;
            SelfSourceId = selfSourceId;
            Ysod = ysod;
            UseCdn = useCdn;
            EnableCrossDomain = enableCrossDomain;
            Applications = applications;
        }

        public string GetProperty(string name)
        {
            return _properties.ContainsKey(name) 
                 ? _properties[name] 
                 : string.Empty;
        }
    }
}