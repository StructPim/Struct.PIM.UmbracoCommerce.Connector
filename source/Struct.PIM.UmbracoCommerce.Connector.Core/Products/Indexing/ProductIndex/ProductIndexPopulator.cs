using Examine;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Indexing.ProductIndex
{
    public class ProductIndexPopulator : IndexPopulator
    {
        private readonly ProductService _productService;
        private readonly ProductIndexValueSetBuilder _productIndexValueSetBuilder;
        private readonly VariantIndexValueSetBuilder _variantIndexValueSetBuilder;

        public ProductIndexPopulator(ProductService productService, ProductIndexValueSetBuilder productIndexValueSetBuilder, VariantIndexValueSetBuilder variantIndexValueSetBuilder)
        {
            _productService = productService;
            _productIndexValueSetBuilder = productIndexValueSetBuilder;
            _variantIndexValueSetBuilder = variantIndexValueSetBuilder;
            RegisterIndex(IndexReferences.Product);
        }

        protected override void PopulateIndexes(IReadOnlyList<IIndex> indexes)
        {
            foreach (IIndex index in indexes)
            {
                var batchSize = 1000;

                var taken = 0;
                var productIds = _productService.GetProductIds();

                while (taken < productIds.Count())
                {
                    var batch = productIds.Skip(taken).Take(batchSize).ToList();
                    var products = _productService.GetProducts(batch, null, null).ToArray();
                    IEnumerable<ValueSet> valueSets = _productIndexValueSetBuilder.GetValueSets(products);
                    index.IndexItems(valueSets);

                    taken += batch.Count();
                }

                taken = 0;
                var variantIds = _productService.GetVariantIds();

                while (taken < variantIds.Count())
                {
                    var batch = variantIds.Skip(taken).Take(batchSize).ToList();
                    var variants = _productService.GetVariants(batch, null, null).ToArray();
                    IEnumerable<ValueSet> valueSets = _variantIndexValueSetBuilder.GetValueSets(variants);
                    index.IndexItems(valueSets);

                    taken += batch.Count();
                }
            }
        }
    }
}