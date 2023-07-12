using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity
{
    public class EntityAttributes
    {
        public EntityAttributes()
        {
            AttributeUids = new List<Guid>();
            PropertyAttributeUids = new List<string>();
            SearchableAttributeUids = new List<string>();
            VariationDefinitionAttributes = new Dictionary<Guid, List<Guid>>();
        }

        public List<Guid> AttributeUids { get; set; }
        public List<string> PropertyAttributeUids { get; set; }
        public List<string> SearchableAttributeUids { get; set; }
        public Dictionary<Guid, List<Guid>> VariationDefinitionAttributes { get; set; }
    }
}
