---
layout: default
title: Onboarding a new device
---

## Create the device data entry in the device store
1. Go to the 'web app' at http://blackradley-dinmore-webapp-develop.azurewebsites.net (the 'develop' bit reflects the 'develop' branch, this may change if working against master or another branch)
1. Go to the 'Devices' page
1. Click 'Create New'
1. Add all the relevant details for your new device and click 'Create'. Do not specify an `ID`, this will be generated for you

## Associating the Device data entry with the physical device
It is recommended that you do these steps using a mobile device that can easily be waved in front of the camera on the Raspberry Pi.
1. Go to the 'web app' at http://blackradley-dinmore-webapp-develop.azurewebsites.net (the 'develop' bit reflect the 'develop' branch, this may change if working against master or another branch)
1. Go to the 'Devices' page
1. Identify the device data entry you created in the previous step and click 'Details'. This will reveal a QR code that contain the device's unique ID (guid)
1. Switch on the Raspberry Pi and wait until it says "I have no device ID. I'm now onboarding which means I am looking for a QR code ..."
1. Show Raspberry Pi's webcam the QR code until it says "I found a QR code, thanks."
1. The device has now onboarded and is associated with the device data entry