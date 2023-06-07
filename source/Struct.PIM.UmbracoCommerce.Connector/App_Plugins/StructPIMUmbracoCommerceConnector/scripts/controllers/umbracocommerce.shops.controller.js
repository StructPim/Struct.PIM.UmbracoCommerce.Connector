var app = angular.module("umbraco");

app.controller("umbracocommerce.shops.controller",
    function ($scope, umbracoCommerceService, structPimUmbracoHelper, $location) {

        $scope.loaded = false;
        $scope.languages = [];
        $scope.dimensions = [];

        $scope.setup = {
            overlay: { show: false, model: {}, view: "" }
        }

        $scope.init = function () {
            structPimUmbracoHelper.updateTree(["stores"]);

            umbracoCommerceService.getIntegrationSettings()
                .then(function (response) {
                    $scope.commerceSettings = response.data;

                    umbracoCommerceService.getLanguages()
                        .then(function (response) {
                            $scope.languages = response.data;

                            _.each($scope.commerceSettings.GeneralSettings.ShopSettings, function (d) {
                                if (d.LanguageId) {
                                    d.LanguageName = _.where($scope.languages, { Id: d.LanguageId })[0].Name;
                                }
                            });
                        },
                        function (response) {
                            structPimUmbracoHelper.handleError(response);
                        });

                    umbracoCommerceService.getDimensions()
                        .then(function (response) {
                            $scope.dimensions = response.data;

                            _.each($scope.commerceSettings.GeneralSettings.ShopSettings, function (d) {
                                var dimensionText = "";

                                if ($scope.dimensions) {
                                    for (var i = 0; i < $scope.dimensions.length; i++) {
                                        var dimension = $scope.dimensions[i];

                                        if (d.DimensionSettings && d.DimensionSettings[dimension.Uid]) {
                                            var dimensionSegmentUid = d.DimensionSettings[dimension.Uid];
                                            var dimensionSegment = _.where(dimension.Segments, { Uid: dimensionSegmentUid });
                                            
                                            if (dimensionSegment.length > 0) {
                                                dimensionText += dimension.Alias + ": " + dimensionSegment[0].Name + "<br>";
                                            }
                                        }
                                    }
                                }
                                d.Dimension = dimensionText;
                            });
                        },
                        function (response) {
                            structPimUmbracoHelper.handleError(response);
                        });


                    $scope.loaded = true;
                },
                function (response) {
                    $scope.loaded = true;
                    structPimUmbracoHelper.handleError(response);
                });
        };

        $scope.editShopSettings = function (settings) {
            $location.path("/settings/structpimsettings/store-edit/" + settings.Uid);
        }

        $scope.init();
    });
