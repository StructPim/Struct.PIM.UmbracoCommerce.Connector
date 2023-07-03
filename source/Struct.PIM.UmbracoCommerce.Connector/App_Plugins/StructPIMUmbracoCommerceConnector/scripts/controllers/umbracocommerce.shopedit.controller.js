var app = angular.module("umbraco");

app.controller("umbracocommerce.shopedit.controller",
    function ($scope, umbracoCommerceService, structPimUmbracoHelper, $location, $routeParams) {

        $scope.loaded = false;

        $scope.languages = [];
        $scope.dimensions = [];
        $scope.pimVariantAttributes = [];
        $scope.pimVariantNumberAttributes = [];
        $scope.pimProductAttributes = [];
        $scope.filterAttributes = [];
        $scope.filterAttributeValueOptions = [];
        $scope.productCatalogues = [];

        $scope.settings = null;
        $scope.commerceSettings = null;

        $scope.languageSelectizeConfig = structPimUmbracoHelper.getDefaultSelectizeConfig();
        $scope.languageSelectizeConfig.labelField = "Name";
        $scope.languageSelectizeConfig.valueField = "Id";

        $scope.dimensionSelectizeConfig = structPimUmbracoHelper.getDefaultSelectizeConfig();
        $scope.dimensionSelectizeConfig.labelField = "Name";
        $scope.dimensionSelectizeConfig.valueField = "Uid";

        $scope.attributeConfig = structPimUmbracoHelper.getDefaultSelectizeConfig();
        $scope.attributeConfig.labelField = "Alias";
        $scope.attributeConfig.searchField = "Alias";
        $scope.attributeConfig.valueField = "Uid";

        $scope.filterAttributeConfig = structPimUmbracoHelper.getDefaultSelectizeConfig();
        $scope.filterAttributeConfig.labelField = "Alias";
        $scope.filterAttributeConfig.searchField = "Alias";
        $scope.filterAttributeConfig.valueField = "Uid";

        $scope.filterAttributeValueConfig = structPimUmbracoHelper.getDefaultSelectizeConfig();
        $scope.filterAttributeValueConfig.labelField = "Value";
        $scope.filterAttributeValueConfig.searchField = "Value";
        $scope.filterAttributeValueConfig.valueField = "Uid";
        $scope.filterAttributeValueConfig.maxItems = 1000;

        $scope.catalogueSelectizeConfig = structPimUmbracoHelper.getDefaultSelectizeConfig();
        $scope.catalogueSelectizeConfig.labelField = "Label";
        $scope.catalogueSelectizeConfig.valueField = "Uid";

        $scope.controlModel = {
            activeTab: "pimSettings"
        };

        $scope.setup = {
            overlay: { show: false, model: {}, view: "" }
        }

        $scope.init = function () {
            umbracoCommerceService.getIntegrationSettings()
                .then(function (response) {
                    $scope.commerceSettings = response.data;
                    $scope.settings = _.where($scope.commerceSettings.GeneralSettings.ShopSettings, { Uid: $routeParams.id })[0];
                    umbracoCommerceService.getLanguages()
                        .then(function (response) {
                            $scope.languages = response.data;
                        },
                        function (response) {
                            structPimUmbracoHelper.handleError(response);
                        });

                    umbracoCommerceService.getDimensions()
                        .then(function (response) {
                            $scope.dimensions = response.data;
                        },
                        function (response) {
                            structPimUmbracoHelper.handleError(response);
                        });

                    umbracoCommerceService.getCatalogues()
                        .then(function (response) {
                            $scope.productCatalogues = response.data;
                        },
                        function (response) {
                            structPimUmbracoHelper.handleError(response);
                        });

                    umbracoCommerceService.getAttributes('Product', 'FixedListAttribute')
                        .then(function (response) {
                            $scope.filterAttributes = response.data;

                            $scope.$watch(function () { return $scope.settings.FilterAttributeUid; },
                                function (newValue, oldValue) {
                                    $scope.attributechange();
                                },
                                true
                            );
                        },
                        function (response) {
                            structPimUmbracoHelper.handleError(response);
                        }); 
                },
                function (response) {
                    structPimUmbracoHelper.handleError(response);
                });

            umbracoCommerceService.getAttributes('Product', '')
                .then(function (response) {
                    $scope.pimProductAttributes = response.data;

                    umbracoCommerceService.getAttributes('Variant', '')
                        .then(function (response) {
                            $scope.pimVariantAttributes = response.data;
                            $scope.pimVariantNumberAttributes = [];

                            for (var i = 0; i < $scope.pimVariantAttributes.length; i++) {
                                if ($scope.pimVariantAttributes[i].Type == "NumberAttribute") {
                                    $scope.pimVariantNumberAttributes.push($scope.pimVariantAttributes[i]);
                                }
                            }

                            $scope.loaded = true;                                                      
                        },
                        function (response) {
                            $scope.loaded = true;
                            structPimUmbracoHelper.handleError(response);
                        });
                },
                function (response) {
                    structPimUmbracoHelper.handleError(response);
                }); 
        }

        $scope.attributechange = function () {
            //If values are already loaded, this is not first load. In that case clear any selections
            if ($scope.filterAttributeValueOptions.length > 0) {
                $scope.settings.FilterAttributeGlobalListValueKeys = [];
            }
            if ($scope.settings.FilterAttributeUid) {
                umbracoCommerceService.getFilterAttributeValues($scope.settings.FilterAttributeUid, $scope.settings.Uid)
                    .then(function (response) {
                        $scope.filterAttributeValueOptions = response.data;
                    },
                    function (response) {
                        structPimUmbracoHelper.handleError(response);
                    });
            }
        }

        $scope.saveGeneralSettings = function (shopSettingUid) {
            umbracoCommerceService.saveGeneralSettings($scope.commerceSettings.GeneralSettings, shopSettingUid)
                .then(
                    function (response) {
                        structPimUmbracoHelper.setSuccessNotification("General settings has been updated");
                    },
                    function (response) {
                        structPimUmbracoHelper.handleError(response);
                    }
                );
        }

        $scope.init();
    });
