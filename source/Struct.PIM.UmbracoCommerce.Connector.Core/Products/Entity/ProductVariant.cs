using Umbraco.Commerce.Core.Models;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity
{
    public class ProductVariant
    {
        public string Reference { get; set; } = string.Empty;

        public string Sku { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public IEnumerable<ProductPrice> Prices { get; set; } = new List<ProductPrice>();

        public List<AttributeCombination> Attributes { get; set; } = new List<AttributeCombination>();
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }
}
