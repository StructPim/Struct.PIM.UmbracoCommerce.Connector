using Examine;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services;
using Umbraco.Cms.Infrastructure.Examine;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Indexing.CategoryIndex
{
    public class CategoryIndexPopulator : IndexPopulator
    {
        private readonly IndexService _indexService;

        public CategoryIndexPopulator(IndexService indexService)
        {
            _indexService = indexService;
            RegisterIndex(IndexReferences.Category);
        }

        protected override void PopulateIndexes(IReadOnlyList<IIndex> indexes)
        {
            _indexService.PopulateCategories(indexes);
        }
    }
}