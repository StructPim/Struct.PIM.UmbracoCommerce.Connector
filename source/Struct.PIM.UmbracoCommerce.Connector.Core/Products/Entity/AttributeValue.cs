using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity
{
    public class AttributeValue
    {
        public string Alias { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public bool HasValue
        {
            get 
            { 
                return !string.IsNullOrEmpty(Alias) && !string.IsNullOrEmpty(Value);
            }
        }
    }
}
