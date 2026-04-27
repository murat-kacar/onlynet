type BarcodeDetectorCtor = new (options?: { formats?: string[] }) => {
    detect(source: ImageBitmapSource): Promise<Array<{ rawValue?: string }>>;
};

type WindowWithBarcodeDetector = Window & {
    BarcodeDetector?: BarcodeDetectorCtor;
};

let activeStream: MediaStream | null = null;
let activeVideo: HTMLVideoElement | null = null;
let activeDetector: InstanceType<BarcodeDetectorCtor> | null = null;
let frameHandle: number | null = null;

function getBarcodeDetectorCtor(): BarcodeDetectorCtor | null {
    return (window as WindowWithBarcodeDetector).BarcodeDetector ?? null;
}

export function isQrScannerSupported(): boolean {
    return typeof navigator !== "undefined"
        && typeof navigator.mediaDevices?.getUserMedia === "function"
        && getBarcodeDetectorCtor() !== null;
}

export async function startQrScanner(video: HTMLVideoElement, dotNetHelper: { invokeMethodAsync(method: string, token: string): Promise<void> }): Promise<boolean> {
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

    const tick = async (): Promise<void> => {
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
        } catch {
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

export async function stopQrScanner(): Promise<void> {
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
