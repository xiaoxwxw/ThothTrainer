using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.IO;
using System.Data;
using System.Management;
using System.Drawing;

using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
//using Emgu.CV.ML;

namespace ThothTrainer.Managers
{
    internal class ThothRecognizer
    {
        private CascadeClassifier _cascadeClassifier = null;
        private FaceRecognizer _recognizer = null;

        private Dictionary<int, string> faceMapping = new Dictionary<int, string>();

        // FILE to be loaded for face detection
        private static string _haarcascadeFile = Environment.CurrentDirectory + "/Data/haarcascades/haarcascade_frontalface_alt.xml";
        // FILE to be loaded from face recognition runtime
        private static string _trainedFile = Environment.CurrentDirectory + ("/Data/trained/data.xml");

        //private SVM svmModel = new SVM();

        public bool IsTraining { get; set; }

        internal void InitializeTraining()
        {
            IsTraining = true;

            _cascadeClassifier = new CascadeClassifier(_haarcascadeFile);
            // Connect to the Database
            // Get image resources with Mappings
            // Train
            DataSet dataSet = UserManager.SearchUser();
            int length = dataSet.Tables[0].Rows.Count;
            int userCount = UserManager.QueryUserCount();

            List<Image<Gray, float>> images = new List<Image<Gray, float>>();
            List<int> labels = new List<int>();

            for (int i = 0; i < length; i++)
            {
                int identity = 0;
                DataRow dr = dataSet.Tables[0].Rows[i];
                int.TryParse(dr["ID"].ToString(), out identity);
                object imageObject = dr["DisplayImage"];
                if (imageObject.Equals(DBNull.Value))
                {
                    continue;
                }

                MemoryStream ms = new MemoryStream((byte[])imageObject);
                Bitmap bmp = new Bitmap(ms);
                images.Add(new Image<Gray, float>(bmp));
                labels.Add(identity);
                faceMapping[identity] = dr["Name"].ToString();
            }
            if (userCount > 1)
            {
                _recognizer = new FisherFaceRecognizer(0, 600.0);
                // _recognizer = new LBPHFaceRecognizer(10, 10, 8, 8, 140.0);
                // _recognizer = new EigenFaceRecognizer(0, 3000);
                //svmModel.SetKernel(SVM.SvmKernelType.Linear);
                //svmModel.Type = SVM.SvmType.CSvc;
                //svmModel.C = 1;
                //svmModel.TermCriteria = new MCvTermCriteria(100, 0.00001);
                //svmModel.TrainAuto(trainData
                _recognizer.Train(images.ToArray(), labels.ToArray());
                // Save
                _recognizer.Save(_trainedFile);
                // _recognizer.Load(_trainedFile);
            }
        }

        internal void OnTrainingCompleted()
        {
            IsTraining = false;
        }

        internal Image<Bgr, byte> DetectFace(Mat frame, int width, int height)
        {
            var image = frame.ToImage<Bgr, byte>();
            var faces = _cascadeClassifier.DetectMultiScale(image, 1.2, 10); //the actual face detection happens here
            for (var i = 0; i < faces.Length; i++)
            {
                var face = faces[i];
                int xPos = face.X;
                int yPos = face.Y;
                var grayFace = image.Copy(face).Resize(width, height, Inter.Cubic).Convert<Gray, byte>();
                // grayFace._EqualizeHist();
                image.Draw(face, new Bgr(Color.LightBlue), 3);
                if (IsTraining.Equals(false) && _recognizer != null)
                {
                    FaceRecognizer.PredictionResult result = _recognizer.Predict(grayFace);
                    // float result = svmModel.Predict(grayFace);
                    if (result.Label != -1 && faceMapping.ContainsKey(result.Label))
                    {
                        string message = faceMapping[result.Label];
                        DrawText(message, image, xPos, yPos);
                        Console.WriteLine("[" + result.Distance + "] " + message);
                    }
                    else
                    {
                        Console.Write(".");
                    }
                }
            }
            return image;
        }

        internal List<Image<Gray, byte>> DetectFace(Image<Bgr, byte> image, int width, int height, out int count)
        {
            var faces = _cascadeClassifier.DetectMultiScale(image, 1.2, 10); //the actual face detection happens here
            count = faces.Length;
            List<Image<Gray, byte>> grayFaces = new List<Image<Gray, byte>>();

            Parallel.ForEach(faces, face => {
                int xPos = face.X;
                int yPos = face.Y;
                var grayFace = image.Copy(face).Resize(width, height, Inter.Cubic).Convert<Gray, byte>();
                grayFaces.Add(grayFace);
                // grayFace._EqualizeHist();
                image.Draw(face, new Bgr(Color.LightBlue), 3);
                if (IsTraining.Equals(false) && _recognizer != null)
                {
                    FaceRecognizer.PredictionResult result = _recognizer.Predict(grayFace);
                    // float result = svmModel.Predict(grayFace);
                    if (result.Label != -1 && faceMapping.ContainsKey(result.Label))
                    {
                        string message = faceMapping[result.Label];
                        DrawText(message, image, xPos, yPos);
                        Console.WriteLine("[" + result.Distance + "] " + message);
                    }
                }
            });
            return grayFaces;
        }

        internal List<Image<Gray, byte>> DetectFace(Mat frame, int width, int height, out int count)
        {
            var image = frame.ToImage<Bgr, byte>();
            return DetectFace(image, width, height, out count);
        }

        private void DrawText(string message, Image<Bgr, byte> image, int xPos, int yPos, bool englishNameOnly = true)
        {
            if (englishNameOnly)
            {
                image.Draw(message, new Point(xPos, yPos - 15), FontFace.HersheyComplexSmall, 1.0, new Bgr(Color.LightBlue), 1, LineType.EightConnected, false);
            }
            else
            {
                Bitmap bmp = new Bitmap(image.Width, image.Height);
                Graphics g = Graphics.FromImage(bmp);
                Font drawFont = new Font("Arial", 16, System.Drawing.FontStyle.Regular);
                g.DrawString(message, drawFont, Brushes.LightBlue, xPos, yPos - 40);
                g.Save();
                for (int i = 0; i < image.Width; i++)
                {
                    for (int j = 0; j < image.Height; j++)
                    {
                        Color c = bmp.GetPixel(i, j);
                        if (c.R > 0 || c.B > 0 || c.G > 0)
                        {
                            CvInvoke.cvSet2D(image, j + 10, i, new MCvScalar(c.B, c.G, c.R));
                        }
                    }
                }
                g.Dispose();
            }
        }

        public static List<USBDeviceInfo> GetUSBDevices()
        {
            List<USBDeviceInfo> devices = new List<USBDeviceInfo>();

            ManagementObjectCollection collection;
            using (var searcher = new ManagementObjectSearcher(@"Select * From Win32_USBHub"))
            {
                collection = searcher.Get();
            }

            foreach (var device in collection)
            {
                devices.Add(new USBDeviceInfo(
                (string)device.GetPropertyValue("DeviceID"),
                (string)device.GetPropertyValue("PNPDeviceID"),
                (string)device.GetPropertyValue("Description")
                ));
            }

            collection.Dispose();
            return devices;
        }
    }

    internal class USBDeviceInfo
    {
        public USBDeviceInfo(string deviceID, string pnpDeviceID, string description)
        {
            this.DeviceID = deviceID;
            this.PnpDeviceID = pnpDeviceID;
            this.Description = description;
        }
        public string DeviceID { get; private set; }
        public string PnpDeviceID { get; private set; }
        public string Description { get; private set; }
    }
}