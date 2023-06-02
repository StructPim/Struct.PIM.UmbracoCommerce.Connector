using Newtonsoft.Json;
using Struct.PIM.ShopifyConnector.Settings.Entity;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings.Entity;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Commerce.Core.Services;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Settings
{
    public class SettingsFacade
    {
        private static IntegrationSettings? _integrationSettings;
        private static object _settingsLock = new object();

        public SettingsFacade(IScopeProvider scopeProvider, IStoreService storeService, ICurrencyService currencyService) {
            _scopeProvider = scopeProvider;
            _storeService = storeService;
            _currencyService = currencyService;
        }

        private readonly IScopeProvider _scopeProvider;
        private readonly IStoreService _storeService;
        private readonly ICurrencyService _currencyService;

        public IntegrationSettings GetIntegrationSettings(bool syncUmbracoCommerce = false)
        {
            if (_integrationSettings != null && !syncUmbracoCommerce)
                return _integrationSettings;

            lock(_settingsLock)
            {
                if (_integrationSettings != null && !syncUmbracoCommerce)
                    return _integrationSettings;

                var integrationSettings = new IntegrationSettings();

                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

                using (var scope = _scopeProvider.CreateScope())
                {
                    var result = scope.Database.Fetch<StructPimIntegrationSettingsDbModel>("select * from StructPIMIntegrationSettings").ToDictionary(x => (string)x.Key, x => (string)x.Value);

                    if (result.TryGetValue("GeneralSettings", out string generalsettings))
                    {
                        integrationSettings.GeneralSettings = JsonConvert.DeserializeObject<GeneralSettings>(generalsettings);

                        if (syncUmbracoCommerce)
                        {
                            var stores = _storeService.GetStores().ToDictionary(x => x.Id);

                            if (integrationSettings.GeneralSettings.ShopSettings == null)
                            {
                                integrationSettings.GeneralSettings.ShopSettings = stores.Select(x => new StoreSettings
                                {
                                    Uid = x.Key,
                                    Name = x.Value.Name,
                                    PriceMapping = _currencyService.GetCurrencies(x.Key).Select(y => new PriceInfo
                                    {
                                        Currency = y.Code,
                                        Uid = y.Id
                                    }).ToList()
                                }).ToList();
                            }
                            else
                            {
                                integrationSettings.GeneralSettings.ShopSettings = integrationSettings.GeneralSettings.ShopSettings.Where(x => stores.ContainsKey(x.Uid)).ToList();

                                foreach (var store in stores.Values)
                                {
                                    var currentSettings = integrationSettings.GeneralSettings.ShopSettings.FirstOrDefault(x => x.Uid == store.Id);
                                    var currencies = _currencyService.GetCurrencies(store.Id).ToDictionary(x => x.Id);

                                    if (currentSettings != null)
                                    {
                                        currentSettings.Name = store.Name;

                                        if (currentSettings.PriceMapping == null)
                                            currentSettings.PriceMapping = currencies.Values.Select(x => new PriceInfo
                                            {
                                                Currency = x.Code,
                                                Uid = x.Id
                                            }).ToList();
                                        else
                                        {
                                            currentSettings.PriceMapping = currentSettings.PriceMapping.Where(x => currencies.ContainsKey(x.Uid)).ToList();

                                            foreach (var currency in currencies.Values)
                                            {
                                                var currentCurrency = currentSettings.PriceMapping.FirstOrDefault(x => x.Uid == currency.Id);

                                                if (currentCurrency != null)
                                                    currentCurrency.Currency = currency.Code;
                                                else
                                                {
                                                    currentSettings.PriceMapping.Add(new PriceInfo
                                                    {
                                                        Currency = currency.Code,
                                                        Uid = currency.Id
                                                    });
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        integrationSettings.GeneralSettings.ShopSettings.Add(new StoreSettings
                                        {
                                            Uid = store.Id,
                                            Name = store.Name,
                                            PriceMapping = currencies.Values.Select(y => new PriceInfo
                                            {
                                                Currency = y.Code,
                                                Uid = y.Id
                                            }).ToList()
                                        });
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        integrationSettings.GeneralSettings = new GeneralSettings();

                        if (syncUmbracoCommerce)
                        {
                            var stores = _storeService.GetStores();
                            integrationSettings.GeneralSettings.ShopSettings = stores.Select(x => new StoreSettings
                            {
                                Uid = x.Id,
                                Name = x.Name
                            }).ToList();
                        }
                    }
                    if (result.TryGetValue("ProductMapping", out string productmapping))
                    {
                        integrationSettings.ProductMapping = JsonConvert.DeserializeObject<ProductMapping>(productmapping);
                    }
                    else
                    {
                        integrationSettings.ProductMapping = new ProductMapping();
                    }
                    if (result.TryGetValue("VariantMapping", out string variantmapping))
                    {
                        integrationSettings.VariantMapping = JsonConvert.DeserializeObject<VariantMapping>(variantmapping);
                    }
                    else
                    {
                        integrationSettings.VariantMapping = new VariantMapping();
                    }
                    if (result.TryGetValue("Setup", out string setup))
                    {
                        integrationSettings.Setup = JsonConvert.DeserializeObject<Setup>(setup);
                    }
                    else
                    {
                        integrationSettings.Setup = new Setup();
                    }

                    _integrationSettings = integrationSettings;

                    return integrationSettings;
                }
            }
        }

        public void SaveGeneralSettings(GeneralSettings editorModel, Guid shopSettingsUid)
        {
            SaveSetting("GeneralSettings", editorModel);
        }

        public void SaveGeneralSettings(GeneralSettings editorModel)
        {
            SaveSetting("GeneralSettings", editorModel);
        }

        public void SaveVariantMapping(VariantMapping editorModel)
        {
            SaveSetting("VariantMapping", editorModel);
        }

        public void SaveProductMapping(ProductMapping editorModel)
        {
            SaveSetting("ProductMapping", editorModel);
        }

        public void SaveSetup(Setup editorModel)
        {
            SaveSetting("Setup", editorModel);
        }

        private void SaveSetting(string key, dynamic value)
        {
            var model = new StructPimIntegrationSettingsDbModel()
            {
                Key = key,
                Value = JsonConvert.SerializeObject(value)
            };

            using (var scope = _scopeProvider.CreateScope())
            {
                
                var sql = $@"
                        UPDATE StructPIMIntegrationSettings
                            SET
                                [Value] =  @value                                
                        WHERE [Key] = @key
                ";
                
                var count = scope.Database.Execute(sql, new { key = model.Key, value = model.Value });
                if (count == 0)
                {
                    sql = $@"INSERT INTO StructPIMIntegrationSettings
                            ([Key], [Value])
                        VALUES
                            (@key, @value)";

                    count = scope.Database.Execute(sql, new { key = model.Key, value = model.Value });

                    if (count != 1)
                    {

                    }
                }

                scope.Complete();
            }

            _integrationSettings = null;
        }
    }
}
