using Umbraco.Commerce.Common.Models;
using Umbraco.Commerce.Core.Adapters;
using Umbraco.Commerce.Core.Models;

namespace Struct.PIM.UmbracoCommerce.Connector.Base
{
    public class ProductAdapter : IProductAdapter
    {
        protected Core.Products.Services.ProductService _productService;

        public ProductAdapter(Core.Products.Services.ProductService productService)
        {
            _productService = productService;
        }

        public IProductSnapshot GetProductSnapshot(Guid storeId, string productReference, string languageIsoCode)
        {
            return _productService.GetProductSnapshot(storeId, productReference, languageIsoCode);
        }

        public IProductSnapshot GetProductSnapshot(Guid storeId, string productReference, string productVariantReference, string languageIsoCode)
        {
            return _productService.GetProductSnapshot(storeId, productReference, productVariantReference, languageIsoCode);
        }

        public IEnumerable<global::Umbraco.Commerce.Core.Models.Attribute> GetProductVariantAttributes(Guid storeId, string productReference, string languageIsoCode)
        {
            return _productService.GetProductVariantAttributes(storeId, productReference, languageIsoCode);
        }

        public PagedResult<IProductSummary> SearchProductSummaries(Guid storeId, string languageIsoCode, string searchTerm, long currentPage = 1, long itemsPerPage = 50)
        {
            return _productService.SearchProductSummaries(storeId, languageIsoCode, searchTerm, currentPage, itemsPerPage);
        }

        public PagedResult<IProductVariantSummary> SearchProductVariantSummaries(Guid storeId, string productReference, string languageIsoCode, string searchTerm, IDictionary<string, IEnumerable<string>> attributes, long currentPage = 1, long itemsPerPage = 50)
        {
            return _productService.SearchProductVariantSummaries(storeId, productReference, languageIsoCode, searchTerm, attributes, currentPage, itemsPerPage);
        }

        public bool TryGetProductReference(Guid storeId, string sku, out string productReference, out string productVariantReference)
        {
            return _productService.TryGetProductReference(storeId, sku, out productReference, out productVariantReference);
        }
    }
}
