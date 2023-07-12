
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services;
using Umbraco.Commerce.Common.Events;
using Umbraco.Commerce.Core.Events.Notification;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.EventHandlers
{
    public class TaxClassUpdatedNotificationHandler : NotificationEventHandlerBase<TaxClassUpdatedNotification>
    {
        private readonly GlobalListService _globalListService;

        public TaxClassUpdatedNotificationHandler(GlobalListService globalListService)
        {
            _globalListService = globalListService;
        }

        public override void Handle(TaxClassUpdatedNotification evt)
        {
            _globalListService.CreateOrUpdateTaxClasses(new List<Umbraco.Commerce.Core.Models.TaxClassReadOnly> { evt.TaxClass });
        }
    }
}
