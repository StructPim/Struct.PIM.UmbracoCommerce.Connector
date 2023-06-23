using Examine;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Indexing.CategoryIndex
{
    public class CategoryIndexPopulator : IndexPopulator
    {
        private readonly CategoryService _categoryService;
        private readonly CategoryIndexValueSetBuilder _categoryIndexValueSetBuilder;

        public CategoryIndexPopulator(CategoryService categoryService, CategoryIndexValueSetBuilder categoryIndexValueSetBuilder)
        {
            _categoryService = categoryService;
            _categoryIndexValueSetBuilder = categoryIndexValueSetBuilder;
            RegisterIndex(IndexReferences.Category);
        }

        protected override void PopulateIndexes(IReadOnlyList<IIndex> indexes)
        {
            foreach (IIndex index in indexes)
            {
                var batchSize = 1000;

                var taken = 0;
                var categoryIds = _categoryService.GetCategoryIds();

                while (taken < categoryIds.Count())
                {
                    var batch = categoryIds.Skip(taken).Take(batchSize).ToList();
                    var categories = _categoryService.GetCategories(batch).ToArray();
                    IEnumerable<ValueSet> valueSets = _categoryIndexValueSetBuilder.GetValueSets(categories);
                    index.IndexItems(valueSets);

                    taken += batch.Count();
                }
            }
        }
    }
}