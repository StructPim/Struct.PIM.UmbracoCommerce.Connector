using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Helpers;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings.Entity;
using Umbraco.Cms.Core.Strings;
using Umbraco.Commerce.Common.Logging;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services
{
    public class CategoryService
    {
        private readonly SettingsFacade _settingsFacade;
        private readonly PimApiHelper _pimApiHelper;
        private readonly PimAttributeHelper _pimAttributeHelper;
        private readonly IShortStringHelper _shortStringHelper;

        public CategoryService(SettingsFacade settingsFacade, IShortStringHelper shortStringHelper)
        {
            _settingsFacade = settingsFacade;
            _shortStringHelper = shortStringHelper;

            _pimApiHelper = new PimApiHelper(settingsFacade);
            _pimAttributeHelper = new PimAttributeHelper(_pimApiHelper);
        }

        public List<Api.Models.Catalogue.CatalogueModel> GetCatalogues()
        {
            return _pimApiHelper.GetCatalogues();
        }

        public List<int> GetCategoryIds()
        {
            return _pimApiHelper.GetCategoryIds();
        }

        public EntityAttributes GetCategoryAttributes(IntegrationSettings integrationSettings)
        {
            var categoryAttributes = new EntityAttributes();

            if (!string.IsNullOrEmpty(integrationSettings.CategoryMapping?.TitleAttributeUid))
            {
                var attributeUids = integrationSettings.CategoryMapping.TitleAttributeUid.Split(".");
                categoryAttributes.AttributeUids.Add(Guid.Parse(attributeUids[0]));
            }
            return categoryAttributes;
        }

        public List<Category> GetCategories(List<int> categoryIds)
        {
            var integrationSettings = _settingsFacade.GetIntegrationSettings();
            var categories = _pimApiHelper.GetCategories(categoryIds).ToDictionary(x => x.Id);
            var attributeInfo = GetCategoryAttributes(integrationSettings);
            var categoryValues = _pimApiHelper.GetCategoryAttributeValues(categoryIds, attributeInfo.AttributeUids).ToDictionary(x => x.CategoryId);
            var catalogues = _pimApiHelper.GetCatalogues().ToDictionary(x => x.Uid);
            var items = new List<Category>();

            foreach (var storeSetting in integrationSettings.GeneralSettings.ShopSettings)
            {
                var dimensionSegmentData = _pimApiHelper.GetDimensionSegmentData(storeSetting);

                foreach (var language in _pimApiHelper.GetLanguages())
                {
                    foreach (var categoryId in categoryIds)
                    {
                        if (categoryValues.TryGetValue(categoryId, out var categoryValue) && categories.TryGetValue(categoryId, out var c))
                        {
                            var category = new Entity.Category()
                            {
                                Id = c.Id,
                                CultureCode = language.CultureCode,
                                StoreId = storeSetting.Uid,
                                ParentId = c.ParentId,
                                CatalogueAlias = catalogues[c.CatalogueUid].Alias
                            };

                            // primary properties of category
                            if (!string.IsNullOrEmpty(integrationSettings.CategoryMapping?.TitleAttributeUid))
                                category.Name = _pimAttributeHelper.GetValue<string>(integrationSettings.CategoryMapping.TitleAttributeUid, categoryValue.Values, language, dimensionSegmentData).Value;

                            // map slug
                            if (!string.IsNullOrEmpty(category.Name))
                                category.Slug = category.Name.ToUrlSegment(_shortStringHelper);
                            if (string.IsNullOrEmpty(category.Slug))
                                category.Slug = category.Id.ToString().ToUrlSegment(_shortStringHelper);

                            items.Add(category);
                        }
                    }
                }
            }

            return items;
        }
    }
}
