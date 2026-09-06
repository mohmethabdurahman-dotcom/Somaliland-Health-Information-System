window.changeObj = { Med: false, NonMed: false, ClinicRemark: false, Management: false, Diagnosis: false };

function fn_CheckBeforeUnload() {
    var hasChange = false;
    $.each(changeObj, function (k, v) {
        if (v) {
            hasChange = v;
        }
    });
    if (hasChange) {
        $(window).unbind('beforeunload');
        $(window).bind('beforeunload', function () { return true; });
    } else {
        $(window).unbind('beforeunload');
    }
}
var aiSuggestion = '';
var aiModified = '';
var originalCkData = '';
let previewTriggerBtn = null;

const ncdDeptCodes = ["3000", "3001", "3002", "6000", "6001", "6002", "6003"];

function buildVisitAiPayload(options) {
    const includeManagement = options && options.includeManagement;
    const patient = window.__patientDto || {};
    const phys = window.__physicalSigns || [];
    const ageRaw = $('#patientData').data('age');
    const parsedAge = ageRaw && ageRaw !== '--' && ageRaw !== 'Insufficient data'
        ? parseInt(ageRaw, 10)
        : null;
    const patientAge = Number.isFinite(parsedAge) ? parsedAge : (patient.age ?? null);
    const gender = $('#patientData').data('gender') || patient.sex || '';
    const icdList = $('.tagify__tag-text').toArray().map(el => $(el).text().trim());
    const { medList } = window.extractMedicationsAndPrescriptionFromTable
        ? window.extractMedicationsAndPrescriptionFromTable()
        : { medList: [] };
    const { nonMedList } = window.extractNonMedOrdersFromTable
        ? window.extractNonMedOrdersFromTable()
        : { nonMedList: [] };
    const editorData = CKEDITOR.instances.editor_clinic_remarks
        ? CKEDITOR.instances.editor_clinic_remarks.getData()
        : '';

    let managementHtml = '';
    if (includeManagement
        && typeof CKEDITOR !== 'undefined'
        && CKEDITOR.instances.editor_managment) {
        managementHtml = CKEDITOR.instances.editor_managment.getData() || '';
    }

    const deptCode = ($('#patientData').data('dept') || patient.regDept || '').toString();

    return {
        inhospid: patient.inhospid || $('#patientInhospid').val() || '',
        healthId: patient.regPatientId || $('#patientHealthId').val() || '',
        deptCode: deptCode,
        patientAge: patientAge,
        patientSex: gender,
        clinicRemarkHtml: editorData,
        managementHtml: managementHtml,
        icdCodes: icdList,
        medications: medList,
        nonMedOrders: nonMedList,
        physicalSigns: phys,
        promptContent:
            `Patient Age: ${patientAge ?? 'not specified'}\nGender: ${gender}\n\n` +
            `Physical Signs:\n${JSON.stringify(phys, null, 2)}\n\n` +
            `ICD-10 Codes:\n${icdList.map(c => '  - ' + c).join('\n')}\n\n` +
            `Medications:\n${medList.map(m => '  - ' + m).join('\n')}\n\n` +
            `Non-medical Orders:\n${nonMedList.map(o => '  - ' + o).join('\n')}\n\n` +
            `Clinical Notes (raw HTML):\n${editorData}`
    };
}

function requestNcdAiAssist(payload) {
    return $.ajax({
        url: "/HisOrder/ClinicAi/NcdAssist",
        method: "POST",
        contentType: "application/json",
        credentials: "same-origin",
        data: JSON.stringify({
            inhospid: payload.inhospid,
            healthId: payload.healthId,
            deptCode: payload.deptCode,
            patientAge: payload.patientAge,
            patientSex: payload.patientSex,
            clinicRemarkHtml: payload.clinicRemarkHtml,
            managementHtml: payload.managementHtml,
            icdCodes: payload.icdCodes,
            medications: payload.medications,
            nonMedOrders: payload.nonMedOrders,
            physicalSigns: payload.physicalSigns
        })
    }).then(function (data) {
        if (data && data.content) {
            window.aiSuggestion = data.content;
            return data.content;
        }
        return $.Deferred().reject("No response from NCD AI Assist");
    });
}

function runClinicAiGeneration(isNcdDept, payload) {
    if (isNcdDept) {
        return requestNcdAiAssist(payload);
    }
    return window.sendToChatGPT(payload.promptContent);
}

function showAiPreviewResult(aiText, $btn) {
    fullscreenLoading(false);
    $('#jsPreviewModal').modal('show');

    $('#jsEditBtn')
        .prop('disabled', true)
        .removeClass('btn-success btn-primary')
        .addClass('btn-primary')
        .text('Loading…')
        .show();
    $('#jsDeclineBtn')
        .prop('disabled', true)
        .removeClass('btn-success btn-secondary')
        .addClass('btn-secondary')
        .text('Confirm Manual Save')
        .show();

    fn_ProcessAndDisplayAIContent(aiText, originalCkData);
}

function handleAiGenerationFailure($btn) {
    fullscreenLoading(false);
    layer.msg('AI SERVICE UNAVAILBLE', { icon: 2 });
    proceedSaveAction($btn);
}

// Function to setup manual button countdown
function setupManualButtonCountdown() {
    const declineBtn = $('#jsDeclineBtn');
    let countdown = 10;

    // Clear any existing interval first
    const existingInterval = $('#jsPreviewModal').data('countdownInterval');
    if (existingInterval) {
        clearInterval(existingInterval);
    }

    // Disable the manual button initially
    declineBtn
        .prop('disabled', true)
        .removeClass('btn-secondary')
        .addClass('btn-secondary')
        .text(`Manual (${countdown}s)`);

    // Start countdown
    const interval = setInterval(() => {
        countdown--;

        if (countdown > 0) {
            declineBtn.text(`Manual (${countdown}s)`);
        } else {
            // Countdown finished
            clearInterval(interval);
            $('#jsPreviewModal').removeData('countdownInterval');

            declineBtn
                .prop('disabled', false)
                .removeClass('btn-secondary')
                .addClass('btn-secondary')
                .text('Manual');
        }
    }, 1000);

    // Store interval for cleanup
    $('#jsPreviewModal').data('countdownInterval', interval);
}

// Cleanup function for when modal closes
function cleanupCountdown() {
    const interval = $('#jsPreviewModal').data('countdownInterval');
    if (interval) {
        clearInterval(interval);
        $('#jsPreviewModal').removeData('countdownInterval');
    }
}




$(document).ready(function () {


    const currentDeptCode = $('#patientData').data('dept').toString();
    // departments with their codes
    const deptNames = {
        "0100": "Medical",
        "0101": "Medical-A",
        "0102": "Medical-B",
        "0103": "Medical-C",
        "0104": "Medical-D",
        "0105": "Medical-E",
        "0200": "Surgery",
        "0201": "General Surgery",
        "0202": "General Surgery 2",
        "0203": "Deputy Director Room",
        "02202": "Dop- Ultrasound",
        "0300": "Gynecology & Obstetric",
        "0301": "Gynecology & Obstetric",
        "0400": "Pediatric",
        "0401": "Pediatric",
        "0402": "Pediatric Surgery",
        "0500": "EYE Department",
        "0501": "EYE Department 1",
        "0502": "EYE Department 2",
        "0600": "ENT",
        "0601": "ENT",
        "0700": "Orthopedic",
        "0701": "Orthopedic 1",
        "0702": "Orthopedic 2",
        "0703": "Orthopedic 3",
        "0704": "Orthopedic 3",
        "0800": "Neurology",
        "0801": "Neurology",
        "0900": "Dermatology",
        "0901": "Dermatology",
        "1600": "Emergency",
        "1601": "ER-Trauma",
        "1602": "Trauma Female",
        "1603": "ER-Ultrasound",
        "1604": "ER-Medical",
        "2000": "ECG",
        "2001": "ECG",
        "2100": "Cardiology",
        "2101": "Cardiology",
        "2200": "Ultrasound",
        "2201": "Ultrasound",
        "2300": "Neurosurgery",
        "2301": "Neurosurgery",
        "2400": "Maxillofacial",
        "2401": "Maxillofacial",
        "2402": "Maxillofacial 2",
        "3000": "NCD Department",
        "3001": "NCD Department 1",
        "3002": "NCD Department 2",
        "4000": "Mental Health Department",
        "4001": "MHD Room 1",
        "4002": "MHD Room 2",
        "4003": "MHD Room 3",
        "4004": "MHD Room 4",
        "5000": "Eye Campaign",
        "5001": "Eye Campaign 1",
        "5002": "Eye Campaign 2",
        "5003": "Eye Campaign 3",
        "5004": "Eye Campaign 4",
        "5005": "Eye Campaign 5",
        "6000": "Center for NCD",
        "6001": "Center for NCD Room 1",
        "6002": "Center for NCD Room 2",
        "6003": "Center for NCD Room 3",
        "8000": "Dental Department",
        "8001": "Dental Room 1",
        "8002": "Dental Room 2",
        "8003": "Dental Ultrasound"
    };

    const currentDeptName = deptNames[currentDeptCode] || currentDeptCode;
    // console.log("🩺 Current Dept Code:", currentDeptCode);
    // console.log("🩺 Current Dept Name:", currentDeptName);

    const aiAllowedDepts = [
        "0100", "0101", "0102", "0103", "0104", "0105",
        "0200", "0201", "0202", "0203", "02202",
        "0300", "0301",
        "0400", "0401", "0402",
        "0500", "0501", "0502",
        "0600", "0601",
        "0700", "0701", "0702", "0703", "0704",
        "0800", "0801",
        "0900", "0901",
        "1600", "1601", "1602", "1603", "1604",
        "2000", "2001",
        "2100", "2101",
        "2200", "2201",
        "2300", "2301",
        "2400", "2401", "2402",
        "3000", "3001", "3002",
        "4000", "4001", "4002", "4003", "4004",
        "5000", "5001", "5002", "5003", "5004", "5005",
        "6000", "6001", "6002", "6003",
        "8000", "8001", "8002", "8003"
    ];

    const aiEnabled = aiAllowedDepts.includes(currentDeptCode);
    const isNcdDept = ncdDeptCodes.includes(currentDeptCode);

    var connection = new signalR.HubConnectionBuilder().withUrl(encodeURI("/chatHub")).build();
    //與Server建立連線
    connection.start().then(function () {
        console.warn("Hub connection successful!");
    }).catch(function (err) {
        alert('connection error: ' + err.toString());
    });


    // Tagify
    $('[name=tags]').tagify();

    // iCheck
    $("input").iCheck({
        labelHover: true,
        cursor: true,
        checkboxClass: "icheckbox_flat-pink",
        radioClass: "iradio_square-blue",
        increaseArea: "15%",
    });


    $(".lock-btn").click(function () {

        $(this).closest('tr')
            .find('span:not([data-status])').toggleClass('badge-primary badge-success')
            .find('i').toggleClass('fa-lock fa-unlock')

        $(this).closest('tr').find('td>input,select')
            .prop('disabled', (i, v) => !v);

    });


    // Reload Menu Basic Data
    $('.Reload-btn').click(function () {
        fullscreenLoading(true);

        $('#ICDMenuSearch').val('');
        $('#MedMenuSearch').val('');
        $('#NonMedMenuSearch').val('');

        RenderIcdMenu();
        RenderMedMenu();
        RenderLabMenu();
        RenderExamMenu();
        RenderPathMenu();
        RenderSupplyMenu();


        $('#Categoryul > li').not('.headli').eq(0).click();


        $('#CategoryitemCol').on('scroll', function () {
            let viewH = $(this).height(); //可見高度
            let contentH = $(this).get(0).scrollHeight; //内容高度
            let scrollTop = $(this).scrollTop(); //滾動高度

            //到達底部 20% 時,往下長100筆內容
            if (contentH - viewH - scrollTop <= contentH * 0.2) {

                let ShowCount = 0;
                $('.SearchNode,.MenuNode > li').each(function () {
                    if ($(this).css('display') == 'none') {
                        $(this).show();
                        ShowCount++;
                    }

                    if (ShowCount == 100) { return false; }
                });
                ShowCount = 0;
            }
        });

        fullscreenLoading(false);
    });


    // 螢幕暫存、完成看診、取消
    $('.cancel-btn').click(function () {

        //var hasChagne = false;

        //$.each(changeObj, function (k, v) {
        //    if (v) {
        //        hasChagne = v;
        //    }
        //});

        //if (hasChagne) {
        //    $(window).bind('beforeunload', function () { return true; });
        //} else {
        //    $(window).unbind('beforeunload');
        //}

        fn_CheckBeforeUnload();

        var sourceType = $('#clinicSourceType').val();
        var ward = $('#wardid').val();
        const params = new URLSearchParams({ sourceType });
        if (ward !== null && ward !== undefined) {
            params.append('wardId', ward);
        }
        window.location.href = `/HisOrder/HisOrder/Index?${params}`;
    });
    const apiKey = "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";

    window.sendToChatGPT = function (promptContent) {
        const systemPrompt = `
        you are a medical note summarization expert. with over 15 years of experience in clinical documentation and medical coding.
        only do what is asked in the user prompt.IMPORTANT:DO NOT ADD ANYTHING THAT IS NOT IN THE PROMPT.
    Generate a comprehensive Clinic Remark note based solely on the patient information below.

    Instructions:
    1. The patient information is completely de-identified. Do not infer or add any details not explicitly stated in the provided data. 
       If there is insufficient information for any section, skip that section entirely. Do not include any placeholder text or mention missing data.
    2. You will be given the template below. Follow it closely.
    3. In the <clinic_remark> section, format the note in clinical style with essential wording, using proper punctuation and medical terminology. 
       Ensure all relevant patient information is included in the summary.
    4. In the <recommendations> section, generate only 1  recommendation or observation based on the patient data. 
       Do not include any other sections, headings, or commentary.
    5. If there is remaining provider input that was not summarized in the <clinic_remark>, include it in a <note> section after <recommendations>. 
       If there is no such remaining content, do not generate the <note> section at all.
    6. NEVER use phrases like "Insufficient data", "Not available", "No data available", or similar placeholder text in any part of your response.
    7. IMPORTANT: Patient Demographics is CRITICAL information. Always include it in the summary if available.

    Here is the note template and follow it closely:
    clinic_remark
    summary of the clinic remark:[Start with Patient Demographics (Age, Gender) followed immediately by a COMPREHENSIVE summary of the clinical note, chief complaints, symptoms, and findings.]
    diagnosis:[if available]
    prescription:[if available]
    recommendations:[Insert your recommendations here]
    
    
    [If applicable:]
    <note>
    [Insert additional provider notes not summarized above]
    </note>

    Patient Information from the current visit:
    ${originalCkData}

    Now, generate the note
        `;

        const requestData = {
            model: "gpt-4o-mini",
            messages: [
                { role: "system", content: systemPrompt },
                { role: "user", content: promptContent }
            ]
        };

        return $.ajax({
            url: "https://api.openai.com/v1/chat/completions",
            method: "POST",
            contentType: "application/json",
            headers: { "Authorization": `Bearer ${apiKey}` },
            data: JSON.stringify(requestData)
        }).then(function (data) {
            if (data.choices && data.choices.length) {
                const aiText = data.choices[0].message.content;
                window.aiSuggestion = aiText;
                return aiText;
            }
            return $.Deferred().reject("No response from ChatGPT");
        });
    }

    window.extractMedicationsAndPrescriptionFromTable = function () {
        const medList = [];
        $('#medorder_table tbody tr').not('.medorder_tr_templete').each(function () {
            const $r = $(this);
            const generic = $r.find('td[data-plan-des]').data('plan-des') || '';
            const brand = $r.find('td').eq(5).text().trim();
            const dose = $r.find('input[data-qty-dose]').val().trim() || '';
            const unit = $r.find('td[data-unit-dose]').data('unit-dose') || '';
            const route = $r.find('select[data-dose-path]').val().trim() || '';
            const freq = $r.find('select[data-freq-code]').val().trim() || '';
            const days = $r.find('input[data-plan-days]').val().trim() || '';
            const qty = $r.find('input[data-total-qty]').val().trim() || '';

            const desc = [generic, brand, dose && unit ? `${dose}${unit}` : dose || unit,
                route ? `route:${route}` : '',
                freq ? `freq:${freq}` : '',
                days ? `${days}d` : '',
                qty ? `qty:${qty}` : ''
            ].filter(Boolean).join(', ');

            medList.push(desc);
        });
        return { medList };
    }

    $('.save-btn,.examining-btn,.completed-btn,.observation-btn,.discharge-btn').click(function (e) {
        const cm_data_Context = CKEDITOR.instances.editor_clinic_remarks.getData();
        let stripped = cm_data_Context
            .replace(/<[^>]*(>|$)/g, '')
            .replace(/&nbsp;|(\r\n|\n|\r)/g, '')
            .replace(/\s/g, '');
        if (stripped.length <= 50) {
            layer.msg('Please write in the clinic remark over 50 letters', {
                skin: 'layui-layer-lan',
                closebtn: 3,
                anim: 5,
                icon: 7,
                btn: ['OK'],
                title: 'Message'
            });
            return;
        }
        if (aiEnabled) {
            e.preventDefault();
            const $btn = $(this);
            previewTriggerBtn = $btn;
            originalCkData = CKEDITOR.instances.editor_clinic_remarks.getData();

            const visitPayload = buildVisitAiPayload({ includeManagement: isNcdDept });

            fullscreenLoading(true);
            runClinicAiGeneration(isNcdDept, visitPayload)
                .done(function (aiText) {
                    showAiPreviewResult(aiText, $btn);
                })
                .fail(function () {
                    handleAiGenerationFailure($btn);
                });
        } else {
            // AI disabled
            proceedSaveAction($(this));
        }
    });

    // Duplicate button logic removed. Refer to the updated handlers at the end of the file.

    // Ensure proper cleanup when modal is hidden
    $('#jsPreviewModal').on('hidden.bs.modal', function () {
        cleanupCountdown();

        // Reset button states
        $('#jsDeclineBtn')
            .prop('disabled', false)
            .removeClass('btn-success')
            .addClass('btn-secondary')
            .text('Manual');

        $('#jsEditBtn')
            .prop('disabled', false)
            .removeClass('btn-success')
            .addClass('btn-primary')
            .text('AI');

        $('#jsModalHelperText').html('Review the content below. Click a button to edit and save.');
    });

    //叫號
    $('.call-btn').click(function () {

        var inhospid = $('#patientInhospid').val();
        var vURL = "/HisOrder/Ajax/callLight";
        var vData = {
            inhospid: inhospid
        };

        var vSuccessFunc = function (msg) {
            var objResult = JSON.parse(msg);
            if (objResult.isSuccess == true) {
                layer.msg(objResult.Message);
                var regData = JSON.parse(objResult.returnValue);

                if (connection.state == "Disconnected") {
                    console.warn("Hub connection again!");
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

});

function proceedSaveAction($btn, aiPayload = { suggestion: null, modifiedContent: null, orginalData: null }) {
    if (aiPayload.suggestion !== null) window.aiSuggestion = aiPayload.suggestion;
    if (aiPayload.modifiedContent !== null) window.aiModified = aiPayload.modifiedContent;
    if (aiPayload.orginalData !== null) window.originalCkData = aiPayload.originalData;

    fullscreenLoading(true);


    let order_status = "";
    let clinic_status = "";
    if ($btn.hasClass('completed-btn')) { order_status = 'confirm'; clinic_status = 'Completed'; }
    else if ($btn.hasClass('observation-btn')) { order_status = 'confirm'; clinic_status = 'Observation'; }
    else if ($btn.hasClass('discharge-btn')) { order_status = 'confirm'; clinic_status = 'Discharge'; }

    if ($btn.hasClass('completed-btn')) {
        order_status = "confirm";
        clinic_status = "Completed";
    } else if ($btn.hasClass('observation-btn')) {
        order_status = "confirm";
        clinic_status = "Observation";
    } else if ($btn.hasClass('discharge-btn')) {
        order_status = "confirm";
        clinic_status = "Discharge";
    }

    const cm_data_Context = CKEDITOR.instances.editor_clinic_remarks.getData();
    let ConvertIntoString = cm_data_Context.toString();
    let RemovingEntity = ConvertIntoString.replace(/<[^>]*(>|$)|&nbsp;|(\r\n|\n|\r)/g, '').replace(/\s/g, '');

    const icdR = $("#icdR").text();
    const cudur = $(".tagify__tag-text").text();

    // Validation
    if (cudur === "" && icdR === "Y") {
        layer.alert('Please select at least one diagnosis', {
            skin: 'layui-layer-lan',
            closebtn: 3,
            anim: 5,
            icon: 7,
            btn: ['OK'],
            title: 'Message'
        });
        fullscreenLoading(false);
        return;
    } else if (RemovingEntity.length <= 50) {
        layer.msg('Please write in the clinic remark over 50 letters', {
            skin: 'layui-layer-lan',
            closebtn: 3,
            anim: 5,
            icon: 7,
            btn: ['OK'],
            title: 'Message'
        });
        fullscreenLoading(false);
        return;
    }

    if (document.readyState !== 'complete') {
        alert('The page has not finished loading. Please try again later.');
        fullscreenLoading(false);
        return;
    }

    const healthId = $('#patientHealthId').val();
    const inHospitalId = $('#patientInhospid').val();
    const dischargeStatus = $('#dischargeStatus').val();
    const transfer_ward = $('#transefer_place').val();
    const transfer_facility = $('#transfer_where').val();

    const dfd_diagnosis = $.Deferred();
    const dfd_med = $.Deferred();
    const dfd_nonmed = $.Deferred();
    const dfd_clinicStatus = $.Deferred();
    const dfd_soap = $.Deferred();

    $.when(
        fn_ChangeClinicStatus(dfd_clinicStatus, clinic_status, healthId, inHospitalId, dischargeStatus, transfer_ward, transfer_facility),
        fn_SaveDiagnosis(dfd_diagnosis, order_status),
        fn_SaveMedOrderByElements(dfd_med, order_status),
        fn_SaveNonMedOrderByElements(dfd_nonmed, order_status, window.originalCkData),
        fn_SaveSoapData(dfd_soap, order_status)
    ).done(function (r_clinic, r_diagnosis, r_med, r_nonmed, r_soap) {
        if (r_diagnosis.isSuccess) console.log('r_diagnosis saved');
        if (r_med.isSuccess) {
            fn_reload_medorder_partial_view();
            console.log('r_med saved');
        }
        if (r_nonmed.isSuccess) {
            fn_reload_nonmedorder_partial_view();
            console.log('r_nonmed saved');
        }
        if (r_soap.isSuccess) {
            const dfd = $.Deferred();
            $.when(getSoapDataByVersion(dfd)).done(function (r_version) {
                if (r_version.isSuccess) {
                    const data = JSON.parse(r_version.returnValue);
                    if (data && data.length) {
                        $(".version-select").empty();
                        $.each(data, function (idx, val) {
                            $(".version-select").append($('<option>', { value: val.VersionCode, text: val.Des }));
                        });
                        initSoapData($(".version-select option:selected").val());
                    } else {
                        initSoapData();
                    }
                }
            });
            console.log('r_soap saved');
        }
        if (r_clinic.isSuccess) console.log('r_clinic saved');
        if (clinic_status == "Discharge") {
            var sourceType = $('#clinicSourceType').val();
            var ward = $('#wardid').val();
            const params = new URLSearchParams({ sourceType });
            if (ward !== null && ward !== undefined) {
                params.append('wardId', ward);
            }
            window.location.href = `/HisOrder/HisOrder/Index?${params}`;
        }
        if (r_diagnosis.isSuccess && r_med.isSuccess && r_nonmed.isSuccess && r_clinic.isSuccess && r_soap.isSuccess) {
            fn_ResetChangeObj();
            fn_reload_nonmedorder_partial_view();
            fn_reload_medorder_partial_view();
            fullscreenLoading(false);
            layer.msg('successfully saved', { time: 1500, icon: 1 });
            fn_OrderPrint(order_status);
        } else {
            if (!r_diagnosis.isSuccess) {
                layer.alert("Save diagnosis order Error：" + r_diagnosis.Message, { icon: 2, title: "Error" });
            }
        }

    });
}


//更改診間狀態
function fn_ChangeClinicStatus(dfd2, status, healthID, inhospID, disch_status, transfer_ward, transfer_facility) {
    var transfer = $("#checkbox_transfer:checked").length;
    var _vTransfer_des = $("#transfer_des").val();
    //var _vTransfer = "N";
    //if (transfer != undefined && transfer > 0) {
    //    _vTransfer = "Y";
    //}

    var transferCode = $(".radio_transfer:checked").attr("data-trans-code");

    var vURL = "";
    var vData = {
        /*transfer: _vTransfer,*/
        transfer: transferCode,
        transfer_des: _vTransfer_des,
        healthId: healthID,
        inHospitalId: inhospID,
        dischargeStatus: disch_status,
        transferWard: transfer_ward,
        transferFacility: transfer_facility
    };
    if (status == "Completed") {
        vURL = "/HisOrder/Ajax/EndVisitSave";
    }
    else if (status == "Observation") {

        vURL = "/HisOrder/Ajax/ObserveSave";
    } else if (status == "Discharge") {
        vURL = "/HisOrder/Ajax/SaveDischargeStatus";
    }
    else {
        vURL = "/HisOrder/Ajax/ScreenSave";
    }

    var vSuccessFunc = function (msg) {
        // console.log("success");
        var objResult = JSON.parse(msg);
        if (objResult.isSuccess == true) {
            dfd2.resolve(objResult);
        }
        else {
            layer.msg("螢幕暫存執行失敗" + objResult.Message);
        }
    };
    var vErrorFunc = function () {
        layer.msg("螢幕暫存執行失敗");
    };

    ajax(vURL, vData, vSuccessFunc, vErrorFunc);
    return dfd2.promise();
}

function fn_ResetChangeObj() {
    $.each(changeObj, function (k, v) {
        changeObj[k] = false;
    });

}


function fn_CheckBeforeUnload() {

    var hasChagne = false;

    $.each(changeObj, function (k, v) {
        if (v) {
            hasChagne = v;
        }
    });

    if (hasChagne) {
        $(window).unbind('beforeunload');
        $(window).bind('beforeunload', function () { return true; });
    } else {
        $(window).unbind('beforeunload');
    }
}

function fn_OrderPrint(order_status) {

    $.ajax({
        type: 'POST',
        url: Root + "Print/PrintForm",
        data: {
            inStatus: order_status,
        },
        async: false,
        dataType: 'text',
        success: function (result) {
            $('#PrintForm').html(result);
            $('#PrintMenu').modal('show');
        },
        error: function (xhr, ajaxOptions, thrownError) { },
        complete: function (XMLHttpRequest, textStatus) { }
    });
}

// ==========================================
// AI / Manual Modal Logic Override
// ==========================================
$(document).ready(function () {
    // Override Button Click Logic
    // Left Button (#jsEditBtn)
    $('#jsEditBtn').off('click').on('click', function () {
        const $btn = $(this);
        const txt = $btn.text();

        if (txt === 'AI') {
            // "AI" workflow starting
            $('#jsPreviewTextarea').prop('readonly', false).show().focus();
            $('#jsPreviewRender').hide();

            // Show ONLY AI Text
            const aiText = $('#jsPreviewModal').data('aiText');
            $('#jsPreviewTextarea').val(aiText);

            // Transform to Save Check
            $btn.removeClass('btn-primary').addClass('btn-success').text('Save');

            // Transform Right Button to Cancel
            $('#jsDeclineBtn').removeClass('btn-secondary').addClass('btn-secondary').text('Cancel');

            // Clear any countdown on manual btn
            cleanupCountdown();

            // Store mode
            $('#jsPreviewModal').data('mode', 'AI');
            return;
        }

        if (txt.includes('Save')) {
            // Save Action
            if ($btn.prop('disabled')) return;

            let newData = $('#jsPreviewTextarea').val();

            // Conditional Save Logic: 
            // Only populate aiModified if the user actually changed the text from the initial generation.
            const initialCombinedText = $('#jsPreviewModal').data('combinedText');

            // Normalize line endings for comparison just in case
            const normNew = newData.replace(/\r\n/g, '\n');
            const normInit = (initialCombinedText || '').replace(/\r\n/g, '\n');

            if (normNew === normInit) {
                // User did NOT modify the text (accepted as-is)
                window.aiModified = "";
            } else {
                // User modified the text (AI or Manual part)
                window.aiModified = newData;
            }

            // Remove the separator line and headers if present for the Context (Final Result saving)
            // Note: We keep the headers in aiModified if they were there, as it represents exactly what the user saw/edited.
            // But for the main CKEditor (Context), we usually strip them? 
            // The previous code stripped them for CKEditor. Let's keep consistent.
            // Wait, previous code stripped them BEFORE assigning to aiModified.
            // The user said "in the jsedit we have both... save it to aimodified". 
            // So aiModified SHOULD contain the headers if the user left them there.
            // The Logic above `window.aiModified = newData` preserves them. Good.

            // Now prepare for CKEditor (Context) - usually we want clean text there?
            // The previous logic cleaned it. 
            let cleanData = newData.replace(/AI_clinic_remark\s*/gi, '')
                .replace(/Manual_clinic_remark\s*/gi, '')
                .replace(/\s*-{10,}\s*/g, '\n\n');

            CKEDITOR.instances.editor_clinic_remarks.setData(cleanData);
            $('#jsPreviewModal').modal('hide');
            if (window.previewTriggerBtn || previewTriggerBtn) {
                const btn = window.previewTriggerBtn || previewTriggerBtn;
                // Ensure variables are defined
                const sugg = window.aiSuggestion || '';
                const mod = window.aiModified || ''; // This now follows the conditional logic
                const orig = window.originalCkData || '';
                proceedSaveAction(btn, { suggestion: sugg, modifiedContent: mod, originalData: orig });
            }
            return;
        }

        if (txt === 'Cancel') {
            // Manual Mode Cancel -> Regenerate
            const mode = $('#jsPreviewModal').data('mode');
            if (mode === 'MANUAL') {
                cleanupCountdown();
                fn_RegenerateContent();
            }
        }
    });

    // Right Button (#jsDeclineBtn)
    $('#jsDeclineBtn').off('click').on('click', function (e) {
        if ($(this).prop('disabled')) {
            e.preventDefault();
            return false;
        }

        const $btn = $(this);
        const txt = $btn.text();

        if (txt === 'Manual') {
            // "Manual" workflow starting
            $('#jsPreviewTextarea').prop('readonly', false).show().focus();
            $('#jsPreviewRender').hide();

            // Show Combined Text (AI + Manual) - Punish the doctors via user request!
            const combinedText = $('#jsPreviewModal').data('combinedText');
            $('#jsPreviewTextarea').val(combinedText);

            // Transform Left Button to Save (Disabled 10s)
            const saveBtn = $('#jsEditBtn');
            saveBtn
                .removeClass('btn-primary').addClass('btn-success')
                .text('Save (10s)')
                .prop('disabled', true);

            // Start Countdown for the Save button (Left button)
            let countdown = 10;
            const interval = setInterval(() => {
                countdown--;
                if (countdown > 0) {
                    saveBtn.text(`Save (${countdown}s)`);
                } else {
                    clearInterval(interval);
                    $('#jsPreviewModal').removeData('countdownInterval');
                    saveBtn.prop('disabled', false).text('Save');
                }
            }, 1000);
            $('#jsPreviewModal').data('countdownInterval', interval);

            // Right Button -> Cancel
            $btn.text('Cancel');

            // Store mode
            $('#jsPreviewModal').data('mode', 'MANUAL');
            return;
        }

        if (txt === 'Cancel') {
            const mode = $('#jsPreviewModal').data('mode');

            // Clear any countdown
            cleanupCountdown();

            if (mode === 'AI') {
                // AI Mode Cancel -> Revert to Initial Selection State
                // Reset texts
                const combinedText = $('#jsPreviewModal').data('combinedText');
                $('#jsPreviewTextarea').val(combinedText).prop('readonly', true);
                renderPreview(combinedText);

                $('#jsEditBtn').removeClass('btn-success').addClass('btn-primary').text('AI').prop('disabled', false);
                $('#jsDeclineBtn').removeClass('btn-success').addClass('btn-secondary').text('Manual').prop('disabled', false);

                // Restart Initial "Manual" Countdown logic
                setupManualButtonCountdown();

            } else if (mode === 'MANUAL') {
                // Manual Mode Cancel -> Regenerate
                fn_RegenerateContent();
            }
        }
    });
});

// Helper to process AI content and setup initial state
function fn_ProcessAndDisplayAIContent(aiText, originalData) {
    // Clean any potential "insufficient data" mentions even if AI outputs them
    let cleanedText = aiText
        .replace(/\bInsufficient\s*data\b/gi, '')
        .replace(/\bNot\s*available\b/gi, '')
        .replace(/\bNo\s*data\b/gi, '')
        .replace(/\bUnavailable\b/gi, '')
        .replace(/<\/?note>/gi, '')
        .replace(/clinic_remark/gi, '')
        .replace(/patient_demographics:?/gi, '')
        .replace(/\n{2,}/g, '\n\n')
        .trim();

    // Strip HTML from originalCkData (preserving line breaks)
    let strippedOriginal = originalData
        .replace(/<br\s*\/?>/gi, '\n')
        .replace(/<\/p>/gi, '\n')
        .replace(/<\/div>/gi, '\n')
        .replace(/<[^>]+>/g, '')
        .replace(/&nbsp;/g, ' ')
        .trim();

    // Text Composition: AI first, Manual second
    let combinedText = "AI_clinic_remark\n" + cleanedText +
        "\n\n--------------------------------------------------\n\n" +
        "Manual_clinic_remark\n" + strippedOriginal;

    // Store texts for button actions
    $('#jsPreviewModal').data('aiText', cleanedText);
    $('#jsPreviewModal').data('manualText', strippedOriginal);
    $('#jsPreviewModal').data('combinedText', combinedText);

    $('#jsPreviewTextarea')
        .val(combinedText)
        .prop('readonly', true);

    renderPreview(combinedText);

    // Initial Button State
    $('#jsEditBtn')
        .prop('disabled', false)
        .removeClass('btn-success btn-primary')
        .addClass('btn-primary')
        .text('AI');

    $('#jsDeclineBtn')
        .prop('disabled', true) // Initially disabled for countdown
        .removeClass('btn-success btn-secondary')
        .addClass('btn-secondary')
        .text('Manual (10s)');

    // Setup countdown for Manual button entry
    if (typeof setupManualButtonCountdown === 'function') {
        setupManualButtonCountdown();
    }
}

// Function to regenerate content (called from Manual Mode cancellation)
function fn_RegenerateContent() {
    const isNcdDept = ncdDeptCodes.includes(($('#patientData').data('dept') || '').toString());
    const visitPayload = buildVisitAiPayload({ includeManagement: isNcdDept });

    $('#jsPreviewTextarea').val(isNcdDept
        ? 'Regenerating NCD AI content…'
        : 'Regenerating AI content…').prop('readonly', true);
    $('#jsEditBtn').text('Loading…').prop('disabled', true);
    $('#jsDeclineBtn').text('Loading…').prop('disabled', true);

    runClinicAiGeneration(isNcdDept, visitPayload)
        .done(function (aiText) {
            fn_ProcessAndDisplayAIContent(aiText, window.originalCkData);
            $('#jsPreviewModal').removeData('mode');
        })
        .fail(function () {
            layer.msg('AI service unavailable', { icon: 2 });
        });
}

function renderPreview(text) {
    const raw = text || $('#jsPreviewTextarea').val();
    let html = raw
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;");

    html = html.replace(/AI_clinic_remark/g, '<div style="text-align: center; font-weight: 800; font-size: 20px; margin: 10px 0;">AI_clinic_remark</div>');
    html = html.replace(/Manual_clinic_remark/g, '<div style="text-align: center; font-weight: 800; font-size: 20px; margin: 10px 0;">Manual_clinic_remark</div>');

    $('#jsPreviewRender').html(html).show();
    $('#jsPreviewTextarea').hide();
}