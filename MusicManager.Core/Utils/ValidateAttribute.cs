using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MusicManager.Core.Utils
{
    public class CompositeValidationResult : ValidationResult
    {
        private readonly List<ValidationResult> _results = new List<ValidationResult>();

        public IEnumerable<ValidationResult> Results => _results;

        public CompositeValidationResult(string errorMessage) : base(errorMessage)
        {
        }

        public CompositeValidationResult(string errorMessage, IEnumerable<string> memberNames) : base(errorMessage,
            memberNames)
        {
        }

        protected CompositeValidationResult(ValidationResult validationResult) : base(validationResult)
        {
        }

        public void AddResult(ValidationResult validationResult)
        {
            _results.Add(validationResult);
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ValidateAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            var results = new List<ValidationResult>();

            if (value is IEnumerable enumerable)
            {
                foreach (var item in enumerable.Cast<object>())
                {
                    var context = new ValidationContext(item, null, null);
                    Validator.TryValidateObject(item, context, results, true);
                }
            }
            else
            {
                var context = new ValidationContext(value, null, null);
                Validator.TryValidateObject(value, context, results, true);
            }

            if (results.Count != 0)
            {
                var compositeResults =
                    new CompositeValidationResult($"Validation for {validationContext.DisplayName} failed.");

                results.ForEach(compositeResults.AddResult);
                return compositeResults;
            }

            return ValidationResult.Success;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"One or more properties of {name} failed to validate.";
        }
    }
}
