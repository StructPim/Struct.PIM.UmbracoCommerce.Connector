using Struct.PIM.UmbracoCommerce.Connector.Base;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Helpers;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings;
using Umbraco.Cms.Core.Composing;
using Umbraco.Commerce.Core.Adapters;
using Umbraco.Commerce.Core.Services;

namespace Struct.PIM.UmbracoCommerce.Connector.App_Plugins
{
    public class StructComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddSingleton<SettingsFacade>();

            builder.Services.AddUnique<Core.Products.Services.ProductService, Core.Products.Services.ProductService>();
            builder.Services.AddUnique<Core.Products.Services.ConfigurationService, Core.Products.Services.ConfigurationService>();
            builder.Services.AddUnique<Core.Products.Services.AttributeService, Core.Products.Services.AttributeService>();
            builder.Services.AddUnique<IStockService, StockService>();
            builder.Services.AddUnique<IProductAdapter, ProductAdapter>();
        }
    }
}