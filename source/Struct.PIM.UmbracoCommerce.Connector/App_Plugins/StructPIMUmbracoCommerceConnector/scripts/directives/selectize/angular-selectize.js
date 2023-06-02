/**
 * Angular Selectize2
 * https://github.com/machineboy2045/angular-selectize
 **/

angular.module("selectize", []).value("selectizeConfig", {}).directive("selectize", ["selectizeConfig", "$timeout", "$log", "$q", "$rootScope", function (selectizeConfig, $timeout, $log, $q, $rootScope) {
    return {
        restrict: "EA",
        require: "^ngModel",
        scope: { ngModel: "=", config: "=?", options: "=?", optgroups: '=?', ngDisabled: "=", ngRequired: "&", ngChange: "&", getSingleElementAsArray: "=?" },
        link: function (scope, element, attrs, modelCtrl) {
            scope.loaded = false;

            scope.$watch(function (scope) { return scope.ngModel == "" || scope.ngModel || scope.ngModel == null; }, function () {
                    if ((scope.ngModel == "" || scope.ngModel || scope.ngModel == null) && !scope.loaded) {
                        scope.init();
                        scope.loaded = true;
                    }
            });

            //Make sure that changes in this form does not make the outer form dirty
            var alwaysFalse = { get: function () { return false; }, set: function () { } };
            Object.defineProperty(modelCtrl, '$pristine', alwaysFalse);
            Object.defineProperty(modelCtrl, '$dirty', alwaysFalse);

            scope.init = function() {
                var promise = $q.when(scope.config.placeholder);
                promise.then(function (placeholder) {
                    Selectize.defaults.maxItems = null; //default to tag editor

                    var selectize,
                    config = angular.extend({}, Selectize.defaults, selectizeConfig, scope.config);

                    config.placeholder = placeholder;

                    modelCtrl.$isEmpty = function (val) {
                        return (val === undefined || val === null || !val.length); //override to support checking empty arrays
                    };

                    function createItem(input) {
                        var data = {};
                        data[config.labelField] = input;
                        data[config.valueField] = input;
                        return data;
                    }

                    function createGroup(input) {
                        var data = {};
                        data[config.optgroupField] = input;
                        data[config.optgroupLabelField] = input;
                        data[config.optgroupValueField] = input;
                        return data;
                    }

                    function toggle(disabled) {
                        disabled ? selectize.disable() : selectize.enable();
                    }

                    var validate = function () {
                        var isInvalid = (scope.ngRequired() || attrs.required || config.required) && (scope.ngModel === undefined || scope.ngModel === '' || scope.ngModel === null || scope.ngModel === NaN || scope.ngModel.length === 0);
                        modelCtrl.$setValidity("required", !isInvalid);
                    };

                    function generateOptions(data) {
                        if (!data)
                            return [];

                        data = angular.isArray(data) ? data : [data];

                        return $.map(data, function (opt) {
                            return typeof opt === "string" ? createItem(opt) : opt;
                        });
                    }

                    function generateGroups(data) {
                        if (!data)
                            return [];

                        data = angular.isArray(data) ? data : [data];

                        return $.map(data, function (grp) {
                            return typeof grp === 'string' ? createGroup(grp) : grp;
                        });
                    }

                    function updateSelectize(curr, prev) {
                        if (curr !== prev) {
                             modelCtrl.$setDirty();
                        }

                        validate();

                        selectize.$control.toggleClass("ng-valid", modelCtrl.$valid);
                        selectize.$control.toggleClass("ng-invalid", modelCtrl.$invalid);
                        selectize.$control.toggleClass("ng-dirty", modelCtrl.$dirty);
                        selectize.$control.toggleClass("ng-pristine", modelCtrl.$pristine);

                        if (!angular.equals(selectize.items, scope.ngModel)) {
                            //selectize.addOption(generateOptions(scope.ngModel));
                            selectize.setValue(scope.ngModel);
                        }

                        if (!angular.equals(selectize.optgroups, scope.optgroups)) {
                            generateGroups(scope.optgroups).map(function (g) {
                                selectize.addOptionGroup(g.id, g);
                            });
                            //selectize.setValue(scope.ngModel);
                        }
                    }

                    var onOptionAdd = config.onOptionAdd, onOptionGroupAdd = config.onOptionGroupAdd;

                    var firstTimeChange = false;

                    config.onChange = function () {
                        
                        var doApply = false;
                        
                        if (!angular.equals(selectize.items, scope.ngModel)) {
                            $timeout(function () {
                                var value = selectize.items.slice();
                                if (config.maxItems == 1 && !scope.getSingleElementAsArray) {
                                    value = value[0];
                                }

                                if (scope.ngModel != value) {
                                    modelCtrl.$setViewValue(value);
                                    scope.ngModel = value;
                                    doApply = true;
                                }
                                $timeout(function () {
                                    if (doApply) {
                                        scope.ngChange.apply(this, { values: arguments });
                                    }
                                });
                            }, 0);

                            if (firstTimeChange == false) {
                                scope.ngChange.apply(this, { values: arguments });
                                firstTimeChange = true;
                            }
                            if (scope.setdirty) {
                                modelCtrl.$setDirty();
                            }
                        }
                    };

                    config.onOptionAdd = function (value, data) {
                        if (scope.options.indexOf(data) === -1) {
                            scope.options.push(data);
                        }

                        if (onOptionAdd) {
                            onOptionAdd.apply(this, arguments);
                        }
                    };

                    config.onOptionGroupAdd = function (id, data) {
                        if (scope.optgroups.indexOf(data) === -1)
                            scope.optgroups.push(data);

                        if (onOptionGroupAdd) {
                            onOptionGroupAdd.apply(this, arguments);
                        }
                    };

                    // ngModel (ie selected items) is included in this because if no options are specified, we
                    // need to create the corresponding options for the items to be visible
                    //scope.options = generateOptions((scope.options || config.options || scope.ngModel).slice());
                    scope.generatedOptions = generateOptions((scope.options || config.options || scope.ngModel).slice());
                    if (scope.options == null || scope.options == undefined) {
                        scope.options = [];
                    }
                    scope.options.length = 0;
                    scope.generatedOptions.forEach(function (item) {
                        scope.options.push(item);
                    });

                    if (scope.optgroups || config.optgroups || scope.ngModel) {
                        scope.optgroups = generateGroups((scope.optgroups || config.optgroups || scope.ngModel).slice());
                    } else {
                        scope.optgroups = generateGroups(null);
                    }

                    var angularCallback = config.onInitialize;

                    config.onInitialize = function () {
                        selectize = element[0].selectize;
                        selectize.addOption(scope.generatedOptions);
                        scope.optgroups.map(function (g) {
                            selectize.addOptionGroup(g.id, g);
                        });
                        selectize.setValue(scope.ngModel);

                        //provides a way to access the selectize element from an
                        //angular controller
                        if (angularCallback) {                            
                            angularCallback(selectize);
                        }

                        scope.$watch("options", function () {
                            scope.generatedOptions = generateOptions( (scope.options || config.options || scope.ngModel).slice() );
                            if (scope.options == null || scope.options == undefined) {
                                scope.options = [];
                            }
                            scope.options.length = 0;
                            scope.generatedOptions.forEach(function (item) {
                                scope.options.push(item);
                            });

                            selectize.clearOptions();
                            if (scope.options != null && scope.options != undefined) {
                                selectize.addOption(scope.generatedOptions);
                            }
                            selectize.setValue(scope.ngModel);
                        }, true);

                        scope.$watch('optgroups', function () {
                            //selectize.clearOptionGroups();
                            scope.optgroups.map(function (g) {
                                selectize.addOptionGroup(g.id, g);
                            });
                            selectize.setValue(scope.ngModel);
                        }, true);

                        scope.$watch("ngModel", updateSelectize, true);
                        //scope.$watchCollection("ngModel", updateSelectize);
                        scope.$watch("ngDisabled", toggle);
                    };

                    element.selectize(config);

                    element.on("$destroy", function () {
                        if (selectize) {
                            selectize.destroy();
                            element = null;
                        }
                    });
                });
            };
        }
            
    };
}]);