type CustomerSessionSnapshot = {
    sessionId: string | null;
    ticketId: string | null;
    tableLabel: string | null;
};

type CustomerSessionPayload = {
    sessionId: string;
    ticketId: string;
    tableLabel: string;
};

const sessionIdKey = "sessionId";
const ticketIdKey = "ticketId";
const tableLabelKey = "tableLabel";

export function getCustomerSessionSnapshot(): CustomerSessionSnapshot {
    return {
        sessionId: window.localStorage.getItem(sessionIdKey),
        ticketId: window.localStorage.getItem(ticketIdKey),
        tableLabel: window.localStorage.getItem(tableLabelKey),
    };
}

export function setCustomerSessionSnapshot(payload: CustomerSessionPayload): void {
    window.localStorage.setItem(sessionIdKey, payload.sessionId);
    window.localStorage.setItem(ticketIdKey, payload.ticketId);
    window.localStorage.setItem(tableLabelKey, payload.tableLabel);
}

export function clearCustomerSessionSnapshot(): void {
    window.localStorage.removeItem(sessionIdKey);
    window.localStorage.removeItem(ticketIdKey);
    window.localStorage.removeItem(tableLabelKey);
}
