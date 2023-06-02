using Struct.PIM.UmbracoCommerce.Connector.Base;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Helpers;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings;
using Umbraco.Cms.Core.Composing;
using Vendr.Core.Adapters;
using Vendr.Core.Services;

namespace Struct.PIM.UmbracoCommerce.Connector.App_Plugins.StructPIMVendr
{
    public class StructComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddSingleton<SettingsFacade>();

            builder.Services.AddUnique<Core.Products.Services.ProductService, Core.Products.Services.ProductService>();
            builder.Services.AddUnique<IStockService, StockService>();
            builder.Services.AddUnique<IProductAdapter, ProductAdapter>();
        }
    }
}