﻿var app = angular.module("umbraco");

app.controller("umbracocommerce.models.controller",
    function ($scope, umbracoCommerceService, structPimUmbracoHelper, $location) {
        $scope.loaded = false;
        
        $scope.commerceSettings;
        $scope.pimVariantAttributes = [];
        $scope.pimProductAttributes = [];
        $scope.pimCategoryAttributes = [];
        $scope.pimScopes = [];
        $scope.isValid = true;
        
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

            umbracoCommerceService.isSetupValid()
                .then(function (response) {
                    $scope.isValid = response.data;
                    if ($scope.isValid) {
                        $scope.loadData();
                    }
                });
        };

        $scope.loadData = function () {
            umbracoCommerceService.getIntegrationSettings()
                .then(function (response) {
                    $scope.commerceSettings = response.data;
                },
                function (response) {
                    structPimUmbracoHelper.handleError(response);
                });

            umbracoCommerceService.getAttributes('Product', '')
                .then(function (response) {
                    $scope.pimProductAttributes = response.data;
                    $scope.loaded = true;
                },
                function (response) {
                    $scope.loaded = true;
                    structPimUmbracoHelper.handleError(response);
                });

            umbracoCommerceService.getAttributes('Variant', '')
                .then(function (response) {
                    $scope.pimVariantAttributes = response.data;
                },
                function (response) {
                    structPimUmbracoHelper.handleError(response);
                });

            umbracoCommerceService.getAttributes('Category', '')
                .then(function (response) {
                    $scope.pimCategoryAttributes = response.data;
                },
                function (response) {
                    structPimUmbracoHelper.handleError(response);
                });

            umbracoCommerceService.getAttributeScopes()
                .then(function (response) {
                    $scope.pimScopes = response.data;
                },
                function (response) {
                    structPimUmbracoHelper.handleError(response);
                });  
        }

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

        $scope.saveCategoryMapping = function () {
            umbracoCommerceService.saveCategoryMapping($scope.commerceSettings.CategoryMapping)
                .then(function (response) {
                    structPimUmbracoHelper.setSuccessNotification("Category mapping has been updated");
                },
                    function (response) {
                        structPimUmbracoHelper.handleError(response);
                    });
        }

        $scope.init();
    });
