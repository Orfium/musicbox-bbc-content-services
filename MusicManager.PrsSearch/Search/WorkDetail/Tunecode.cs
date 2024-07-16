using System;
using WorkDetailsServiceReference;

namespace MusicManager.PrsSearch.WorkDetail
{
    public class Tunecode : WorkDetailRequest
    {
        public string Value { get; set; }


        protected override WorkDetailsSearchExtendedRequest CreateRequest()
        {
            return new WorkDetailsSearchExtendedRequest
            {
                Tunecode = Value,
            };
        }
        
        public static Models.Work GetWork(string prsToken,string tunecode)
        {
            if (tunecode == null)
                throw new ArgumentNullException(nameof(tunecode));
            if (tunecode.Length == 0)
                throw new ArgumentException("tunecode must not be empty", nameof(tunecode));

            var search = new Tunecode {Value = tunecode};

            return search.GetResults(prsToken);
        }
    }
}