
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services;
using Umbraco.Commerce.Common.Events;
using Umbraco.Commerce.Core.Events.Notification;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.EventHandlers
{
    public class TaxClassDeletedNotificationHandler : NotificationEventHandlerBase<TaxClassDeletedNotification>
    {
        private readonly GlobalListService _globalListService;

        public TaxClassDeletedNotificationHandler(GlobalListService globalListService)
        {
            _globalListService = globalListService;
        }

        public override void Handle(TaxClassDeletedNotification evt)
        {
            _globalListService.DeleteTaxClass(evt.TaxClass);
        }
    }
}
