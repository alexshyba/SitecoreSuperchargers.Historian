using System;
using System.Collections;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Pipelines;
using Sitecore.StringExtensions;

namespace SitecoreSuperchargers.Historian
{
   public class Handler
   {
      private readonly ArrayList _databases = new ArrayList();

      public ArrayList Databases
      {
         get { return _databases; }
      }

      public void Process(object sender, EventArgs args)
      {
         Assert.ArgumentNotNull(sender, "sender");
         Assert.ArgumentNotNull(args, "args");

         Log.Info("Historian.Handler. Starting processing for databases ({0}).".FormatWith(_databases.Count), this);

         foreach (string dbName in _databases)
         {
            if (dbName.IsNullOrEmpty())
            {
               Log.Error("Historian.Handler. Database parameter was invalid. Processing skipped", this);
               continue;
            }

            if (!StringUtil.Join(Factory.GetDatabaseNames(), ",").Contains(dbName))
            {
               Log.Error("Historian.Handler. Database '{0} does not exist. Processing skipped".FormatWith(dbName), this);
               continue;
            }

            var database = Factory.GetDatabase(dbName);
            if (database == null)
            {
               Log.Error("Historian.Handler. Database '{0} does not exist. Processing skipped".FormatWith(dbName), this);
               continue;
            }

            Log.Info("Historian.Handler. Starting processing for database '{0}'...".FormatWith(dbName), this);

            try
            {
               var jobOptions = new JobOptions("Historian.Handler.ProcessDatabase", "", Context.Site.Name, this, "ProcessDatabase", new object[] { database });
               var job = new Job(jobOptions);
               job.Start();
            }
            catch (Exception exception)
            {
               Log.Error("Historian.Handler. Background job ProcessDatabase failed. ", exception);
            }
         }
      }

      private bool ProcessDatabase(Database db)
      {
         bool success;
         try
         {
            var args = new HistoryCollectorPipelineArgs(db);
            CorePipeline.Run("historycollector", args);
            success = args.Success;
         }
         catch (Exception exception)
         {
            success = false;
            Log.Error("Historian.Handler failed when calling historyCollector pipeline", exception);
         }

         Log.Info("Historian.Handler returned with '{0}'".FormatWith(success), this);
         return success;
      }
   }
}

