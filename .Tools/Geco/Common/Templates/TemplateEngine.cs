using System;
using System.Collections.Generic;
using System.Reflection;
using Geco.Common.SimpleMetadata;
using static Geco.Common.Util.ColorConsole;

namespace Geco.Common.Templates
{
    [Service(typeof(TemplateEngine))]
    public class TemplateEngine
    {
        private readonly IMetadataProvider metadataProvider;
        private readonly Dictionary<string, IDbTemplate> templates = new Dictionary<string, IDbTemplate>();

        public TemplateEngine(IEnumerable<IDbTemplate> dbTemplates, IMetadataProvider metadataProvider)
        {
            this.metadataProvider = metadataProvider;
            foreach (var dbTemplate in dbTemplates)
            {
                var typeInfo = dbTemplate.GetType().GetTypeInfo();
                var templateAttribute = typeInfo.GetCustomAttribute<TemplateAttribute>();
                var templateName = templateAttribute?.TemplateName;
                if (string.IsNullOrEmpty(templateName))
                {
                    WriteLine($"Template [{typeInfo.Name}] does not contain the [TemplateAttribute] and was Ignored.", ConsoleColor.DarkYellow);
                    continue;
                }

                templates[templateName] = dbTemplate;
            }
        }

        public string RunTemplate(string name, MetadataItem item)
        {
            return templates[name].GetTemplate(item, metadataProvider.GetMetadata());
        }
    }
}