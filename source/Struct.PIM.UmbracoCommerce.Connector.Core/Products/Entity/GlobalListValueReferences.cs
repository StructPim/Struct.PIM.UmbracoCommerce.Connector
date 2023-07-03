using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity
{
    public class GlobalListValueReferences
    {
        public List<int> ProductIds { get; set; }
        public List<int> VariantIds { get; set; }
        public List<int> CategoryIds { get; set; }
    }
}
