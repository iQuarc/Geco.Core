﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Geco.Database
{
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global")]
    public class SeedDataGeneratorOptions
    {
        public string ConnectionName { get; set; }
        public string OutputFileName { get; set; }
        public List<string> Tables { get; } = new List<string>();
        public string TablesRegex { get; set; }
        public List<string> ExcludedTables { get; } = new List<string>();
        public string ExcludedTablesRegex { get; set; }
        public int ItemsPerStatement { get; set; } = 1000;
    }
}