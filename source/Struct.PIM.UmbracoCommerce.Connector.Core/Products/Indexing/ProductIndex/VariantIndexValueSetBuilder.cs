using Examine;
using Newtonsoft.Json;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Infrastructure.Examine;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Indexing.ProductIndex
{
    public class VariantIndexValueSetBuilder : IValueSetBuilder<Variant>
    {
        public IEnumerable<ValueSet> GetValueSets(params Variant[] variants)
        {
            foreach (Variant variant in variants.Where(CanAddToIndex))
            {
                var indexValues = new Dictionary<string, object>
                {
                    [UmbracoExamineFieldNames.NodeNameFieldName] = variant.Name,
                    ["name"] = variant.Name,
                    ["id"] = variant.Id,
                    ["productReference"] = variant.ProductReference,
                    ["language"] = variant.CultureCode,
                    ["store"] = variant.StoreId,
                    ["sku"] = variant.Sku,
                    ["searchableText"] = string.Join(" ", variant.SearchableProperties.Values),
                    ["prices"] = JsonConvert.SerializeObject(variant.Prices),
                    ["properties"] = JsonConvert.SerializeObject(variant.Properties),
                    ["attributes"] = JsonConvert.SerializeObject(variant.Attributes),
                    ["stock"] = variant.Stock
                };

                yield return new ValueSet($"variant_{variant.Id}_{variant.StoreId}_{variant.CultureCode}", IndexTypes.Variant, variant.ConfigurationAlias, indexValues);
            }
        }

        private bool CanAddToIndex(Variant variant) => true;
    }
}