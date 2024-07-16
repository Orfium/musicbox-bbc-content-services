using System;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.PrsSearch
{
    public class PrsServiceException : ApplicationException
    {
        public PrsServiceException(Exception inner)
            : base("Error response received from PRS web service", inner)
        {
        }
    }
}
