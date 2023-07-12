using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity
{
    internal class TaxClassValue
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("storeId")]
        public string StoreId { get; set; }
        [JsonProperty("storeName")]
        public string StoreName { get; set; }
    }
}

