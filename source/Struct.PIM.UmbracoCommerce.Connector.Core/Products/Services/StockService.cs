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
        private readonly AsyncLocal<Dictionary<int, decimal?>> _productStock = new AsyncLocal<Dictionary<int, decimal?>>();
        private readonly AsyncLocal<Dictionary<int, decimal?>> _variantStock = new AsyncLocal<Dictionary<int, decimal?>>();

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
        }

        public void IncreaseStock(Guid storeId, string productReference, string productVariantReference, decimal increaseBy)
        {
        }

        public void InvalidateStockCache(Guid storeId, string productReference, string productVariantReference)
        {
        }

        public void InvalidateStockCache()
        {
        }

        public void ReduceStock(Guid storeId, string productReference, decimal reduceBy)
        {
        }

        public void ReduceStock(Guid storeId, string productReference, string productVariantReference, decimal reduceBy)
        {
        }

        public void SetStock(Guid storeId, string productReference, decimal value)
        {
        }

        public void SetStock(Guid storeId, string productReference, string productVariantReference, decimal value)
        {
        }

        public bool TryGetStock(Guid storeId, string productReference, out decimal? stock)
        {
            if (string.IsNullOrEmpty(productReference))
            {
                stock = null;
                return false;
            }

            if (!int.TryParse(productReference, out var productId))
                throw new InvalidCastException("productReference must be integer");

            return GetStock(storeId, productId, null, out stock);
        }

        public bool TryGetStock(Guid storeId, string productReference, string productVariantReference, out decimal? stock)
        {
            if (string.IsNullOrEmpty(productReference) && string.IsNullOrEmpty(productVariantReference))
            {
                stock = null;
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
            return false;
        }

        public bool TryIncreaseStock(Guid storeId, string productReference, string productVariantReference, decimal increaseBy)
        {
            return false;
        }

        public bool TryReduceStock(Guid storeId, string productReference, decimal reduceBy)
        {
            return false;
        }

        public bool TryReduceStock(Guid storeId, string productReference, string productVariantReference, decimal reduceBy)
        {
            return false;
        }

        public bool TrySetStock(Guid storeId, string productReference, decimal value)
        {
            return false;
        }

        public bool TrySetStock(Guid storeId, string productReference, string productVariantReference, decimal value)
        {
            return false;
        }

        private bool GetStock(Guid storeId, int? productId, int? variantId, out decimal? stock)
        {
            if(_variantStock.Value != null && variantId.HasValue && _variantStock.Value.TryGetValue(variantId.Value, out var variantStock))
            {
                stock = variantStock;
                return variantStock.HasValue;
            }
            else if (_productStock.Value != null && productId.HasValue && _productStock.Value.TryGetValue(productId.Value, out var productStock))
            {
                stock = productStock;
                return productStock.HasValue;
            }
            var integrationSettings = _settingsFacade.GetIntegrationSettings();

            stock = null;
            var language = _pimApiHelper.GetLanguage(null);
            var storeSetting = integrationSettings?.GeneralSettings?.ShopSettings?.Where(s => s.Uid == storeId).FirstOrDefault();
            var dimensionSegmentData = _pimApiHelper.GetDimensionSegmentData(storeSetting);

            Guid? stockAttributeUid = null;
            Api.Models.Attribute.Attribute? stockAttribute = null;

            if (!string.IsNullOrEmpty(storeSetting?.StockAttributeUid))
            {
                var attributeUids = storeSetting?.StockAttributeUid.Split(".");
                stockAttributeUid = Guid.Parse(attributeUids[0]);
                stockAttribute = _pimApiHelper.GetAttribute(stockAttributeUid.Value);
            }

            if (stockAttribute != null)
            {
                if (variantId.HasValue)
                {
                    if (_variantStock.Value == null)
                        _variantStock.Value = new Dictionary<int, decimal?>();

                    var variant = _pimApiHelper.GetVariant(variantId.Value);
                    var variantValues = _pimApiHelper.GetVariantsAttributeValuesByProductId(variant.ProductId, new List<Guid> { stockAttributeUid.Value }, null).ToDictionary(x => x.VariantId);
                    
                    foreach (var v in variantValues)
                    {
                        if (variantValues.TryGetValue(v.Key, out var variantValue))
                        {
                            var value = _pimAttributeHelper.GetDecimalValue(storeSetting.StockAttributeUid, variantValue.Values, language, dimensionSegmentData);

                            if(!_variantStock.Value.ContainsKey(v.Key))
                                _variantStock.Value.TryAdd(v.Key, value);
                        }
                    }

                    if (_variantStock.Value.TryGetValue(variantId.Value, out var vStock))
                    {
                        stock = vStock;
                        return vStock.HasValue;
                    }
                }
                else
                {
                    if (_productStock.Value == null)
                        _productStock.Value = new Dictionary<int, decimal?>();

                    var product = _pimApiHelper.GetProduct(productId.Value);
                    
                    if (!product.VariationDefinitionUid.HasValue)
                    {
                        var productValues = _pimApiHelper.GetProductAttributeValues(new List<int> { productId.Value }, new List<Guid> { stockAttribute.Uid }, null).ToDictionary(x => x.ProductId);

                        if (productValues.TryGetValue(productId.Value, out var productValue))
                        {
                            var value = _pimAttributeHelper.GetDecimalValue(storeSetting.StockAttributeUid, productValue.Values, language, dimensionSegmentData);

                            _productStock.Value.Add(productId.Value, value);

                            stock = value;
                            return stock.HasValue;
                        }
                    }
                    else
                    {
                        if (_variantStock.Value == null)
                            _variantStock.Value = new Dictionary<int, decimal?>();

                        var variantValues = _pimApiHelper.GetVariantsAttributeValuesByProductId(productId.Value, new List<Guid> { stockAttributeUid.Value }, null).ToDictionary(x => x.VariantId);
                        decimal? totalStock = null;
                        
                        foreach(var v in variantValues)
                        {
                            if (variantValues.TryGetValue(v.Key, out var variantValue))
                            {
                                var value = _pimAttributeHelper.GetDecimalValue(storeSetting.StockAttributeUid, variantValue.Values, language, dimensionSegmentData);

                                if (!_variantStock.Value.ContainsKey(v.Key))
                                    _variantStock.Value.Add(v.Key, value);

                                if (value.HasValue)
                                    totalStock = totalStock.HasValue ? totalStock.Value + value.Value : value.Value;
                                
                            }
                        }

                        _productStock.Value.Add(productId.Value, totalStock);

                        stock = totalStock;
                        return totalStock.HasValue;
                    }
                }
            }
            return false;
        }
    }
}