using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Helpers;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services
{
    public class ConfigurationService
    {
        private readonly SettingsFacade _settingsFacade;
        private readonly PimApiHelper _pimApiHelper;

        public ConfigurationService(SettingsFacade settingsFacade)
        {
            _settingsFacade = settingsFacade;
            _pimApiHelper = new PimApiHelper(settingsFacade);
        }

        public List<Api.Models.Dimension.DimensionModel> GetDimensions()
        {
            return _pimApiHelper.GetDimensions();
        }

        public List<Api.Models.Language.LanguageModel> GetLanguages()
        {
            return _pimApiHelper.GetLanguages();
        }
    }
}
