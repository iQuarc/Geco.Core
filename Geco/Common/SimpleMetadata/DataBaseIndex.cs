﻿using System.Diagnostics;

namespace Geco.Common.SimpleMetadata;

[DebuggerDisplay(
   "[{Name}] IsUnique:{IsUnique} IsClustered:{IsClustered} Columns:{Columns} IncludedColumns:{IncludedColumns}")]
public class DataBaseIndex : MetadataItem
{
   public DataBaseIndex(string name, Table table, bool isUnique, bool isClustered)
   {
      Name               = name;
      FullyQualifiedName = $"{table.FullyQualifiedName}.[{name}]";
      Table              = table;
      IsUnique           = isUnique;
      IsClustered        = isClustered;
      Columns            = new MetadataCollection<Column>(OnColumnAdded);
      IncludedColumns    = new MetadataCollection<Column>(OnIncludedColumnAdded);

      Db.AddToIndex(this);
   }

   public override             string            Name               { get; }
   public override             string            FullyQualifiedName { get; }
   protected internal override IDatabaseMetadata Db                 => Table.Db;

   public Table Table       { get; }
   public bool  IsUnique    { get; }
   public bool  IsClustered { get; }

   public MetadataCollection<Column> Columns         { get; }
   public MetadataCollection<Column> IncludedColumns { get; }

   private void OnColumnAdded(Column column)
   {
      column.Indexes.Add(this);
   }

   private void OnIncludedColumnAdded(Column column)
   {
      column.IndexIncludes.Add(this);
   }

   protected override void OnRemove()
   {
      Db.RemoveFromIndex(this);
   }
}