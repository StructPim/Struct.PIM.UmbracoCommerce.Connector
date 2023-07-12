using Struct.PIM.UmbracoCommerce.Connector.Core.Products.EventHandlers;
using Umbraco.Commerce.Core;
using Umbraco.Commerce.Core.Events.Notification;
using Umbraco.Commerce.Extensions;

namespace Struct.PIM.UmbracoCommerce.Connector.App_Plugins
{
    public static class UmbracoCommerceUmbracoBuilderExtensions
    {
        public static IUmbracoCommerceBuilder AddConnectorEventHandlers(this IUmbracoCommerceBuilder builder)
        {
            builder.WithNotificationEvent<TaxClassCreatedNotification>()
                .RegisterHandler<TaxClassCreatedNotificationHandler>();

            builder.WithNotificationEvent<TaxClassUpdatedNotification>()
                .RegisterHandler<TaxClassUpdatedNotificationHandler>();

            builder.WithNotificationEvent<TaxClassDeletedNotification>()
                .RegisterHandler<TaxClassDeletedNotificationHandler>();

            return builder;
        }
    }
}
