
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services;
using Umbraco.Commerce.Common.Events;
using Umbraco.Commerce.Core.Events.Notification;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.EventHandlers
{
    public class TaxClassCreatedNotificationHandler : NotificationEventHandlerBase<TaxClassCreatedNotification>
    {
        private readonly GlobalListService _globalListService;

        public TaxClassCreatedNotificationHandler(GlobalListService globalListService) 
        {
            _globalListService = globalListService;
        }

        public override void Handle(TaxClassCreatedNotification evt)
        {
            _globalListService.CreateOrUpdateTaxClasses(new List<Umbraco.Commerce.Core.Models.TaxClassReadOnly> { evt.TaxClass });
        }
    }
}
