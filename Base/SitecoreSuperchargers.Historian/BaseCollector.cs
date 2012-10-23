using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.Data.Engines;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;

namespace SitecoreSuperchargers.Historian
{
   public abstract class BaseCollector : IHistoryCollectorProcessor
   {
      protected abstract string LastUpdateKey { get; }
      private readonly List<ID> _validEntries = new List<ID>();

      public void Process(HistoryCollectorPipelineArgs args)
      {
         Assert.ArgumentNotNull(args, "HistoryCollectorPipelineArgs cannot be null");

         var database = args.Database;

         if (database.Engines.HistoryEngine == null ||
             database.Engines.HistoryEngine.Storage == null)
         {
            Log.Error("HistoryCollector. History engine for database '{0}' is not configured. ".FormatWith(database.Name), this);
            args.Success = false;
            return;
         }

         var lastUpdateDate = GetLastUpdateDate(database);

         var utcNow = DateTime.UtcNow;

         if (ProcessEntries(database, lastUpdateDate, utcNow))
         {
            Log.Info("HistoryCollector. Collecting Urls. Total: {0}".FormatWith(_validEntries.Count), this);
            var urls = CollectTokens(database);
            Log.Info("HistoryCollector. Starting Processing Urls. Total: {0}...".FormatWith(urls.Count), this);

            try
            {
               var result = ProcessUrls(urls);
               database.Properties[LastUpdateKey] = DateUtil.ToIsoDate(utcNow);

               Log.Info("HistoryCollector. Processing Urls Done. Returned with {0}".FormatWith(result), this);
               args.Success = true;
               return;
            }
            catch (Exception)
            {
               Log.Info("HistoryCollector. ProcessUrls failed.", this);
               args.Success = false;
               return;
            }
         }

         Log.Info("HistoryCollector. ProcessEntries failed.", this);
         args.Success = false;
      }

      #region Abstract Methods

      /// <summary>
      /// The method to be called after the successful collection
      /// </summary>
      /// <param name="item"></param>
      /// <returns></returns>
      protected abstract void ProcessCollected(IEnumerable<string> urls);

      /// <summary>
      /// Verifying if an entity is valid
      /// </summary>
      /// <param name="item"></param>
      /// <returns></returns>
      protected abstract bool IsValidEntry(Item item);

      /// <summary>
      /// Figuring out what the token is going to be
      /// </summary>
      /// <param name="item"></param>
      /// <returns></returns>
      protected abstract string GetToken(Item item);

      #endregion

      #region Protected Methods

      protected bool ProcessEntries(Database database, DateTime from, DateTime to)
      {
         Assert.ArgumentNotNull(database, "database");

         var entrys = GetHistory(database, from, to);
         if (entrys.Count <= 0)
         {
            Log.Info("HistoryCollector. Skipping no entries were found.".FormatWith(database.Name, entrys.Count), this);
            return false;
         }

         Log.Info("HistoryCollector. Starting adding history entries for database '{0}'. '{1}' entries pending".FormatWith(database.Name, entrys.Count), this);
         foreach (var entry in entrys)
         {
            AddEntry(entry);
         }

         Log.Info("HistoryCollector. Processing for the database '{0}' done.".FormatWith(database.Name), this);

         return true;
      }

      protected bool ProcessUrls(List<string> urls)
      {
         Log.Info("HistoryCollector. Processing Collected Count of '{0}' urls: '{1}'".FormatWith(urls.Count, StringUtil.Join(urls, ", ")), this);

         if (!urls.Any())
         {
            Log.Info("HistoryCollector. No urls to process. Returning.", this);
            return false;
         }
         try
         {
            ProcessCollected(urls);
         }
         catch (Exception exception)
         {
            Log.Error("HistoryCollector. Processing failed", exception);
            return false;
         }

         Log.Info("HistoryCollector. Processing Done. Total processed: {0}".FormatWith(_validEntries.Count), this);
         return true;
      }

      protected List<string> CollectTokens(Database database)
      {
         var urls = new List<string>();
         foreach (var id in _validEntries)
         {
            var item = database.GetItem(id);

            if (IsValidEntry(item))
            {
               Log.Info("HistoryCollector. Processing Token for item '{0}'".FormatWith(id), this);
               var token = GetToken(item);
               urls.Add(token);
               Log.Info("HistoryCollector. Token Collection processed for item '{0}'. Added '{1}'".FormatWith(id, token), this);
            }
            else
            {
               Log.Info("HistoryCollector. Token Collection skipped for item '{0}'. Reason: not a valid item.".FormatWith(id), this);
            }
         }
         return urls;
      }

      protected HistoryEntryCollection GetHistory(Database database, DateTime from, DateTime to)
      {
         var source = HistoryManager.GetHistory(database, from, to);
         if (source.Count == 0)
         {
            return source;
         }
         var entrys = new HistoryEntryCollection();
         using (var enumerator = source.GetEnumerator())
         {
            Func<HistoryEntry, bool> predicate = null;
            HistoryEntry entry;
            while (enumerator.MoveNext())
            {
               entry = enumerator.Current;
               if (predicate == null)
               {
                  predicate = historyEntry => (((historyEntry.Category == HistoryCategory.Item) && (historyEntry.Action == entry.Action) && (historyEntry.ItemId == entry.ItemId)) && ((historyEntry.ItemLanguage == entry.ItemLanguage) && (historyEntry.ItemVersion == entry.ItemVersion))) && (historyEntry.Created > entry.Created);
               }
               if (!source.Where(predicate).Any())
               {
                  entrys.Add(entry);
               }
            }
         }
         return entrys;
      }

      protected void AddEntry(HistoryEntry entry)
      {
         try
         {
            if (!_validEntries.Contains(entry.ItemId))
            {
               _validEntries.Add(entry.ItemId);
            }
            else
            {
               Log.Info("HistoryCollector. ProcessEntry. Entry '{0}' was already added. Skipping.".FormatWith(entry.ItemId), this);
            }
         }
         catch (Exception exception)
         {
            Log.Error("HistoryCollector. Could not process entry. Action: '{0}', Item: '{1}'.".FormatWith(entry.Action, entry.ItemPath), exception);
         }
      }

      protected DateTime GetLastUpdateDate(Database database)
      {
         var date = database.Properties[LastUpdateKey];
         return date.Length > 0 ? DateUtil.ParseDateTime(date, DateTime.MinValue) : DateTime.MinValue;
      }

      #endregion
   }
}
