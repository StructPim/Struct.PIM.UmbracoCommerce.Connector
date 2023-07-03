using Examine;
using Newtonsoft.Json;
using Umbraco.Commerce.Core.Models;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Extensions;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity
{
    public class Product
    {
        public Product() { }

        public Product(ISearchResult searchResult)
        {
            Id = int.Parse(searchResult["id"]);
            CultureCode = searchResult["language"];
            ConfigurationAlias = searchResult[UmbracoExamineFieldNames.ItemTypeFieldName];
            ProductReference = searchResult["id"];
            Sku = searchResult["sku"];
            Name = searchResult["name"];
            Slug = searchResult["slug"];
            PrimaryImage = searchResult["primaryImageUrl"];
            StoreId = Guid.Parse(searchResult["store"]);
            HasVariants = bool.Parse(searchResult["hasVariants"]);
            IsGiftCard = bool.Parse(searchResult["isGiftCard"]);
            Prices = !string.IsNullOrEmpty(searchResult["prices"]) ? JsonConvert.DeserializeObject<List<ProductPrice>>(searchResult["prices"]) ?? new List<ProductPrice>() : new List<ProductPrice>();
            Properties = !string.IsNullOrEmpty(searchResult["properties"]) ? JsonConvert.DeserializeObject<Dictionary<string, string>>(searchResult["properties"]) ?? new Dictionary<string, string>() : new Dictionary<string, string>();
            Categories = !string.IsNullOrEmpty(searchResult["categories"]) ? JsonConvert.DeserializeObject<List<int>>(searchResult["categories"]) ?? new List<int>() : new List<int>();
            Stock = int.Parse(searchResult["stock"]);
        }

        public int Id { get; set; }
        public string ConfigurationAlias { get; set; } = string.Empty;
        public Guid StoreId { get; set; }
        public string CultureCode { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        public string ProductReference { get; set; } = string.Empty;

        public Guid? TaxClassId { get; set; }


        public bool IsGiftCard { get; set; }

        public string Sku { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string PrimaryImage { get; set; } = string.Empty;
        public string PrimaryImageUrl { get; set; } = string.Empty;
        
        public int Stock { get; set; }

        public IEnumerable<ProductPrice> Prices { get; set; } = new List<ProductPrice>();
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> SearchableProperties { get; set; } = new Dictionary<string, string>();
        public List<int> Categories { get; set; } = new List<int>();

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

        public IProductSnapshot AsSnapshot()
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

        public ValueSet AsValueSet()
        {
            var indexValues = new Dictionary<string, object>
            {
                [UmbracoExamineFieldNames.NodeNameFieldName] = Name,
                ["name"] = Name,
                ["id"] = Id,
                ["language"] = CultureCode,
                ["store"] = StoreId,
                ["slug"] = Slug,
                ["sku"] = Sku,
                ["primaryImageUrl"] = PrimaryImageUrl,
                ["hasVariants"] = HasVariants,
                ["isGiftCard"] = IsGiftCard,
                ["searchableText"] = string.Join(" ", SearchableProperties.Values),
                ["prices"] = JsonConvert.SerializeObject(Prices),
                ["properties"] = JsonConvert.SerializeObject(Properties),
                ["categories"] = JsonConvert.SerializeObject(Categories),
                ["stock"] = Stock
            };

            return new ValueSet($"product_{Id}_{StoreId}_{CultureCode}", Indexing.IndexTypes.Product, ConfigurationAlias, indexValues);
        }
    }
}
