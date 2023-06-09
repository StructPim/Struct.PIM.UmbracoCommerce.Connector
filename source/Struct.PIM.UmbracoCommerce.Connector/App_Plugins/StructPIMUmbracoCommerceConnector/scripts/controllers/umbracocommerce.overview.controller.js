var app = angular.module("umbraco");

app.controller("umbracocommerce.overview.controller",
    function ($scope, umbracoCommerceService, structPimUmbracoHelper, $location) {
        $scope.loaded = false;
        $scope.isValid = false;

        $scope.setup = {
            overlay: { show: false, model: {}, view: "" }
        }

        $scope.init = function () {
            umbracoCommerceService.isSetupValid()
                .then(function (response) {
                    $scope.isValid = response.data;
                    $scope.loaded = true;
                });
        };

        $scope.init();
    });
