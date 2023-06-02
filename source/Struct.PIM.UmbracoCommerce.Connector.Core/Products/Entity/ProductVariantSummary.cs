using Umbraco.Commerce.Core.Models;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity
{
    public class ProductVariantSummary : IProductVariantSummary
    {
        public ProductVariantSummary(ProductVariant variant) 
        {
            Reference = variant.Reference;
            Sku = variant.Sku;
            Name = variant.Name;
            Prices = variant.Prices;
            Attributes = variant.Attributes.ToDictionary(x => x.Name.Name, x => x.Value.Name);
        }
        public string Reference { get; set; } = string.Empty;

        public string Sku { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public IEnumerable<ProductPrice> Prices { get; set; } = new List<ProductPrice>();

        public IReadOnlyDictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
    }
}
