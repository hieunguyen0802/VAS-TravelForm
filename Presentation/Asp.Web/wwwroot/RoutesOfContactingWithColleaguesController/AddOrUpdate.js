$(function () {
    /* routesOfContactingWithColleaguesObj = $('#routesOfContactingWithColleagues').DataTable(
        {
            "aoColumnDefs": [{ "sClass": "hide_me", "aTargets": [4, 5, 6, 7] }],
            destroy: true,
            cache: false,
            "language": {
                "emptyTable": "No data found."
            },
            "lengthChange": false,
        });
 */
    $("#routesOfContactingWithColleaguesProvinceId").on("change", function () {
        LoadDistrict("routesOfContactingWithColleaguesProvinceId", "routesOfContactingWithColleaguesDistrictId", "routesOfContactingWithColleaguesWardId");
    });
    $("#routesOfContactingWithColleaguesDistrictId").on("change", function () {
        LoadWard("routesOfContactingWithColleaguesDistrictId", "routesOfContactingWithColleaguesWardId");
    });
    var
        routesOfContactingWithColleaguesDate = $("#routesOfContactingWithColleaguesDate"),
        routesOfContactingWithColleaguesToDate = $("#routesOfContactingWithColleaguesToDate"),
        routesOfContactingWithColleaguesInfor = $("#routesOfContactingWithColleaguesInfor"),
        routesOfContactingWithColleaguesCampus = $("#routesOfContactingWithColleaguesCampus"),
        routesOfContactingWithColleaguesProvinceId = $("#routesOfContactingWithColleaguesProvinceId"),
        routesOfContactingWithColleaguesDistrictId = $("#routesOfContactingWithColleaguesDistrictId"),
        routesOfContactingWithColleaguesWardId = $("#routesOfContactingWithColleaguesWardId"),
        routesOfContactingWithColleaguesAddress = $("#routesOfContactingWithColleaguesAddress"),
        routesOfContactingWithColleaguesSituation = $("#routesOfContactingWithColleaguesContactSituation");

    var col_routesOfContactingWithColleaguesDate,
        col_routesOfContactingWithColleaguesToDate,

        col_routesOfContactingWithColleaguesInfor,
        col_routesOfContactingWithColleaguesCampus,

        routesOfContactingWithColleaguesFullAddress,

        col_routesOfContactingWithColleaguesProvinceId,
        col_routesOfContactingWithColleaguesDistrictId,
        col_routesOfContactingWithColleaguesWardId,
        col_routesOfContactingWithColleaguesAddress,


        col_routesOfContactingWithColleaguesSituation,
        col_deleteRoutesOfContactingWithColleagues,
        col_editRoutesOfContactingWithColleagues,
        col_routesOfContactingWithColleaguesFullAddress;

    function fillDataToControlRoutesOfcontactingWithColleagues(rowIndex) {

        col_deleteRoutesOfContactingWithColleagues = '<a class="btn btn-danger deleteRoutesOfContactingWithColleagues" title="delete"><i class="fa fa-remove"></i></a>'; 

        col_routesOfContactingWithColleaguesInfor = '<input style="min-width: 100%!important;" type="text" class="form-control" name="routesOfContactingWithColleagues[' + rowIndex + '].routesOfContactingWithColleaguesInfor" value="' + routesOfContactingWithColleaguesInfor.val() + '" readonly>';

        col_routesOfContactingWithColleaguesDate = '<input style="min-width: 100%!important;" type="text" class="form-control" name="routesOfContactingWithColleagues[' + rowIndex + '].routesOfContactingWithColleaguesDate" value="' + routesOfContactingWithColleaguesDate.val() + '" readonly>';

        col_routesOfContactingWithColleaguesToDate = '<input style="min-width: 100%!important;" type="text" class="form-control" name="routesOfContactingWithColleagues[' + rowIndex + '].routesOfContactingWithColleaguesToDate" value="' + routesOfContactingWithColleaguesToDate.val() + '" readonly>';

        col_routesOfContactingWithColleaguesCampus = '<input style="min-width: 100%!important;" type="text" class="form-control" name="routesOfContactingWithColleagues[' + rowIndex + '].routesOfContactingWithColleaguesCampus" value="' + routesOfContactingWithColleaguesCampus.val() + '" readonly>';

        routesOfContactingWithColleaguesFullAddress = routesOfContactingWithColleaguesAddress.val() + " - " + $('#routesOfContactingWithColleaguesWardId option:selected').text() + " - "
            + $('#routesOfContactingWithColleaguesDistrictId option:selected').text() + " - " + $('#routesOfContactingWithColleaguesProvinceId option:selected').text();

        col_routesOfContactingWithColleaguesProvinceId = '<input type="hidden" class="form-control" name="routesOfContactingWithColleagues[' + rowIndex + '].routesOfContactingWithColleaguesProvinceId" value="' + routesOfContactingWithColleaguesProvinceId.val() + '" readonly>';

        col_routesOfContactingWithColleaguesDistrictId = '<input type="hidden" class="form-control" name="routesOfContactingWithColleagues[' + rowIndex + '].routesOfContactingWithColleaguesDistrictId" value="' + routesOfContactingWithColleaguesDistrictId.val() + '" readonly>';

        col_routesOfContactingWithColleaguesWardId = '<input type="hidden" class="form-control" name="routesOfContactingWithColleagues[' + rowIndex + '].routesOfContactingWithColleaguesWardId" value="' + routesOfContactingWithColleaguesWardId.val() + '" readonly>';

        col_routesOfContactingWithColleaguesAddress = '<input type="hidden" class="form-control" name="routesOfContactingWithColleagues[' + rowIndex + '].routesOfContactingWithColleaguesAddress" value="' + routesOfContactingWithColleaguesAddress.val() + '" readonly>';

        col_routesOfContactingWithColleaguesSituation = '<input style="min-width: 100%!important;"  type="text" class="col-lg-12 form-control" name="routesOfContactingWithColleagues[' + rowIndex + '].routesOfContactingWithColleaguesContactSituation" value="' + routesOfContactingWithColleaguesSituation.val() + '" readonly>';

        col_routesOfContactingWithColleaguesFullAddress = '<input style="min-width: 100%!important;"  type="text" class="col-lg-12 form-control" name="routesOfContactingWithColleagues[' + rowIndex + '].routesOfContactingWithColleaguesFullAddress" value="' + routesOfContactingWithColleaguesFullAddress + '" readonly>';


        col_editRoutesOfContactingWithColleagues = '<a data-toggle="modal" data-target="#addNewRoutesOfContactingWithColleagues"'
            + 'data-date="' + routesOfContactingWithColleaguesDate.val() + '"'
            + 'data-hieu="' + routesOfContactingWithColleaguesToDate.val() + '"'
            + 'data-colleagues="' + routesOfContactingWithColleaguesInfor.val() + '"'
            + 'data-campus="' + routesOfContactingWithColleaguesCampus.val() + '"'
            + ' data-province="' + routesOfContactingWithColleaguesProvinceId.val() + '"'
            + ' data-district="' + routesOfContactingWithColleaguesDistrictId.val() + '"'
            + ' data-ward="' + routesOfContactingWithColleaguesWardId.val() + '"'
            + ' data-address="' + routesOfContactingWithColleaguesAddress.val() + '"'
            + ' data-value3="' + routesOfContactingWithColleaguesSituation.val() + '"'
            + ' class="btn btn-warning editRoutesOfContactingWithColleagues" title = "Edit" data-toggle="tooltip" style="margin-right: 10px;"> <i class="fa fa-pencil"></i></a> ';
    }

    $("#addNewRoutesOfContactingWithColleaguesBtn").click(function () {
        $("#saveRoutesOfContactingWithColleagues").show();
        $("#updateRoutesOfContactingWithColleagues").hide();
    })

    var formRoutesOfContactingWithColleaguesValid = false;
    function validateRoutesOfContactingWithColleaguesForm() {
        if (!routesOfContactingWithColleaguesDate.val()) {
            routesOfContactingWithColleaguesDate.closest('.form-group').removeClass('has-success').addClass('has-error');
            formRoutesOfContactingWithColleaguesValid = false;
        }
        else if (!routesOfContactingWithColleaguesToDate.val()) {
            routesOfContactingWithColleaguesToDate.closest('.form-group').removeClass('has-success').addClass('has-error');
            formRoutesOfContactingWithColleaguesValid = false;
        }
        else if (routesOfContactingWithColleaguesInfor.val() == 0 || routesOfContactingWithColleaguesInfor.val() == "") {
            routesOfContactingWithColleaguesInfor.closest('.form-group').removeClass('has-success').addClass('has-error');
            formRoutesOfContactingWithColleaguesValid = false;
        }
        else if (routesOfContactingWithColleaguesCampus.val() == 0 || routesOfContactingWithColleaguesCampus.val() == "") {
            routesOfContactingWithColleaguesCampus.closest('.form-group').removeClass('has-success').addClass('has-error');
            formRoutesOfContactingWithColleaguesValid = false;
        }
        else if (routesOfContactingWithColleaguesProvinceId.val() == 0 || routesOfContactingWithColleaguesProvinceId.val() == "") {
            routesOfContactingWithColleaguesProvinceId.closest('.form-group').removeClass('has-success').addClass('has-error');
            routesOfContactingWithColleaguesProvinceId.next().find('.select2-selection').addClass('has-error');
            formRoutesOfContactingWithColleaguesValid = false;
        }
        else if (routesOfContactingWithColleaguesDistrictId.val() == 0 || routesOfContactingWithColleaguesDistrictId.val() == "") {
            routesOfContactingWithColleaguesDistrictId.closest('.form-group').removeClass('has-success').addClass('has-error');
            routesOfContactingWithColleaguesDistrictId.next().find('.select2-selection').addClass('has-error');
            formRoutesOfContactingWithColleaguesValid = false;
        }
        else if (routesOfContactingWithColleaguesWardId.val() == 0 || routesOfContactingWithColleaguesWardId.val() == "") {
            routesOfContactingWithColleaguesWardId.closest('.form-group').removeClass('has-success').addClass('has-error');
            routesOfContactingWithColleaguesWardId.next().find('.select2-selection').addClass('has-error');
            formRoutesOfContactingWithColleaguesValid = false;
        }
        else if (!routesOfContactingWithColleaguesAddress.val()) {
            routesOfContactingWithColleaguesAddress.closest('.form-group').removeClass('has-success').addClass('has-error');
            formRoutesOfContactingWithColleaguesValid = false;
        }
        else if (!routesOfContactingWithColleaguesSituation.val()) {
            routesOfContactingWithColleaguesSituation.closest('.form-group').removeClass('has-success').addClass('has-error');
            formRoutesOfContactingWithColleaguesValid = false;
        }
        else {
            routesOfContactingWithColleaguesDate.closest('.form-group').removeClass('has-error')
            routesOfContactingWithColleaguesToDate.closest('.form-group').removeClass('has-error')
            routesOfContactingWithColleaguesInfor.closest('.form-group').removeClass('has-error');
            routesOfContactingWithColleaguesCampus.closest('.form-group').removeClass('has-error');
            routesOfContactingWithColleaguesProvinceId.closest('.form-group').removeClass('has-error');
            routesOfContactingWithColleaguesDistrictId.closest('.form-group').removeClass('has-error');
            routesOfContactingWithColleaguesWardId.closest('.form-group').removeClass('has-error');
            routesOfContactingWithColleaguesAddress.closest('.form-group').removeClass('has-error');
            routesOfContactingWithColleaguesSituation.closest('.form-group').removeClass('has-error');
            formRoutesOfContactingWithColleaguesValid = true;
        }
    }

    $('body').on('click', '.editRoutesOfContactingWithColleagues', function () {
        $("#updateRoutesOfContactingWithColleagues").show();
        $("#saveRoutesOfContactingWithColleagues").hide();
        currentRowEdit = $(this).closest("tr").index();
        $("#routesOfContactingWithColleaguesDate").val($(this).data('date'));
        $("#routesOfContactingWithColleaguesToDate").val($(this).data('hieu'));
        $("#routesOfContactingWithColleaguesInfor").val($(this).data('colleagues'));
        $("#routesOfContactingWithColleaguesCampus").val($(this).data('campus'));
        $("#routesOfContactingWithColleaguesProvinceId").val($(this).data('province')).change();
        $("#routesOfContactingWithColleaguesDistrictId").val($(this).data('district')).change();
        $("#routesOfContactingWithColleaguesWardId").val($(this).data('ward')).change();
        $("#routesOfContactingWithColleaguesAddress").val($(this).data('address'));
        $("#routesOfContactingWithColleaguesContactSituation").val($(this).data('value3'));
    });

    $("#saveRoutesOfContactingWithColleagues").click(function () {
        validateRoutesOfContactingWithColleaguesForm();
        if (formRoutesOfContactingWithColleaguesValid == true) {
            var rowCount = routesOfContactingWithColleaguesObj.data().length;
            fillDataToControlRoutesOfcontactingWithColleagues(rowCount);
            var rData = [
                col_routesOfContactingWithColleaguesDate,
                col_routesOfContactingWithColleaguesToDate,
                col_routesOfContactingWithColleaguesInfor,
                col_routesOfContactingWithColleaguesCampus,
                col_routesOfContactingWithColleaguesFullAddress,
                col_routesOfContactingWithColleaguesProvinceId,
                col_routesOfContactingWithColleaguesDistrictId,
                col_routesOfContactingWithColleaguesWardId,
                col_routesOfContactingWithColleaguesAddress,
                col_routesOfContactingWithColleaguesSituation,
                col_editRoutesOfContactingWithColleagues + col_deleteRoutesOfContactingWithColleagues
            ];
            routesOfContactingWithColleaguesObj.row.add(rData).draw(false);
            $('#addNewRoutesOfContactingWithColleagues').modal('hide');
            $('[data-toggle="tooltip"]').tooltip();
        }
       
    });

    $('#updateRoutesOfContactingWithColleagues').click(function () {
        validateRoutesOfContactingWithColleaguesForm();
        if (formRoutesOfContactingWithColleaguesValid == true) {
            fillDataToControlRoutesOfcontactingWithColleagues(currentRowEdit);

            routesOfContactingWithColleaguesObj.cell({ row: currentRowEdit, column: 0 }).data(col_routesOfContactingWithColleaguesDate).draw();

            routesOfContactingWithColleaguesObj.cell({ row: currentRowEdit, column: 1 }).data(col_routesOfContactingWithColleaguesToDate).draw();

            routesOfContactingWithColleaguesObj.cell({ row: currentRowEdit, column: 2 }).data(col_routesOfContactingWithColleaguesInfor).draw();

            routesOfContactingWithColleaguesObj.cell({ row: currentRowEdit, column: 3 }).data(col_routesOfContactingWithColleaguesCampus).draw();

            routesOfContactingWithColleaguesObj.cell({ row: currentRowEdit, column: 4 }).data(col_routesOfContactingWithColleaguesFullAddress).draw();

            routesOfContactingWithColleaguesObj.cell({ row: currentRowEdit, column: 5 }).data(col_routesOfContactingWithColleaguesProvinceId).draw();

            routesOfContactingWithColleaguesObj.cell({ row: currentRowEdit, column: 6 }).data(col_routesOfContactingWithColleaguesDistrictId).draw();

            routesOfContactingWithColleaguesObj.cell({ row: currentRowEdit, column: 7 }).data(col_routesOfContactingWithColleaguesWardId).draw();

            routesOfContactingWithColleaguesObj.cell({ row: currentRowEdit, column: 8 }).data(col_routesOfContactingWithColleaguesAddress).draw();

            routesOfContactingWithColleaguesObj.cell({ row: currentRowEdit, column: 9 }).data(col_routesOfContactingWithColleaguesSituation).draw();

            routesOfContactingWithColleaguesObj.cell({ row: currentRowEdit, column: 10 }).data(col_editRoutesOfContactingWithColleagues + col_deleteRoutesOfContactingWithColleagues).draw();
            $('#addNewRoutesOfContactingWithColleagues').modal('hide');
        }

    });

    $('#addNewRoutesOfContactingWithColleagues').on('hidden.bs.modal', function () {
        $(this).find("input").val('').end()
    })
    $('body').on('click', '.deleteRoutesOfContactingWithColleagues', function () {
        var row = $(this).parents('tr');
        routesOfContactingWithColleaguesObj.row(row).remove().draw();
    })
});