/// <reference path="..\lib\jquery\dist\jquery.js" />
/// <reference path="..\lib\ckeditor4\ckeditor.js" />
/// <reference path="..\lib\ckeditor4\plugins\confighelper\plugin.js" />

var oldSoap = [];


$(document).ready(function () {

    //CKEDITOR.addCss(".cke_editable{cursor:text; font-size: 16px; font-family: Roboto, sans-serif;}");
    //CKEDITOR.addCss(".cke_editable{cursor:text;  'default': 'Default custom field value.'}");

    CKEDITOR.replace("editor_clinic_remarks", {
        on: {
            // maximize the editor on startup
            'instanceReady': function (evt) {
                evt.editor.resize("100%", $(".test1").height() - 68);
            },
            'change': function (evt) {

                var cm = oldSoap.find(({ Kind }) => Kind == "CM");

                if (cm != null && cm.Context != CKEDITOR.instances.editor_clinic_remarks.getData()) {
                    changeObj.ClinicRemark = true;
                }



                /*changeObj.ClinicRemark = true;*/
            }
        },
        enterMode: CKEDITOR.ENTER_DIV,
    });

    CKEDITOR.replace("editor_managment", {
        on: {
            // maximize the editor on startup
            'instanceReady': function (evt) {
                evt.editor.resize("100%", $(".test2").height() - 68);
            },
            'change': function (evt) {

                var mg = oldSoap.find(({ Kind }) => Kind == "MG");

                if (mg != null && mg.Context != CKEDITOR.instances.editor_managment.getData()) {
                    changeObj.Management = true;
                }


            },

        },
        enterMode: CKEDITOR.ENTER_DIV,
    });


    if ($(".version-select option:selected") != undefined) {
        initSoapData($(".version-select option:selected").val())
    } else {
        initSoapData();
    }



    $('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
        if (e.target.hash == "#Soap_tab") {

            var rr = $(".test1").height();
            CKEDITOR.instances.editor_clinic_remarks.resize("100%", $(".test1").height() - 68);
            CKEDITOR.instances.editor_managment.resize("100%", $(".test2").height() - 68);

            /*            editor.resize("100%", $(".test2").height() - 36);*/
        }// newly activated tab

    })



    $(".add_soap").click(function () {
        layer.alert('Are you sure to add new version clinic remarks & management ? ', {
            skin: 'layui-layer-lan',
            closebtn: 1,
            anim: 5,
            icon: 1,
            btn: ['Yes', 'cancel'],
            title: 'Edit Message',
            yes: function (index) {
                $.when(fn_SaveSoapData($.Deferred(), ""))
                    .done(function (r_soap) {
                        if (r_soap.isSuccess == true) {
                            console.log('r_soap saved');
                            $.when(fn_SaveEmptySoapData($.Deferred(), ""))
                                .done(function (r_empty_soap) {
                                    if (r_empty_soap.isSuccess == true) {
                                        //initSoapData();
                                        //getSoapDataByVersion();
                                        var dfd = $.Deferred();
                                        $.when(getSoapDataByVersion(dfd)).done(
                                            function (r_version) {
                                                if (r_version.isSuccess == true) {
                                                    var data = JSON.parse(r_version.returnValue);
                                                    if (data != null && data != undefined && data.length != 0) {
                                                        $(".version-select").empty();
                                                        $.each(data, function (idx, val) {
                                                            $(".version-select").append($('<option>', { value: val.VersionCode, text: val.Des }));
                                                        });

                                                        initSoapData($(".version-select option:selected").val());
                                                    } else {
                                                        initSoapData();
                                                    }
                                                }
                                            })
                                    }
                                });
                        }
                    });

                layer.close(index);
            }
        });
    });

    $(".delete_soap").click(function () {
        var targetVer = $(".version-select option:selected").val();
        layer.alert('Are you sure to delete this version 【' + targetVer + '】 ? ', {
            skin: 'layui-layer-lan',
            closebtn: 1,
            anim: 5,
            icon: 2,
            btn: ['Yes', 'cancel'],
            title: 'Edit Message'
        }, function (index) {
            fn_DeleteSoapData(targetVer);
            layer.close(index);
        });
    });


    $(".version-select").change(function () {
        fn_SaveSoapData($.Deferred(), "");
        initSoapData(this.value);
    });


    $("#patient_remarks_label").click(function () {
        var text = $(this).text();
        var title = "【Remarks】:";
        var org_text = CKEDITOR.instances.editor_clinic_remarks.getData();
        if (org_text != undefined && org_text != "") {
            CKEDITOR.instances.editor_clinic_remarks.insertText("\r\n" + title + text);
        } else {
            CKEDITOR.instances.editor_clinic_remarks.insertText(title + text);
        }

    });

    initClinicalReviewButton();


    //var dfd = $.Deferred();
    //$.when(getSoapDataByVersion(dfd)).done(
    //    function (r_version) {
    //        if (r_version.isSuccess == true) {
    //            var data = JSON.parse(r_version.returnValue);
    //            if (data != null && data != undefined && data.length != 0) {
    //                $(".version-select").empty();
    //                $.each(data, function (idx, val) {
    //                    $(".version-select").append($('<option>', { value: val.VersionCode, text: val.Des }));
    //                });

    //                initSoapData($(".version-select option:selected").val());
    //            } else {
    //                initSoapData();
    //            }
    //        }
    //    })


    /*getSoapDataByVersion();*/
    //initSoapData();
});

function initClinicalReviewButton() {
    const btn = $("#btnClinicalReview");
    if (btn.length === 0) return;

    const inhospid = btn.data("inhospid");
    const patientId = btn.data("patientid");
    if (!inhospid && !patientId) return;

    fetch(`/Radiology/Interpretation/ClinicalReviewLookup?inhospid=${encodeURIComponent(inhospid || "")}&patientId=${encodeURIComponent(patientId || "")}`, {
        credentials: "same-origin"
    })
        .then(async (resp) => {
            if (!resp.ok) return null;
            return await resp.json();
        })
        .then((data) => {
            if (!data || !data.examRequestId) return;
            btn.show();
            btn.off("click").on("click", function () {
                const width = Math.floor(window.screen.width * 0.9);
                const height = Math.floor(window.screen.height * 0.85);
                const left = Math.floor((window.screen.width - width) / 2);
                const top = Math.floor((window.screen.height - height) / 2);
                window.open(
                    `/Radiology/Interpretation/ClinicalReviewWindow?examRequestId=${data.examRequestId}`,
                    `clinicalReview_${data.examRequestId}`,
                    `popup=yes,resizable=yes,scrollbars=yes,width=${width},height=${height},left=${left},top=${top}`
                );
            });
        })
        .catch((err) => {
            console.warn("Clinical review lookup failed", err);
        });
}


function getSoapDataByVersion(dfd) {
    var inhospid = $('#patientInhospid').val();
    var vURL = "/HisOrder/Soap/getSoapVerList";
    var vData = {
        inhospid: inhospid
    };
    var vSuccessFunc = function (msg) {
        var result = JSON.parse(msg);
        if (result.isSuccess == true) {
            //var data = JSON.parse(result.returnValue);
            //if (data != null && data != undefined && data.length != 0) {
            //    $(".version-select").empty();
            //    $.each(data, function (idx, val) {
            //        $(".version-select").append($('<option>', { value: val.VersionCode, text: val.Des }));
            //    });

            //    initSoapData($(".version-select option:selected").val());
            //} else {
            //    initSoapData();
            //}

            dfd.resolve(result);

        }
    }

    var vErrorFunc = function () {

        return this;
    };
    ajax(vURL, vData, vSuccessFunc, vErrorFunc);

    return dfd.promise();
}


function initSoapData(targetVersion) {
    var inhospid = $('#patientInhospid').val();
    var vURL = "/HisOrder/Soap/getSoapData";
    var vData = {
        inhospid: inhospid,
        inVersion: targetVersion
    };
    var vSuccessFunc = function (msg) {
        var result = JSON.parse(msg);
        if (result.isSuccess == true) {
            var data = JSON.parse(result.returnValue);
            if (data != null && data != undefined && data.length != 0) {
                oldSoap.length = 0;
                $.each(data, function (k, v) {
                    oldSoap.push(v);
                });
                var cm = oldSoap.find(({ Kind }) => Kind == "CM");

                if (cm != null) {
                    CKEDITOR.instances.editor_clinic_remarks.setData(cm.Context);
                    $("#editor_clinic_remarks").attr("data-soaid", cm.Soaid);
                    $("#editor_clinic_remarks").attr("data-version-code", cm.VersionCode);
                }

                var mg = oldSoap.find(({ Kind }) => Kind == "MG");
                if (mg != null) {
                    CKEDITOR.instances.editor_managment.setData(mg.Context);
                    $("#editor_managment").attr("data-soaid", mg.Soaid);
                    $("#editor_managment").attr("data-version-code", mg.VersionCode);
                }

                $('.delete_soap').show();
                $('.add_soap').show();
            } else {

                //重制ckeditor
                CKEDITOR.instances.editor_clinic_remarks.setData("");
                $("#editor_clinic_remarks").attr("data-soaid", "-1");
                $("#editor_clinic_remarks").attr("data-version-code", "");

                CKEDITOR.instances.editor_managment.setData("");
                $("#editor_managment").attr("data-soaid", "-1");
                $("#editor_managment").attr("data-version-code", "");

                $('.version-select').empty();
                $('.delete_soap').hide();
                $('.add_soap').hide();
            }
        }
    }

    var vErrorFunc = function () {

        return this;
    };
    ajaxGet(vURL, vData, vSuccessFunc, vErrorFunc);
};



function fn_DeleteSoapData(targetVersion) {
    var inhospid = $('#patientInhospid').val();
    var vURL = "/HisOrder/Soap/deleteSoapData";
    var vData = {
        inhospid: inhospid,
        inTargetVersion: targetVersion
    };

    var vSuccessFunc = function (msg) {
        var result = JSON.parse(msg);
        if (result.isSuccess == true) {
            /*getSoapDataByVersion();*/

            var dfd = $.Deferred();
            $.when(getSoapDataByVersion(dfd)).done(
                function (r_version) {
                    if (r_version.isSuccess == true) {
                        var data = JSON.parse(r_version.returnValue);
                        if (data != null && data != undefined && data.length != 0) {
                            $(".version-select").empty();
                            $.each(data, function (idx, val) {
                                $(".version-select").append($('<option>', { value: val.VersionCode, text: val.Des }));
                            });

                            initSoapData($(".version-select option:selected").val());
                        } else {
                            initSoapData();
                        }
                    }
                })
        }
    }

    var vErrorFunc = function () {

        return this;
    };
    ajax(vURL, vData, vSuccessFunc, vErrorFunc);

}


function fn_SaveEmptySoapData(dfd, status) {
    var inhospid = $('#patientInhospid').val();
    var healthId = $('#patientHealthId').val();
    var vURL = "/HisOrder/Soap/saveSoapData";

    const soaAry = [];

    //cm
    var cm_data_Context = "";
    var cm_obj = {
        Soaid: "-1",
        Inhospid: inhospid,
        HealthId: healthId,
        Kind: "CM",
        Context: cm_data_Context,
        SourceType: $('#clinicSourceType').val(),
        VersionCode: ""
    }

    soaAry.push(cm_obj);

    //mg
    var mg_data_Context = ""
    var mg_obj = {
        Soaid: "-1",
        Inhospid: inhospid,
        HealthId: healthId,
        Kind: "MG",
        Context: mg_data_Context,
        SourceType: $('#clinicSourceType').val(),
        VersionCode: ""
    }

    soaAry.push(mg_obj);

    var vData = {
        inhospid: inhospid,
        inData: JSON.stringify(soaAry)
    };

    var vSuccessFunc = function (msg) {
        var result = JSON.parse(msg);
        if (result.isSuccess == true) {
            dfd.resolve(result);
        }
    };
    var vErrorFunc = function () {
        fullscreenLoading(false);
        return this;
    };

    ajax(vURL, vData, vSuccessFunc, vErrorFunc);

    return dfd.promise();
}


function fn_SaveSoapData(dfd, order_status, aiPayload) {
    var inhospid = $('#patientInhospid').val();
    var healthId = $('#patientHealthId').val();
    var vURL = "/HisOrder/Soap/saveSoapData";
    const sourceType = $('#clinicSourceType').val();
    const version = $('#editor_clinic_remarks').attr('data-version-code') || "";
    const soaidCm = parseInt($('#editor_clinic_remarks').attr('data-soaid'), 10) || -1;
    const soaidMg = parseInt($('#editor_managment').attr('data-soaid'), 10) || -1;
    const createNewVersion = sourceType === "WARD";
    let newVersionCode = version;

    if (createNewVersion) {
        // Increment version code for ward
        newVersionCode = (parseInt(version) || 0) + 1;
    }

    const soaAry = [];
    //cm
    var cm_data_Context = CKEDITOR.instances.editor_clinic_remarks.getData();
    var cm_obj = {
        Soaid: $("#editor_clinic_remarks").attr("data-soaid"),
        Inhospid: inhospid,
        HealthId: healthId,
        Kind: "CM",
        Context: cm_data_Context,
        SourceType: $('#clinicSourceType').val(),
        VersionCode: $("#editor_clinic_remarks").attr("data-version-code"),
        Aigeneratedcontent: window.aiSuggestion,
        Ai_modified: window.aiModified,
        OriginalRemark: window.originalCkData,

    }

    soaAry.push(cm_obj);
    //mg
    var mg_data_Context = CKEDITOR.instances.editor_managment.getData();
    var mg_obj = {
        Soaid: $("#editor_managment").attr("data-soaid"),
        Inhospid: inhospid,
        HealthId: healthId,
        Kind: "MG",
        Context: mg_data_Context,
        SourceType: $('#clinicSourceType').val(),
        VersionCode: $("#editor_managment").attr("data-version-code")
    }

    soaAry.push(mg_obj);

    var vData = {
        inhospid: inhospid,
        originalRemark: window.originalCkData,
        inData: JSON.stringify(soaAry)
    };

    var vSuccessFunc = function (msg) {

        var result = JSON.parse(msg);
        console.log('soap ok');
        if (result.isSuccess == true) {
            dfd.resolve(result);
        } 1
    };
    var vErrorFunc = function () {
        fullscreenLoading(false);
        return this;
    };

    ajax(vURL, vData, vSuccessFunc, vErrorFunc);

    return dfd.promise();
}