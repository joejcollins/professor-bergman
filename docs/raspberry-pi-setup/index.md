---
layout: default
title: Set up Pi
---

1. Download and install the [Windows IoT Dashboard](https://developer.microsoft.com/en-us/windows/iot/docs/iotdashboard).  The Dashboard provides a convenient means of setting up the devices (essentially running the command line tools) and accessing the devices. 

1. Stick a Class 10 Micro SD card into your laptop.

    ![SD Card](./01-sd-card.png)

1. Use the Windows IoT Dashboard to set up new device.  Don't check the "Wi-Fi Network Connection".  It is easiest (apparently most reliable) to configure the device with a direct connnection using an ethernet cable.  If you don't have access to the router on the Wi-Fi network you might not be able to find it's IP address.  Theoretically the Windows IoT Dashboard should show you all the devices attached to the network but this isn't reliable.

    ![SD Card](./02-new-device.png)
    
    The password is unique to the device.  So keep a record.  To administer the device you will need to password.  Accept the terms and conditions, then "Download and install".  If you haven't already done this, or if the OS Build has changed then you'll have to wait for the download. 

1. If you have set up an SD card previously you should get a warning about erasing the SD card.

    ![SD Card](./03-erase-card.png)

1. And the tool to clean the previous install will be run.

    ![SD Card](./04-clean-previous.png)
    
    If it is a fresh SD card you shouldn't see these two.

1. The DISM commandline tool is then used to apply the image to the SD card.

    ![SD Card](./05-apply-image.png)

1. Once the card is setup you should see this.
    ![SD Card](./06-card-complete.png)

1. Put the SD card in the Raspberry Pi, attach it directly to your laptop with the ethernet cable and turn it on.
    ![SD Card](./07-ethernet-to-pi.png)

1. Once the Raspberry Pi has started it should appear in the device list on the Dashboard.
    ![SD Card](./08-device-list.png)
    Sometimes this doesn't happen.  This process relies on ebootpinger.exe running on the Raspberry Pi.  So the recommendation is to restart ebootpinger.exe.  If you are relying on the dashboard to find the Raspberry Pi (if you don't have access to the router) this isn't very useful advice.

5. First boot is the longest whilst Windows sets up, so be patient.
Attach a screen to the Raspberry Pi so you can see what is going on.

6. May not appear on "My Devices" in which case get the Ethernet IP address from the screen.

Now proceed to [on boarding]({{ site.baseurl }}/onboarding) to create the device data entry and associate the physical device with it. This is required before the device can start using the patrons API.
