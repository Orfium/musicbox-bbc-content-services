using MusicManager.Core.ViewModules;
using System.Linq;

namespace MusicManager.Logics.Extensions
{
    public static class PRSSearchExtention
    {
        public static ClearanceCTags ClearanceCTagsFromMLTrackDocument(this MLTrackDocument mLTrackDocument,
            ClearanceCTags clearanceCTags)
        {
            ClearanceCTags _clearanceCTags = new ClearanceCTags()
            {
                cTags = clearanceCTags.cTags,
                dateTime = mLTrackDocument.prsSearchDateTime,
                workTunecode = mLTrackDocument.prsWorkTunecode,
                workTitle = mLTrackDocument.prsWorkTitle
            };

            //-- Update only PRS Ctags
            foreach (var item in mLTrackDocument.cTags?.Where(a => a.groupId == 3))
            {
                var index = _clearanceCTags.cTags.FindIndex(d => d.id == item.id);
                if (index >= 0)
                    _clearanceCTags.cTags[index] = item;
            }

            if (mLTrackDocument.prsWorkPublishers?.Count > 0)
                _clearanceCTags.workPublishers = string.Join(",", mLTrackDocument.prsWorkPublishers);

            if (mLTrackDocument.prsWorkWriters?.Count > 0)
                _clearanceCTags.workWriters = string.Join(",", mLTrackDocument.prsWorkWriters);

            return _clearanceCTags;
        }
    }
}
