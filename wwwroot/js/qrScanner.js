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

// Task 26.2: Connect each Scan QR Code button to the camera scanner.
document.addEventListener("DOMContentLoaded", function () {
    const scanButtons = document.querySelectorAll(".qr-scan-button");
    const scannerContainer = document.getElementById("qrScannerContainer");
    const videoElement = document.getElementById("qrScannerVideo");
    const errorElement = document.getElementById("qrScannerError");
    const cancelButton = document.getElementById("qrScannerCancel");

    // Start the scanner for the task whose Scan QR Code button was clicked.
    scanButtons.forEach(button => {
        button.addEventListener("click", async function () {
            const taskId = this.dataset.taskId;

            // Clear any previous scanner error.
            errorElement.textContent = "";
            errorElement.style.display = "none";

            // Show the camera area.
            scannerContainer.style.display = "block";

            try {
                await startQrScanner(videoElement, function (scannedCode) {

                    // Task 26.3: Pass the scanned QR value to the existing
                    // task completion and validation system.
                    submitScannedCode(taskId, scannedCode);
                });
            }
            catch (error) {
                // Show a useful message if camera access or QR scanning
                // is unavailable in the player's browser.
                errorElement.textContent = error.message;
                errorElement.style.display = "block";
            }
        });
    });

    // Allow the player to cancel scanning without changing task progress.
    cancelButton.addEventListener("click", function () {
        stopQrScanner(videoElement);
        scannerContainer.style.display = "none";

        errorElement.textContent = "";
        errorElement.style.display = "none";
    });
});