using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Playout.Build.MetadataMapping
{
    public class PmapComparer : IComparer<PMAP_SUBFUNC>
    {
        private IList<PMAP_SUBFUNC> _orderedTypes;


        public PmapComparer(List<PMAP_SUBFUNC> orderedTypes)
        {
            _orderedTypes = orderedTypes;
        }

        public int Compare(PMAP_SUBFUNC x, PMAP_SUBFUNC y)
        {
            var xIndex = _orderedTypes.IndexOf(x);
            var yIndex = _orderedTypes.IndexOf(y);

            return xIndex.CompareTo(yIndex);
        }
    };
}
