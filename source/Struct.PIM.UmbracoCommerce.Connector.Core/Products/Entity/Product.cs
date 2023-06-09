using Umbraco.Commerce.Core.Models;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity
{
    public class Product
    {
        public Guid StoreId { get; set; }

        public string ProductReference { get; set; }

        public Guid? TaxClassId { get; set; }

        public IDictionary<string, string> Properties { get; set; }

        public bool IsGiftCard { get; set; }

        public string Sku { get; set; }

        public string Name { get; set; }

        public IEnumerable<ProductPrice> Prices { get; set; }

        public bool HasVariants { get; set; }

        public IProductSummary AsSummary()
        {
            return new ProductSummary
            {
                Reference = ProductReference,
                Sku = Sku,
                Name = Name,
                Prices = Prices,
                HasVariants = HasVariants
            };
        }

        public IProductSnapshot AsSnapShot()
        {
            return new ProductSnapshot
            {
                ProductReference = ProductReference,
                Sku = Sku,
                Name = Name,
                Prices = Prices,
                IsGiftCard = IsGiftCard,
                Properties = Properties,
                StoreId = StoreId,
                TaxClassId = TaxClassId
            };
        }
    }
}
