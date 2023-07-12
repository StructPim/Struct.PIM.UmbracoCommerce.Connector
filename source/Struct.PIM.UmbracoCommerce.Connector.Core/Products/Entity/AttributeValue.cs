using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity
{
    public class AttributeValue<T>
    {
        public string Alias { get; set; } = string.Empty;
        public T Value { get; set; }

        public bool HasValue
        {
            get 
            { 
                return !string.IsNullOrEmpty(Alias) && Value != null;
            }
        }

        public static T? TryParse(object? value)
        {
            if (value is JArray)
            {
                var arrayValues = (value as JArray).ToObject<T>();
                return arrayValues;
            }
            
            var result = value.TryConvertTo(typeof(T));
            return (T?)result.Result;
        }
    }
}
