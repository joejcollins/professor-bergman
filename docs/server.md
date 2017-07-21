---
layout: default
title: Server
---

Working with the API

Endpoint: [POST] http://dinmore-api.azurewebsites.net/api/patrons

Headers: {"Content-Type":"application/octet-stream"}

Body: a binary image file

Optional params:

device: A string which indicates a label for the device that took the picture. Used by the tracking / data analystics to track a patrons route around a museum

returnFaceLandmarks=false|true: Choose whether to return the 27 face landmarks. False if ommited

returnFaceAttributes: Choose to return specific attributes about the face. Accepts a comma-delimited list. Defaults to 'age,gender,headPose,smile,facialHair,glasses,emotion' which is teh full list that the cognitive api supports if omited