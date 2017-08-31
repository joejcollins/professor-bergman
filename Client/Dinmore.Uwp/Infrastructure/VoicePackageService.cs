using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Dinmore.Uwp.Infrastructure
{
    public class VoicePackageService
    {
        private async static void UnpackVoice(string voiceGUID) {
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
            System.IO.Compression.ZipFile.ExtractToDirectory($"{storageFolder.Path}\\{voiceGUID}.zip", storageFolder.Path);
     
        }

        public async static void DownloadVoice(string voiceGUID)
        {
            StorageFolder storageFolder = (StorageFolder)await ApplicationData.Current.LocalFolder.TryGetItemAsync("Assets\\Voice\\");
            if (storageFolder == null)
            {
                storageFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Assets\\Voice\\");
            }

            var outputFolderLocation = $"Assets\\Voice\\{voiceGUID}\\";

            StorageFile sf = await storageFolder.CreateFileAsync($"/{voiceGUID}.zip", CreationCollisionOption.ReplaceExisting);
            var downloadFolder = (await sf.GetParentAsync()).ToString();
            HttpClient client = new HttpClient();
            byte[] buffer = await client.GetByteArrayAsync("https://thebeebscontent.blob.core.windows.net/intelligentexhibits/de724d1e-85ba-416b-8784-93ab178b68a8.zip");
            using (Stream stream = await sf.OpenStreamForWriteAsync())
            {
                stream.Write(buffer, 0, buffer.Length);
            }
            
            await UnpackVoice(voiceGUID);
        }
    }
}
