---
layout: default
title: Installing the Intelligent Exhibit
---

1. Create a package in Visual Studio.

    ![Create Appx](./01-create-package.png)
    
    This will place the packages in \Client\Dinmore.Uwp\AppPackages.

1. Open the device portal from the Windows IoT dashboard.
 
    ![Deploy Package](./02-open-from-dashboard.png)

1. Open the Apps menu in the device portal and select "Add".

    ![Deploy Package](./03-package-deploy.png)
    
1. Then select the bundle you have just created in \Client\Dinmore.Uwp\AppPackages.  The bundle depends on some other frameworks so at least for the first install you will have to upload the dependencies as well so select "I want to specify framework packages".

    ![Select Package](./04-select-appx-bundle.png)   
    
1. The relevant packages for the target platform are in a subdirectory below the package, so select them all.

    ![Select Dependencies](./05-select-dependencies.png)
    
1. Once the package is intalled it should be listed in the Apps, but not set as the startup application.  It might be tempting to delete IoTCoreDefaultApp as a housekeeping measure.  The application contains a browser and various other useful bits and pieces.  When connecting to a public network which requires browser authentication these will come in handy so I suggest you leave it.

    ![Package Installed](./06-package-installed.png)   
    
1. Set the Black Radley Intelligent Exhibit as the startup application.

    ![SD Card](./07-set-as-startup.png)
    
1. Then reboot the device.

    ![Restart](./08-restart-device.png)

    If the reboot doesn't work, turn it off and on again instead.

Now proceed to [on boarding]({{ site.baseurl }}/onboarding) to create the device data entry and associate the physical device with it. This is required before the device can start using the patrons API.
