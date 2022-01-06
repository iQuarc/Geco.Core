using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Geco.Database
{
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global")]
    public class SeedScriptRunnerOptions
    {
        public string ConnectionName { get; set; }
        public List<string> Files { get; } = new List<string>();
        public bool OpenTransaction { get; set; } = true;
        public bool AddFKGuards { get; set; } = false;
        public int CommandTimeout { get; set; } = 60;
        public List<string> ExcludedTables { get; } = new List<string>();
        public string ExcludedTablesRegex { get; set; }
        public int StartIndex { get; set; } = 0;
    }
}