(function () {

    SPClientTemplates.TemplateManager.RegisterTemplateOverrides({

        OnPostRender: function (ctx) {
            var GragAndDropList = {
                data: ctx.ListData.Row.map(function (x) {
                    return {
                        ID: x.ID,
                        ElementOrderNumber: arguments[1]
                    };
                }),
                listTitle: ctx.ListTitle,
                table: null
            };


            var startOrderNumber,
			rows,
			rowElementId,
			tr,
			tbody;

            //Get Table and set it sorteble with JQuery UI
            rows = ctx.ListData.Row;
            rowElementId = GenerateIIDForListItem(ctx, rows[0]);
            tr = document.getElementById(rowElementId);
            tbody = tr.parentElement;
            GragAndDropList.table = $(tbody).sortable({
                stop: function (event, ui) {
                    var newOrderNumer = ui.item.index();
                    updateData(GragAndDropList.data, startOrderNumber, newOrderNumer);
                },
                start: function (event, ui) {
                    startOrderNumber = ui.item.index();
                }
            });

            //Hide view header   
            $(tbody.parentElement.parentElement.firstElementChild).hide();

            //Save changes
            function updateData(data, start, finish) {
                var d = data.slice(0);
                d.sort(function (a, b) {
                    return a.ElementOrderNumber - b.ElementOrderNumber
                });

                var dragged = d[start],
				a = start,
				b = finish,
				c = -1;
                var goUp = (start - finish) > 0;
                if (goUp) {
                    a = finish;
                    b = start;
                    c = 1
                }

                for (var i = 0; i <= d.length; i++) {
                    saveState = d[i];
                    if ((i > a) && (i < b)) {
                        d[i].ElementOrderNumber = d[i].ElementOrderNumber + 1 * c;
                        updateItem(d[i], success, failure);
                    } else if (!goUp && (i == b)) {
                        d[i].ElementOrderNumber--;
                        updateItem(d[i], success, failure);
                    } else if (goUp && (i == a)) {
                        d[i].ElementOrderNumber++;
                        updateItem(d[i], success, failure);
                    } else if (i > finish) {
                        dragged.ElementOrderNumber = finish;
                        updateItem(dragged, success, failure);
                        break;
                    }
                }

                function success() {
                    data = d;
                }

                function failure() {
                    GragAndDropList.table.sortable("cancel");
                }

            }

            function getItemTypeForListName(name) {
                return "SP.Data." + name.charAt(0).toUpperCase() + name.slice(1) + "ListItem";
            }

            function updateItem(item, success, failure) {
                var listItemId = item.ID;
                var itemProperties = {
                    ElementOrderNumber: item.ElementOrderNumber
                }
                var webUrl = _spPageContextInfo.webAbsoluteUrl
                var listItemUri = webUrl + "/_api/web/lists/getbytitle('" + GragAndDropList.listTitle + "')/items(" + listItemId + ")";
                var itemPayload = {
                    '__metadata': {
                        'type': getItemTypeForListName(GragAndDropList.listTitle)
                    }
                };
                for (var prop in itemProperties) {
                    itemPayload[prop] = itemProperties[prop];
                }
                updateJson(listItemUri, itemPayload, success, failure);
            }

            function updateJson(endpointUri, payload, success, error) {
                $.ajax({
                    url: endpointUri,
                    type: "POST",
                    data: JSON.stringify(payload),
                    contentType: "application/json;odata=verbose",
                    headers: {
                        "Accept": "application/json;odata=verbose",
                        "X-RequestDigest": $("#__REQUESTDIGEST").val(),
                        "X-HTTP-Method": "MERGE",
                        "If-Match": "*"
                    },
                    success: success,
                    error: error
                });
            }

        }

    });

})();
