using System;
using Sitecore.Data;
using Sitecore.Pipelines;

namespace SitecoreSuperchargers.Historian
{
   [Serializable]
   public class HistoryCollectorPipelineArgs : PipelineArgs
   {
      public HistoryCollectorPipelineArgs(Database database)
      {
         Database = database;
      }

      public Database Database { get; set; }
      public bool Success { get; set; }
   }
}