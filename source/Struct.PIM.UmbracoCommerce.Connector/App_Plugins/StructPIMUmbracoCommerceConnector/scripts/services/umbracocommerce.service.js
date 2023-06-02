angular.module("umbraco")
    .factory("umbracoCommerceService", function ($http, $log, $cacheFactory) {
        return {
            getIntegrationSettings: function () {
                return $http.get("backoffice/vendr/GetIntegrationSettings");
            },
            getAttributes: function (type) {
                return $http.get("backoffice/vendr/GetAttributes?type=" + type);
            },
            getAttributeScopes: function () {
                return $http.get("backoffice/vendr/GetAttributeScopes");
            },
            getDimensions: function () {
                return $http.get("backoffice/vendr/GetDimensions");
            },
            getLanguages: function () {
                return $http.get("backoffice/vendr/GetLanguages");
            },
            saveGeneralSettings: function (settings, shopSettingUid) {
                return $http.post("backoffice/vendr/SaveGeneralSettings", angular.toJson({ generalSettings: settings, shopSettingUid: shopSettingUid }));
            },
            saveProductMapping: function (model) {
                return $http.post("backoffice/vendr/SaveProductMapping", angular.toJson(model));
            },
            saveVariantMapping: function (model) {
                return $http.post("backoffice/vendr/SaveVariantMapping", angular.toJson(model));
            },
            deleteShopSetting: function (shopSettingUid) {
                return $http.get("backoffice/vendr/DeleteShopSetting?settingUid=" + shopSettingUid);
            },
            saveSetup: function (model) {
                return $http.post("backoffice/vendr/SaveSetup", angular.toJson(model));
            },
            syncProductAttributes: function () {
                return $http.post("backoffice/vendr/SyncProductAttributes", {});
            }
        };
    });