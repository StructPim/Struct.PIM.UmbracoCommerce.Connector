using Examine;
using Newtonsoft.Json;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Commerce.Core.Models;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity
{
    public class Variant
    {
        public Variant() { }

        public Variant(ISearchResult searchResult) 
        {
            Id = int.Parse(searchResult["id"]);
            CultureCode = searchResult["language"];
            ConfigurationAlias = searchResult[UmbracoExamineFieldNames.ItemTypeFieldName];
            Reference = searchResult["id"];
            ProductReference = searchResult["productReference"];
            Sku = searchResult["sku"];
            Name = searchResult["name"];
            StoreId = Guid.Parse(searchResult["store"]);
            Prices = JsonConvert.DeserializeObject<List<ProductPrice>>(searchResult["prices"]) ?? new List<ProductPrice>();
            Attributes = JsonConvert.DeserializeObject<List<AttributeCombination>>(searchResult["attributes"]) ?? new List<AttributeCombination>();
            Properties = JsonConvert.DeserializeObject<Dictionary<string, string>>(searchResult["properties"]) ?? new Dictionary<string, string>();
            Stock = int.Parse(searchResult["stock"]);
        }

        public int Id { get; set; }
        public string CultureCode { get; set; } = string.Empty;
        public string ConfigurationAlias { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string ProductReference { get; set; } = string.Empty;

        public string Sku { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public int Stock { get; set; }

        public Guid StoreId { get; set; }

        public Guid? TaxClassId { get; set; }

        public List<ProductPrice> Prices { get; set; } = new List<ProductPrice>();

        public List<AttributeCombination> Attributes { get; set; } = new List<AttributeCombination>();
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> SearchableProperties { get; set; } = new Dictionary<string, string>();
            
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
