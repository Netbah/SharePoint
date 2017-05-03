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
                listTitle: ctx.listUrlDir.split('/').pop(),
                listGUID: ctx.listName,
                table: null
            };

            initDraggeble();


            function initDraggeble() {
                var startOrderNumber,
                    rows,
                    rowElementId,
                    tr,
                    tbody;

                rows = ctx.ListData.Row;
                rowElementId = GenerateIIDForListItem(ctx, rows[0]);
                tr = document.getElementById(rowElementId);
                tbody = tr.parentElement;
                //Hide view header
                $(tbody.parentElement.parentElement.firstElementChild).hide();
                GragAndDropList.table = $(tbody).sortable({
                    stop: function (event, ui) {
                        var newOrderNumer = ui.item.index();
                        var data = JSON.parse(JSON.stringify(GragAndDropList.data))

                        updateData(data, startOrderNumber, newOrderNumer);
                    },
                    start: function (event, ui) {
                        startOrderNumber = ui.item.index();
                    }
                });
            }

            function updateData(data, start, finish) {
                data.sort(function (a, b) {
                    return a.ElementOrderNumber - b.ElementOrderNumber
                });

                var dragged = data[start],
                    a = start,
                    b = finish,
                    c = -1;
                var goUp = (start - finish) > 0;
                if (goUp) {
                    a = finish;
                    b = start;
                    c = 1
                }

                for (var i = 0; i <= data.length; i++) {
                    saveState = data[i];
                    if ((i > a) && (i < b)) {
                        data[i].ElementOrderNumber = data[i].ElementOrderNumber + 1 * c;
                    } else if (!goUp && (i == b)) {
                        data[i].ElementOrderNumber--;
                    } else if (goUp && (i == a)) {
                        data[i].ElementOrderNumber++;
                    } else if (i > finish) {
                        dragged.ElementOrderNumber = finish;
                        break;
                    }
                }

                updateTRowsBatchRequest(data.slice(a, b + 1)).
                    done(function (response) {
                        console.log("Items updated successfuly");
                        GragAndDropList.data = data;
                    })
                    .fail(function () {
                        GragAndDropList.table.sortable("cancel");
                        console.log("Items didn't update");
                    });
            }


            /**       
            * @name updateRowsBatchRequest
            * @description
            * Submits the updates as a single batch request.
            * 
            * @param {Array{Object}} rowsToUpdate - JSON array of drivers to update.
            */
            function updateTRowsBatchRequest(rowsToUpdate) {

                if (rowsToUpdate.length == 0) return;

                // generate a batch boundary
                var batchGuid = generateUUID();

                // creating the body
                var batchContents = new Array();
                var changeSetId = generateUUID();

                // get current host
                var temp = document.createElement('a');
                temp.href = _spPageContextInfo.webAbsoluteUrl;
                var host = temp.hostname;

                // create the request endpoint 
                var endpointBatch = _spPageContextInfo.webAbsoluteUrl
                    + '/_api/$batch';

                // batches need a specific header
                var batchRequestHeader = {
                    'X-RequestDigest': jQuery("#__REQUESTDIGEST").val(),
                    'Content-Type': 'multipart/mixed; boundary="batch_' + batchGuid + '"'
                };

                for (var rowIndex = 0; rowIndex < rowsToUpdate.length; rowIndex++) {

                    var row = rowsToUpdate[rowIndex];

                    var rowUpdater = {
                        __metadata: {
                            'type': getItemTypeForListName(GragAndDropList.listTitle)
                        },
                        ElementOrderNumber: row.ElementOrderNumber
                    };

                    // create the request endpoint
                    var endpoint = _spPageContextInfo.webAbsoluteUrl
                        + '/_api/web/lists/getbytitle(\'Drag and Drop\')'
                        + '/items(' + row.ID + ')';

                    // create the changeset
                    batchContents.push('--changeset_' + changeSetId);
                    batchContents.push('Content-Type: application/http');
                    batchContents.push('Content-Transfer-Encoding: binary');
                    batchContents.push('');
                    batchContents.push('PATCH ' + endpoint + ' HTTP/1.1');
                    batchContents.push('Content-Type: application/json;odata=verbose');
                    batchContents.push('Accept: application/json;odata=verbose');
                    batchContents.push('If-Match: *');;
                    batchContents.push('');
                    batchContents.push(JSON.stringify(rowUpdater));
                    batchContents.push('');
                }
                // END changeset to update data
                batchContents.push('--changeset_' + changeSetId + '--');
                batchBody = batchContents.join('\r\n');

                // start with a clean array
                batchContents = new Array();
                batchContents.push('--batch_' + batchGuid);
                batchContents.push('Content-Type: multipart/mixed; boundary="changeset_' + changeSetId + '"');
                batchContents.push('Content-Length: ' + batchBody.length);
                batchContents.push('Content-Transfer-Encoding: binary');
                batchContents.push('');
                batchContents.push(batchBody);
                batchContents.push('');

                batchContents.push('--batch_' + batchGuid + '--');

                batchBody = batchContents.join('\r\n');

                // create request
                return jQuery.ajax({
                    url: endpointBatch,
                    type: 'POST',
                    headers: batchRequestHeader,
                    data: batchBody
                });
            }


            /*
            * @name generateUUID
            * @description
            * Generates a GUID-like string, used in OData HTTP batches.
            * 
            * @returns {string} - A GUID-like string.
            */
            function generateUUID() {
                var d = new Date().getTime();
                var uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
                    var r = (d + Math.random() * 16) % 16 | 0;
                    d = Math.floor(d / 16);
                    return (c == 'x' ? r : (r & 0x7 | 0x8)).toString(16);
                });
                return uuid;
            };

            function getItemTypeForListName(name) {
                return "SP.Data." + name.charAt(0).toUpperCase() + name.slice(1) + "ListItem";
            }

        }

    });

})();
