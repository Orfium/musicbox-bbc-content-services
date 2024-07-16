using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.ViewModules
{
    public partial class SearchData<T> where T : class
    {
        public object TotalCount { get; set; }
        public T Data { get; set; }        
    }

    public partial class TableRowCount
    {
        public int count { get; set; }
    }
}
