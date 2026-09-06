$(document).ready(function () {

    //modal show
    var defalutShow = $("#switchClinic_modal").attr("data-showModal");

    console.log(defalutShow);

    if (defalutShow == "True") {
        $("#switchClinic_modal").modal('show');
    }

/*    moment.locale("en-AU");*/
    $('input[name="clinic-date"]').daterangepicker({
        singleDatePicker: true,
        showDropdowns: true,
        locale: {
            format: 'DD/MM/YYYY'
        }
    });

    $('input[name="clinic-date"]').on('apply.daterangepicker', function (ev, picker) {
        console.log(picker.startDate.format('DD-MM-YYYY'));
        console.log(picker.endDate.format('DD-MM-YYYY'));

        var clinicDate = picker.startDate.format('YYYY/MM/DD')
        var clinicDoctorCode = $('input[name="clinic-doctor-code"]').attr('data-clinic-doctor-code');
        var sourceType = $('#SwitchSourceType').attr('data-source-type');

        var vURL = "/HisOrder/Ajax/GetClinicScheList"
        var vData = {
            inDoctorCode: clinicDoctorCode,
            inRegDate: clinicDate,
            inSourceType: sourceType
        }

        console.log(clinicDate);
        console.log(clinicDoctorCode);


        var vSuccessFunc = function (msg) {
            var objResult = JSON.parse(msg);
            if (objResult.isSuccess === true) {
                console.log("successssss");

                var ClinicScheList = JSON.parse(objResult.returnValue);
                //版面更新
                $("#switch_dept_card").find('.row:first').empty();
                if (ClinicScheList.length > 0) {

                    $.each(ClinicScheList, function (i, val) {
                        var $el = $(".clinic_dept_box_templete")
                            .clone(true, true)
                            .removeClass('clinic_dept_box_templete')
                            .attr('hidden', false)

                        $el.find('.clinic_dept_box')
                            .attr('data-dept-code', val.SCHE_DPT)
                            .attr('data-room-no', val.SCHE_ROOM)
                            .attr('date-source-type', sourceType)

                            .attr('data-doctor-code', val.SCHE_DOCTOR_CODE)
                            .attr('data-login-code', val.SCHE_DOCTOR_CODE)

                        $el.find('.clinic_dept_title').text(val.SCHE_DPT_NAME)
                        $el.find('.clinic_dept_room').text(val.SCHE_ROOM)
                        $el.find('.clinic_doctor_name').text(val.SCHE_DOCTOR_NAME)

                        $("#switch_dept_card")
                            .find('.row:first')
                            .append($el);
                    });
                }
            } else {
                $("#switch_dept_card").find('.row:first').empty();
            }

        }

        var vErrorFunc = function () {
            fullscreenLoading(false);
            layer.msg("GetClinicScheList失敗");
        };

        ajax(vURL, vData, vSuccessFunc, vErrorFunc);
    });



   
    $('#page_content').css('min-height', '100vh');



    $(".clinic_dept_box").click(function () {

        $("#switch_dept_card")
            .find(".ok")
            .removeClass("ok");
        $(this).addClass("ok");
    });

    $(".pt-call-btn").click(function () {

        var vInhospid = $(this).closest('tr').attr('data-inhospid');
        var vURL = "/HisOrder/Ajax/callLight";
        var vData = {
            inhospid: vInhospid
        };

        var vSuccessFunc = function (msg) {
            var objResult = JSON.parse(msg);
            if (objResult.isSuccess == true) {
                layer.msg(objResult.Message);
                //var roomNub = $()
                var regData = JSON.parse(objResult.returnValue);

                if (connection.state == "Disconnected") {
                    console.warn("Hub connection again!");
                    connection.start();
                }

                connection.invoke("SendMessage", regData.RegRoomNo, regData.RegSeqNo.toString()).catch(function (err) {
                    console.log('傳送錯誤: ' + err.toString());
                });

            }
            else {
                layer.msg("叫號失敗" + objResult.Message);
            }
        };
        var vErrorFunc = function () {
            layer.msg("叫號失敗");
        };

        ajax(vURL, vData, vSuccessFunc, vErrorFunc);
    });


    $("#checkbox_opd").change(function () {
        console.log('checkbox_opd');
        var n = $("input:checked").length;
        console.log(n);
        if (n > 0) {
            console.log('n > 0');
            $("#switch_dept_card").attr("hidden", false);

            console.log('hidden');
        }
    });


    var switchPatientFunc = function () {
        fullscreenLoading(true);

        $('#patientInhospid').val($(this).data('inhospid'));
        $('#patientPatientid').val($(this).data('patient-id'));
        $('#patientVisitStatus').val($(this).data('visit-status'));

        if ($('body').hasClass('nav-sm')) {
            $('#htmlBody').val('nav-sm');
        } else {
            $('#htmlBody').val('nav-md');
        }


        var vURL = "/HisOrder/HisOrder/CheckPatientVisit";
        var vData = {
            patientInhospid: $(this).data('inhospid'),
            patientPatientid: $(this).data('patient-id')
        };

        var vSuccessFunc = function (msg) {
            var objResult = JSON.parse(msg);
            if (objResult.isSuccess == true) {
                $('#patient-list-form').submit();        
            }
            else {
                fullscreenLoading(false);
                layer.alert(objResult.Message, {
                    skin: 'layui-layer-lan',
                    closebtn: 1,
                    anim: 5,
                    icon: 2,
                    btn: ['OK'],
                    title: 'Message'
                }, function (index) {
                    layer.close(index);
                });
            }
        };
        var vErrorFunc = function () {
            fullscreenLoading(false);
            layer.alert(objResult.Message, {
                skin: 'layui-layer-lan',
                closebtn: 1,
                anim: 5,
                icon: 2,
                btn: ['OK'],
                title: 'Message'
            }, function (index) {
                layer.close(index);
            });
        };

        ajax(vURL, vData, vSuccessFunc, vErrorFunc);
    };

    $('tr.clickable-patient').dblclick(function () {
        switchPatientFunc.call(this);
    });



    //切換診間
    var switchClinicFunc = function () {
        fullscreenLoading(true);

        var box = $(".clinic_dept_box.ok");
        var dept_code = box.attr('data-dept-code');
        var room_no = box.attr('data-room-no');
        var sourceType = $('#SwitchSourceType').attr('data-source-type');

        $('#clinicDate').val($('input[name="clinic-date"]').val().toString("yyyy/MM/dd"));
        $('#clinicDeptCode').val(dept_code);
        $('#clinicRoomNo').val(room_no);
        $('#sourceType').val(sourcetype);

        

        $('#switchClinic-modal-form').submit();
    };

    $('.switch_confirm').click(function () {
        switchClinicFunc.call(this);
    });


    $("#patientlist_tb").DataTable({
        "iDisplayLength": 100
    });
});
