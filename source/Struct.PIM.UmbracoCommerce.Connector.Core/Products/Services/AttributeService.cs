using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Helpers;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services
{
    public class AttributeService
    {
        private readonly SettingsFacade _settingsFacade;
        private readonly PimApiHelper _pimApiHelper;

        public AttributeService(SettingsFacade settingsFacade)
        {
            _settingsFacade = settingsFacade;
            _pimApiHelper = new PimApiHelper(settingsFacade);
        }

        public List<Api.Models.Attribute.AttributeScope> GetAttributeScopes()
        {
            return _pimApiHelper.GetAttributeScopes();
        }

        public List<Entity.Attribute> GetAttributeWithProductReference()
        {
            return _pimApiHelper.GetAttributeWithProductReference();
        }

        public List<Entity.Attribute> GetAttributeWithVariantReference()
        {
            return _pimApiHelper.GetAttributeWithVariantReference();
        }

        public List<Entity.Attribute> GetAttributeWithCategoryReference()
        {
            return _pimApiHelper.GetAttributeWithCategoryReference();
        }
    }
}
