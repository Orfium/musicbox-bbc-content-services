using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public partial class SearchPayload
    {
        public int page { get; set; }
        public int size { get; set; }
        public string q { get; set; }
        public string order { get; set; }
        public string orderBy { get; set; }
        public List<SearchFilter> filters { get; set; }

    }

    public partial class SearchFilter
    {
        public string @operator { get; set; }
        public string field { get; set; }
        public List<string> value { get; set; }
    }
}
