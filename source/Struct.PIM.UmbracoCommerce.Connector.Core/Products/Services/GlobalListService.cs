using Struct.PIM.Api.Models.Attribute;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Helpers;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings;
using Umbraco.Cms.Core.Models;
using Umbraco.Commerce.Core.Models;
using Umbraco.Commerce.Core.Services;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services
{
    public class GlobalListService
    {
        private readonly SettingsFacade _settingsFacade;
        private readonly PimApiHelper _pimApiHelper;
        private readonly PimAttributeHelper _pimAttributeHelper;
        private readonly IStoreService _storeService;

        public GlobalListService(SettingsFacade settingsFacade, IStoreService storeService, ITaxService taxService)
        {
            _settingsFacade = settingsFacade;
            _storeService = storeService;

            _pimApiHelper = new PimApiHelper(settingsFacade);
            _pimAttributeHelper = new PimAttributeHelper(_pimApiHelper);

            foreach(var store in storeService.GetStores())
            {
                CreateOrUpdateTaxClasses(taxService.GetTaxClasses(store.Id));
            }
        }

        public List<Api.Models.GlobalList.GlobalListValue<T>> GetGlobalListAttributeValues<T>(Guid uid)
        {
            var values = _pimApiHelper.GetGlobalListAttributeValues<T>(uid);
            return values;
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

        public void CreateOrUpdateTaxClasses(IEnumerable<TaxClassReadOnly> values)
        {
            // ensure global list exists
            var globalListUid = FindOrCreateTaxClassGlobalList();

            // get global list values
            var existingValues = GetGlobalListAttributeValues<TaxClassValue>(globalListUid);
            var stores = _storeService.GetStores().ToDictionary(x => x.Id);

            foreach(var value in values)
            {
                // find existing and update
                var existingValue = existingValues.FirstOrDefault(x => x.Value.Key == value.Id.ToString());
                if(existingValue != null)
                {
                    existingValue.Value.Name = value.Name;
                    _pimApiHelper.UpdateGlobalListValues(globalListUid, new List<Api.Models.GlobalList.GlobalListValue<TaxClassValue>> { existingValue });
                }
                // create new value
                else
                {
                    _pimApiHelper.CreateGlobalListValues(globalListUid, new List<Api.Models.GlobalList.GlobalListValue<TaxClassValue>> { new Api.Models.GlobalList.GlobalListValue<TaxClassValue>
                    {
                        Uid = Guid.NewGuid(),
                        Value = new TaxClassValue { Key = value.Id.ToString(), Name = value.Name, StoreName = stores[value.StoreId].Name, StoreId = value.StoreId.ToString() }
                    }});
                }
            }
        }

        public void DeleteTaxClass(TaxClassReadOnly value)
        {
            // ensure global list exists
            var globalListUid = FindOrCreateTaxClassGlobalList();

            // find existing value
            var existingValues = GetGlobalListAttributeValues<TaxClassValue>(globalListUid);

            var existingValue = existingValues.FirstOrDefault(x => x.Value.Key == value.Id.ToString());
            if (existingValue != null)
            {
                // delete values

                // find references and delete
            }
        }

        private Guid FindOrCreateTaxClassGlobalList()
        {
            var globalList = _pimApiHelper.GetGlobalList(Constants.TAX_CLASS_GLOBAL_LIST_ALIAS);
            Guid? globalListUid = null;
            int? globalListId = null;
            var languages = _pimApiHelper.GetLanguages();

            if(globalList == null)
            {
                var globalListFolder = _pimApiHelper.GetGlobalListFolders()?.FirstOrDefault(x => x.Name == Constants.GLOBAL_LIST_FOLDER)?.Uid;
                if(globalListFolder == null)
                    globalListFolder = _pimApiHelper.CreateGlobalListFolder(Constants.GLOBAL_LIST_FOLDER);

                globalListUid = Guid.NewGuid();
                var nameUid = Guid.NewGuid();
                var storeNameUid = Guid.NewGuid();

                globalListId = _pimApiHelper.CreateGlobalList(Constants.TAX_CLASS_GLOBAL_LIST_ALIAS, globalListFolder.Value, new Api.Models.Attribute.ComplexAttribute
                {
                    Alias = Constants.TAX_CLASS_GLOBAL_LIST_ALIAS,
                    BackofficeName = "Tax Class",
                    BackofficeDescription = "Available Tax Classes taken from Umbraco Commerce",
                    RenderedValueInBackofficeSeparator = ", ",
                    RenderValuesForBackofficeAttributeFieldUids = new List<Guid> { storeNameUid, nameUid },
                    SubAttributes = new List<Api.Models.Attribute.Attribute>
                    {
                        new Api.Models.Attribute.TextAttribute
                        {
                            Alias = Constants.TAX_CLASS_GLOBAL_LIST_KEY_ALIAS,
                            BackofficeName = "Key",
                            BackofficeDescription = "Unique Id from Umbraco Commerce",
                            Name = languages.ToDictionary(x => x.CultureCode, x => "Key"),
                            Uid = Guid.NewGuid()
                        },
                        new Api.Models.Attribute.TextAttribute
                        {
                            Alias = Constants.TAX_CLASS_GLOBAL_LIST_NAME_ALIAS,
                            BackofficeName = "Name",
                            BackofficeDescription = "Name of Tax Class",
                            Name = languages.ToDictionary(x => x.CultureCode, x => "Name"),
                            Uid = nameUid
                        },
                        new Api.Models.Attribute.TextAttribute
                        {
                            Alias = Constants.TAX_CLASS_GLOBAL_LIST_STORE_ID_ALIAS,
                            BackofficeName = "Store Id",
                            BackofficeDescription = "Id of store in Umbraco Commerce",
                            Name = languages.ToDictionary(x => x.CultureCode, x => "Store Id"),
                            Uid = Guid.NewGuid()
                        },
                        new Api.Models.Attribute.TextAttribute
                        {
                            Alias = Constants.TAX_CLASS_GLOBAL_LIST_STORE_NAME_ALIAS,
                            BackofficeName = "Store name",
                            BackofficeDescription = "Name of store",
                            Name = languages.ToDictionary(x => x.CultureCode, x => "Store name"),
                            Uid = storeNameUid
                        }
                    },
                    Uid = globalListUid.Value
                }, new List<string> { Constants.TAX_CLASS_GLOBAL_LIST_KEY_ALIAS });
            }
            else
            {
                globalListUid = globalList.Uid;
                globalListId = globalList.Id;
            }

            // ensure attribute using Global list exists
            var taxClassAttribute = _pimApiHelper.GetAttribute(Constants.TAX_CLASS_ATTRIBUTE_ALIAS);

            if (taxClassAttribute == null)
            {
                // ensure scope exists
                var scopeUid = _pimApiHelper.GetAttributeScopes().FirstOrDefault(x => x.Alias == Constants.ATTRIBUTE_SCOPE)?.Uid;
                
                if(scopeUid == null)
                {
                    scopeUid = Guid.NewGuid();
                    _pimApiHelper.CreateAttributeScope(new Api.Models.Attribute.AttributeScope
                    {
                        Alias = Constants.ATTRIBUTE_SCOPE,
                        Uid = scopeUid.Value
                    });
                }

                // create taxClass attribute
                _pimApiHelper.CreateAttribute(new Api.Models.Attribute.FixedListAttribute
                {
                    Alias = Constants.TAX_CLASS_ATTRIBUTE_ALIAS,
                    BackofficeName = "Tax Class",
                    Name = languages.ToDictionary(x => x.CultureCode, x => "Tax Class"),
                    AllowMultipleValues = true,
                    BackofficeDescription = "Specify tax class for item. Available values are taken from Umbraco Commerce. Remember to select only single per store.",
                    GlobalListId = globalListId.Value,
                    AttributeScopes = new List<Guid> { scopeUid.Value },
                    Uid = Guid.NewGuid()
                });
            }

            return globalListUid.Value;
        }
    }
}
