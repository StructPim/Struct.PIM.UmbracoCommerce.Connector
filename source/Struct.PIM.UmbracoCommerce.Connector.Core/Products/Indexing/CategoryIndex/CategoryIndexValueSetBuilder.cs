using Examine;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Infrastructure.Examine;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Indexing.CategoryIndex
{
    public class CategoryIndexValueSetBuilder : IValueSetBuilder<Category>
    {
        public IEnumerable<ValueSet> GetValueSets(params Category[] categories)
        {
            foreach (Category category in categories.Where(CanAddToIndex))
            {
                var indexValues = new Dictionary<string, object>
                {
                    [UmbracoExamineFieldNames.NodeNameFieldName] = category.Name,
                    ["name"] = category.Name,
                    ["slug"] = category.Slug,
                    ["id"] = category.Id,
                    ["language"] = category.CultureCode,
                    ["store"] = category.StoreId,
                    ["parent"] = category.ParentId.GetValueOrDefault()
                };

                yield return new ValueSet($"{category.Id}_{category.StoreId}_{category.CultureCode}", IndexTypes.Category, category.CatalogueAlias, indexValues);
            }
        }

        private bool CanAddToIndex(Category category) => true;
    }
}