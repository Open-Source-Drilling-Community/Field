const installations = new Map();

export function install(chartId, dotNetReference) {
    uninstall(chartId);

    const plot = document.getElementById(chartId);
    if (!plot || !window.Plotly) {
        window.setTimeout(() => install(chartId), 50);
        return;
    }

    const state = {
        camera: cloneCamera(getCamera(plot)),
        suppressCaptureUntil: 0
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
    const clickHandler = (eventData) => notifyTraceClick(eventData, dotNetReference);

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
    plot.addEventListener("wheel", captureHandler, true);
    plot.addEventListener("pointerup", captureHandler, true);
    plot.addEventListener("mouseup", captureHandler, true);
    plot.addEventListener("touchend", captureHandler, true);
    plot.addEventListener("mouseleave", captureHandler, true);
    document.addEventListener("pointerdown", modebarHandler, true);
    document.addEventListener("mousedown", modebarHandler, true);
    document.addEventListener("click", modebarHandler, true);

    installations.set(chartId, { plot, relayoutHandler, captureHandler, clickHandler, modebarHandler });
}

export function uninstall(chartId) {
    const installation = installations.get(chartId);
    if (!installation) {
        return;
    }

    installation.plot.removeListener?.("plotly_relayout", installation.relayoutHandler);
    installation.plot.removeListener?.("plotly_relayouting", installation.relayoutHandler);
    installation.plot.removeListener?.("plotly_click", installation.clickHandler);
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
    }
}

function captureCameraFromEvent(eventData, state) {
    if (Date.now() < state.suppressCaptureUntil) {
        return;
    }

    const camera = getCameraFromEvent(eventData, state.camera);
    if (camera) {
        state.camera = camera;
    }
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

function notifyTraceClick(eventData, dotNetReference) {
    if (!dotNetReference || !eventData?.points?.length) {
        return;
    }

    const traceIndex = eventData.points[0]?.curveNumber;
    if (Number.isInteger(traceIndex)) {
        dotNetReference.invokeMethodAsync("OnPlotlyTraceClicked", traceIndex);
    }
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
