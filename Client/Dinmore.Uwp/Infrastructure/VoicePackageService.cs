using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Dinmore.Uwp.Infrastructure.Media;
using Windows.Storage;
using Windows.Data.Json;
using Dinmore.Uwp.Constants;
using Dinmore.Uwp.Helpers;

namespace Dinmore.Uwp.Infrastructure
{
    public class VoicePackageService
    {
        public async static Task<string> DownloadUnpackVoicePackage(string voicePackageUrl)
        {
            try
            {
                // Create voices storage folder
                StorageFolder storageFolder = (StorageFolder)await ApplicationData.Current.LocalFolder.TryGetItemAsync(AppConsts.VoiceAssetsFolderPath);
                if (storageFolder == null)
                {
                    storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(AppConsts.VoiceAssetsFolderPath);
                }

                // Get file name
                var packageFileName = ExtractFileNameFromUrl(voicePackageUrl);

                // Download and store voice package zip
                StorageFile sf = await storageFolder.CreateFileAsync($"{packageFileName}.zip", CreationCollisionOption.ReplaceExisting);
                var downloadFolder = (await sf.GetParentAsync()).ToString();
                HttpClient client = new HttpClient();
                byte[] buffer = await client.GetByteArrayAsync(voicePackageUrl);
                using (Stream stream = await sf.OpenStreamForWriteAsync())
                {
                    stream.Write(buffer, 0, buffer.Length);
                }

                // Unpack voice files
                var outputFolderLocation = $"{AppConsts.VoiceAssetsFolderPath}{packageFileName}\\";
                var outputputFolder = await ApplicationData.Current.LocalFolder.TryGetItemAsync(outputFolderLocation);
                if (outputputFolder != null)
                {
                    await outputputFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
                System.IO.Compression.ZipFile.ExtractToDirectory($"{storageFolder.Path}\\{packageFileName}.zip", $"{storageFolder.Path}\\{packageFileName}");


                return packageFileName;
            }
            catch (Exception ex)
            {
                //Log here when it is a glbal function
                return null;
            }
        }

        internal async static Task<IVoicePlayer> VoicePlayerFactory(string voicePackageUrl)
        {
            // Get file name
            var packageFileName = ExtractFileNameFromUrl(voicePackageUrl);

            IStorageItem file = await ApplicationData.Current.LocalFolder.TryGetItemAsync($"{AppConsts.VoiceAssetsFolderPath}{packageFileName}\\voice.json");
            if (file == null)
            {
                var vp = new VoicePlayerGenerated();
                vp.Say("Error: We could not load the Voice Package");
                return vp;
            }
            StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync($"{AppConsts.VoiceAssetsFolderPath}{packageFileName}\\");
 
            var jsonstring = await FileIO.ReadTextAsync(((StorageFile)file));
            JsonObject ja = JsonValue.Parse(jsonstring).GetObject();
            
            var voicetype = ja.GetNamedString("Type", "");
            if (voicetype == "wav")
            {
                return new VoicePlayer(ja, folder);
            }
            return new VoicePlayerGenerated();
        }

        static string ExtractFileNameFromUrl(string url)
        {
            return url.Substring(url.LastIndexOf('/') + 1).Replace(".zip", "").Trim();
        }

        internal static async Task<IVoicePlayer> VoicePlayerFactory()
        {
            if (Settings.GetString(DeviceSettingKeys.VoicePackageUrlKey) != null)
            {
                return await VoicePlayerFactory(Settings.GetString(DeviceSettingKeys.VoicePackageUrlKey));
            }
            else
            {
                return new VoicePlayerGenerated();
            }
        }
    }
}
