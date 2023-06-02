angular.module("umbraco")
    .factory("structPimUmbracoHelper", [
        "navigationService", "localizationService", "notificationsService", "treeService", "appState", "$rootScope", "$location", function (navigationService, localizationService, notificationsService, treeService, appState, $rootScope, $location) {
            return {
                hideNavigation: function (node) {
                    navigationService.hideNavigation();
                },
                reloadNode: function (node, reloadChildren) {
                    if (node) {
                        if (reloadChildren) {
                            treeService.loadNodeChildren({ node: node });
                        }
                        treeService.reloadNode(node);
                    }
                },
                getCurrentNode: function () {
                    return appState.getMenuState("currentNode");
                },
                reloadCurrentNode: function (reloadChildren) {
                    var node = appState.getTreeState("selectedNode");
                    if (node) {
                        this.reloadNode(node, reloadChildren != null ? reloadChildren : false);
                    }
                },
                reloadNodes: function (nodes, reloadChildren) {
                    if (Array.isArray(nodes)) {
                        var treeRoot = appState.getTreeState("currentRootNode");
                        for (var i = 0; i < nodes.length; i++) {
                            var node;
                            if (nodes[i] != null) {
                                if (!isNaN(nodes[i])) {
                                    node = treeService.getDescendantNode(treeRoot.root, nodes[i].toString());
                                }
                                else {
                                    node = nodes[i];
                                }

                                if (node) {
                                    this.reloadNode(node, reloadChildren);
                                }
                            }
                        }
                    }
                },
                reloadNodeByUniqueId: function (id, reloadChildren) {
                    var treeRoot = appState.getTreeState("currentRootNode");
                    var node = treeService.getDescendantNode(treeRoot.root, id);
                    this.reloadNode(node, reloadChildren);
                },
                updateTree: function (path /* array of ids */, resetTree) {
                    if (resetTree) {
                        var treeRoot = appState.getTreeState("currentRootNode");
                        var treeNode = treeService.getDescendantNode(treeRoot.root, path[0], "structpimsettings");
                        treeService.removeChildNodes(treeNode);
                    }

                    navigationService.syncTree({ tree: "structpimsettings", path: path, forceReload: true, activate: true }).then(function (syncArgs) {
                        // ensure parents of the active node are expanded
                        var activeNode = appState.getTreeState("selectedNode");
                        if (activeNode && typeof activeNode.parent === "function") {
                            var next = activeNode.parent();

                            while (next != null) {
                                next.expanded = true;

                                if (next.id === "-1") {
                                    break;
                                }

                                next = next.parent();
                            }
                        }
                    });
                },
                removeNodeFromParents: function (currentNode, parents) {
                    if (Array.isArray(parents)) {
                        var treeRoot = treeService.getTreeRoot(currentNode);
                        for (var i = 0; i < parents.length; i++) {
                            var parentNode = treeService.getDescendantNode(treeRoot, parents[i]);

                            if (parentNode) {
                                var childNode = treeService.getChildNode(parentNode, currentNode.id);

                                if (childNode) {
                                    treeService.removeNode(childNode);
                                    this.reloadNode(parentNode);
                                }
                            }
                        }
                    }
                },
                hideNavigationAndGoToUrl: function (url, querystringId, querystringValue) {
                    navigationService.hideNavigation();
                    if (querystringId != '' && querystringValue != '') {
                        $location.path(url).search(querystringId, querystringValue);
                    } else {
                        $location.path(url);
                    }
                },
                openNavigationDialog: function (actionAlias, title, sectionName, id) {
                    var treeAlias = [], metaData = [];
                    treeAlias["treeAlias"] = sectionName == undefined ? "structpimsettings" : sectionName;
                    metaData["dialogTitle"] = title;

                    var dialogArgs = {
                        node: {
                            metaData: treeAlias,
                            id: id
                        },
                        action: {
                            metaData: metaData,
                            alias: actionAlias
                        }
                    }
                    navigationService.showDialog(dialogArgs);
                },
                getDefaultSelectizeConfig: function (valueField, labelField, placeholder, render) {
                    var localizedText = this.localize("general_chooseAnOption");
                    var config = {
                        create: false,
                        maxItems: 1,
                        valueField: valueField != null ? valueField : "id",
                        labelField: labelField != null ? labelField : "text",
                        sortField: labelField != null ? labelField : "text",
                        searchField: labelField != null ? labelField : "text",
                        placeholder: placeholder != null ? placeholder : localizedText,
                        plugins: ["remove_button", "drag_drop"],
                        selectOnTab: false
                    };
                    if (render != null) {
                        config.render = render;
                    }
                    return config;
                },
                getSelectizeConfig: function (valueField, labelField, sortField, searchField, placeholder, render) {
                    var config = {
                        create: false,
                        maxItems: 1,
                        valueField: valueField,
                        labelField: labelField,
                        sortField: sortField,
                        searchField: searchField,
                        placeholder: placeholder,
                        plugins: ["remove_button", "drag_drop"],
                        selectOnTab: false
                    };
                    if (render !== null) {
                        config.render = render;
                    }
                    return config;
                },
                setLocalizedSuccessNotificationWithInlineText: function (successTextKey, inlineText) {
                    var tokens = inlineText;
                    if (!angular.isArray(inlineText)) {
                        tokens = [inlineText];
                    }

                    localizationService.localize(successTextKey, tokens)
                        .then(function (value) {
                            notificationsService.success(value);
                        });

                },
                setLocalizedSuccessNotification: function (successTextKey) {
                    localizationService.localize(successTextKey)
                        .then(function (value) {
                            notificationsService.success(value);
                        });
                },
                setSuccessNotification: function (text) {
                    notificationsService.success(text);
                },
                setLocalizedErrorNotificationWithInlineText: function (errorTextKey, inlineText) {
                    var tokens = inlineText;
                    if (!angular.isArray(inlineText)) {
                        tokens = [inlineText];
                    }
                    localizationService.localize(errorTextKey, tokens)
                        .then(function (value) {
                            notificationsService.error(value);
                        });
                },
                setLocalizedErrorNotification: function (errorTextKey) {
                    localizationService.localize(errorTextKey)
                        .then(function (value) {
                            notificationsService.error(value);
                        });
                },
                setErrorNotification: function (errorText) {
                    notificationsService.error(errorText);
                },
                setLocalizedInfoNotification: function (infoTextKey) {
                    localizationService.localize(infoTextKey)
                        .then(function (value) {
                            notificationsService.info(value);
                        });
                },
                localize: function (key) {
                    return localizationService.localize(key);
                },
                localizeWithInlineText: function (key, inlineText) {
                    var tokens = inlineText;
                    if (!angular.isArray(inlineText)) {
                        tokens = [inlineText];
                    }
                    return localizationService.localize(key, tokens);
                },
                handleError: function (response) {
                    console.log(response)
                    if (response == null) {
                        notificationsService.error("An unknown error occurred. Please contact your system administrator.");
                    }
                    
                    else if (response.Errors != null && response.Errors.length > 0) {
                        var txt;
                        var err = response.Errors[0];
                        if (err.Message.length < 1) {
                            txt = err.StackTrace;
                        } else {
                            txt = err.Message;
                        }
                        notificationsService.error(txt);
                    }                    
                    else if (response.data == null) {
                        notificationsService.error("An unknown error occurred. Please contact your system administrator.");
                        console.error(response);
                    }
                    else if (response.data.ExceptionMessage) {
                        notificationsService.error(response.data.ExceptionMessage);
                    }
                    else if (response.data.statusText) {
                        notificationsService.error(response.data.statusText);
                    }
                    else {
                        var text;
                        if (response.data.Errors != null && response.data.Errors.length > 0) {
                            var error = response.data.Errors[0];
                            if (error.Message.length < 1) {
                                text = error.StackTrace;
                            } else {
                                text = error.Message;
                            }
                        } else {
                            text = response.data.Message;
                        }
                        notificationsService.error(text);
                    }
                },
                setCookie: function (cname, cvalue, exdays) {
                    var d = new Date();
                    d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
                    var expires = "expires=" + d.toUTCString();
                    var path = "path=/";
                    document.cookie = cname + "=" + angular.toJson(cvalue) + "; " + expires + "; " + path;
                },
                getCookie: function (cname) {
                    var name = cname + "=";
                    var ca = document.cookie.split(';');
                    for (var i = 0; i < ca.length; i++) {
                        var c = ca[i];
                        while (c.charAt(0) == ' ') c = c.substring(1);
                        if (c.indexOf(name) == 0) {
                            var substring = c.substring(name.length, c.length);
                            return substring != undefined && substring != 'undefined' ? angular.fromJson(substring) : null;

                        }
                    }
                    return null;
                },
                getMenuQueryString: function (scope) {
                    var querystring = scope.currentNode.menuUrl.split("?")[1];
                    var queryElements = querystring.split("&");
                    var listOfItems = {};
                    angular.forEach(queryElements, function (element) {
                        var temp = element.split("=");
                        listOfItems[temp[0]] = temp[1];
                    });
                    return listOfItems;
                },
                getParam: function (parameterName, value) {
                    var idstring = value;
                    var params = idstring.split("_");

                    for (var i = 0; i < params.length; i++) {
                        if (params[i].indexOf(parameterName) == 0) {
                            return params[i].split(":")[1];
                        }
                    }

                    return null;
                },
                createUid: function () {
                    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
                        var r = Math.random() * 16 | 0, v = c == 'x' ? r : (r & 0x3 | 0x8);
                        return v.toString(16);
                    });
                }
            }
        }
    ]);