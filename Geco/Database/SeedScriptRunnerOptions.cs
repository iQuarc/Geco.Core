using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Geco.Database
{
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global")]
    public class SeedScriptRunnerOptions
    {
        public string ConnectionName { get; set; }
        public List<string> Files { get; } = new List<string>();
        public int CommandTimeout { get; set; } = 60;
    }
}