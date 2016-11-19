
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace ThothTrainer.Models
{
    public class Face
    {
        public const int W = 100;
        public const int H = 100;

        public long ID { get; set; }
        public string DisplayName { get; set; }

        public BitmapImage DisplayImage {
            get {
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = new MemoryStream(Image);
                image.EndInit();
                return image;
            }
        }
        public byte[] Image { get; set; }

        public Face(byte[] _Image, long _ID = 0, string _Name = "")
        {
            Image = _Image;
            ID = _ID;

            DisplayName = _Name;
        }
    }
}
