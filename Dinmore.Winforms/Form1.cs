using System;
using System.Windows.Forms;
using System.Drawing;
using Emgu.CV;
using Emgu.Util;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.IO;

namespace Dinmore.Winforms
{
    public partial class Form1 : Form
    {
        Capture cap;
        //HaarCascade haar;
        CascadeClassifier classifier;

        public Form1()
        {
            InitializeComponent();

            // passing 0 gets zeroth webcam
            cap = new Capture(0);
            // adjust path to find your xml
            //haar = new HaarCascade(@"C:\Emgu\emgucv-windows-universal-cuda 2.4.10.1940\opencv\data\haarcascades\haarcascade_frontalface_default.xml");
            //haar = new HaarCascade(@"C:\Emgu\emgucv-windows-universal-cuda 2.4.10.1940\opencv\data\haarcascades\aGest.xml");
            // TODO: Figure out app path and use cascades/xxx.

            var path = Path.Combine(Environment.CurrentDirectory, "cascades");
            var file = Path.Combine(path, "haarcascade_frontalface_default.xml");

            classifier = new CascadeClassifier(file);

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            using (Image<Bgr, byte> nextFrame = cap.QueryFrame())
            {
                if (nextFrame != null)
                {
                    // there's only one channel (greyscale), hence the zero index
                    //var faces = nextFrame.DetectHaarCascade(haar)[0];
                    Image<Gray, byte> grayframe = nextFrame.Convert<Gray, byte>();

                    var minSize = new System.Drawing.Size(nextFrame.Width / 16, nextFrame.Height / 16);
                    var maxSize = new System.Drawing.Size(nextFrame.Width, nextFrame.Height);

                    //var faces = haar.Detect(grayframe, 1.4D, 4, HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, minSize, maxSize );
                    var faces = classifier.DetectMultiScale(grayframe, 1.4, 4, minSize, maxSize);

                    //var faces = grayframe.DetectHaarCascade(haar, 1.4, 4,
                    //                HAAR_DETECTION_TYPE.DO_CANNY_PRUNING,
                    //                new Size(nextFrame.Width / 8, nextFrame.Height / 8)
                    //                );

                    //foreach (var item in faces)
                    {
                        foreach (var face in faces)
                        {
                            nextFrame.Draw(new Rectangle(face.X, face.Y, face.Width, face.Height), new Bgr(0, double.MaxValue, 0), 3);
                        }
                    }
                    pictureBox1.Image = nextFrame.ToBitmap();
                }
            }
        }
    }
}
