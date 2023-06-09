using Umbraco.Commerce.Core.Models;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity
{
    public class Variant
    {
        public string Reference { get; set; } = string.Empty;
        public string ProductReference { get; set; } = string.Empty;

        public string Sku { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public Guid StoreId { get; set; }

        public Guid? TaxClassId { get; set; }

        public IEnumerable<ProductPrice> Prices { get; set; } = new List<ProductPrice>();

        public List<AttributeCombination> Attributes { get; set; } = new List<AttributeCombination>();
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        public IProductVariantSummary AsSummary()
        {
            return new VariantSummary
            {
                Reference = Reference,
                Sku = Sku,
                Name = Name,
                Prices = Prices,
                Attributes = Attributes.ToDictionary(x => x.Name.Name, x => x.Value.Name)
            };
        }

        public IProductSnapshot AsSnapshot()
        {
            return new VariantSnapshot
            {
                ProductVariantReference = Reference,
                ProductReference = ProductReference,
                Sku = Sku,
                Name = Name,
                Prices = Prices,
                Properties = Properties,
                StoreId = StoreId,
                TaxClassId = TaxClassId,
                Attributes = Attributes
            };
        }
    }
}
