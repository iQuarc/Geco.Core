using Geco.Common.SimpleMetadata;

namespace Geco.Common.Templates
{
    public interface IDbTemplate
    {
        string GetTemplate(MetadataItem item, DatabaseMetadata db);
    }
}