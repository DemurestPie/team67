let qrScanner = null;
let activeVideoElement = null;

async function startQrScanner(videoElement, onCodeDetected) {
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        throw new Error("Camera access is not supported by this browser.");
    }

    if (typeof Html5Qrcode === "undefined") {
        throw new Error("QR scanner could not be loaded.");
    }

    try {
        activeVideoElement = videoElement;

        // html5-qrcode needs an element to render the camera scanner into.
        let scannerElement = document.getElementById("html5QrScanner");

        if (!scannerElement) {
            scannerElement = document.createElement("div");
            scannerElement.id = "html5QrScanner";

            videoElement.parentNode.insertBefore(
                scannerElement,
                videoElement
            );
        }

        // Hide the old video element because html5-qrcode
        // creates and manages its own camera preview.
        videoElement.style.display = "none";

        qrScanner = new Html5Qrcode("html5QrScanner");

        await qrScanner.start(
            {
                facingMode: "environment"
            },
            {
                fps: 10,
                qrbox: {
                    width: 250,
                    height: 250
                }
            },
            async function (decodedText) {

                // Stop the camera after a QR code is successfully detected.
                await stopQrScanner(videoElement);

                if (onCodeDetected) {
                    onCodeDetected(decodedText);
                }
            },
            function () {
                // QR scan failures are normal while the camera
                // is looking for a code, so nothing is shown here.
            }
        );
    }
    catch (error) {
        console.error("Unable to start QR scanner:", error);

        await stopQrScanner(videoElement);

        throw error;
    }
}

async function stopQrScanner(videoElement) {
    if (qrScanner) {
        try {
            await qrScanner.stop();
        }
        catch (error) {
            console.log("QR scanner was already stopped.");
        }

        try {
            qrScanner.clear();
        }
        catch (error) {
            console.log("QR scanner could not be cleared.");
        }

        qrScanner = null;
    }

    const scannerElement = document.getElementById("html5QrScanner");

    if (scannerElement) {
        scannerElement.innerHTML = "";
    }

    if (videoElement) {
        videoElement.style.display = "";
        videoElement.srcObject = null;
    }

    activeVideoElement = null;
}


// Task 26.2: Connect each Scan QR Code button to the camera scanner.
document.addEventListener("DOMContentLoaded", function () {
    const scanButtons = document.querySelectorAll(".qr-scan-button");
    const scannerContainer = document.getElementById("qrScannerContainer");
    const videoElement = document.getElementById("qrScannerVideo");
    const errorElement = document.getElementById("qrScannerError");
    const cancelButton = document.getElementById("qrScannerCancel");

    // Stop if the scanner interface is not available on this page.
    if (!scannerContainer || !videoElement || !errorElement || !cancelButton) {
        return;
    }

    scanButtons.forEach(button => {
        button.addEventListener("click", async function () {
            const taskId = this.dataset.taskId;

            errorElement.textContent = "";
            errorElement.style.display = "none";

            scannerContainer.style.display = "block";

            try {
                // Stop an existing scanner before starting another one.
                if (qrScanner) {
                    await stopQrScanner(videoElement);
                }

                await startQrScanner(
                    videoElement,
                    function (scannedCode) {

                        // Task 26.3: Use the existing task completion
                        // and validation system.
                        submitScannedCode(taskId, scannedCode);
                    }
                );
            }
            catch (error) {
                errorElement.textContent =
                    error.message || "Unable to start QR scanner.";

                errorElement.style.display = "block";
            }
        });
    });

    // Cancel scanning without changing player progress.
    cancelButton.addEventListener("click", async function () {
        await stopQrScanner(videoElement);

        scannerContainer.style.display = "none";

        errorElement.textContent = "";
        errorElement.style.display = "none";
    });
});