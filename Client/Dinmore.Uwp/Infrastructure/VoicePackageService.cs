using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Dinmore.Uwp.Infrastructure
{
    public class VoicePackageService
    {
        public async static void UnpackVoice(string voiceGUID) {
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
    }
}
