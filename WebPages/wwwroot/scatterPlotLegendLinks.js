const installations = new Map();
const trajectoryTraceKind = "trajectory";
const otherTraceKind = "other";

export function install(chartId) {
    uninstall(chartId);

    const plot = document.getElementById(chartId);
    if (!plot || !window.Plotly) {
        window.setTimeout(() => install(chartId), 50);
        return;
    }

    const state = {
        legendLinkedTraceIds: [],
        legendControlsLinkedTraces: [],
        legendTraceKinds: [],
        legendVisibilityByKey: new Map()
    };

    const legendClickHandler = (eventData) => handleLegendClick(eventData, plot, state);
    plot.on?.("plotly_legendclick", legendClickHandler);
    installations.set(chartId, { plot, state, legendClickHandler });
}

export function setLegendLinks(chartId, legendLinkedTraceIds = [], legendControlsLinkedTraces = [], legendTraceKinds = []) {
    const installation = installations.get(chartId);
    if (!installation) {
        return;
    }

    installation.state.legendLinkedTraceIds = normalizeLegendTraceIds(legendLinkedTraceIds);
    installation.state.legendControlsLinkedTraces = normalizeLegendControls(legendControlsLinkedTraces);
    installation.state.legendTraceKinds = normalizeLegendTraceKinds(legendTraceKinds);
    scheduleStoredLegendVisibilityRestore(installation.plot, installation.state);
}

export function uninstall(chartId) {
    const installation = installations.get(chartId);
    if (!installation) {
        return;
    }

    installation.plot.removeListener?.("plotly_legendclick", installation.legendClickHandler);
    installations.delete(chartId);
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
    rememberLegendVisibility(state, linkedTraceIndexes, nextVisibility);
    window.Plotly.restyle(plot, { visible: nextVisibility }, linkedTraceIndexes);
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

    if (traceIndexes.length) {
        window.Plotly.restyle(plot, { visible: visibilityValues }, traceIndexes);
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
        ? values.map((value) => value == null ? "" : `${value}`)
        : [];
}
