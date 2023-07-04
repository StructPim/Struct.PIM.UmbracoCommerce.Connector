using Examine;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Indexing;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services
{
    public class IndexService
    {
        private readonly IExamineManager _examineManager;
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;
        private readonly GlobalListService _globalListService;

        public IndexService(IExamineManager examineManager, ProductService productService, CategoryService categoryService, GlobalListService globalListService)
        {
            _examineManager = examineManager;
            _productService = productService;
            _categoryService = categoryService;
            _globalListService = globalListService;
        }

        public void PopulateProducts(IReadOnlyList<IIndex> indexes)
        {
            var productIds = _productService.GetProductIds();
            UpdateIndexesByProduct(indexes, productIds);
        }

        public void UpdateProducts(List<int> productIds)
        {
            if (_examineManager.TryGetIndex(IndexReferences.Product, out var productIndex))
            {
                var indexes = new List<IIndex> { productIndex };

                UpdateIndexesByProduct(indexes, productIds);

                var variantIds = _productService.GetVariantIds(productIds).Values.SelectMany(x => x).ToList();

                UpdateIndexesByVariant(indexes, variantIds);
            }
        }

        public void PopulateVariants(IReadOnlyList<IIndex> indexes)
        {
            var variantIds = _productService.GetVariantIds();
            UpdateIndexesByVariant(indexes, variantIds);
        }

        public void UpdateVariants(List<int> variantIds)
        {
            if (_examineManager.TryGetIndex(IndexReferences.Product, out var productIndex))
                UpdateIndexesByVariant(new List<IIndex> { productIndex }, variantIds);
        }
        
        public void PopulateCategories(IReadOnlyList<IIndex> indexes)
        {
            var categoryIds = _categoryService.GetCategoryIds();
            UpdateIndexesByCategory(indexes, categoryIds);
        }

        public void UpdateCategories(List<int> categoryIds)
        {
            if (_examineManager.TryGetIndex(IndexReferences.Product, out var productIndex))
                UpdateReferenceIndexesByCategory(new List<IIndex> { productIndex }, categoryIds);

            if (_examineManager.TryGetIndex(IndexReferences.Category, out var categoryIndex))
                UpdateIndexesByCategory(new List<IIndex> { categoryIndex }, categoryIds);
        }

        public void UpdateIndexesByProduct(IReadOnlyList<IIndex> indexes, List<int> productIds)
        {
            var batchSize = 1000;

            foreach (IIndex index in indexes)
            {
                if (index.Name != IndexReferences.Product)
                    continue;

                var taken = 0;

                while (taken < productIds.Count())
                {
                    var batch = productIds.Skip(taken).Take(batchSize).ToList();
                    var products = _productService.GetProducts(batch, null, null).ToArray();
                    IEnumerable<ValueSet> valueSets = products.Select(x => x.AsValueSet());
                    index.IndexItems(valueSets);
                    taken += batch.Count();
                }
            }
        }

        public void UpdateIndexesByVariant(IReadOnlyList<IIndex> indexes, List<int> variantIds)
        {
            var batchSize = 1000;

            foreach (IIndex index in indexes)
            {
                if (index.Name != IndexReferences.Product)
                    continue;

                var taken = 0;

                while (taken < variantIds.Count())
                {
                    var batch = variantIds.Skip(taken).Take(batchSize).ToList();
                    var variants = _productService.GetVariants(batch, null, null).ToArray();
                    IEnumerable<ValueSet> valueSets = variants.Select(x => x.AsValueSet());
                    index.IndexItems(valueSets);

                    taken += batch.Count();
                }
            }
        }

        public void UpdateReferenceIndexesByCategory(IReadOnlyList<IIndex> indexes, List<int> categoryIds)
        {
            var productIds = _productService.GetProductsInCategories(categoryIds);
            UpdateIndexesByProduct(indexes, productIds);            
        }

        public void UpdateIndexesByCategory(IReadOnlyList<IIndex> indexes, List<int> categoryIds)
        {
            var batchSize = 1000;

            foreach (IIndex index in indexes)
            {
                if (index.Name != IndexReferences.Category)
                    continue;

                var taken = 0;

                while (taken < categoryIds.Count())
                {
                    var batch = categoryIds.Skip(taken).Take(batchSize).ToList();
                    var categories = _categoryService.GetCategories(batch).ToArray();
                    IEnumerable<ValueSet> valueSets = categories.Select(x => x.AsValueSet());
                    index.IndexItems(valueSets);

                    taken += batch.Count();
                }
            }
        }

        public void UpdateIndexesByGlobalListValues(Dictionary<int, List<Guid>> globalListValues)
        {
            var references = _globalListService.GetGlobalListReferences(globalListValues.Values.SelectMany(x => x).ToList());

            if (_examineManager.TryGetIndex(IndexReferences.Product, out var productIndex))
            {
                if (references.ProductIds.Any())
                    UpdateIndexesByProduct(new List<IIndex> { productIndex }, references.ProductIds);

                if (references.VariantIds.Any())
                    UpdateIndexesByVariant(new List<IIndex> { productIndex }, references.VariantIds);
            }

            if (_examineManager.TryGetIndex(IndexReferences.Category, out var categoryIndex))
            {
                if (references.CategoryIds.Any())
                    UpdateIndexesByCategory(new List<IIndex> { categoryIndex }, references.CategoryIds);
            }
        }
    }
}
