using Examine;
using Newtonsoft.Json;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Infrastructure.Examine;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Indexing.ProductIndex
{
    public class ProductIndexValueSetBuilder : IValueSetBuilder<Product>
    {
        public IEnumerable<ValueSet> GetValueSets(params Product[] products)
        {
            foreach (Product product in products.Where(CanAddToIndex))
            {
                var indexValues = new Dictionary<string, object>
                {
                    [UmbracoExamineFieldNames.NodeNameFieldName] = product.Name,
                    ["name"] = product.Name,
                    ["id"] = product.Id,
                    ["language"] = product.CultureCode,
                    ["store"] = product.StoreId,
                    ["slug"] = product.Slug,
                    ["sku"] = product.Sku,
                    ["primaryImage"] = product.PrimaryImage,
                    ["hasVariants"] = product.HasVariants,
                    ["isGiftCard"] = product.IsGiftCard,
                    ["searchableText"] = string.Join(" ", product.SearchableProperties.Values),
                    ["prices"] = JsonConvert.SerializeObject(product.Prices),
                    ["properties"] = JsonConvert.SerializeObject(product.Properties),
                    ["categories"] = JsonConvert.SerializeObject(product.Categories)
                };

                yield return new ValueSet($"product_{product.Id}_{product.StoreId}_{product.CultureCode}", IndexTypes.Product, product.ConfigurationAlias, indexValues);
            }
        }

        private bool CanAddToIndex(Product content) => true;
    }
}