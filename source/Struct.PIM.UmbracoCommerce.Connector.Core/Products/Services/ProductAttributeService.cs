using Struct.PIM.Api.Models.Language;
using Struct.PIM.Api.Models.Shared;
using Struct.PIM.Api.Models.Variant;
using Struct.PIM.Vendr.Core.Products.Entity;
using Struct.PIM.Vendr.Core.Products.Helpers;
using Struct.PIM.Vendr.Core.Settings;
using Struct.PIM.Vendr.Core.Settings.Entity;
using Vendr.Common;
using Vendr.Core.Models;
using Vendr.Core.Services;

namespace Struct.PIM.Vendr.Core.Products.Services
{
    public class ProductAttributeService
    {
        private readonly SettingsFacade _settingsFacade;
        private readonly IProductAttributeService _vendrProductAttributeService;
        private readonly IStoreService _storeService;
        private readonly IUnitOfWorkProvider _unitOfWorkProvider;

        public ProductAttributeService(SettingsFacade settingsFacade, IUnitOfWorkProvider unitOfWorkProvider, IProductAttributeService vendrProductAttributeService, IStoreService storeService)
        {
            _settingsFacade = settingsFacade;
            _vendrProductAttributeService = vendrProductAttributeService;
            _storeService = storeService;
            _unitOfWorkProvider = unitOfWorkProvider;
        }

        protected Struct.PIM.Api.Client.StructPIMApiClient PIMClient(IntegrationSettings integrationSettings)
        {
            if (string.IsNullOrEmpty(integrationSettings.Setup.PimApiUrl))
                throw new InvalidOperationException("StructPIM.ApiUrl must be set in settings to use Struct PIM Vendr");

            if (string.IsNullOrEmpty(integrationSettings.Setup.PimApiKey))
                throw new InvalidOperationException("StructPIM.ApiKey must be set in settings to use Struct PIM Vendr");

            return new Struct.PIM.Api.Client.StructPIMApiClient(integrationSettings.Setup.PimApiUrl, integrationSettings.Setup.PimApiKey);
        }

        public void SyncProductAttributes()
        {
            var integrationSettings = _settingsFacade.GetIntegrationSettings();
            var pimApiHelper = new PimApiHelper(_settingsFacade);
            var pimAttributeHelper = new PimAttributeHelper(pimApiHelper);

            var pimClient = PIMClient(integrationSettings);
            var structures = pimClient.ProductStructures.GetProductStructures();
            var variantDefinitions = structures.Where(x => x.HasVariants && (x.VariationDefinitions?.Any() ?? false)).SelectMany(x => x.VariationDefinitions).ToList();

            var attributesToSync = variantDefinitions.SelectMany(x => x.DefiningAttributes).Distinct().ToList();
            var attributesByAlias = pimClient.Attributes.GetAttributes(attributesToSync).ToDictionary(x => x.Alias);
            var attributesByUid = attributesByAlias.ToDictionary(x => x.Value.Uid, x => x.Value);
            var variantIds = pimClient.Variants.GetVariantIds();

            var batchSize = 1000;
            var taken = 0;

            var attributeValues = new Dictionary<Guid, Dictionary<string, TranslatedValue<string>>>();
            var stores = _storeService.GetStores();
            var languages = pimApiHelper.GetLanguages();

            while (taken < variantIds.Count)
            {
                var batch = variantIds.Skip(taken).Take(batchSize).ToList();

                var values = pimClient.Variants.GetVariantAttributeValues(new VariantValuesRequestModel
                {
                    Uids = attributesToSync,
                    IncludeValues = ValueIncludeMode.Uids,
                    VariantIds = batch
                }) ;

                foreach(var variant in values)
                {
                    foreach (var val in variant.Values)
                    {
                        var attribute = attributesByAlias[val.Key];

                        if (!attributeValues.ContainsKey(attributesByAlias[val.Key].Uid))
                            attributeValues.Add(attributesByAlias[val.Key].Uid, new Dictionary<string, TranslatedValue<string>>());

                        var localizedValues = new Dictionary<string, string>();
                        var defaultValue = "";
                        foreach (var lang in languages)
                        {
                            var value = pimAttributeHelper.RenderAttribute(
                                attribute,
                                attribute,
                                variant,
                                pimApiHelper.Map(new List<Api.Models.Attribute.Attribute> { attribute }),
                                lang,
                                attribute.Uid.ToString());

                            if(!string.IsNullOrWhiteSpace(value))
                                localizedValues.Add(lang.CultureCode, value);
                        }

                        if (localizedValues.Any())
                        {
                            if (localizedValues.ContainsKey(integrationSettings.Setup.DefaultLanguage))
                                defaultValue = localizedValues[integrationSettings.Setup.DefaultLanguage];
                            else
                                defaultValue = localizedValues.First().Value;

                            if (!attributeValues[attributesByAlias[val.Key].Uid].ContainsKey(localizedValues.First().Value))
                                attributeValues[attributesByAlias[val.Key].Uid].Add(localizedValues.First().Value, new TranslatedValue<string>(defaultValue, localizedValues));
                        }
                    }
                }

                taken += batch.Count;
            }

            foreach(var store in _storeService.GetStores())
            {
                _unitOfWorkProvider.Execute(uow =>
                {
                    foreach (var val in attributeValues)
                    {
                        var attribute = attributesByUid[val.Key];
                        ProductAttribute productAttribute;

                        if (!_vendrProductAttributeService.ProductAttributeExists(store.Id, attribute.Alias))
                            productAttribute = ProductAttribute.Create(uow, store.Id, attribute.Alias, attribute.BackofficeName);   
                        
                        else
                            productAttribute = _vendrProductAttributeService.GetProductAttribute(store.Id, attribute.Alias).AsWritable(uow);
                        
                        productAttribute.SetName(new TranslatedValue<string>(attribute.BackofficeName, attribute.Name));
                        productAttribute.SetValues(val.Value);
                        _vendrProductAttributeService.SaveProductAttribute(productAttribute);
                    }

                    uow.Complete();
                });
            }
        }
    }
}
