using Struct.PIM.Api.Models.Attribute;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Helpers;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services
{
    public class GlobalListService
    {
        private readonly SettingsFacade _settingsFacade;
        private readonly PimApiHelper _pimApiHelper;
        private readonly PimAttributeHelper _pimAttributeHelper;

        public GlobalListService(SettingsFacade settingsFacade)
        {
            _settingsFacade = settingsFacade;

            _pimApiHelper = new PimApiHelper(settingsFacade);
            _pimAttributeHelper = new PimAttributeHelper(_pimApiHelper);
        }

        public List<GlobalListValue> GetGlobalListAttributeValues(Guid uid, Guid storeId)
        {
            var storeSetting = _settingsFacade.GetIntegrationSettings().GeneralSettings?.ShopSettings?.Where(s => s.Uid == storeId).FirstOrDefault();
            var defaultLanguage = _pimApiHelper.GetLanguage(null);
            var dimensionSegmentData = _pimApiHelper.GetDimensionSegmentData(storeSetting);

            var attribute = _pimApiHelper.GetAttribute(uid);
            var globalListId = (attribute as FixedListAttribute).GlobalListId;
            var globalList = _pimApiHelper.GetGlobalList(globalListId);
            var values = _pimApiHelper.GetGlobalListAttributeValues(globalList.Uid);

            var result = new List<GlobalListValue>();

            foreach (var val in values)
            {
                result.Add(new GlobalListValue
                {
                    Uid = val.Uid.ToString(),
                    Value = _pimAttributeHelper.RenderRootAttribute(
                        globalList.Attribute,
                        new Dictionary<string, dynamic> { { globalList.Attribute.Alias, val.Value } },
                        defaultLanguage,
                        dimensionSegmentData
                    )
                });
            }

            return result.OrderBy(x => x.Value).ToList();
        }

        public GlobalListValueReferences GetGlobalListReferences(List<Guid> globalListValues)
        {
            var references = _pimApiHelper.GetGlobalListValueReferences(globalListValues);

            return new GlobalListValueReferences
            {
                ProductIds = references.Where(x => x.ReferencingProducts?.Any() ?? false).SelectMany(x => x.ReferencingProducts.Select(y => y.EntityId)).ToList(),
                VariantIds = references.Where(x => x.ReferencingVariants?.Any() ?? false).SelectMany(x => x.ReferencingVariants.Select(y => y.EntityId)).ToList(),
                CategoryIds = references.Where(x => x.ReferencingCategories?.Any() ?? false).SelectMany(x => x.ReferencingCategories.Select(y => y.EntityId)).ToList()
            };
        }
    }
}
