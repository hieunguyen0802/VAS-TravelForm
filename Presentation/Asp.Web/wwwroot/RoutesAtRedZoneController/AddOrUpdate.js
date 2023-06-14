$(function () {
    
    $("#routeAtRedZoneProvinceId").on("change", function () {
        LoadDistrict("routeAtRedZoneProvinceId", "routeAtRedZoneDistrictId", "routeAtRedZoneWardId");
    });
    $("#routeAtRedZoneDistrictId").on("change", function () {
        LoadWard("routeAtRedZoneDistrictId", "routeAtRedZoneWardId");
    });
    var
        routeAtRedZoneDate = $("#routeAtRedZoneDate"),
        routeAtRedZoneToDate = $("#routeAtRedZoneToDate"),
        routeAtRedZoneProvinceId = $("#routeAtRedZoneProvinceId"),
        routeAtRedZoneDistrictId = $("#routeAtRedZoneDistrictId"),
        routeAtRedZoneWardId = $("#routeAtRedZoneWardId"),
        routeAtRedZoneAddress = $("#routeAtRedZoneDateAddress"),
        routeAtRedZonetransportation = $("#routeAtRedZoneDatetransportation");
    var colDate, colToDate, Address, colAddress, colFullAddress, colProvince, colDistrict, colWard, colTransportation, deleteBnt, editBtn;

    function fillDataToContorl(rowIndex) {
        deleteBnt = '<a class="btn btn-danger delete" title="delete"><i class="fa fa-remove"></i></a>';
        colDate = '<input type="text" class="form-control" name="routesAtRedZones[' + rowIndex + '].routeAtRedZoneDate" value="' + routeAtRedZoneDate.val() + '" readonly>';
        colToDate = '<input type="text" class="form-control" name="routesAtRedZones[' + rowIndex + '].routeAtRedZoneToDate" value="' + routeAtRedZoneToDate.val() + '" readonly>';
        Address = routeAtRedZoneAddress.val() + " - " + $('#routeAtRedZoneWardId option:selected').text() + " - "
            + $('#routeAtRedZoneDistrictId option:selected').text() + " - " + $('#routeAtRedZoneProvinceId option:selected').text();
        colProvince = '<input type="hidden" class="form-control" name="routesAtRedZones[' + rowIndex + '].routeAtRedZoneProvinceId" value="' + routeAtRedZoneProvinceId.val() + '" readonly>';
        colDistrict = '<input type="hidden" class="form-control" name="routesAtRedZones[' + rowIndex + '].routeAtRedZoneDistrictId" value="' + routeAtRedZoneDistrictId.val() + '" readonly>';
        colWard = '<input type="hidden" class="form-control" name="routesAtRedZones[' + rowIndex + '].routeAtRedZoneWardId" value="' + routeAtRedZoneWardId.val() + '" readonly>';
        colAddress = '<input type="hidden" class="form-control" name="routesAtRedZones[' + rowIndex + '].routeAtRedZoneDateAddress" value="' + routeAtRedZoneAddress.val() + '" readonly>';
        colFullAddress = '<input style="min-width: 100%!important;"  type="text" class="col-lg-12 form-control" name="routesAtRedZones[' + rowIndex + '].routeAtRedZoneFullAddress" value="' + Address + '" readonly>';
        colTransportation = '<input style="min-width: 100%!important;" type="text" class="form-control" name="routesAtRedZones[' + rowIndex + '].routeAtRedZoneDatetransportation" value="' + routeAtRedZonetransportation.val() + '" readonly>';

        editBtn = '<a data-toggle="modal" data-target="#addNewRouteAtRedZones"'
            + ' data-date="' + routeAtRedZoneDate.val() + '"'
            + ' data-hieu="' + routeAtRedZoneToDate.val() + '"'
            + ' data-province="' + routeAtRedZoneProvinceId.val() + '"'
            + ' data-district="' + routeAtRedZoneDistrictId.val() + '"'
            + ' data-ward="' + routeAtRedZoneWardId.val() + '"'
            + ' data-address="' + routeAtRedZoneAddress.val() + '"'
            + ' data-trans="' + routeAtRedZonetransportation.val() + '"' +
            ' class="btn btn-warning editRouteAtRedZone" title = "Edit" data-toggle="tooltip" style="margin-right: 10px;"> <i class="fa fa-pencil"></i></a> ';
    }

    $('body').on('click', '.editRouteAtRedZone', function () {
        $("#updateRouteAtRedZoneDate").show();
        $("#saveRouteAtRedZoneDate").hide();
        currentRowEdit = $(this).closest("tr").index();
        $("#routeAtRedZoneDate").val($(this).data('date'));
        $("#routeAtRedZoneToDate").val($(this).data('hieu'));
        $("#routeAtRedZoneProvinceId").val($(this).data('province')).change();
        $("#routeAtRedZoneDistrictId").val($(this).data('district')).change();
        $("#routeAtRedZoneWardId").val($(this).data('ward')).change();
        $("#routeAtRedZoneDateAddress").val($(this).data('address'));
        $("#routeAtRedZoneDatetransportation").val($(this).data('trans'));
        console.log($("#routeAtRedZoneToDate").val());
        console.log($("#routeAtRedZoneDate").val());
    });

    $("#addNewRouteAtRedZonesBtn").click(function () {
        $("#saveRouteAtRedZoneDate").show();
        $("#updateRouteAtRedZoneDate").hide();
    })
    window.setDistrict = function (DistrictId) {
        $("#routeAtRedZoneDistrictId").val(DistrictId).change();
    }

    var formValid = false;
    function validateForm() {
        if (!routeAtRedZoneDate.val()) {
            routeAtRedZoneDate.closest('.form-group').removeClass('has-success').addClass('has-error');
            formValid = false;
        }
        else if (!routeAtRedZoneToDate.val()) {
            routeAtRedZoneToDate.closest('.form-group').removeClass('has-success').addClass('has-error');
            formValid = false;
        }
        else if (routeAtRedZoneProvinceId.val() == 0 || routeAtRedZoneProvinceId.val() == "") {
            routeAtRedZoneProvinceId.closest('.form-group').removeClass('has-success').addClass('has-error');
            routeAtRedZoneProvinceId.next().find('.select2-selection').addClass('has-error');
            formValid = false;
        }
        else if (routeAtRedZoneDistrictId.val() == 0 || routeAtRedZoneDistrictId.val() == "") {
            routeAtRedZoneDistrictId.closest('.form-group').removeClass('has-success').addClass('has-error');
            routeAtRedZoneDistrictId.next().find('.select2-selection').addClass('has-error');
            formValid = false;
        }
        else if (routeAtRedZoneWardId.val() == 0 || routeAtRedZoneWardId.val() == "") {
            routeAtRedZoneWardId.closest('.form-group').removeClass('has-success').addClass('has-error');
            routeAtRedZoneWardId.next().find('.select2-selection').addClass('has-error');
            formValid = false;
        }
        else if (!routeAtRedZoneAddress.val()) {
            routeAtRedZoneAddress.closest('.form-group').removeClass('has-success').addClass('has-error');
            formValid = false;
        }
        else if (!routeAtRedZonetransportation.val()) {
            routeAtRedZonetransportation.closest('.form-group').removeClass('has-success').addClass('has-error');
            formValid = false;
        }
        else {
            routeAtRedZoneDate.closest('.form-group').removeClass('has-error')
            routeAtRedZoneToDate.closest('.form-group').removeClass('has-error')
            routeAtRedZoneProvinceId.closest('.form-group').removeClass('has-error');
            routeAtRedZoneDistrictId.closest('.form-group').removeClass('has-error');
            routeAtRedZoneWardId.closest('.form-group').removeClass('has-error');
            routeAtRedZoneAddress.closest('.form-group').removeClass('has-error');
            routeAtRedZonetransportation.closest('.form-group').removeClass('has-error');
            formValid = true;
        }
    }
    $("#saveRouteAtRedZoneDate").click(function () {
        validateForm();
        if (formValid == true) {
            var rowCount = routesAtRedZonesObj.data().length;
            fillDataToContorl(rowCount);
            var rData = [
                colDate,
                colToDate,
                colFullAddress,
                colProvince,
                colDistrict,
                colWard,
                colAddress,
                colTransportation,
                editBtn + deleteBnt
            ];
            routesAtRedZonesObj.row.add(rData).draw(false);
            $('#addNewRouteAtRedZones').modal('hide');
            $('[data-toggle="tooltip"]').tooltip();
            console.log($("#routeAtRedZoneToDate").val());
            console.log($("#routeAtRedZoneDate").val());
        }
    });


    $('#updateRouteAtRedZoneDate').click(function () {
        fillDataToContorl(currentRowEdit);
        validateForm();
        if (formValid == true) {
            routesAtRedZonesObj.cell({ row: currentRowEdit, column: 0 }).data(colDate).draw();
            routesAtRedZonesObj.cell({ row: currentRowEdit, column: 1 }).data(colToDate).draw();
            routesAtRedZonesObj.cell({ row: currentRowEdit, column: 2 }).data(colFullAddress).draw();
            routesAtRedZonesObj.cell({ row: currentRowEdit, column: 3 }).data(colProvince).draw();
            routesAtRedZonesObj.cell({ row: currentRowEdit, column: 4 }).data(colDistrict).draw();
            routesAtRedZonesObj.cell({ row: currentRowEdit, column: 5 }).data(colWard).draw();
            routesAtRedZonesObj.cell({ row: currentRowEdit, column: 6 }).data(colAddress).draw();
            routesAtRedZonesObj.cell({ row: currentRowEdit, column: 7 }).data(colTransportation).draw();
            routesAtRedZonesObj.cell({ row: currentRowEdit, column: 8 }).data(editBtn + deleteBnt).draw();
            $('#addNewRouteAtRedZones').modal('hide');
        }

    });
    $('body').on('click', '.delete', function () {
        var row = $(this).parents('tr');
        routesAtRedZonesObj.row(row).remove().draw();
    })
    $('#addNewRouteAtRedZones').on('hidden.bs.modal', function () {
        $(this).find("input,textarea").val('').end()
    })
});