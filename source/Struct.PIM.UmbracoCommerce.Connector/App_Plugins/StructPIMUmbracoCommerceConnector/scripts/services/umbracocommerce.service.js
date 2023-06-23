angular.module("umbraco")
    .factory("umbracoCommerceService", function ($http, $log, $cacheFactory) {
        return {
            getIntegrationSettings: function () {
                return $http.get("backoffice/structpimumbracocommerce/GetIntegrationSettings");
            },
            getAttributes: function (type, attributeType) {
                return $http.get("backoffice/structpimumbracocommerce/GetAttributes?type=" + type + "&attributetype=" + attributeType);
            },
            getFilterAttributeValues: function (filter, storeId) {
                return $http.get("backoffice/structpimumbracocommerce/GetFilterAttributeValues?filter=" + filter + "&storeid=" + storeId);
            },
            getAttributeScopes: function () {
                return $http.get("backoffice/structpimumbracocommerce/GetAttributeScopes");
            },
            getDimensions: function () {
                return $http.get("backoffice/structpimumbracocommerce/GetDimensions");
            },
            getCatalogues: function () {
                return $http.get("backoffice/structpimumbracocommerce/GetCatalogues");
            },
            getLanguages: function () {
                return $http.get("backoffice/structpimumbracocommerce/GetLanguages");
            },
            saveGeneralSettings: function (settings, shopSettingUid) {
                return $http.post("backoffice/structpimumbracocommerce/SaveGeneralSettings", angular.toJson({ generalSettings: settings, shopSettingUid: shopSettingUid }));
            },
            saveProductMapping: function (model) {
                return $http.post("backoffice/structpimumbracocommerce/SaveProductMapping", angular.toJson(model));
            },
            saveVariantMapping: function (model) {
                return $http.post("backoffice/structpimumbracocommerce/SaveVariantMapping", angular.toJson(model));
            },
            saveCategoryMapping: function (model) {
                return $http.post("backoffice/structpimumbracocommerce/SaveCategoryMapping", angular.toJson(model));
            },
            deleteShopSetting: function (shopSettingUid) {
                return $http.get("backoffice/structpimumbracocommerce/DeleteShopSetting?settingUid=" + shopSettingUid);
            },
            saveSetup: function (model) {
                return $http.post("backoffice/structpimumbracocommerce/SaveSetup", angular.toJson(model));
            },
            syncProductAttributes: function () {
                return $http.post("backoffice/structpimumbracocommerce/SyncProductAttributes", {});
            },
            isSetupValid: function () {
                return $http.get("backoffice/structpimumbracocommerce/IsSetupValid");
            }
        };
    });