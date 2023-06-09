var app = angular.module("umbraco");

app.controller("umbracocommerce.setup.controller",
    function ($scope, umbracoCommerceService, structPimUmbracoHelper, $location) {
        $scope.loaded = false;
        
        $scope.commerceSettings;
        $scope.languages = [];
        
        $scope.setup = {
            overlay: { show: false, model: {}, view: "" }
        }

        $scope.languageSelectizeConfig = structPimUmbracoHelper.getDefaultSelectizeConfig();
        $scope.languageSelectizeConfig.labelField = "Name";
        $scope.languageSelectizeConfig.valueField = "CultureCode";

        $scope.controlModel = {
            activeTab: "general"
        };

        $scope.init = function () {
            structPimUmbracoHelper.updateTree(["setup"]);

            umbracoCommerceService.getIntegrationSettings()
                .then(function (response) {
                    $scope.commerceSettings = response.data;
                    $scope.loaded = true;
                },
                function (response) {
                    $scope.loaded = true;
                    structPimUmbracoHelper.handleError(response);
                });

            umbracoCommerceService.isSetupValid()
                .then(function (response) {
                    $scope.isValid = response.data;
                    $scope.loaded = true;

                    if ($scope.isValid) {
                        $scope.loadData();
                    }
                });
        };

        $scope.loadData = function () {
            umbracoCommerceService.getLanguages()
                .then(function (response) {
                    $scope.languages = response.data;
                },
                function (response) {
                    structPimUmbracoHelper.handleError(response);
                });    
        }

        $scope.saveSetup = function () {
            umbracoCommerceService.saveSetup($scope.commerceSettings.Setup)
                .then(function (response) {
                    structPimUmbracoHelper.setSuccessNotification("Setup has been updated");

                    umbracoCommerceService.isSetupValid()
                        .then(function (response) {
                            $scope.isValid = response.data;
                            if ($scope.isValid) {
                                $scope.loadData();
                            }
                        });
                },
                function (response) {
                    structPimUmbracoHelper.handleError(response);
                });
        }

        $scope.syncProductAttributes = function () {
            umbracoCommerceService.syncProductAttributes()
                .then(function (response) {
                    structPimUmbracoHelper.setSuccessNotification("Product attributes synced");
                },
                function (response) {
                    structPimUmbracoHelper.handleError(response);
                });
        }

        $scope.init();
    });
