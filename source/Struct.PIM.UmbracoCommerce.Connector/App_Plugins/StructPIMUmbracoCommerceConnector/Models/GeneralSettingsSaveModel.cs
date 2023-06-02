using Struct.PIM.UmbracoCommerce.Connector.Core.Settings.Entity;

namespace Struct.PIM.UmbracoCommerce.Connector.App_Plugins.Models
{
    public class GeneralSettingsSaveModel
    {
        public GeneralSettings GeneralSettings { get; set; }
        public Guid ShopSettingUid { get; set; }
    }

}
