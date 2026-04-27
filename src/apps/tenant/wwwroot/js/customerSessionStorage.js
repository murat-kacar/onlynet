const sessionIdKey = "sessionId";
const ticketIdKey = "ticketId";
const tableIdKey = "tableId";
const tableLabelKey = "tableLabel";
export function getCustomerSessionSnapshot() {
    return {
        sessionId: window.localStorage.getItem(sessionIdKey),
        ticketId: window.localStorage.getItem(ticketIdKey),
        tableId: window.localStorage.getItem(tableIdKey),
        tableLabel: window.localStorage.getItem(tableLabelKey),
    };
}
export function setCustomerSessionSnapshot(payload) {
    window.localStorage.setItem(sessionIdKey, payload.sessionId);
    window.localStorage.setItem(ticketIdKey, payload.ticketId);
    window.localStorage.setItem(tableIdKey, payload.tableId);
    window.localStorage.setItem(tableLabelKey, payload.tableLabel);
}
export function clearCustomerSessionSnapshot() {
    window.localStorage.removeItem(sessionIdKey);
    window.localStorage.removeItem(ticketIdKey);
    window.localStorage.removeItem(tableIdKey);
    window.localStorage.removeItem(tableLabelKey);
}
