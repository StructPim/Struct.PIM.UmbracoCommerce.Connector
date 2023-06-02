using Umbraco.Commerce.Core.Models;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity
{
    public class ProductSnapshot : IProductSnapshot
    {
        public Guid StoreId { get; set; }

        public string ProductReference { get; set; }

        public string ProductVariantReference { get; set; }

        public IEnumerable<AttributeCombination> Attributes { get; set; }

        public Guid? TaxClassId { get; set; }

        public IDictionary<string, string> Properties { get; set; }

        public bool IsGiftCard { get; set; }

        public string Sku { get; set; }

        public string Name { get; set; }

        public IEnumerable<ProductPrice> Prices { get; set; }
    }
}
