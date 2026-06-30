const installations = new Map();
const trajectoryTraceKind = "trajectory";
const otherTraceKind = "other";

export function install(chartId, dotNetReference, legendLinkedTraceIds = [], legendControlsLinkedTraces = [], legendTraceKinds = [], selectableTraceIds = [], selectedTraceIds = []) {
    uninstall(chartId);

    const plot = document.getElementById(chartId);
    if (!plot || !window.Plotly) {
        window.setTimeout(() => install(chartId, dotNetReference, legendLinkedTraceIds, legendControlsLinkedTraces, legendTraceKinds, selectableTraceIds, selectedTraceIds), 50);
        return;
    }

    const state = {
        camera: cloneCamera(getCamera(plot)),
        suppressCaptureUntil: 0,
        legendLinkedTraceIds: normalizeLegendTraceIds(legendLinkedTraceIds),
        legendControlsLinkedTraces: normalizeLegendControls(legendControlsLinkedTraces),
        legendTraceKinds: normalizeLegendTraceKinds(legendTraceKinds),
        selectableTraceIds: normalizeLegendTraceIds(selectableTraceIds),
        selectedTraceIds: new Set(normalizeLegendTraceIds(selectedTraceIds).filter(Boolean)),
        dotNetReference,
        lastNotifiedTraceId: null,
        lastNotifiedAt: 0,
        lastCameraIsAboveScene: null,
        legendVisibilityByKey: new Map()
    };

    const relayoutHandler = (eventData) => {
        if (isDragModeEvent(eventData) && state.camera) {
            restoreCameraRepeated(plot, state, state.camera);
            return;
        }

        if (isCameraEvent(eventData)) {
            captureCameraFromEvent(eventData, state);
            scheduleCameraCapture(plot, state);
        }
    };

    const captureHandler = () => scheduleCameraCapture(plot, state);
    const clickHandler = (eventData) => notifyTraceClick(eventData, dotNetReference, state);
    const legendClickHandler = (eventData) => handleLegendClick(eventData, plot, state);

    const modebarHandler = (event) => {
        const button = findModebarButton(event);
        const dragMode = get3dDragMode(button);
        if (!button || !dragMode || !belongsToPlot(button, plot)) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();
        event.stopImmediatePropagation?.();

        captureCamera(plot, state, true);
        if (state.camera) {
            setDragModeAndCamera(plot, state, dragMode, state.camera);
        }
        else {
            setDragMode(plot, dragMode);
        }
    };

    plot.on?.("plotly_relayout", relayoutHandler);
    plot.on?.("plotly_relayouting", relayoutHandler);
    plot.on?.("plotly_click", clickHandler);
    plot.on?.("plotly_legendclick", legendClickHandler);
    plot.addEventListener("wheel", captureHandler, true);
    plot.addEventListener("pointerup", captureHandler, true);
    plot.addEventListener("mouseup", captureHandler, true);
    plot.addEventListener("touchend", captureHandler, true);
    plot.addEventListener("mouseleave", captureHandler, true);
    document.addEventListener("pointerdown", modebarHandler, true);
    document.addEventListener("mousedown", modebarHandler, true);
    document.addEventListener("click", modebarHandler, true);

    installations.set(chartId, { plot, state, relayoutHandler, captureHandler, clickHandler, legendClickHandler, modebarHandler });
    notifyCameraPlane(state.camera, state);
}

export function setLegendLinks(chartId, legendLinkedTraceIds = [], legendControlsLinkedTraces = [], legendTraceKinds = [], selectableTraceIds = [], selectedTraceIds = []) {
    const installation = installations.get(chartId);
    if (!installation) {
        return;
    }

    installation.state.legendLinkedTraceIds = normalizeLegendTraceIds(legendLinkedTraceIds);
    installation.state.legendControlsLinkedTraces = normalizeLegendControls(legendControlsLinkedTraces);
    installation.state.legendTraceKinds = normalizeLegendTraceKinds(legendTraceKinds);
    installation.state.selectableTraceIds = normalizeLegendTraceIds(selectableTraceIds);
    installation.state.selectedTraceIds = new Set(normalizeLegendTraceIds(selectedTraceIds).filter(Boolean));
    scheduleStoredLegendVisibilityRestore(installation.plot, installation.state);
}

export function uninstall(chartId) {
    const installation = installations.get(chartId);
    if (!installation) {
        return;
    }

    installation.plot.removeListener?.("plotly_relayout", installation.relayoutHandler);
    installation.plot.removeListener?.("plotly_relayouting", installation.relayoutHandler);
    installation.plot.removeListener?.("plotly_click", installation.clickHandler);
    installation.plot.removeListener?.("plotly_legendclick", installation.legendClickHandler);
    installation.plot.removeEventListener("wheel", installation.captureHandler, true);
    installation.plot.removeEventListener("pointerup", installation.captureHandler, true);
    installation.plot.removeEventListener("mouseup", installation.captureHandler, true);
    installation.plot.removeEventListener("touchend", installation.captureHandler, true);
    installation.plot.removeEventListener("mouseleave", installation.captureHandler, true);
    document.removeEventListener("pointerdown", installation.modebarHandler, true);
    document.removeEventListener("mousedown", installation.modebarHandler, true);
    document.removeEventListener("click", installation.modebarHandler, true);
    installations.delete(chartId);
}

function getCamera(plot) {
    return getSceneRuntimeCamera(plot) ??
        plot?._fullLayout?.scene?.camera ??
        plot?.layout?.scene?.camera ??
        null;
}

function getSceneRuntimeCamera(plot) {
    const scene = plot?._fullLayout?.scene?._scene;
    if (!scene) {
        return null;
    }

    if (typeof scene.getCamera === "function") {
        return scene.getCamera();
    }

    return scene.camera ?? scene.view?.camera ?? null;
}

function captureCamera(plot, state, force = false) {
    if (!force && Date.now() < state.suppressCaptureUntil) {
        return;
    }

    const camera = cloneCamera(getCamera(plot));
    if (camera) {
        state.camera = camera;
        notifyCameraPlane(camera, state);
    }
}

function captureCameraFromEvent(eventData, state) {
    const camera = getCameraFromEvent(eventData, state.camera);
    if (camera) {
        state.camera = camera;
        notifyCameraPlane(camera, state);
    }
}

function notifyCameraPlane(camera, state) {
    const eyeZ = Number(camera?.eye?.z);
    if (!Number.isFinite(eyeZ)) {
        return;
    }

    const cameraIsAboveScene = eyeZ >= 0;
    if (state.lastCameraIsAboveScene === cameraIsAboveScene) {
        return;
    }

    state.lastCameraIsAboveScene = cameraIsAboveScene;
    state.dotNetReference?.invokeMethodAsync("OnPlotlyCameraPlaneChanged", cameraIsAboveScene);
}

function getCameraFromEvent(eventData, currentCamera) {
    if (!eventData) {
        return null;
    }

    if (eventData["scene.camera"]) {
        return cloneCamera(eventData["scene.camera"]);
    }

    if (eventData.scene?.camera) {
        return cloneCamera(eventData.scene.camera);
    }

    const camera = cloneCamera(currentCamera) ?? {};
    let found = false;

    for (const part of ["eye", "center", "up"]) {
        camera[part] ??= {};
        for (const axis of ["x", "y", "z"]) {
            const key = `scene.camera.${part}.${axis}`;
            if (Object.prototype.hasOwnProperty.call(eventData, key)) {
                camera[part][axis] = eventData[key];
                found = true;
            }
        }
    }

    return found ? camera : null;
}

function scheduleCameraCapture(plot, state) {
    window.requestAnimationFrame(() => captureCamera(plot, state));
    window.setTimeout(() => captureCamera(plot, state), 0);
    window.setTimeout(() => captureCamera(plot, state), 50);
    window.setTimeout(() => captureCamera(plot, state), 150);
}

function notifyTraceClick(eventData, dotNetReference, state) {
    if (!dotNetReference || !eventData?.points?.length) {
        return;
    }

    const firstTraceIndex = eventData.points[0]?.curveNumber;
    const candidates = eventData.points
        .map((point) => {
            const traceIndex = point?.curveNumber;
            if (!Number.isInteger(traceIndex)) {
                return null;
            }

            const traceMeta = point?.data?.meta;
            return typeof traceMeta === "string" && traceMeta
                ? { traceId: traceMeta, traceIndex }
                : { traceId: state.selectableTraceIds[traceIndex], traceIndex };
        })
        .filter((candidate) => candidate?.traceId);

    const candidate = candidates.find(({ traceId }) => !state.selectedTraceIds.has(traceId)) ?? candidates[0];
    const traceId = candidate?.traceId;
    if (traceId) {
        const now = Date.now();
        if (state.lastNotifiedTraceId === traceId && now - state.lastNotifiedAt < 500) {
            return;
        }

        state.lastNotifiedTraceId = traceId;
        state.lastNotifiedAt = now;
        dotNetReference.invokeMethodAsync("OnPlotlyTraceIdClicked", traceId);
    }
    else if (Number.isInteger(firstTraceIndex)) {
        dotNetReference.invokeMethodAsync("OnPlotlyTraceClicked", firstTraceIndex);
    }
}

function handleLegendClick(eventData, plot, state) {
    const traceIndex = eventData?.curveNumber;
    if (!Number.isInteger(traceIndex)) {
        return true;
    }

    const traceId = state.legendLinkedTraceIds[traceIndex];
    const traceKind = state.legendTraceKinds[traceIndex];
    if (!traceId || !traceKind || state.legendControlsLinkedTraces[traceIndex] !== true) {
        return true;
    }

    const linkedTraceIndexes = getLinkedTraceIndexes(plot, state, traceId, traceKind);
    if (!linkedTraceIndexes.length || !window.Plotly) {
        return true;
    }

    const nextVisibility = isTraceVisible(plot.data[traceIndex]) ? "legendonly" : true;
    captureCamera(plot, state, true);
    const camera = cloneCamera(state.camera) ?? cloneCamera(getCamera(plot));
    rememberLegendVisibility(state, linkedTraceIndexes, nextVisibility);
    window.Plotly.restyle(plot, { visible: nextVisibility }, linkedTraceIndexes);
    if (camera) {
        restoreCameraRepeated(plot, state, camera);
    }

    return false;
}

function getLinkedTraceIndexes(plot, state, traceId, traceKind) {
    const linkedTraceIndexes = [];
    const traceCount = Math.min(
        plot?.data?.length ?? 0,
        state.legendLinkedTraceIds.length,
        state.legendTraceKinds.length);

    for (let i = 0; i < traceCount; i++) {
        if (state.legendLinkedTraceIds[i] !== traceId) {
            continue;
        }

        const candidateKind = state.legendTraceKinds[i];
        if (traceKind === trajectoryTraceKind) {
            if (candidateKind !== otherTraceKind) {
                linkedTraceIndexes.push(i);
            }
        }
        else if (candidateKind === traceKind) {
            linkedTraceIndexes.push(i);
        }
    }

    return linkedTraceIndexes;
}

function rememberLegendVisibility(state, traceIndexes, visibility) {
    for (const traceIndex of traceIndexes) {
        const visibilityKey = getLegendVisibilityKey(state, traceIndex);
        if (visibilityKey) {
            state.legendVisibilityByKey.set(visibilityKey, visibility);
        }
    }
}

function scheduleStoredLegendVisibilityRestore(plot, state) {
    for (const delay of [0, 50, 150]) {
        window.setTimeout(() => restoreStoredLegendVisibility(plot, state), delay);
    }
}

function restoreStoredLegendVisibility(plot, state) {
    if (!plot || !window.Plotly || !state.legendVisibilityByKey?.size) {
        return;
    }

    const traceIndexes = [];
    const visibilityValues = [];
    const traceCount = Math.min(
        plot.data?.length ?? 0,
        state.legendLinkedTraceIds.length,
        state.legendTraceKinds.length);

    for (let i = 0; i < traceCount; i++) {
        const visibility = getStoredLegendVisibility(state, i);
        if (visibility !== null) {
            traceIndexes.push(i);
            visibilityValues.push(visibility);
        }
    }

    if (!traceIndexes.length) {
        return;
    }

    const camera = cloneCamera(state.camera) ?? cloneCamera(getCamera(plot));
    window.Plotly.restyle(plot, { visible: visibilityValues }, traceIndexes);
    if (camera) {
        restoreCameraRepeated(plot, state, camera);
    }
}

function getStoredLegendVisibility(state, traceIndex) {
    const visibilityKey = getLegendVisibilityKey(state, traceIndex);
    if (!visibilityKey) {
        return null;
    }

    if (state.legendVisibilityByKey.has(visibilityKey)) {
        return state.legendVisibilityByKey.get(visibilityKey);
    }

    const traceId = state.legendLinkedTraceIds[traceIndex];
    const traceKind = state.legendTraceKinds[traceIndex];
    if (!traceId || traceKind === trajectoryTraceKind || traceKind === otherTraceKind) {
        return null;
    }

    const trajectoryVisibility = state.legendVisibilityByKey.get(`${traceId}|${trajectoryTraceKind}`);
    return trajectoryVisibility === "legendonly" ? "legendonly" : null;
}

function getLegendVisibilityKey(state, traceIndex) {
    const traceId = state.legendLinkedTraceIds[traceIndex];
    const traceKind = state.legendTraceKinds[traceIndex];
    return traceId && traceKind && traceKind !== otherTraceKind
        ? `${traceId}|${traceKind}`
        : null;
}

function isTraceVisible(trace) {
    return trace?.visible !== "legendonly" && trace?.visible !== false;
}

function normalizeLegendTraceIds(values) {
    return Array.isArray(values)
        ? values.map((value) => value == null ? null : `${value}`)
        : [];
}

function normalizeLegendControls(values) {
    return Array.isArray(values)
        ? values.map((value) => value === true)
        : [];
}

function normalizeLegendTraceKinds(values) {
    return Array.isArray(values)
        ? values.map((value) => value == null ? otherTraceKind : `${value}`)
        : [];
}

function isCameraEvent(eventData) {
    if (!eventData) {
        return false;
    }

    if (eventData["scene.camera"] || eventData.scene?.camera) {
        return true;
    }

    for (const part of ["eye", "center", "up"]) {
        for (const axis of ["x", "y", "z"]) {
            if (Object.prototype.hasOwnProperty.call(eventData, `scene.camera.${part}.${axis}`)) {
                return true;
            }
        }
    }

    return false;
}

function isDragModeEvent(eventData) {
    return Object.prototype.hasOwnProperty.call(eventData ?? {}, "scene.dragmode") ||
        Object.prototype.hasOwnProperty.call(eventData ?? {}, "dragmode");
}

function findModebarButton(event) {
    const path = typeof event.composedPath === "function" ? event.composedPath() : [];
    for (const element of path) {
        if (element?.classList?.contains("modebar-btn")) {
            return element;
        }
    }

    return event.target?.closest?.(".modebar-btn") ?? null;
}

function belongsToPlot(button, plot) {
    return Boolean(button && plot && (plot.contains(button) || button.closest?.(".js-plotly-plot") === plot));
}

function get3dDragMode(button) {
    if (!button) {
        return null;
    }

    const text = `${button.getAttribute("data-title") ?? ""} ${button.getAttribute("aria-label") ?? ""} ${button.title ?? ""}`.toLowerCase();
    if (text.includes("turntable")) {
        return "turntable";
    }

    if (text.includes("orbit")) {
        return "orbit";
    }

    if (text.includes("pan")) {
        return "pan";
    }

    if (text.includes("zoom")) {
        return "zoom";
    }

    return null;
}

function restoreCameraRepeated(plot, state, camera) {
    const cameraSnapshot = cloneCamera(camera);
    if (!cameraSnapshot) {
        return;
    }

    state.camera = cameraSnapshot;
    state.suppressCaptureUntil = Date.now() + 500;

    for (const delay of [0, 50, 150, 300]) {
        window.setTimeout(() => restoreCamera(plot, state.camera), delay);
    }
}

function restoreCamera(plot, camera) {
    if (!plot || !camera || !window.Plotly) {
        return;
    }

    window.Plotly.relayout(plot, { "scene.camera": cloneCamera(camera) });
}

function setDragModeAndCamera(plot, state, dragMode, camera) {
    const cameraSnapshot = cloneCamera(camera);
    if (!plot || !window.Plotly || !cameraSnapshot) {
        return;
    }

    state.camera = cameraSnapshot;
    state.suppressCaptureUntil = Date.now() + 500;
    window.Plotly.relayout(plot, {
        "scene.dragmode": dragMode,
        "scene.camera": cameraSnapshot
    });

    for (const delay of [0, 50, 150, 300]) {
        window.setTimeout(() => restoreCamera(plot, state.camera), delay);
    }
}

function setDragMode(plot, dragMode) {
    if (!plot || !window.Plotly) {
        return;
    }

    window.Plotly.relayout(plot, { "scene.dragmode": dragMode });
}

function cloneCamera(camera) {
    if (!camera) {
        return null;
    }

    return JSON.parse(JSON.stringify(camera));
}
