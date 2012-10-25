using System.Collections.Generic;
using System.Linq;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Resources.Media;
using Sitecore.StringExtensions;

namespace SitecoreSuperchargers.Historian.Media
{
   public class MediaUrlCollector : BaseCollector
   {
      protected override string LastUpdateKey { get { return "MediaUrlCollector_LastUpdateTime"; } }

      protected override void ProcessCollected(IEnumerable<string> tokens)
      {
         // TODO: add the code that will process the tokens
         var urls = tokens as List<string> ?? tokens.ToList();

         Log.Info("MediaUrlCollector: collected tokens. Count '{0}'".FormatWith(urls.Count()), this);
         foreach (var url in urls)
         {
            Log.Info("MediaUrlCollector: Token '{0}'".FormatWith(url), this);
         }
      }

      protected override bool IsValidEntry(Item item)
      {
         return item != null && item.Paths.IsMediaItem && !item.TemplateID.Equals(TemplateIDs.MediaFolder);
      }

      protected override string GetToken(Item item)
      {
         MediaItem mediaItem = item;
         var mediaUrl = MediaManager.GetMediaUrl(mediaItem);
         return mediaUrl;
      }
   }
}
