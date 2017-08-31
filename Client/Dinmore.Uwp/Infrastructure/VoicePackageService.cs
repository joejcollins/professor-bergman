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

namespace Dinmore.Uwp.Infrastructure
{
    public class VoicePackageService
    {
        public async static Task<string> UnpackVoice(string voiceGUID) {
            // Will be null if folder dosen't exist
            var storageFolder = await ApplicationData.Current.LocalFolder.TryGetItemAsync("Assets\\Voice\\");
            if (storageFolder == null) {
                storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Assets\\Voice\\");
            }
                        
            var outputFolderLocation = $"Assets\\Voice\\{voiceGUID}\\";
            var outputputFolder = await ApplicationData.Current.LocalFolder.TryGetItemAsync(outputFolderLocation);
            if (outputputFolder != null)
            {
                await outputputFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            System.IO.Compression.ZipFile.ExtractToDirectory($"{storageFolder.Path}\\{voiceGUID}.zip", $"{storageFolder.Path}\\{voiceGUID}");

            return voiceGUID;
        }

        public async static Task<string> DownloadVoice(string voiceGUID)
        {
            StorageFolder storageFolder = (StorageFolder)await ApplicationData.Current.LocalFolder.TryGetItemAsync("Assets\\Voice\\");
            if (storageFolder == null)
            {
                storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Assets\\Voice\\");
            }

            var outputFolderLocation = $"Assets\\Voice\\{voiceGUID}\\";

            StorageFile sf = await storageFolder.CreateFileAsync($"{voiceGUID}.zip", CreationCollisionOption.ReplaceExisting);
            var downloadFolder = (await sf.GetParentAsync()).ToString();
            HttpClient client = new HttpClient();
            byte[] buffer = await client.GetByteArrayAsync("https://thebeebscontent.blob.core.windows.net/intelligentexhibits/" + voiceGUID + ".zip");
            using (Stream stream = await sf.OpenStreamForWriteAsync())
            {
                stream.Write(buffer, 0, buffer.Length);
            }

            return voiceGUID;
        }

        internal async static Task<IVoicePlayer> VoicePlayerFactory(string voiceGUID)
        {
            IStorageItem file = await ApplicationData.Current.LocalFolder.TryGetItemAsync($"Assets\\Voice\\{voiceGUID}\\voice.json");
            if (file == null)
            {
                var vp = new VoicePlayerGenerated();
                vp.Say("Error: We could not load the Voice");
                return vp;
            }
            StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync($"Assets\\Voice\\{voiceGUID}\\");
 
            var jsonstring = await FileIO.ReadTextAsync(((StorageFile)file));
            JsonObject ja = JsonValue.Parse(jsonstring).GetObject();
            
            var voicetype = ja.GetNamedString("Type", "");
            if (voicetype == "wav")
            {
                return new VoicePlayer(ja, folder);
            }
            return new VoicePlayerGenerated();
        }
    }
}
