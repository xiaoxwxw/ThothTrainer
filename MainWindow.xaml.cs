using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.IO;
using System.Data;
using System.Drawing;

// Emgu.CV
using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

//
using ThothTrainer.Models;
using ThothTrainer.Managers;
using ThothTrainer.Views;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Threading;

namespace ThothTrainer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<Face> _faces = new ObservableCollection<Face>();
        private ThothRecognizer _recognizer;
        private Capture _capture = null;
        private bool _captureInProgress = false;
        private User _user = new User();
        private int cameraIndex = 0;
        private int cameraCount = 1;

        // COVER of Camera 
        private static string _cameraCoverFile = Environment.CurrentDirectory + ("/Resources/Obama.jpg");

        public MainWindow()
        {
            InitializeComponent();

            InitializeDataBind();
            InitializeCamera();

            StartTraining();
        }

        private void StartTraining()
        {
            SetStatus("Thoth training is in progress...");
            Task task = new Task(() => {
                _recognizer.InitializeTraining();
            });
            task.ContinueWith(OnTrainingCompleted);
            task.Start();
        }

        private void OnTrainingCompleted(Task task)
        {
            _recognizer.OnTrainingCompleted();

            string message = string.Empty;
            
            if (task.IsCompleted)
            {
                message = ("Thoth training is completed");
            }
            else if (task.IsFaulted)
            {
                message = ("Thoth training is failed");
            }
            else if (task.IsCanceled)
            {
                message = ("Thoth training is canceled");
            }
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            Dispatcher.BeginInvoke(DispatcherPriority.Normal, (ThreadStart)delegate () {
                SetStatus(message);
            });
        }

        private void InitializeDataBind()
        {
            _recognizer = new ThothRecognizer();
            PictureList.ItemsSource = _faces;
        }

        private void InitializeCamera()
        {
            CameraImageBox.Image = new Mat(_cameraCoverFile, LoadImageType.Color);
        }

        /// <summary>
        /// Update message statusbar
        /// </summary>
        /// <param name="message"></param>
        private void SetStatus(string message)
        {
            StatusBar.Content = message;
        }

        private void OnProcessingFrame(object sender, EventArgs e)
        {
            Mat frame = new Mat();
            try
            {
                bool isOK = _capture.Retrieve(frame);
                if (isOK)
                {
                    var image = _recognizer.DetectFace(frame, Face.W, Face.H);
                    CameraImageBox.Image = image;
                }

                if (_captureInProgress.Equals(false))
                {
                    frame = new Mat(_cameraCoverFile, LoadImageType.Color);
                    var image = _recognizer.DetectFace(frame, Face.W, Face.H);
                    CameraImageBox.Image = image;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void AddFace(Mat image)
        {
            var faceCount = 0;
            long identity = 0;
            long.TryParse(IdentityTextBox.Text, out identity);
            string name = NameTextBox.Text;
            var grayFaces = _recognizer.DetectFace(image, Face.W, Face.H, out faceCount);
            foreach (var grayFace in grayFaces)
            {
                if (grayFace == null)
                {
                    throw new NoFacesException("No faces detected");
                }

                AssignFormValues();

                var data = grayFace.ToJpegData(100); // Store image in JPEG format, highest quality

                _faces.Add(new Face(data, identity, name));
            }
            ScrollToBottom(PictureList);
            SetStatus(faceCount + " face" + (faceCount > 1 ? "s" : "") + " detected");
        }

        private void AssessForm()
        {
            if (IsInvalid())
            {
                NameTextBox.Focus();
                throw new InvalidFormException("The form is incomplete");
            }
        }

        private void AssignFormValues()
        {
            long identity = 0;
            long.TryParse(IdentityTextBox.Text, out identity);

            _user.ID = identity;
            _user.Email = EmailTextBox.Text;
            _user.Name = NameTextBox.Text;
        }

        private bool IsInvalid()
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                return true;
            }
            return false;
        }

        private void ClearFaces()
        {
            while (_faces.Count > 0)
            {
                _faces.RemoveAt(0);
            }
            SearchResult.ItemsSource = null;

        }

        private void TakePicture()
        {
            Mat frame = new Mat();
            if (_captureInProgress)
            {
                frame = _capture.QueryFrame();
            }
            else
            {
                frame = new Mat(_cameraCoverFile, LoadImageType.Color);
                CameraImageBox.Image = frame;
                CameraImageBox.Refresh();
            }

            if (frame == null)
            {
                throw new NoImagesException("No images detected");
            }
            AddFace(frame);
        }

        private void ChoosePicture()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = ".jpg";
            dialog.Filter = "JPG image|*.jpg|PNG image|*.png";
            bool? isSelected = dialog.ShowDialog();
            if (isSelected.Equals(true))
            {
                var image = new Mat(dialog.FileName, LoadImageType.Color);
                AddFace(image);
                SetStatus("Face is detected succesfully");
            }
            else
            {
                SetStatus("Selection cancelled");
            }
        }

        private void DropPictures(string[] files, List<int> results)
        {
            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);

                if (fi.Attributes.Equals(FileAttributes.Directory))
                {
                    string[] f = Directory.GetFiles(file);
                    DropPictures(f, results);
                    continue;
                }

                var image = new Mat(file, LoadImageType.Color);
                results[0]++;
                try
                {
                    AddFace(image);
                }
                catch (NoFacesException)
                {
                    results[1]++;
                }
            }
        }

        private void SaveAndTrain()
        {
            AssessForm();
            AssignFormValues();
            var faceCount = _faces.Count();
            string spString = (faceCount > 1 ? "s" : "");
            var confirmMessage = string.Empty;

            if (_user.ID.Equals(User.NEWID))
            {
                confirmMessage = string.Format("Are you sure to ADD a NEW user [{0}] with {1} face picture{2}?", _user.Name, faceCount, spString);
            }
            else
            {
                confirmMessage = string.Format("Are you sure to ADD [{0}] face picture{1} for [{2}]?", faceCount, spString, _user.Name);
            }

            var messageResult = MessageBox.Show(confirmMessage, "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (messageResult == MessageBoxResult.Yes)
            {
                long identity = _user.ID;
                int result = UserManager.AddUserWithFaces(_user, _faces);
                string message = string.Empty;
                if (_user.ID > identity)
                {
                    message = "A new user has been created, ";
                    IdentityTextBox.Text = _user.ID.ToString();
                }

                message += result + " face image" + (result > 1 ? "s" : "") + " are imported into database";

                SetStatus(message);

                if (result > 0)
                {
                    ClearFaces();
                    ClearForm();
                }

                StartTraining();
            }
            
        }

        private void SearchUsers()
        {
            AssignFormValues();
            DataSet dataSet = UserManager.SearchUser(_user);
            SetStatus("Found " + dataSet.Tables[0].Rows.Count + " people");

            SearchResult.SelectionChanged -= SearchResult_SelectionChanged;

            SearchResult.ItemsSource = dataSet.Tables[0].DefaultView;
            SearchResult.SelectionChanged += SearchResult_SelectionChanged;
        }

        private void DeleteUser()
        {
            int result = UserManager.DeleteUser(_user);
            if (result > 0)
            {
                SetStatus("User [" + _user.Name + "] has been DELETED");
            }
            else
            {
                SetStatus("User [" + _user.Name + "] is NOT deleted");
            }
            ClearForm();
            SearchUsers();
        }

        private void DeletePictures()
        {
            int result = UserManager.DeleteUser(_user, false);
            if (result > 0)
            {
                SetStatus("Pictures of [" + _user.Name + "] has been DELETED");
            }
            else
            {
                SetStatus("Pictures of [" + _user.Name + "] is NOT deleted");
            }
        }

        #region helpers
        private void ScrollToBottom(ListBox listBox)
        {
            listBox.SelectedIndex = listBox.Items.Count - 1;
            listBox.ScrollIntoView(listBox.SelectedItem);
            listBox.SelectedIndex = -1;
        }

        private void ClearForm()
        {
            IdentityTextBox.Text = EmailTextBox.Text = NameTextBox.Text = string.Empty;
            SearchResult.SelectedIndex = -1;
            _user.ID = 0;
            _user.Email = _user.Name = string.Empty;
            ResetFacesID();
        }

        private void ResetFacesID()
        {
            for (int i = 0; i < _faces.Count; i++)
            {
                _faces[i].DisplayName = _user.Name;
            }
            PictureList.Items.Refresh();
        }

        #endregion helpers

        #region events

        /// <summary>
        /// Take picture from camera
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TakePictures_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TakePicture();
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message);
            }
        }
        

        /// <summary>
        /// Take picture from file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChoosePictures_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChoosePicture();
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message);
            }
        }

        /// <summary>
        /// Remove all the pictures from list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetPictures_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            ClearFaces();
            SetStatus("");
            NameTextBox.Focus();
        }

        /// <summary>
        /// Placeholder for dragging
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            ThothTrainer.Opacity = 0.5;
        }

        /// <summary>
        /// Read data from dropped files
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Drop(object sender, DragEventArgs e)
        {
            try
            {
                ThothTrainer.Opacity = 1;
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                var results = new List<int>() { 0, 0 };
                DropPictures(files, results);

                SetStatus("" + results[0] + " file" + (results[0] > 1 ? "s" : "") + ", " + results[1] + " failure" + (results[1] > 1 ? "s" : ""));
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message);
            }
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            ThothTrainer.Opacity = 1;
        }

        /// <summary>
        /// Pause or resume camera
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CameraImageBox_Click(object sender, EventArgs e)
        {
            if(_capture != null)
            {
                _capture.ImageGrabbed -= OnProcessingFrame;
                _capture.Stop();
                _capture.Dispose();
            }

            if (cameraIndex > cameraCount - 1)
            {
                _captureInProgress = false;
                CameraImageBox.Image = new Mat(_cameraCoverFile, LoadImageType.Color);
                cameraIndex = 0;
            }
            else
            {
                _capture = new Capture(cameraIndex);
                _capture.ImageGrabbed += OnProcessingFrame;
                _capture.Start();
                _captureInProgress = true;
                cameraIndex++;
            }
            
        }

        /// <summary>
        /// Store data into database or file system
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SubmitAndTrain_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveAndTrain();
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message);
            }
        }

        private void NameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            SearchUsers();
        }

        private void SearchResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataRowView item = (sender as ListBox).SelectedItem as DataRowView;
            if(item == null)
            {
                return;
            }

            _user.Name = NameTextBox.Text = item["Name"].ToString();
            _user.Email = EmailTextBox.Text = item["Email"].ToString();
            _user.ID = 0;
            long identity = 0;
            long.TryParse(item["ID"].ToString(), out identity);
            _user.ID = identity;
            IdentityTextBox.Text = identity.ToString();

            ResetFacesID();
        }


        private void DeleteSelectedUser_Click(object sender, RoutedEventArgs e)
        {
            if (_user.ID.Equals(0))
            {
                return;
            }
            if (MessageBox.Show("Are you sure to DELETE ["+_user.Name+"]?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                DeleteUser();
            }
            else
            {

            }
        }

        private void RemovePicture_Click(object sender, RoutedEventArgs e)
        {
            int index = PictureList.SelectedIndex;
            if (index.Equals(-1))
            {
                return;
            }
            
            _faces.RemoveAt(index);
        }

        private void ResetFormOnly_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            SetStatus("");
            NameTextBox.Focus();
        }


        private void NameTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            _user.Name = NameTextBox.Text;
            ResetFacesID();
        }

        private void DeletePicOnly_Click(object sender, RoutedEventArgs e)
        {
            if (_user.ID.Equals(0))
            {
                return;
            }
            if (MessageBox.Show("Are you sure to DELETE pictures of [" + _user.Name + "]?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                DeletePictures();
            }
            else
            {

            }
        }

        private void NameTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //UserWindow userWindow = new UserWindow();
            //userWindow.User = _user;
            //userWindow.InitializeUI();
            //userWindow.Show();
        }
        #endregion events

    }
}
