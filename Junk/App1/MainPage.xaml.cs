using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App1
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private UsbCamera usbCamera = new UsbCamera();

        public MainPage()
        {
            this.InitializeComponent();
            Task.Run(func);
        }

        private async Task func()
        {
            await usbCamera.InitializeAsync();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await usbCamera.StartCameraPreview();
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var result = await usbCamera.CapturePhoto();
            Result.Text = "snapped";
        }
    }
}
