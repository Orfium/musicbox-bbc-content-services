using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MusicManager.Core.Utils
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class NotEmptyAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            var g = value as Guid?;

            // ignore non-guids
            if (g == null)
                return true;

            return g.Value != Guid.Empty;
        }

        public override string FormatErrorMessage(string name)
        {
            return name + " must not be empty";
        }
    }
}
