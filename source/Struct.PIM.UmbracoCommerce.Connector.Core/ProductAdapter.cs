using Examine;
using Examine.Search;
using NUglify.Helpers;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Indexing;
using Umbraco.Commerce.Common.Models;
using Umbraco.Commerce.Core.Adapters;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.Services;

namespace Struct.PIM.UmbracoCommerce.Connector.Base
{
    public class ProductAdapter : IProductAdapter
    {
        protected Core.Products.Services.ProductService _productService;
        protected IStoreService _storeService;
        protected IExamineManager _examineManager;

        public ProductAdapter(IStoreService storeService, Core.Products.Services.ProductService productService, IExamineManager examineManager)
        {
            _storeService = storeService;
            _productService = productService;
            _examineManager = examineManager;
        }

        public IProductSnapshot GetProductSnapshot(Guid storeId, string productReference, string languageIsoCode)
        {
            if (_examineManager.TryGetIndex(IndexReferences.Product, out var productIndex))
            {
                var searcher = productIndex.Searcher;
                var query = searcher.CreateQuery(IndexTypes.Product)
                    .Field("id", productReference.Escape())
                    .And()
                    .Field("language", _productService.GetLanguage(languageIsoCode).CultureCode.Escape())
                    .And()
                    .Field("store", storeId.ToString().Escape());

                var searchResult = query.Execute(new Examine.Search.QueryOptions(0, 1));

                if(searchResult?.Any() ?? false)
                    return new Product(searchResult.First()).AsSnapshot();
            }

            return null;
        }

        public IProductSnapshot GetProductSnapshot(Guid storeId, string productReference, string productVariantReference, string languageIsoCode)
        {
            if (_examineManager.TryGetIndex(IndexReferences.Product, out var productIndex))
            {
                var searcher = productIndex.Searcher;
                var query = searcher.CreateQuery(IndexTypes.Variant)
                    .Field("id", productVariantReference.Escape())
                    .And()
                    .Field("language", _productService.GetLanguage(languageIsoCode).CultureCode.Escape())
                    .And()
                    .Field("store", storeId.ToString().Escape());

                var searchResult = query.Execute(new Examine.Search.QueryOptions(0, 1));

                if (searchResult?.Any() ?? false)
                    return new Variant(searchResult.First()).AsSnapshot();
            }

            return null;
        }

        public IEnumerable<global::Umbraco.Commerce.Core.Models.Attribute> GetProductVariantAttributes(Guid storeId, string productReference, string languageIsoCode)
        {
            return _productService.GetProductVariantAttributes(storeId, productReference, languageIsoCode);
        }

        public PagedResult<IProductSummary> SearchProductSummaries(Guid storeId, string languageIsoCode, string searchTerm, long currentPage = 1, long itemsPerPage = 50)
        {
            if (_examineManager.TryGetIndex(IndexReferences.Product, out var productIndex))
            {
                var skip = (currentPage - 1) * itemsPerPage;

                var searcher = productIndex.Searcher;
                var query = searcher.CreateQuery(IndexTypes.Product)
                    .Field("language", _productService.GetLanguage(languageIsoCode).CultureCode.Escape())
                    .And()
                    .Field("store", storeId.ToString().Escape());

                // search on term
                if (!string.IsNullOrEmpty(searchTerm))
                    query = query.And().GroupedOr(new string[] { "searchableText" }, searchTerm.Split(' ').Select(x => x.MultipleCharacterWildcard()).ToArray());

                query.OrderByDescending(new SortableField[] { new SortableField("name") });

                var searchResult = query.Execute(new Examine.Search.QueryOptions((int)skip, (int)itemsPerPage));

                return new PagedResult<IProductSummary>(searchResult.TotalItemCount, currentPage, itemsPerPage)
                {
                    Items = searchResult.Select(x => new Product(x).AsSummary()).ToList()
                };
            }

            return new PagedResult<IProductSummary>(0, currentPage, itemsPerPage);
        }

        public PagedResult<IProductVariantSummary> SearchProductVariantSummaries(Guid storeId, string productReference, string languageIsoCode, string searchTerm, IDictionary<string, IEnumerable<string>> attributes, long currentPage = 1, long itemsPerPage = 50)
        {
            if (_examineManager.TryGetIndex(IndexReferences.Product, out var productIndex))
            {
                var skip = (currentPage - 1) * itemsPerPage;

                var searcher = productIndex.Searcher;
                var query = searcher.CreateQuery(IndexTypes.Variant)
                    .Field("productReference", productReference.Escape())
                    .And()
                    .Field("language", _productService.GetLanguage(languageIsoCode).CultureCode.Escape())
                    .And()
                    .Field("store", storeId.ToString().Escape());

                // filter on attributes
                var filterVariantIds = attributes.Values?.SelectMany(x => x.SelectMany(y => y.Split(";")))?.ToList();

                if (filterVariantIds?.Any() ?? false)
                    query = query.And().GroupedOr(new string[] { "id" }, filterVariantIds.ToArray());

                // search on term
                if (!string.IsNullOrEmpty(searchTerm))
                    query = query.And().GroupedOr(new string[] { "searchableText" }, searchTerm.Split(' ').Select(x => x.MultipleCharacterWildcard()).ToArray());

                query.OrderByDescending(new SortableField[] { new SortableField("name") });

                var searchResult = query.Execute(new Examine.Search.QueryOptions((int)skip, (int)itemsPerPage));

                return new PagedResult<IProductVariantSummary>(searchResult.TotalItemCount, currentPage, itemsPerPage)
                {
                    Items = searchResult.Select(x => new Variant(x).AsSummary()).ToList()
                };
            }

            return new PagedResult<IProductVariantSummary>(0, currentPage, itemsPerPage);
        }

        public bool TryGetProductReference(Guid storeId, string sku, out string productReference, out string productVariantReference)
        {
            var defaultLanguage = _productService.GetLanguage(null);
            productReference = null;
            productVariantReference = null;

            if (_examineManager.TryGetIndex(IndexReferences.Product, out var productIndex))
            {
                var searcher = productIndex.Searcher;
                var query = searcher.CreateQuery(IndexTypes.Variant)
                    .Field("sku", sku.Escape())
                    .And()
                    .Field("language", defaultLanguage.CultureCode.Escape())
                    .And()
                    .Field("store", storeId.ToString().Escape());

                var searchResult = query.Execute(new Examine.Search.QueryOptions(0, 1));

                if (searchResult?.Any() ?? false)
                {
                    productVariantReference = searchResult.First()["id"];
                    return true;
                }

                query = searcher.CreateQuery(IndexTypes.Product)
                    .Field("sku", sku.Escape())
                    .And()
                    .Field("language", defaultLanguage.CultureCode.Escape())
                    .And()
                    .Field("store", storeId.ToString().Escape());

                searchResult = query.Execute(new Examine.Search.QueryOptions(0, 1));

                if (searchResult?.Any() ?? false)
                {
                    productReference = searchResult.First()["id"];
                    return true;
                }
            }

            return false;
        }
    }
}
