$(function () {
    InformationContactSuspectCaseCovidObj = $('#informationSuspectCaseOfCovid').DataTable();
    $("#suspectCaseProvinceId").on("change", function () {
        LoadDistrict("suspectCaseProvinceId", "suspectCaseDistrictId", "suspectCaseWardId");
    });

    $("#suspectCaseDistrictId").on("change", function () {
        LoadWard("suspectCaseDistrictId", "suspectCaseWardId");
    });
    var
        suspectCaseDate = $("#suspectCaseDate"),
        suspectCaseToDate = $("#suspectCaseToDate"),
        suspectCaseProvinceId = $("#suspectCaseProvinceId"),
        suspectCaseDistrictId = $("#suspectCaseDistrictId"),
        suspectCaseWardId = $("#suspectCaseWardId"),
        suspectCaseAddress = $("#suspectCaseAddress"),
        suspectCaseRelationShip = $("#suspectCaseRelationShip"),
        suspectCaseContactSituation = $("#suspectCaseContactSituation"),
        suspectCase = $("#suspectCase");
    var col_suspectCaseDate,
        col_suspectCaseToDate,
        col_suspectCaseProvinceId,
        col_suspectCaseDistrictId,
        col_suspectCaseWardId,
        col_suspectCaseAddress,
        col_SuspectCaseFullAddress,
        col_suspectCaseRelationShip,
        col_suspectCaseContactSituation,
        deleteBnt_suspectCase,
        editBtn_suspectCase,
        SuspectCaseFullAddress,
        col_suspectCase;

    function fillDataToControlSuspectCase(rowIndex) {
        deleteBnt_suspectCase = '<a class="btn btn-danger deleteSuspect" title="delete"><i class="fa fa-remove"></i></a>';

        col_suspectCaseDate = '<input type="text" class="form-control" name="informationContactSuspectCaseCovid[' + rowIndex + '].suspectCaseDate" value="' + suspectCaseDate.val() + '" readonly>';

        col_suspectCaseToDate = '<input type="text" class="form-control" name="informationContactSuspectCaseCovid[' + rowIndex + '].suspectCaseToDate" value="' + suspectCaseToDate.val() + '" readonly>';

        SuspectCaseFullAddress = suspectCaseAddress.val() + " - " + $('#suspectCaseWardId option:selected').text() + " - "
            + $('#suspectCaseDistrictId option:selected').text() + " - " + $('#suspectCaseProvinceId option:selected').text();

        col_suspectCaseProvinceId = '<input type="hidden" class="form-control" name="informationContactSuspectCaseCovid[' + rowIndex + '].suspectCaseProvinceId" value="' + suspectCaseProvinceId.val() + '" readonly>';

        col_suspectCaseDistrictId = '<input type="hidden" class="form-control" name="informationContactSuspectCaseCovid[' + rowIndex + '].suspectCaseDistrictId" value="' + suspectCaseDistrictId.val() + '" readonly>';

        col_suspectCaseWardId = '<input type="hidden" class="form-control" name="informationContactSuspectCaseCovid[' + rowIndex + '].suspectCaseWardId" value="' + suspectCaseWardId.val() + '" readonly>';

        col_suspectCaseAddress = '<input type="hidden" class="form-control" name="informationContactSuspectCaseCovid[' + rowIndex + '].suspectCaseAddress" value="' + suspectCaseAddress.val() + '" readonly>';

        col_suspectCaseRelationShip = '<input type="text" class="col-lg-12 form-control" name="informationContactSuspectCaseCovid[' + rowIndex + '].suspectCaseRelationShip" value="' + suspectCaseRelationShip.val() + '" readonly>';

        col_suspectCaseContactSituation = '<input type="text" class="col-lg-12 form-control" name="informationContactSuspectCaseCovid[' + rowIndex + '].suspectCaseContactSituation" value="' + suspectCaseContactSituation.val() + '" readonly>';

        col_suspectCase = '<input style="min-width: 100%!important;"  type="text" class="col-lg-12 form-control" name="informationContactSuspectCaseCovid[' + rowIndex + '].suspectCase" value="' + suspectCase.val() + '" readonly>';

        col_SuspectCaseFullAddress = '<input style="min-width: 100%!important;"  type="text" class="col-lg-12 form-control" name="informationContactSuspectCaseCovid[' + rowIndex + '].suspectCaseFullAddress" value="' + SuspectCaseFullAddress + '" readonly>';


        editBtn_suspectCase = '<a data-toggle="modal" data-target="#addNewInformationContactSuspectCaseCovid"'
            + 'data-date="' + suspectCaseDate.val() + '"'
            + 'data-hieu="' + suspectCaseToDate.val() + '"'
            + ' data-value1="' + suspectCase.val() + '"'
            + ' data-province="' + suspectCaseProvinceId.val() + '"'
            + ' data-district="' + suspectCaseDistrictId.val() + '"'
            + ' data-ward="' + suspectCaseWardId.val() + '"'
            + ' data-address="' + suspectCaseAddress.val() + '"'
            + ' data-relationship="' + suspectCaseRelationShip.val() + '"'
            + ' data-value2="' + suspectCaseContactSituation.val() + '"'
            + ' class="btn btn-warning editsuspectCase" title = "Edit" data-toggle="tooltip" style="margin-right: 10px;"> <i class="fa fa-pencil"></i></a> ';
    }

    $("#addNewInformationContactSuspectCaseCovidBtn").click(function () {
        $("#saveInformationContactSuspectCaseCovid").show();
        $("#updateInformationContactSuspectCaseCovid").hide();
    })

    var suspectFormValid = false;
    function validateSuspectForm() {
        if (!suspectCaseDate.val()) {
            suspectCaseDate.closest('.form-group').removeClass('has-success').addClass('has-error');
            suspectFormValid = false;
        }
        else if (!suspectCaseToDate.val()) {
            suspectCaseToDate.closest('.form-group').removeClass('has-success').addClass('has-error');
            suspectFormValid = false;
        }
        else if (suspectCaseProvinceId.val() == 0 || suspectCaseProvinceId.val() == "") {
            suspectCaseProvinceId.closest('.form-group').removeClass('has-success').addClass('has-error');
            suspectCaseProvinceId.next().find('.select2-selection').addClass('has-error');
            suspectFormValid = false;
        }
        else if (suspectCaseDistrictId.val() == 0 || suspectCaseDistrictId.val() == "") {
            suspectCaseDistrictId.closest('.form-group').removeClass('has-success').addClass('has-error');
            suspectCaseDistrictId.next().find('.select2-selection').addClass('has-error');
            suspectFormValid = false;
        }
        else if (suspectCaseWardId.val() == 0 || suspectCaseWardId.val() == "") {
            suspectCaseWardId.closest('.form-group').removeClass('has-success').addClass('has-error');
            suspectCaseWardId.next().find('.select2-selection').addClass('has-error');
            suspectFormValid = false;
        }
        else if (!suspectCaseAddress.val()) {
            suspectCaseAddress.closest('.form-group').removeClass('has-success').addClass('has-error');
            suspectFormValid = false;
        }
        else if (!suspectCaseRelationShip.val()) {
            suspectCaseRelationShip.closest('.form-group').removeClass('has-success').addClass('has-error');
            suspectFormValid = false;
        }
        else if (!suspectCaseContactSituation.val()) {
            suspectCaseContactSituation.closest('.form-group').removeClass('has-success').addClass('has-error');
            suspectFormValid = false;
        }
        else if (!suspectCase.val()) {
            suspectCase.closest('.form-group').removeClass('has-success').addClass('has-error');
            suspectFormValid = false;
        }
        else {
            suspectCaseDate.closest('.form-group').removeClass('has-error')
            suspectCaseToDate.closest('.form-group').removeClass('has-error')
            suspectCaseProvinceId.closest('.form-group').removeClass('has-error');
            suspectCaseDistrictId.closest('.form-group').removeClass('has-error');
            suspectCaseWardId.closest('.form-group').removeClass('has-error');
            suspectCaseAddress.closest('.form-group').removeClass('has-error');
            suspectCaseRelationShip.closest('.form-group').removeClass('has-error');
            suspectCaseContactSituation.closest('.form-group').removeClass('has-error');
            suspectCase.closest('.form-group').removeClass('has-error');
            suspectFormValid = true;
        }
    }

    $('body').on('click', '.editsuspectCase', function () {
        $("#updateInformationContactSuspectCaseCovid").show();
        $("#saveInformationContactSuspectCaseCovid").hide();
        currentRowEdit = $(this).closest("tr").index();
        $("#suspectCaseDate").val($(this).data('date'));
        $("#suspectCaseToDate").val($(this).data('hieu'));
        $("#suspectCase").val($(this).data('value1'));
        $("#suspectCaseProvinceId").val($(this).data('province')).change();
        $("#suspectCaseDistrictId").val($(this).data('district')).change();
        $("#suspectCaseWardId").val($(this).data('ward')).change();
        $("#suspectCaseAddress").val($(this).data('address'));
        $("#suspectCaseRelationShip").val($(this).data('relationship'));
        $("#suspectCaseContactSituation").val($(this).data('value2'));
    });

    $("#saveInformationContactSuspectCaseCovid").click(function () {
        validateSuspectForm();
        if (suspectFormValid == true) {
            var rowCount = InformationContactSuspectCaseCovidObj.data().length;
            fillDataToControlSuspectCase(rowCount);
            var rData = [
                col_suspectCaseDate,
                col_suspectCaseToDate,
                col_suspectCase,
                col_SuspectCaseFullAddress,
                col_suspectCaseRelationShip,
                col_suspectCaseProvinceId,
                col_suspectCaseDistrictId,
                col_suspectCaseWardId,
                col_suspectCaseAddress,
                col_suspectCaseContactSituation,
                editBtn_suspectCase + deleteBnt_suspectCase
            ];
            InformationContactSuspectCaseCovidObj.row.add(rData).draw(false);
            $('#addNewInformationContactSuspectCaseCovid').modal('hide');
            $('[data-toggle="tooltip"]').tooltip();
        }
       
    });

    $('#updateInformationContactSuspectCaseCovid').click(function () {
        fillDataToControlSuspectCase(currentRowEdit);
        validateSuspectForm();
        if (suspectFormValid == true) {
            InformationContactSuspectCaseCovidObj.cell({ row: currentRowEdit, column: 0 }).data(col_suspectCaseDate).draw();

            InformationContactSuspectCaseCovidObj.cell({ row: currentRowEdit, column: 1 }).data(col_suspectCaseToDate).draw();

            InformationContactSuspectCaseCovidObj.cell({ row: currentRowEdit, column: 2 }).data(col_suspectCase).draw();

            InformationContactSuspectCaseCovidObj.cell({ row: currentRowEdit, column: 3 }).data(col_SuspectCaseFullAddress).draw();

            InformationContactSuspectCaseCovidObj.cell({ row: currentRowEdit, column: 4 }).data(col_suspectCaseRelationShip).draw();

            nformationContactSuspectCaseCovidObj.cell({ row: currentRowEdit, column: 5 }).data(col_suspectCaseContactSituation).draw();

            InformationContactSuspectCaseCovidObj.cell({ row: currentRowEdit, column: 6 }).data(col_suspectCaseProvinceId).draw();

            InformationContactSuspectCaseCovidObj.cell({ row: currentRowEdit, column: 7 }).data(col_suspectCaseDistrictId).draw();

            InformationContactSuspectCaseCovidObj.cell({ row: currentRowEdit, column: 8 }).data(col_suspectCaseWardId).draw();

            InformationContactSuspectCaseCovidObj.cell({ row: currentRowEdit, column: 9 }).data(col_suspectCaseAddress).draw();

            InformationContactSuspectCaseCovidObj.cell({ row: currentRowEdit, column: 10 }).data(editBtn_suspectCase + deleteBnt_suspectCase).draw();
            $('#addNewInformationContactSuspectCaseCovid').modal('hide');
        }
    });



    $('#addNewInformationContactSuspectCaseCovid').on('hidden.bs.modal', function () {
        $(this).find("input,textarea").val('').end()
    })
    $('body').on('click', '.deleteSuspect', function () {
        var row = $(this).parents('tr');
        InformationContactSuspectCaseCovidObj.row(row).remove().draw();
    })
});