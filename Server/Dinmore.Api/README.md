## How do I get set up?

* Summary of set up

UWP running on a Raspberry Pi, using API fascade on Face API.

* Configuration

The API keys are stored away from the source code. 

```
\Users\USERNAME\AppData\Roaming\Microsoft\UserSecrets\e6a4d621-9576-4a11-aa11-21f768f831c4\secrets.json
```

In thit format.

```
{
  "AppSettings:FaceApiKey": "XXXXXXXX",
  "AppSettings:EmotionApiKey": "XXXXXXXX",
  "AppSettings:TableStorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=XXXXXXXX;AccountKey=XXXXXXXXX;EndpointSuffix=core.windows.net"
}
```

Alternatively use the commandline interface like this

```
dotnet user-secrets list
```