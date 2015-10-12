namespace ElmahR.Core.Config
{
    #region Imports

    using System;
    using System.Configuration;
    using System.Linq;
    using System.Xml;
    using System.Collections.Generic;
    using Extensions;

    #endregion

    public class SectionHandler : IConfigurationSectionHandler
    {
        public virtual object Create(object parent, object configContext, XmlNode section)
        {
            var type              = GetStringValue(section, "persistorType");
            var selfSourceId      = GetStringValue(section, "selfSourceId");
            var ysod              = GetStringValue(section, "ysod");
            var useCdn            = (GetStringValue(section, "useCdn") + "").Equals("true", StringComparison.OrdinalIgnoreCase);
            var enableCrossDomain = (GetStringValue(section, "enableCrossDomain") + "").Equals("true", StringComparison.OrdinalIgnoreCase);
            var extended          = section.Attributes == null
                                  ? Enumerable.Empty<KeyValuePair<string, string>>()
                                  : section.Attributes.Cast<XmlAttribute>()
                                           .Select(a => new {a.Name, a.Value})
                                           .AsKeyTo(e => e.Name, e => e.Value);

            var configurators = from XmlNode node in section.SelectNodes("application")
                                select new SourceSection(
                                    GetStringValue(node, "name"),
                                    GetStringValue(node, "sourceId"),
                                    GetStringValue(node, "testExceptionUrl"),
                                    GetStringValue(node, "secret"),
                                    node.Attributes == null
                                    ? Enumerable.Empty<KeyValuePair<string, string>>()
                                    : node.Attributes.Cast<XmlAttribute>()
                                                     .Select(a => new { a.Name, a.Value })
                                                     .AsKeyTo(e => e.Name, e => e.Value));

            return new RootSection(type, selfSourceId, ysod, useCdn, enableCrossDomain, extended, configurators.ToArray());
        }

        static string GetStringValue(XmlNode node, string attribute)
        {
            System.Diagnostics.Debug.Assert(node != null);
            System.Diagnostics.Debug.Assert(node.Attributes != null);

            var a = node.Attributes[attribute];
            return a == null
                   ? null
                   : a.Value;
        }
    }
}