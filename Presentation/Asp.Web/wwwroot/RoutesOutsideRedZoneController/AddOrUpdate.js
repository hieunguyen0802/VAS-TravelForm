$(function () {
    
    $("#routeOutsideRedZoneProvinceId").on("change", function () {
        LoadDistrict("routeOutsideRedZoneProvinceId", "routeOutsideRedZoneDistrictId", "routeOutsideRedZoneWardId");
    });
    $("#routeOutsideRedZoneDistrictId").on("change", function () {
        LoadWard("routeOutsideRedZoneDistrictId", "routeOutsideRedZoneWardId");
    });
    var
        routeOutsideRedZoneDate = $("#routeOutsideRedZoneDate"),
        routeOutsideRedZoneToDate = $("#routeOutsideRedZoneToDate"),
        routeOutsideRedZoneProvinceId = $("#routeOutsideRedZoneProvinceId"),
        routeOutsideRedZoneDistrictId = $("#routeOutsideRedZoneDistrictId"),
        routeOutsideRedZoneWardId = $("#routeOutsideRedZoneWardId"),
        routeOutsideRedZoneAddress = $("#routeOutsideRedZoneDateAddress"),
        routeOutsideRedZonetransportation = $("#routeOutsideRedZoneDatetransportation");
    var colDateOutSide, AddressOutSide, colAddressOutSide, colFullAddressOutSide, colProvinceOutSide, colDistrictOutSide, colWardOutSide, colTransportationOutSide, deleteOutSideBnt, editOutSideBtn;

    function fillDataToControlOutsideRedZone(rowIndex) {
        deleteOutSideBnt = '<a class="btn btn-danger deleteRouteOutsideRedZone" title="delete"><i class="fa fa-remove"></i></a>';
        colDateOutSide = '<input type="text" class="form-control" name="routesOutsizeRedZones[' + rowIndex + '].routeOutsideRedZoneDate" value="' + routeOutsideRedZoneDate.val() + '" readonly>';
        colToDateOutSide = '<input type="text" class="form-control" name="routesOutsizeRedZones[' + rowIndex + '].routeOutsideRedZoneToDate" value="' + routeOutsideRedZoneToDate.val() + '" readonly>';
        AddressOutSide = routeOutsideRedZoneAddress.val() + " - " + $('#routeOutsideRedZoneWardId option:selected').text() + " - "
            + $('#routeOutsideRedZoneDistrictId option:selected').text() + " - " + $('#routeOutsideRedZoneProvinceId option:selected').text();
        colProvinceOutSide = '<input type="hidden" class="form-control" name="routesOutsizeRedZones[' + rowIndex + '].routeOutsideRedZoneProvinceId" value="' + routeOutsideRedZoneProvinceId.val() + '" readonly>';
        colDistrictOutSide = '<input type="hidden" class="form-control" name="routesOutsizeRedZones[' + rowIndex + '].routeOutsideRedZoneDistrictId" value="' + routeOutsideRedZoneDistrictId.val() + '" readonly>';
        colWardOutSide = '<input type="hidden" class="form-control" name="routesOutsizeRedZones[' + rowIndex + '].routeOutsideRedZoneWardId" value="' + routeOutsideRedZoneWardId.val() + '" readonly>';
        colAddressOutSide = '<input type="hidden" class="form-control" name="routesOutsizeRedZones[' + rowIndex + '].routeOutsideRedZoneDateAddress" value="' + routeOutsideRedZoneAddress.val() + '" readonly>';
        colFullAddressOutSide = '<input style="min-width: 100%!important;"  type="text" class="col-lg-12 form-control" name="routesOutsizeRedZones[' + rowIndex + '].routeOutsideRedZoneFullAddress" value="' + AddressOutSide + '" readonly>';
        colTransportationOutSide = '<input style="min-width: 100%!important;" type="text" class="form-control" name="routesOutsizeRedZones[' + rowIndex + '].routeOutsideRedZoneDatetransportation" value="' + routeOutsideRedZonetransportation.val() + '" readonly>';

        editOutSideBtn = '<a data-toggle="modal" data-target="#addNewRoutesOutsizeRedZones"'
            + 'data-date="' + routeOutsideRedZoneDate.val() + '"'
            + 'data-hieu="' + routeOutsideRedZoneToDate.val() + '"'
            + ' data-province="' + routeOutsideRedZoneProvinceId.val() + '"'
            + ' data-district="' + routeOutsideRedZoneDistrictId.val() + '"'
            + ' data-ward="' + routeOutsideRedZoneWardId.val() + '"'
            + ' data-address="' + routeOutsideRedZoneAddress.val() + '"'
            + ' data-trans="' + routeOutsideRedZonetransportation.val() + '"' +
            ' class="btn btn-warning editRouteOutsideRedZone" title = "Edit" data-toggle="tooltip" style="margin-right: 10px;"> <i class="fa fa-pencil"></i></a> ';
    }

    $('body').on('click', '.editRouteOutsideRedZone', function () {
        $("#updateRouteOutsideRedZoneDate").show();
        $("#saveRouteOutsideRedZoneDate").hide();
        currentRowEdit = $(this).closest("tr").index();
        $("#routeOutsideRedZoneDate").val($(this).data('date'));
        $("#routeOutsideRedZoneToDate").val($(this).data('hieu'));
        $("#routeOutsideRedZoneProvinceId").val($(this).data('province')).change();
        $("#routeOutsideRedZoneDistrictId").val($(this).data('district')).change();
        $("#routeOutsideRedZoneWardId").val($(this).data('ward')).change();
        $("#routeOutsideRedZoneDateAddress").val($(this).data('address'));
        $("#routeOutsideRedZoneDatetransportation").val($(this).data('trans'));
    });

    $("#addNewRoutesOutsizeRedZonesBtn").click(function () {
        $("#saveRouteOutsideRedZone").show();
        $("#updateRouteOutsideRedZone").hide();
    })
    window.setDistrict = function (DistrictId) {
        $("#routeOutsideRedZoneDistrictId").val(DistrictId).change();
    }

    var formValidOutSide = false;
    function validateFormRouteOutsideRedZone() {
        if (!routeOutsideRedZoneDate.val()) {
            routeOutsideRedZoneDate.closest('.form-group').removeClass('has-success').addClass('has-error');
            formValidOutSide = false;
        }
        else if (!routeOutsideRedZoneToDate.val()) {
            routeOutsideRedZoneToDate.closest('.form-group').removeClass('has-success').addClass('has-error');
            formValidOutSide = false; 
        }
        else if (routeOutsideRedZoneProvinceId.val() == 0 || routeOutsideRedZoneProvinceId.val() == "") {
            routeOutsideRedZoneProvinceId.closest('.form-group').removeClass('has-success').addClass('has-error');
            routeOutsideRedZoneProvinceId.next().find('.select2-selection').addClass('has-error');
            formValidOutSide = false;
        }
        else if (routeOutsideRedZoneDistrictId.val() == 0 || routeOutsideRedZoneDistrictId.val() == "") {
            routeOutsideRedZoneDistrictId.closest('.form-group').removeClass('has-success').addClass('has-error');
            routeAtRedZoneDistrictId.next().find('.select2-selection').addClass('has-error');
            formValidOutSide = false;
        }
        else if (routeOutsideRedZoneWardId.val() == 0 || routeOutsideRedZoneWardId.val() == "") {
            routeOutsideRedZoneWardId.closest('.form-group').removeClass('has-success').addClass('has-error');
            routeOutsideRedZoneWardId.next().find('.select2-selection').addClass('has-error');
            formValidOutSide = false;
        }
        else if (!routeOutsideRedZoneAddress.val()) {
            routeOutsideRedZoneAddress.closest('.form-group').removeClass('has-success').addClass('has-error');
            formValidOutSide = false;
        }
        else if (!routeOutsideRedZonetransportation.val()) {
            routeOutsideRedZonetransportation.closest('.form-group').removeClass('has-success').addClass('has-error');
            formValidOutSide = false;
        }
        else {
            routeOutsideRedZoneDate.closest('.form-group').removeClass('has-error')
            routeOutsideRedZoneToDate.closest('.form-group').removeClass('has-error')
            routeOutsideRedZoneProvinceId.closest('.form-group').removeClass('has-error');
            routeOutsideRedZoneDistrictId.closest('.form-group').removeClass('has-error');
            routeOutsideRedZoneWardId.closest('.form-group').removeClass('has-error');
            routeOutsideRedZoneAddress.closest('.form-group').removeClass('has-error');
            routeOutsideRedZonetransportation.closest('.form-group').removeClass('has-error');
            formValidOutSide = true;
        }
    }
    $("#saveRouteOutsideRedZone").click(function () {
        validateFormRouteOutsideRedZone();
        if (formValidOutSide == true) {
            var rowCount = routesOutsideRedZonesObj.data().length;
            fillDataToControlOutsideRedZone(rowCount);
            var rData = [
                colDateOutSide,
                colToDateOutSide,
                colFullAddressOutSide,
                colProvinceOutSide,
                colDistrictOutSide,
                colWardOutSide,
                colAddressOutSide,
                colTransportationOutSide,
                editOutSideBtn + deleteOutSideBnt
            ];
            routesOutsideRedZonesObj.row.add(rData).draw(false);
            $('#addNewRoutesOutsizeRedZones').modal('hide');
            $('[data-toggle="tooltip"]').tooltip();
        }
    });


    $('#updateRouteOutsideRedZoneDate').click(function () {
        fillDataToContorl(currentRowEdit);
        validateForm();
        if (formValid == true) {
            routesOutsideRedZonesObj.cell({ row: currentRowEdit, column: 0 }).data(colDateOutSide).draw();
            routesOutsideRedZonesObj.cell({ row: currentRowEdit, column: 1 }).data(colToDateOutSide).draw();
            routesOutsideRedZonesObj.cell({ row: currentRowEdit, column: 2 }).data(colFullAddresOutSides).draw();
            routesOutsideRedZonesObj.cell({ row: currentRowEdit, column: 3 }).data(colProvinceOutSide).draw();
            routesOutsideRedZonesObj.cell({ row: currentRowEdit, column: 4 }).data(colDistrictOutSide).draw();
            routesOutsideRedZonesObj.cell({ row: currentRowEdit, column: 5 }).data(colWardOutSide).draw();
            routesOutsideRedZonesObj.cell({ row: currentRowEdit, column: 6 }).data(colAddressOutSide).draw();
            routesOutsideRedZonesObj.cell({ row: currentRowEdit, column: 7 }).data(colTransportationOutSide).draw();
            routesOutsideRedZonesObj.cell({ row: currentRowEdit, column: 8 }).data(editOutSideBtn + deleteOutSideBnt).draw();
            $('#addNewRoutesOutsizeRedZones').modal('hide');
        }

    });
    $('body').on('click', '.deleteRouteOutsideRedZone', function () {
        var row = $(this).parents('tr');
        routesOutsideRedZonesObj.row(row).remove().draw();
    })
    $('#addNewRoutesOutsizeRedZones').on('hidden.bs.modal', function () {
        $(this).find("input,textarea").val('').end()
    })
});