using Soundmouse.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elasticsearch.Configuration
{
    public class DictionaryConfigurationReader : ConfigurationReader
    {
        public static IDictionary<string, string> Values { get; set; }

        public override string GetString(string key)
        {
            return Values[key];
        }
        
    }
}
