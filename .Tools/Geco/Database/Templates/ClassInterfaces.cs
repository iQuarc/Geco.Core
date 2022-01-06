using Geco.Common.SimpleMetadata;
using Geco.Common.Templates;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Console;

namespace Geco.Database.Templates
{
    /// <summary>
    /// Determine the interfaces that must be added to an entity
    /// </summary>
    [Template("ClassInterfaces")]
    public class ClassInterfacesTemplate : DbTemplate<Table>
    {
        protected override string GetTemplate(Table table, DatabaseMetadata db)
        {
            return string.Join(", ", GetAll());

            IEnumerable<string> GetAll()
            {
                // AuditModel
                if (CheckTypeAndNullability("CreatedDateTime", "datetime2", false, "AuditModel")
                    && CheckTypeAndNullability("ModifiedDateTime", "datetime2", false, "AuditModel")
                    && CheckTypeAndNullability("ModifiedBy", "nvarchar", true, "AuditModel"))
                    yield return "AuditModel";

                // ICompanyEntity
                if (CheckTypeAndNullability("CompanyId", "uniqueidentifier", false, "ICompanyEntity")
                    && CheckTypeAndNullability("Id", "int", false, "ICompanyEntity"))
                    yield return "ICompanyEntity";

                // IHasProcessStatus
                if (CheckTypeAndNullability("Status", "int", false, "IHasProcessStatus"))
                    yield return "IHasProcessStatus";

                // IFromCatalogEntity
                if (CheckTypeAndNullability("CatalogUniqueId", "uniqueidentifier", true, "IFromCatalogEntity"))
                    yield return "IFromCatalogEntity";
            }

            bool CheckTypeAndNullability(string columnName, string expectedType, bool expectedIsNullable, string interfaceOrBaseClass, bool warnType = true, bool warnNullable = true)
            {
                if (!table.Columns.ContainsKey(columnName))
                    return false;

                Column column = table.Columns[columnName];
                if (column.DataType != expectedType)
                {
                    if (warnType)
                        WriteLine($"Column {($"[{column.Table.Schema.Name}].[{column.Table.Name}].[{column.Name}]", Yellow)} has unexpected data type and the {($"[{interfaceOrBaseClass}]", Yellow)} class was not Implemented.", DarkYellow);
                    return false;
                }

                if (column.IsNullable != expectedIsNullable)
                {
                    if (warnNullable)
                        WriteLine($"Column {($"[{column.Table.Schema.Name}].[{column.Table.Name}].[{column.Name}]", Yellow)} is [{(table.Columns["CreatedDateTime"].IsNullable ? "Nullable" : "Not Nullable", Yellow)}] and the {($"[{interfaceOrBaseClass}]", Yellow)} class was not Implemented.", DarkYellow);
                    return false;
                }

                return true;
            }
        }
    }
}
