using Examine;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Commerce.Core.Events.Validation;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Indexing.ProductIndex
{
    public class ProductIndexPopulator : IndexPopulator
    {
        private readonly IndexService _indexService;

        public ProductIndexPopulator(IndexService indexService)
        {
            _indexService = indexService;
            RegisterIndex(IndexReferences.Product);
        }

        protected override void PopulateIndexes(IReadOnlyList<IIndex> indexes)
        {
            _indexService.PopulateProducts(indexes);
            _indexService.PopulateVariants(indexes);
        }        
    }
}