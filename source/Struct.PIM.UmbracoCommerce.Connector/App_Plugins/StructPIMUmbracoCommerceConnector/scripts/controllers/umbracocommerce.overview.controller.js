﻿var app = angular.module("umbraco");

app.controller("umbracocommerce.overview.controller",
    function ($scope, umbracoCommerceService, structPimUmbracoHelper, $location) {
        $scope.loaded = false;
                
        $scope.setup = {
            overlay: { show: false, model: {}, view: "" }
        }

        $scope.init = function () {
           
        };

        $scope.init();
    });
