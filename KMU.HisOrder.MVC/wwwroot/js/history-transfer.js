(function () {
    'use strict';

    var REMARKS_SEPARATOR = '<p style="color:#888"><strong>--- Past History ---</strong></p>';

    function isCkEditorVisuallyEmpty(html) {
        return !(html || '').replace(/<[^>]+>/g, '').replace(/&nbsp;/gi, ' ').trim();
    }

    function appendToCkEditorWithRetry(instanceName, innerHtml, attempts, delay) {
        attempts = attempts || 10;
        delay = delay || 200;
        return new Promise(function (resolve, reject) {
            function trySet(remaining) {
                if (window.CKEDITOR && CKEDITOR.instances && CKEDITOR.instances[instanceName]) {
                    try {
                        var editor = CKEDITOR.instances[instanceName];
                        var existing = editor.getData() || '';
                        var prefix = isCkEditorVisuallyEmpty(existing) ? '' : REMARKS_SEPARATOR;
                        editor.setData(existing + prefix + innerHtml);
                        return resolve(true);
                    } catch (ex) {
                        // fallthrough to retry
                    }
                }
                if (remaining <= 0) {
                    return reject(new Error('CKEditor instance not found: ' + instanceName));
                }
                setTimeout(function () { trySet(remaining - 1); }, delay);
            }
            trySet(attempts);
        });
    }

    function collectClinicRemarksHtml($tabPane) {
        var newHtml = '';
        $tabPane.find('.clinic-remark-entry .clinic-remark-html').each(function () {
            var inner = $(this).html() || '';
            if (!inner.replace(/<[^>]+>/g, '').replace(/&nbsp;/g, ' ').trim()) {
                return;
            }
            newHtml += '<div class="history-entry">' + inner + '</div><hr/>';
        });
        return newHtml;
    }

    /** Ignore template row, soft-deleted rows (hidden + data-modify-type D), and rows removed only visually. */
    function isActiveMedOrderRow($tr) {
        if (!$tr || !$tr.length) {
            return false;
        }
        if ($tr.hasClass('medorder_tr_templete')) {
            return false;
        }
        if (($tr.attr('data-modify-type') || '') === 'D') {
            return false;
        }
        if ($tr.is('[hidden]')) {
            return false;
        }
        return true;
    }

    function isMedRowDuplicate(planCode, planDes) {
        var dup = false;
        $('#medorder_table tbody tr').not('.medorder_tr_templete').each(function () {
            var $tr = $(this);
            if (!isActiveMedOrderRow($tr)) {
                return;
            }
            var code = ($tr.find('td[data-plan-code]').attr('data-plan-code') || '').trim();
            var name = ($tr.find('td[data-plan-des]').attr('data-plan-des') || $tr.find('td[data-plan-des]').text() || '').trim();
            if (planCode && code && code === planCode) {
                dup = true;
                return false;
            }
            if (planDes && name && name === planDes) {
                dup = true;
                return false;
            }
        });
        return dup;
    }

    /** Align copied meds with fn_addMedOrder / medicine table: use KmuMedicines catalog when available (global medItem from medOrder.js). */
    function enrichMedRowFromCatalog(row) {
        try {
            if (typeof medItem === 'undefined' || !medItem || medItem.length === 0) {
                return row;
            }
            var code = (row.plancode || '').toString().trim();
            var des = (row.plandes || '').toString().trim();
            var hit = medItem.find(function (x) {
                return ((x.MedId || '') + '').trim() === code;
            });
            if (!hit && des) {
                hit = medItem.find(function (x) {
                    return ((x.GenericName || '') + '').trim() === des;
                });
            }
            if (!hit) {
                return row;
            }
            if (hit.GenericName) {
                row.plandes = String(hit.GenericName).trim();
            }
            if (hit.BrandName) {
                row.plangen = String(hit.BrandName).trim();
            }
            if ((!row.MedUnitSpec || !String(row.MedUnitSpec).trim()) && hit.UnitSpec) {
                row.MedUnitSpec = String(hit.UnitSpec).trim();
            }
            if ((!row.MedDefaultFreq || !String(row.MedDefaultFreq).trim()) && hit.DefaultFreq) {
                row.MedDefaultFreq = String(hit.DefaultFreq).trim();
            }
            return row;
        } catch (ex) {
            return row;
        }
    }

    function collectMedicationsFromTab($tab) {
        var meds = [];
        $tab.find('.history-med-row').each(function () {
            var $r = $(this);
            var planCode = ($r.attr('data-plan-code') || '').toString().trim();
            var planDes = $r.find('.med-plan-des').text().trim() || '';
            if (!planDes) {
                return;
            }
            if (isMedRowDuplicate(planCode, planDes)) {
                return;
            }
            meds.push(enrichMedRowFromCatalog({
                orderplaid: '',
                plancode: planCode,
                plandes: planDes,
                plangen: '',
                MedUnitSpec: $r.find('.med-unit').text().trim() || '',
                MedDefaultFreq: $r.find('.med-freq').text().trim() || '',
                MedQtyDose: $r.find('.med-qty-dose').text().trim() || '',
                MedDosePath: $r.find('.med-route').text().trim() || '',
                MedPlanDays: $r.find('.med-days').text().trim() || '',
                MedTotalQty: $r.find('.med-qty').text().trim() || '',
                MedRemark: $r.find('.med-remark').text().trim() || ''
            }));
        });
        return meds;
    }

    function isDiagnosisDuplicate(planCode) {
        var pc = (planCode || '').toString().trim();
        if (!pc) {
            return false;
        }
        if (typeof window.tagify === 'undefined' || !window.tagify || typeof window.tagify.value === 'undefined') {
            return false;
        }
        return window.tagify.value.some(function (t) {
            return ((t.plancode || '') + '').trim() === pc;
        });
    }

    /** Diagnosis rows live in .history-diagnosis-block (see _HistoryRecordPartialView). Parse data-* or "1. CODE Description" text. */
    function collectDiagnosesFromTab($tab) {
        var list = [];
        $tab.find('.history-diagnosis-block h6').each(function () {
            var $h = $(this);
            var planCode = ($h.attr('data-plan-code') || '').toString().trim();
            var planDes = ($h.attr('data-plan-des') || '').toString().trim();
            if (!planCode || !planDes) {
                var raw = ($h.text() || '').trim();
                var m = raw.match(/^\d+\.\s+(\S+)\s+([\s\S]+)$/);
                if (m) {
                    if (!planCode) {
                        planCode = m[1].trim();
                    }
                    if (!planDes) {
                        planDes = m[2].trim();
                    }
                }
            }
            if (!planCode && !planDes) {
                return;
            }
            if (planCode && isDiagnosisDuplicate(planCode)) {
                return;
            }
            list.push({
                orderplanid: -1,
                plancode: planCode,
                plandes: planDes,
                qty: 1
            });
        });
        return list;
    }

    function isActiveNonMedOrderRow($tr) {
        if (!$tr || !$tr.length) {
            return false;
        }
        if ($tr.hasClass('nonmedorder_tr_templete')) {
            return false;
        }
        if (($tr.attr('data-modify-type') || '') === 'D') {
            return false;
        }
        if ($tr.is('[hidden]')) {
            return false;
        }
        return true;
    }

    function isNonMedRowDuplicate(planCode) {
        var pc = (planCode || '').toString().trim();
        if (!pc) {
            return false;
        }
        var dup = false;
        $('#nonmedorder_table tbody tr').not('.nonmedorder_tr_templete').each(function () {
            var $tr = $(this);
            if (!isActiveNonMedOrderRow($tr)) {
                return;
            }
            var code = ($tr.find('td[data-plan-code]').attr('data-plan-code') || $tr.find('td[data-plan-code]').text() || '').trim();
            if (code && code === pc) {
                dup = true;
                return false;
            }
        });
        return dup;
    }

    function collectNonMedFromTab($tab) {
        var list = [];
        $tab.find('.history-nonmed-table tbody tr').each(function () {
            var $r = $(this);
            if ($r.find('th').length) {
                return;
            }
            var planCode = ($r.attr('data-plan-code') || '').toString().trim();
            var planDes = $r.find('.history-nonmed-des').text().trim() || '';
            if (!planCode) {
                planCode = ($r.find('td').eq(0).text() || '').trim();
            }
            if (!planDes) {
                planDes = ($r.find('td').eq(1).text() || '').trim();
            }
            if (!planCode && !planDes) {
                return;
            }
            if (planCode && isNonMedRowDuplicate(planCode)) {
                return;
            }
            var htype = ($r.attr('data-hplan-type') || '').toString().trim();
            var qtyRaw = ($r.attr('data-qty') || '').toString().trim();
            if (qtyRaw === '' && $r.find('td').length > 2) {
                qtyRaw = ($r.find('td').eq(2).text() || '').trim();
            }
            var qty = qtyRaw !== '' ? qtyRaw : '1';
            var loc = ($r.attr('data-location') || '').toString().trim();
            var rmk = ($r.attr('data-remark') || '').toString().trim();
            if (!loc && $r.find('td').length > 4) {
                loc = ($r.find('td').eq(4).text() || '').trim();
            }
            if (!rmk && $r.find('td').length > 5) {
                rmk = ($r.find('td').eq(5).text() || '').trim();
            }
            list.push({
                orderplaid: '',
                plancode: planCode,
                plandes: planDes,
                qty: qty,
                NonMedItemType: htype || 'Material',
                LocationCode: loc,
                Remark: rmk
            });
        });
        return list;
    }

    function transferClinicRemarks($tab, options) {
        options = options || {};
        var inner = collectClinicRemarksHtml($tab);
        if (!inner) {
            if (options.showEmptyMsg !== false) {
                layer.msg('No clinic remarks to transfer', { icon: 0, offset: 'rb' });
            }
            return Promise.reject(new Error('empty'));
        }
        var fallbackPayload = (function () {
            var $el = $('#editor_clinic_remarks');
            if (!$el.length) {
                return '';
            }
            var existing = $el.html() || $el.val() || '';
            var prefix = isCkEditorVisuallyEmpty(existing) ? '' : REMARKS_SEPARATOR;
            return existing + prefix + inner;
        })();

        return appendToCkEditorWithRetry('editor_clinic_remarks', inner)
            .then(function () {
                if (!options.silent) {
                    layer.msg('Clinic remarks transferred', { icon: 1, offset: 'rb' });
                }
            })
            .catch(function (err) {
                var $el = $('#editor_clinic_remarks');
                if ($el.length && fallbackPayload) {
                    $el.html(fallbackPayload);
                    if (!options.silent) {
                        layer.msg('Clinic remarks transferred (fallback)', { icon: 1, offset: 'rb' });
                    }
                    return;
                }
                console.error(err);
                layer.msg('Editor not available to receive clinic remarks', { icon: 2, offset: 'rb' });
            });
    }

    function transferMedications($tab, options) {
        options = options || {};
        var meds = collectMedicationsFromTab($tab);
        if (meds.length === 0) {
            if (options.showEmptyMsg !== false) {
                layer.msg('No new medicines to transfer (duplicates skipped)', { icon: 0, offset: 'rb' });
            }
            return 0;
        }
        if (typeof fn_addMedOrder === 'function') {
            fn_addMedOrder(meds);
            if (!options.silent) {
                layer.msg('Medicines transferred', { icon: 1, offset: 'rb' });
            }
        } else if (typeof SetMedicalItemAddRow === 'function') {
            meds.forEach(function (m) {
                SetMedicalItemAddRow(m.plancode, m.plandes, 'Med', {
                    MedType: '',
                    GenericName: m.plangen,
                    UnitSpec: m.MedUnitSpec,
                    PackSpec: '',
                    DefaultFreq: m.MedDefaultFreq,
                    RefDuration: '',
                    Remarks: m.MedRemark || ''
                }, null);
            });
            if (!options.silent) {
                layer.msg('Medicines transferred (fallback)', { icon: 1, offset: 'rb' });
            }
        } else {
            layer.msg('Medication API not found on page', { icon: 2, offset: 'rb' });
            return 0;
        }
        return meds.length;
    }

    function transferDiagnoses($tab, options) {
        options = options || {};
        var rows = collectDiagnosesFromTab($tab);
        if (rows.length === 0) {
            if (options.showEmptyMsg !== false) {
                layer.msg('No new diagnoses to transfer (duplicates skipped or none in visit)', { icon: 0, offset: 'rb' });
            }
            return 0;
        }
        if (typeof fn_addIcdOrder !== 'function') {
            layer.msg('Diagnosis editor not ready on this page', { icon: 2, offset: 'rb' });
            return 0;
        }
        if (typeof window.tagify === 'undefined' || !window.tagify || typeof window.tagify.addTags !== 'function') {
            layer.msg('Diagnosis tags are not available (open Clinic Remarks tab if diagnosis is hidden)', { icon: 2, offset: 'rb' });
            return 0;
        }
        fn_addIcdOrder(rows);
        if (!options.silent) {
            layer.msg('Diagnoses added to this visit (save order to persist)', { icon: 1, offset: 'rb' });
        }
        return rows.length;
    }

    function transferNonMedOrders($tab, options) {
        options = options || {};
        var items = collectNonMedFromTab($tab);
        if (items.length === 0) {
            if (options.showEmptyMsg !== false) {
                layer.msg('No new other orders to transfer (duplicates skipped)', { icon: 0, offset: 'rb' });
            }
            return 0;
        }
        if (typeof fn_addNonMedorder !== 'function') {
            layer.msg('Other orders table not available on this page', { icon: 2, offset: 'rb' });
            return 0;
        }
        fn_addNonMedorder(items);
        if (!options.silent) {
            layer.msg('Other orders transferred', { icon: 1, offset: 'rb' });
        }
        return items.length;
    }

    $(document).on('click', '.btn-transfer-clinic', function (e) {
        e.preventDefault();
        try {
            var $tab = $(this).closest('.tab-pane');
            if ($tab.length === 0) {
                layer.msg('Cannot locate history container', { icon: 2, offset: 'rb' });
                return;
            }
            transferClinicRemarks($tab, {});
        } catch (ex) {
            console.error(ex);
            layer.msg('Error transferring clinic remarks: ' + ex.message, { icon: 2, offset: 'rb' });
        }
    });

    $(document).on('click', '.btn-transfer-med', function (e) {
        e.preventDefault();
        try {
            var $tab = $(this).closest('.tab-pane');
            if ($tab.length === 0) {
                layer.msg('Cannot locate history container', { icon: 2, offset: 'rb' });
                return;
            }
            transferMedications($tab, {});
        } catch (ex) {
            console.error(ex);
            layer.msg('Error transferring medicines: ' + ex.message, { icon: 2, offset: 'rb' });
        }
    });

    $(document).on('click', '.btn-transfer-all', function (e) {
        e.preventDefault();
        try {
            var $tab = $(this).closest('.tab-pane');
            if ($tab.length === 0) {
                layer.msg('Cannot locate history container', { icon: 2, offset: 'rb' });
                return;
            }

            var hadRemarksContent = collectClinicRemarksHtml($tab).length > 0;

            var remarkWork = hadRemarksContent
                ? transferClinicRemarks($tab, { silent: true, showEmptyMsg: false })
                : Promise.resolve();

            Promise.resolve(remarkWork)
                .catch(function () { })
                .then(function () {
                    var addedDiag = transferDiagnoses($tab, { silent: true, showEmptyMsg: false });
                    var addedMeds = transferMedications($tab, { silent: true, showEmptyMsg: false });
                    var addedNonMed = transferNonMedOrders($tab, { silent: true, showEmptyMsg: false });

                    var bits = [];
                    if (hadRemarksContent) {
                        bits.push('clinic remarks');
                    }
                    if (addedDiag > 0) {
                        bits.push('diagnosis (' + addedDiag + ')');
                    }
                    if (addedMeds > 0) {
                        bits.push('medications (' + addedMeds + ')');
                    }
                    if (addedNonMed > 0) {
                        bits.push('other orders (' + addedNonMed + ')');
                    }

                    if (bits.length === 0) {
                        layer.msg('Nothing new to copy from this visit (duplicates skipped or empty)', { icon: 0, offset: 'rb' });
                    } else {
                        layer.msg('Copied: ' + bits.join(', ') + '. Save the order when finished.', { icon: 1, offset: 'rb' });
                    }
                });
        } catch (ex) {
            console.error(ex);
            layer.msg('Error transferring history: ' + ex.message, { icon: 2, offset: 'rb' });
        }
    });
})();
