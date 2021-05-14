using System;

namespace Geco.Common.Templates
{
    /// <summary>
    /// Identifies a generator template
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TemplateAttribute : ServiceAttribute
    {
        public string TemplateName { get; }

        public TemplateAttribute(string templateName)
            :base(typeof(IDbTemplate))
        {
            TemplateName = templateName;
        }
    }
}