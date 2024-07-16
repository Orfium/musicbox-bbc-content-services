using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MusicManager.Core.Utils
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
	public class MinLengthAttribute : ValidateAttribute
	{
		private readonly int minLength;

		public MinLengthAttribute(int minLength)
		{
			this.minLength = minLength;
		}

		public override bool IsValid(object value)
		{
			var collection = value as ICollection;

			// Ignore non-collection parameter
			if (collection == null)
				return true;

			return collection.Count >= minLength;
		}

		public override string FormatErrorMessage(string name)
		{
			return string.Format("{0} must contain at least {1} items.", name, minLength);
		}
	}
}
