using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Entity;
using Struct.PIM.UmbracoCommerce.Connector.Core.Products.Helpers;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings;
using Struct.PIM.UmbracoCommerce.Connector.Core.Settings.Entity;
using Umbraco.Commerce.Core.Services;

namespace Struct.PIM.UmbracoCommerce.Connector.Core.Products.Services
{
    public class StockService : IStockService
    {
        private readonly PimApiHelper _pimApiHelper;
        private readonly PimAttributeHelper _pimAttributeHelper;
        private readonly SettingsFacade _settingsFacade;

        public StockService(SettingsFacade settingsFacade)
        {
            _settingsFacade = settingsFacade;
            _pimApiHelper = new PimApiHelper(settingsFacade);
            _pimAttributeHelper = new PimAttributeHelper(_pimApiHelper);
        }
        public decimal? GetStock(Guid storeId, string productReference)
        {
            if (string.IsNullOrEmpty(productReference))
            {
                return null;
            }

            if (!int.TryParse(productReference, out var productId))
                throw new InvalidCastException("productReference must be integer");

            GetStock(storeId, productId, null, out decimal? stock);
            return stock;
        }

        public decimal? GetStock(Guid storeId, string productReference, string productVariantReference)
        {
            if (string.IsNullOrEmpty(productReference) && string.IsNullOrEmpty(productVariantReference))
            {
                return null;
            }

            if (!int.TryParse(productReference, out var productId))
                throw new InvalidCastException("productReference must be integer");

            if (!int.TryParse(productVariantReference, out var variantId))
            {
                if (!string.IsNullOrEmpty(productVariantReference))
                {
                    throw new InvalidCastException("productVariantReference must be integer");
                }
            }

            if (!string.IsNullOrEmpty(productVariantReference))
            {
                GetStock(storeId, null, variantId, out decimal? stock);
                return stock;
            }
            else
            {
                GetStock(storeId, productId, null, out decimal? stock);
                return stock;
            }
        }

        public void IncreaseStock(Guid storeId, string productReference, decimal increaseBy)
        {
            //throw new NotImplementedException();
        }

        public void IncreaseStock(Guid storeId, string productReference, string productVariantReference, decimal increaseBy)
        {
            //throw new NotImplementedException();
        }

        public void InvalidateStockCache(Guid storeId, string productReference, string productVariantReference)
        {
            //throw new NotImplementedException();
        }

        public void InvalidateStockCache()
        {
            //throw new NotImplementedException();
        }

        public void ReduceStock(Guid storeId, string productReference, decimal reduceBy)
        {
            //throw new NotImplementedException();
        }

        public void ReduceStock(Guid storeId, string productReference, string productVariantReference, decimal reduceBy)
        {
            //throw new NotImplementedException();
        }

        public void SetStock(Guid storeId, string productReference, decimal value)
        {
            //throw new NotImplementedException();
        }

        public void SetStock(Guid storeId, string productReference, string productVariantReference, decimal value)
        {
            //throw new NotImplementedException();
        }

        public bool TryGetStock(Guid storeId, string productReference, out decimal? stock)
        {
            throw new NotImplementedException();
        }

        public bool TryGetStock(Guid storeId, string productReference, string productVariantReference, out decimal? stock)
        {
            if (string.IsNullOrEmpty(productReference) && string.IsNullOrEmpty(productVariantReference))
            {
                stock = default(decimal);
                return false;
            }

            if (!int.TryParse(productReference, out var productId))
                throw new InvalidCastException("productReference must be integer");

            if (!int.TryParse(productVariantReference, out var variantId))
            {
                if (!string.IsNullOrEmpty(productVariantReference))
                {
                    throw new InvalidCastException("productVariantReference must be integer");
                }
            }

            if (!string.IsNullOrEmpty(productVariantReference))
            {
                return GetStock(storeId, null, variantId, out stock);
            }
            else
            {
                return GetStock(storeId, productId, null, out stock);
            }
        }

        public bool TryIncreaseStock(Guid storeId, string productReference, decimal increaseBy)
        {
            //throw new NotImplementedException();
            return false;
        }

        public bool TryIncreaseStock(Guid storeId, string productReference, string productVariantReference, decimal increaseBy)
        {
            //throw new NotImplementedException();
            return false;
        }

        public bool TryReduceStock(Guid storeId, string productReference, decimal reduceBy)
        {
            //throw new NotImplementedException();
            return false;
        }

        public bool TryReduceStock(Guid storeId, string productReference, string productVariantReference, decimal reduceBy)
        {
            //throw new NotImplementedException();
            return false;
        }

        public bool TrySetStock(Guid storeId, string productReference, decimal value)
        {
            //throw new NotImplementedException();
            return false;
        }

        public bool TrySetStock(Guid storeId, string productReference, string productVariantReference, decimal value)
        {
            //throw new NotImplementedException();
            return false;
        }

        private bool GetStock(Guid storeId, int? productId, int? variantId, out decimal? stock)
        {
            var integrationSettings = _settingsFacade.GetIntegrationSettings();

            stock = default(decimal);
            var language = _pimApiHelper.GetLanguage(null);
            var storeSetting = integrationSettings?.GeneralSettings?.ShopSettings?.Where(s => s.Uid == storeId).FirstOrDefault();

            var attributeValueUids = new List<Guid>();

            if (!string.IsNullOrEmpty(storeSetting?.StockAttributeUid))
            {
                var attributeUids = storeSetting?.StockAttributeUid.Split(".");
                attributeValueUids.Add(Guid.Parse(attributeUids[0]));
            }

            if (variantId.HasValue)
            {
                var variantValues = _pimApiHelper.GetVariantsAttributeValues(new List<int> { variantId.Value }, attributeValueUids, null).ToDictionary(x => x.VariantId);

                if (variantValues.TryGetValue(variantId.Value, out var variantValue))
                {
                    if (!string.IsNullOrEmpty(storeSetting?.StockAttributeUid))
                    {
                        PimAttributeValueDTO value = _pimAttributeHelper.GetValueForAttribute(storeSetting?.StockAttributeUid, variantValue, language);
                        if (value.Value != null)
                        {
                            if (decimal.TryParse(value.Value, out decimal stockValue))
                            {
                                stock = stockValue;
                                return true;
                            }
                        }
                        else
                        {
                            stock = 0;
                            return false;
                        }
                    }

                }
            }
            else
            {
                var productValues = _pimApiHelper.GetProductsAttributeValues(new List<int> { productId.Value }, attributeValueUids, null).ToDictionary(x => x.ProductId);

                if (productValues.TryGetValue(productId.Value, out var productValue))
                {
                    if (!string.IsNullOrEmpty(storeSetting?.StockAttributeUid))
                    {
                        PimAttributeValueDTO value = _pimAttributeHelper.GetValueForAttribute(storeSetting?.StockAttributeUid, productValue, language);
                        if (value.Value != null)
                        {
                            if (decimal.TryParse(value.Value, out decimal stockValue))
                            {
                                stock = stockValue;
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }

                }
            }
            return false;
        }
    }
}