let activeStream = null;
let activeVideo = null;
let activeDetector = null;
let frameHandle = null;
function getBarcodeDetectorCtor() {
    return window.BarcodeDetector ?? null;
}
export function isQrScannerSupported() {
    return typeof navigator !== "undefined"
        && typeof navigator.mediaDevices?.getUserMedia === "function"
        && getBarcodeDetectorCtor() !== null;
}
export async function startQrScanner(video, dotNetHelper) {
    await stopQrScanner();
    const BarcodeDetector = getBarcodeDetectorCtor();
    if (!BarcodeDetector) {
        return false;
    }
    activeStream = await navigator.mediaDevices.getUserMedia({
        video: {
            facingMode: { ideal: "environment" }
        },
        audio: false
    });
    activeVideo = video;
    activeDetector = new BarcodeDetector({ formats: ["qr_code"] });
    activeVideo.srcObject = activeStream;
    activeVideo.setAttribute("playsinline", "true");
    activeVideo.muted = true;
    await activeVideo.play();
    const tick = async () => {
        if (!activeVideo || !activeDetector) {
            return;
        }
        try {
            const barcodes = await activeDetector.detect(activeVideo);
            const qrCode = barcodes.find(code => typeof code.rawValue === "string" && code.rawValue.length > 0);
            if (qrCode?.rawValue) {
                await dotNetHelper.invokeMethodAsync("HandleDetectedQrToken", qrCode.rawValue);
                await stopQrScanner();
                return;
            }
        }
        catch {
            // Keep scanning; camera frames can briefly fail while warming up.
        }
        frameHandle = window.requestAnimationFrame(() => {
            void tick();
        });
    };
    frameHandle = window.requestAnimationFrame(() => {
        void tick();
    });
    return true;
}
export async function stopQrScanner() {
    if (frameHandle !== null) {
        window.cancelAnimationFrame(frameHandle);
        frameHandle = null;
    }
    if (activeVideo) {
        activeVideo.pause();
        activeVideo.srcObject = null;
        activeVideo = null;
    }
    if (activeStream) {
        for (const track of activeStream.getTracks()) {
            track.stop();
        }
        activeStream = null;
    }
    activeDetector = null;
}
