(function () {
    const root = document.getElementById("interpretationDesktop");
    if (!root) return;

    const reportId = Number(root.dataset.reportId);
    const examRequestId = Number(root.dataset.examRequestId);
    const studyUid = root.dataset.studyUid || "";
    const saveStatus = document.getElementById("saveStatus");
    const statusEl = document.getElementById("reportStatus");
    const impressionEl = document.getElementById("reportImpression");
    const structuredEl = document.getElementById("reportStructuredFindings");
    const saveBtn = document.getElementById("btnSaveDraft");
    const finalizeBtn = document.getElementById("btnFinalize");
    const aiAssistBtn = document.getElementById("btnAiAssist");
    const aiAssistToast = document.getElementById("aiAssistToast");
    const transitionButtons = root.querySelectorAll("[data-transition]");
    const viewerRoot = document.getElementById("dicomViewerRoot");
    const viewerStatus = document.getElementById("dicomViewerStatus");
    const viewerImage = document.getElementById("dicomRenderedImage");
    const viewerPlaceholder = document.getElementById("dicomViewerPlaceholder");
    const viewerPrevBtn = document.getElementById("dicomPrevBtn");
    const viewerNextBtn = document.getElementById("dicomNextBtn");
    const viewerPopoutBtn = document.getElementById("dicomPopoutBtn");

    let dirty = false;
    let token = "";
    let renderedInstanceUrls = [];
    let renderedIndex = 0;

    // Initialize CKEDITOR safely
    if (typeof CKEDITOR !== "undefined") {
        CKEDITOR.replace("reportEditor");
        CKEDITOR.instances.reportEditor.on("change", () => { dirty = true; });
    }
    [statusEl, impressionEl, structuredEl].forEach(el => {
        if (el) el.addEventListener("input", () => { dirty = true; });
    });

    function setStatus(message) {
        if (saveStatus) saveStatus.innerText = message;
    }

    let aiAssistToastTimer = null;

    function showToast(message, type) {
        if (!aiAssistToast) return;
        const variant = type === "error" ? "alert-danger"
            : type === "success" ? "alert-success"
                : "alert-info";
        aiAssistToast.className = `alert shadow ${variant}`;
        aiAssistToast.textContent = message;
        aiAssistToast.style.display = "block";
        if (aiAssistToastTimer) clearTimeout(aiAssistToastTimer);
        aiAssistToastTimer = setTimeout(() => {
            aiAssistToast.style.display = "none";
        }, type === "error" ? 8000 : 5000);
    }

    async function runAiAssist() {
        if (!aiAssistBtn) return;

        const ageRaw = aiAssistBtn.dataset.patientAge;
        const parsedAge = ageRaw && ageRaw !== "--" ? parseInt(ageRaw, 10) : null;
        const patientAge = Number.isFinite(parsedAge) ? parsedAge : null;
        const patientSex = aiAssistBtn.dataset.patientSex || "";

        aiAssistBtn.disabled = true;
        showToast("AI Assist is analyzing DICOM images. This may take a minute...", "info");

        try {
            const response = await fetch("/Radiology/Interpretation/AIAssist", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                credentials: "same-origin",
                body: JSON.stringify({
                    examRequestId: examRequestId,
                    patientAge: patientAge,
                    patientSex: patientSex
                })
            });

            const responseText = await response.text();
            let payload = {};
            if (responseText) {
                try {
                    payload = JSON.parse(responseText);
                } catch {
                    payload = { message: responseText.trim() };
                }
            }
            if (!response.ok) {
                const serverMessage = payload.message || payload.title || payload.detail;
                throw new Error(serverMessage || `AI Assist failed (${response.status})`);
            }

            if (impressionEl && payload.impression) {
                impressionEl.value = payload.impression;
            }
            if (typeof CKEDITOR !== "undefined" && CKEDITOR.instances.reportEditor && payload.narrative) {
                CKEDITOR.instances.reportEditor.setData(payload.narrative);
            }
            dirty = true;

            const imagesNote = payload.imagesAnalyzed
                ? ` (${payload.imagesAnalyzed} image${payload.imagesAnalyzed === 1 ? "" : "s"} analyzed)`
                : "";
            const disclaimer = payload.disclaimer ? ` ${payload.disclaimer}` : "";
            showToast(`AI draft applied${imagesNote}.${disclaimer}`, "success");
            setStatus("AI Assist draft applied — review before finalizing");
        } catch (err) {
            console.error("AI Assist failed", err);
            showToast(err.message || "AI Assist unavailable.", "error");
            setStatus("AI Assist failed");
        } finally {
            aiAssistBtn.disabled = false;
        }
    }

    function setViewerStatus(message) {
        if (viewerStatus) viewerStatus.innerText = message;
    }

    function showViewerPlaceholder(message) {
        if (viewerPlaceholder) {
            viewerPlaceholder.style.display = "block";
            viewerPlaceholder.innerText = message;
        }
        if (viewerImage) {
            viewerImage.style.display = "none";
            viewerImage.removeAttribute("src");
        }
    }

    async function loadCurrent() {
        try {
            const response = await fetch(`/Radiology/Interpretation/GetReport?reportId=${reportId}`, {
                credentials: 'same-origin'
            });

            // ✅ Check status BEFORE parsing JSON
            if (!response.ok) {
                const text = await response.text();
                throw new Error(`GetReport failed: ${response.status} ${text.substring(0, 200)}`);
            }

            const data = await response.json();
            if (statusEl) statusEl.value = data.status || "Draft";
            if (impressionEl) impressionEl.value = data.impression || "";
            if (structuredEl) structuredEl.value = JSON.stringify(data.structuredFindings || {}, null, 2);
            if (typeof CKEDITOR !== "undefined" && CKEDITOR.instances.reportEditor) {
                CKEDITOR.instances.reportEditor.setData(data.reportBodyHtml || "");
            }
            token = data.concurrencyToken || "";
            dirty = false;
        } catch (err) {
            console.error("Failed to load report", err);
            setStatus("Failed to load report");
        }
    }

    function getPayload() {
        let findings = {};
        try {
            findings = JSON.parse(structuredEl?.value || "{}");
        } catch {
            findings = {};
        }
        return {
            reportId: reportId,
            status: statusEl?.value || "Draft",
            impression: impressionEl?.value || "",
            reportBodyHtml: typeof CKEDITOR !== "undefined" && CKEDITOR.instances.reportEditor
                ? CKEDITOR.instances.reportEditor.getData()
                : "",
            structuredFindings: findings,
            concurrencyToken: token
        };
    }

    // ✅ FIXED: Robust preloadRenderedImage with proper error handling
    async function preloadRenderedImage(url) {
        console.log("🖼️ Preloading rendered image:", url);

        try {
            const resp = await fetch(url, {
                headers: {
                    'Accept': 'image/jpeg'
                },
                credentials: 'same-origin'
            });

            console.log("📥 Rendered response status:", resp.status);

            // ✅ Check status BEFORE reading body
            if (!resp.ok) {
                let errorText = '';
                try { errorText = await resp.text(); } catch (e) { }
                throw new Error(`Rendered fetch failed (${resp.status}): ${errorText.substring(0, 200)}`);
            }

            const blob = await resp.blob();
            console.log("✅ Blob loaded, size:", blob.size, "bytes");

            return URL.createObjectURL(blob);
        } catch (err) {
            console.error("❌ preloadRenderedImage failed:", err);
            throw err;
        }
    }

    async function resolveOrthancInstanceId(sopInstanceUid) {
        const response = await fetch("/Radiology/Stream/tools/find", {
            method: "POST",
            headers: { "Content-Type": "application/json", "Accept": "application/json" },
            credentials: "same-origin",
            body: JSON.stringify({
                Level: "Instance",
                Query: {
                    SOPInstanceUID: sopInstanceUid
                }
            })
        });

        if (!response.ok) {
            throw new Error(`tools/find failed (${response.status})`);
        }

        const payload = await response.json();
        if (!Array.isArray(payload) || payload.length === 0 || typeof payload[0] !== "string") {
            throw new Error("No Orthanc instance ID found for SOP");
        }

        return payload[0];
    }

    // ✅ FIXED: Robust renderViewerImage with memory leak prevention
    async function renderViewerImage(index) {
        if (!viewerImage || !viewerPlaceholder) return;
        if (!renderedInstanceUrls.length) {
            showViewerPlaceholder("No rendered instances available.");
            return;
        }

        renderedIndex = Math.max(0, Math.min(index, renderedInstanceUrls.length - 1));
        setViewerStatus(`Loading image ${renderedIndex + 1}/${renderedInstanceUrls.length}...`);

        try {
            // Revoke previous URL to prevent memory leak
            if (viewerImage.src && viewerImage.src.startsWith('blob:')) {
                URL.revokeObjectURL(viewerImage.src);
            }

            console.log("🖼️ Rendering image", renderedIndex + 1, "of", renderedInstanceUrls.length);

            // Get the object URL from preloadRenderedImage
            const objectUrl = await preloadRenderedImage(renderedInstanceUrls[renderedIndex]);

            viewerImage.src = objectUrl;
            viewerImage.style.display = "block";
            viewerPlaceholder.style.display = "none";
            setViewerStatus(`Showing image ${renderedIndex + 1}/${renderedInstanceUrls.length}`);

            console.log("✅ Image rendered successfully");

        } catch (err) {
            console.error("❌ Failed to render DICOM instance", err);
            showViewerPlaceholder(`Unable to render image ${renderedIndex + 1}.`);
            setViewerStatus("Viewer render failed");
        }

        if (viewerPrevBtn) viewerPrevBtn.disabled = renderedIndex === 0;
        if (viewerNextBtn) viewerNextBtn.disabled = renderedIndex >= renderedInstanceUrls.length - 1;
    }

    // Load viewer using study -> series -> instances hierarchy.
    async function loadViewer() {
        if (!viewerRoot || !studyUid) return;
        setViewerStatus("Loading study series...");
        showViewerPlaceholder("Loading study series...");

        try {
            const encodedStudyUid = encodeURIComponent(studyUid);
            console.log("🔍 loadViewer: Fetching series for Study UID:", studyUid);

            // Step 1: Fetch series list under this study (JSON-friendly endpoint).
            const seriesResp = await fetch(
                `/Radiology/Stream/dicom-web/studies/${encodedStudyUid}/series`,
                {
                    headers: { "Accept": "application/json" },
                    credentials: 'same-origin'
                }
            );

            if (!seriesResp.ok) {
                let errorText = '';
                try { errorText = await seriesResp.text(); } catch (e) { }
                console.error("❌ Series fetch failed:", seriesResp.status, errorText);
                throw new Error(`Series fetch failed (${seriesResp.status}): ${errorText.substring(0, 200)}`);
            }

            const seriesJson = await seriesResp.json();
            const seriesList = Array.isArray(seriesJson) ? seriesJson : [seriesJson];
            if (!seriesList.length) {
                throw new Error("No series found in study");
            }
            console.log(`📋 Series found: ${seriesList.length}`);

            // Step 2: Fetch instances using Orthanc-supported study endpoint first.
            const sopUids = [];
            let instancesResp = await fetch(
                `/Radiology/Stream/dicom-web/studies/${encodedStudyUid}/instances`,
                {
                    headers: { "Accept": "application/json" },
                    credentials: "same-origin"
                }
            );

            // Fallback for Orthanc variants that only expose series-scoped instances.
            if (!instancesResp.ok) {
                for (let i = 0; i < seriesList.length; i++) {
                    const series = seriesList[i];
                    const seriesUid = series?.["0020000E"]?.Value?.[0] || series?.SeriesInstanceUID;
                    if (!seriesUid || typeof seriesUid !== "string") {
                        continue;
                    }

                    const scopedResp = await fetch(
                        `/Radiology/Stream/dicom-web/studies/${encodedStudyUid}/series/${encodeURIComponent(seriesUid)}/instances`,
                        {
                            headers: { "Accept": "application/json" },
                            credentials: "same-origin"
                        }
                    );
                    if (!scopedResp.ok) {
                        continue;
                    }

                    const scopedJson = await scopedResp.json();
                    const scopedInstances = Array.isArray(scopedJson) ? scopedJson : [scopedJson];
                    for (const inst of scopedInstances) {
                        const sop = inst?.["00080018"]?.Value?.[0] || inst?.SOPInstanceUID;
                        if (sop && typeof sop === "string" && sop.length > 0) {
                            sopUids.push(sop);
                        }
                    }
                }
            } else {
                const instancesJson = await instancesResp.json();
                const instances = Array.isArray(instancesJson) ? instancesJson : [instancesJson];
                for (const inst of instances) {
                    const sop = inst?.["00080018"]?.Value?.[0] || inst?.SOPInstanceUID;
                    if (sop && typeof sop === "string" && sop.length > 0) {
                        sopUids.push(sop);
                    }
                }
            }

            if (!sopUids.length) {
                throw new Error("No valid SOPInstanceUIDs found from series instances");
            }

            // Step 3: Convert SOP UID -> Orthanc instance ID, then use REST rendered endpoint.
            const orthancInstanceIds = [];
            for (const sop of sopUids) {
                try {
                    const orthancId = await resolveOrthancInstanceId(sop);
                    orthancInstanceIds.push(orthancId);
                } catch (err) {
                    console.warn("⚠️ Could not resolve Orthanc instance ID for SOP", sop, err);
                }
            }

            if (!orthancInstanceIds.length) {
                throw new Error("No Orthanc instance IDs resolved for rendering");
            }

            renderedInstanceUrls = orthancInstanceIds.map(id =>
                `/Radiology/Stream/instances/${encodeURIComponent(id)}/rendered`
            );
            console.log(`✅ Collected renderable instances: ${renderedInstanceUrls.length}`);

            await renderViewerImage(0);
            setViewerStatus(`Loaded ${renderedInstanceUrls.length} images`);

        } catch (err) {
            console.error("❌ DICOM viewer load failed", err);
            setViewerStatus("Viewer load failed");
            showViewerPlaceholder(`Unable to load images: ${err.message}`);
        }
    }

    async function saveDraft() {
        if (!dirty) return;
        setStatus("Saving draft...");
        try {
            const response = await fetch("/Radiology/Interpretation/SaveReport", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(getPayload()),
                credentials: 'same-origin'
            });

            if (response.status === 409) {
                const localText = typeof CKEDITOR !== "undefined" && CKEDITOR.instances.reportEditor
                    ? CKEDITOR.instances.reportEditor.getData()
                    : "";
                navigator.clipboard.writeText(localText).catch(() => { });
                alert("409 conflict detected. Your unsaved text was copied to clipboard. Please refresh to merge with latest report.");
                setStatus("Conflict detected");
                return;
            }

            if (!response.ok) {
                const errText = await response.text();
                throw new Error(`Save failed: ${response.status} ${errText.substring(0, 200)}`);
            }

            const data = await response.json();
            token = data.concurrencyToken || token;
            dirty = false;
            setStatus(`Saved at ${new Date().toLocaleTimeString()}`);
        } catch (err) {
            console.error("Save draft failed", err);
            setStatus(`Save error: ${err.message}`);
        }
    }

    async function finalize() {
        try {
            const impression = (impressionEl?.value || "").trim();

            // Client-side validation: require impression before finalizing
            if (!impression) {
                alert("Please enter an Impression before finalizing the report.");
                impressionEl?.focus();
                return;
            }

            const response = await fetch(`/Radiology/Interpretation/FinalizeReport?reportId=${reportId}`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    impression: impression,
                    radiologistSignature: "SIGNED",
                    concurrencyToken: token
                }),
                credentials: 'same-origin'
            });

            if (response.status === 409) {
                alert("Finalize blocked due to concurrency conflict. Refresh the report first.");
                return;
            }

            if (!response.ok) {
                const err = await response.json();
                alert(err.message || "Finalize failed");
                return;
            }

            const data = await response.json();
            token = data.concurrencyToken || token;
            setStatus("Finalized");
        } catch (err) {
            console.error("Finalize failed", err);
            alert(`Finalize error: ${err.message}`);
        }
    }

    async function transition(targetStatus) {
        try {
            // If there are unsaved changes, save them first.
            if (dirty) {
                setStatus("Saving draft before transition...");
                await saveDraft();
            } else {
                // Also perform a best-effort save to ensure latest content persisted (no-op if unchanged)
                await saveDraft().catch(() => { /* ignore save errors handled downstream */ });
            }

            const response = await fetch("/Radiology/Interpretation/Transition", {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ examRequestId, targetStatus }),
                credentials: 'same-origin'
            });

            if (!response.ok) {
                const err = await response.json().catch(() => ({ message: "Workflow transition failed" }));
                alert(err.message || "Workflow transition failed");
                return;
            }

            // Reload current report state after successful transition
            await loadCurrent();
            setStatus(`Transitioned to ${targetStatus}`);
        } catch (err) {
            console.error("Transition failed", err);
            alert(`Transition error: ${err.message}`);
        }
    }

    // Event listeners with null checks
    if (aiAssistBtn) aiAssistBtn.addEventListener("click", () => runAiAssist());
    if (saveBtn) saveBtn.addEventListener("click", () => saveDraft().catch(() => setStatus("Save error")));
    if (finalizeBtn) finalizeBtn.addEventListener("click", () => finalize().catch(() => setStatus("Finalize failed")));
    transitionButtons?.forEach(btn => btn.addEventListener("click", () => transition(btn.dataset.transition)));
    if (viewerPrevBtn) viewerPrevBtn.addEventListener("click", () => renderViewerImage(renderedIndex - 1));
    if (viewerNextBtn) viewerNextBtn.addEventListener("click", () => renderViewerImage(renderedIndex + 1));
    if (viewerPopoutBtn) {
        viewerPopoutBtn.addEventListener("click", () => {
            if (!renderedInstanceUrls.length) return;
            const width = Math.floor(window.screen.width * 0.88);
            const height = Math.floor(window.screen.height * 0.88);
            const left = Math.floor((window.screen.width - width) / 2);
            const top = Math.floor((window.screen.height - height) / 2);
            const pop = window.open("", `diagnostic_view_${examRequestId}`, `popup=yes,resizable=yes,scrollbars=yes,width=${width},height=${height},left=${left},top=${top}`);
            if (!pop) return;
            const safeUrls = JSON.stringify(renderedInstanceUrls);
            pop.document.write(`
<!DOCTYPE html>
<html><head><title>Diagnostic View</title>
<style>body{margin:0;background:#111;color:#eee;font-family:Arial;} .bar{padding:8px;background:#222;display:flex;gap:8px;align-items:center;} .wrap{height:calc(100vh - 44px);display:flex;align-items:center;justify-content:center;} img{max-width:100%;max-height:100%;object-fit:contain;}</style>
</head><body>
<div class="bar"><button id="prev">Prev</button><button id="next">Next</button><span id="status"></span></div>
<div class="wrap"><img id="img" /></div>
<script>
const urls=${safeUrls}; let idx=${renderedIndex};
const img=document.getElementById('img'); const status=document.getElementById('status');
function render(){ img.src=urls[idx]; status.innerText='Image '+(idx+1)+'/'+urls.length; document.getElementById('prev').disabled=idx===0; document.getElementById('next').disabled=idx===urls.length-1; }
document.getElementById('prev').onclick=()=>{ if(idx>0){idx--;render();} };
document.getElementById('next').onclick=()=>{ if(idx<urls.length-1){idx++;render();} };
render();
</script></body></html>`);
            pop.document.close();
        });
    }

    // Autosave with error handling
    setInterval(() => {
        if (dirty) saveDraft().catch(err => setStatus(`Autosave error: ${err.message}`));
    }, 25000);

    // Cleanup blob URLs on page unload to prevent memory leaks
    window.addEventListener('beforeunload', () => {
        if (viewerImage?.src?.startsWith('blob:')) {
            URL.revokeObjectURL(viewerImage.src);
        }
    });

    // Initialize
    loadCurrent().catch(err => setStatus(`Initial load failed: ${err.message}`));
    loadViewer().catch(err => showViewerPlaceholder(`Unable to initialize viewer: ${err.message}`));
})();