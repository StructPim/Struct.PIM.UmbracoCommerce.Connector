var app = angular.module("umbraco");

app.controller("umbracocommerce.models.controller",
    function ($scope, umbracoCommerceService, structPimUmbracoHelper, $location) {
        $scope.loaded = false;
        
        $scope.commerceSettings;
        $scope.pimVariantAttributes = [];
        $scope.pimProductAttributes = [];
        $scope.pimScopes = [];
        
        $scope.setup = {
            overlay: { show: false, model: {}, view: "" }
        }

        $scope.attributeConfig = structPimUmbracoHelper.getDefaultSelectizeConfig();
        $scope.attributeConfig.labelField = "Alias";
        $scope.attributeConfig.searchField = "Alias";
        $scope.attributeConfig.valueField = "Uid";

        $scope.attributesConfig = structPimUmbracoHelper.getDefaultSelectizeConfig();
        $scope.attributesConfig.labelField = "Alias";
        $scope.attributesConfig.searchField = "Alias";
        $scope.attributesConfig.valueField = "Uid";
        $scope.attributesConfig.maxItems = 100;

        $scope.scopeConfig = structPimUmbracoHelper.getDefaultSelectizeConfig();
        $scope.scopeConfig.labelField = "Alias";
        $scope.scopeConfig.searchField = "Alias";
        $scope.scopeConfig.valueField = "Uid";
        $scope.scopeConfig.maxItems = 100;

        $scope.controlModel = {
            activeTab: "productModel"
        };

        $scope.init = function () {
            structPimUmbracoHelper.updateTree(["data-models"]);

            umbracoCommerceService.getAttributes('Product', '')
                .then(function (response) {
                    $scope.pimProductAttributes = response.data;

                    umbracoCommerceService.getAttributes('Variant', '')
                        .then(function (response) {
                            $scope.pimVariantAttributes = response.data;

                            umbracoCommerceService.getIntegrationSettings()
                                .then(function (response) {
                                    $scope.commerceSettings = response.data;
                                    $scope.loaded = true;
                                },
                                function (response) {
                                    $scope.loaded = true;
                                    structPimUmbracoHelper.handleError(response);
                                });                            
                        },
                        function (response) {
                            $scope.loaded = true;
                            structPimUmbracoHelper.handleError(response);
                        });
                },
                function (response) {
                    $scope.loaded = true;
                    structPimUmbracoHelper.handleError(response);
                });          

            umbracoCommerceService.getAttributeScopes()
                .then(function (response) {
                    $scope.pimScopes = response.data;
                },
                function (response) {
                    structPimUmbracoHelper.handleError(response);
                });    
        };

        $scope.saveProductMapping = function () {
            umbracoCommerceService.saveProductMapping($scope.commerceSettings.ProductMapping)
                .then(function (response) {
                    structPimUmbracoHelper.setSuccessNotification("Product mapping has been updated");
                },
                function (response) {
                    structPimUmbracoHelper.handleError(response);
                });
        }
        $scope.saveVariantMapping = function () {
            umbracoCommerceService.saveVariantMapping($scope.commerceSettings.VariantMapping)
                .then(function (response) {
                    structPimUmbracoHelper.setSuccessNotification("Variant mapping has been updated");
                },
                function (response) {
                    structPimUmbracoHelper.handleError(response);
                });
        }

        $scope.init();
    });
