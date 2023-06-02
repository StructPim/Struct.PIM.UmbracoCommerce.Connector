angular.module("umbraco")
    .factory("umbracoCommerceService", function ($http, $log, $cacheFactory) {
        return {
            getIntegrationSettings: function () {
                return $http.get("backoffice/structpimumbracocommerce/GetIntegrationSettings");
            },
            getAttributes: function (type) {
                return $http.get("backoffice/structpimumbracocommerce/GetAttributes?type=" + type);
            },
            getAttributeScopes: function () {
                return $http.get("backoffice/structpimumbracocommerce/GetAttributeScopes");
            },
            getDimensions: function () {
                return $http.get("backoffice/structpimumbracocommerce/GetDimensions");
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
            deleteShopSetting: function (shopSettingUid) {
                return $http.get("backoffice/structpimumbracocommerce/DeleteShopSetting?settingUid=" + shopSettingUid);
            },
            saveSetup: function (model) {
                return $http.post("backoffice/structpimumbracocommerce/SaveSetup", angular.toJson(model));
            },
            syncProductAttributes: function () {
                return $http.post("backoffice/structpimumbracocommerce/SyncProductAttributes", {});
            }
        };
    });