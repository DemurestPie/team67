let qrVideoStream = null;
let qrScanTimer = null;

async function startQrScanner(videoElement, onCodeDetected) {
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        throw new Error("Camera access is not supported by this browser.");
    }

    if (!("BarcodeDetector" in window)) {
        throw new Error("QR scanning is not supported by this browser.");
    }

    try {
        qrVideoStream = await navigator.mediaDevices.getUserMedia({
            video: {
                facingMode: "environment"
            },
            audio: false
        });

        videoElement.srcObject = qrVideoStream;
        await videoElement.play();

        const detector = new BarcodeDetector({
            formats: ["qr_code"]
        });

        async function scanFrame() {
            if (!qrVideoStream) {
                return;
            }

            try {
                const barcodes = await detector.detect(videoElement);

                if (barcodes.length > 0) {
                    const scannedCode = barcodes[0].rawValue;

                    stopQrScanner(videoElement);

                    if (onCodeDetected) {
                        onCodeDetected(scannedCode);
                    }

                    return;
                }
            }
            catch (error) {
                console.error("QR scan failed:", error);
            }

            qrScanTimer = setTimeout(scanFrame, 250);
        }

        scanFrame();
    }
    catch (error) {
        console.error("Unable to start QR scanner:", error);
        throw error;
    }
}

function stopQrScanner(videoElement) {
    if (qrScanTimer) {
        clearTimeout(qrScanTimer);
        qrScanTimer = null;
    }

    if (qrVideoStream) {
        qrVideoStream.getTracks().forEach(track => {
            track.stop();
        });

        qrVideoStream = null;
    }

    if (videoElement) {
        videoElement.srcObject = null;
    }
}