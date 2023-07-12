using Examine;
using Struct.PIM.UmbracoCommerce.Connector.Base;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Helpers;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Indexing;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Indexing.CategoryIndex;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Indexing.ProductIndex;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Commerce.Core.Adapters;
using Umbraco.Commerce.Core.Cache;
using Umbraco.Commerce.Core.Services;
using Umbraco.Commerce.Extensions;

namespace Struct.PIM.UmbracoCommerce.Connector.App_Plugins
{
    public class StructComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddSingleton<SettingsFacade>();

            builder.Services.AddUnique<Core.Products.Services.ProductService, Core.Products.Services.ProductService>();
            builder.Services.AddUnique<CategoryService, CategoryService>();
            builder.Services.AddUnique<GlobalListService, GlobalListService>();
            builder.Services.AddUnique<ConfigurationService, ConfigurationService>();
            builder.Services.AddUnique<AttributeService, AttributeService>();
            builder.Services.AddUnique<IndexService, IndexService>();
            builder.Services.AddUnique<IStockService, StockService>();
            builder.Services.AddUnique<IProductAdapter, ProductAdapter>();

            builder.Services.AddExamineLuceneIndex<ProductIndex, ConfigurationEnabledDirectoryFactory>(IndexReferences.Product);
            builder.Services.ConfigureOptions<ConfigureProductIndexOptions>();
            builder.Services.AddSingleton<IIndexPopulator, ProductIndexPopulator>();

            builder.Services.AddExamineLuceneIndex<CategoryIndex, ConfigurationEnabledDirectoryFactory>(IndexReferences.Category);
            builder.Services.ConfigureOptions<ConfigureCategoryIndexOptions>();
            builder.Services.AddSingleton<IIndexPopulator, CategoryIndexPopulator>();

            builder.AddUmbracoCommerce(umbracoCommerceBuilder => {
                umbracoCommerceBuilder.AddConnectorEventHandlers();
            });
        }
    }
}