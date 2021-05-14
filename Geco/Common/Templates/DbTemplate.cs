using Geco.Common.SimpleMetadata;

namespace Geco.Common.Templates
{
    public abstract class DbTemplate<T> : IDbTemplate
        where  T: MetadataItem
    {
        protected abstract string GetTemplate(T item, DatabaseMetadata db);

        string IDbTemplate.GetTemplate(MetadataItem item, DatabaseMetadata db)
        {
            return this.GetTemplate((T) item, db);
        }
    }
}