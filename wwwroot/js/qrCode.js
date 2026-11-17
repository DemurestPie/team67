const qrCode = require('qrcode');
const fs = require('fs');
const path = require('path');

const tasks = 
[
  {
    "id": 1,
    "label": "Find the Buccaneer Statue",
    "completionCode": "BUC-001"
  },
  {
    "id": 2,
    "label": "Visit the Sherrod Library",
    "completionCode": "BUC-002"
  },
  {
    "id": 3,
    "label": "Locate the Culp Student Center",
    "completionCode": "BUC-003"
  },
  {
    "id": 4,
    "label": "Find the Mini-Dome",
    "completionCode": "BUC-004"
  },
  {
    "id": 5,
    "label": "Visit the Campus Center Building",
    "completionCode": "BUC-005"
  },
  {
    "id": 6,
    "label": "Find a Piece of ETSU Artwork",
    "completionCode": "BUC-006"
  },
  {
    "id": 7,
    "label": "Take a Photo at an ETSU Landmark",
    "completionCode": "BUC-007"
  },
  {
    "id": 8,
    "label": "Locate a Designated Scavenger Hunt Checkpoint",
    "completionCode": "BUC-008"
  }
];


async function generateQRCode() 
{
    const outputDir = path.join(__dirname, '../../wwwroot/qrCodes');

    await Promise.all(tasks.map(async ({completionCode, label}) =>
        {
            const qrCodeFile = `qrcode_${completionCode}.png`;

            await qrCode.toFile(path.join(outputDir, qrCodeFile), completionCode, {width: 300, margin: 2});

        }));
}

generateQRCode();