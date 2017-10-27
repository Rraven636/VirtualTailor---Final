using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;
using System.IO;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.BackgroundRemoval;

namespace ColourSkel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        /////////////////////////Background Removal/////////////////////////////
        /// <summary>
        /// Format we will use for the depth stream
        /// </summary>
        private const DepthImageFormat _DepthFormat = DepthImageFormat.Resolution640x480Fps30;

        /// <summary>
        /// Format we will use for the color stream
        /// </summary>
        private const ColorImageFormat _ColorFormat = ColorImageFormat.RgbResolution640x480Fps30;

        /// <summary>
        /// Bitmap that will hold color information from BackgroundRemovedStream and sent to SkeletonLib for processing
        /// </summary>
        private WriteableBitmap _foregroundBitmap;

        /// <summary>
        /// Our core library which does background 
        /// </summary>
        private BackgroundRemovedColorStream _backgroundRemovedColorStream;

        /// <summary>
        /// Intermediate storage for the skeleton data received from the sensor
        /// </summary>
        private Skeleton[] _skeletons;

        /// <summary>
        /// the skeleton that is currently tracked by the app
        /// </summary>
        private int _currentlyTrackedSkeletonId;

        /// <summary>
        /// Track whether Dispose has been called
        /// </summary>
        private bool _disposed;
        /////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensorChooser _sensorChooser;

        /// <summary>
        /// Object to handle skeleton operations
        /// </summary>
        private SkeletonLib _skelObj;

        private ColorImageFrame _colourFrameIn;

        private DepthImageFrame _depthFrameIn;

        private String _neckStringOutput = "Not available";

        private String _chestStringOutput = "Not available";

        private String _waistStringOutput = "Not available";

        private String _armUpperLeftStringOutput = "Not available";

        private String _armLowerLeftStringOutput = "Not available";

        private String _armUpperRightStringOutput = "Not available";

        private String _armLowerRightStringOutput = "Not available";

        private String _legUpperLeftStringOutput = "Not available";

        private String _legLowerLeftStringOutput = "Not available";

        private String _legUpperRightStringOutput = "Not available";

        private String _legLowerRightStringOutput = "Not available";

        private Boolean _frontMeasure = false;

        private Boolean _leftMeasure = false;

        private Boolean _rightMeasure = false;

        private Boolean _backMeasure = false;

        private String _allLengthsOutput;

        private Circumference3D _circumferenceObj;

        public MainWindow()
        {
            InitializeComponent();

            _frontMeasure = true;
            _leftMeasure = false;
            _rightMeasure = false;
            _backMeasure = false;

            _circumferenceObj = new Circumference3D();

            _allLengthsOutput = "";

            // Initialize the sensor chooser and UI
            _sensorChooser = new KinectSensorChooser();
            this.sensorChooserUi.KinectSensorChooser = _sensorChooser;
            _sensorChooser.KinectChanged += this.SensorChooserOnKinectChanged;
            _sensorChooser.Start();
        }

        /// <summary>
        /// Finalizes an instance of the MainWindow class.
        /// This destructor will run only if the Dispose method does not get called.
        /// </summary>
        ~MainWindow()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Dispose the allocated frame buffers and reconstruction.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            // This object will be cleaned up by the Dispose method.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Frees all memory associated with the FusionImageFrame.
        /// </summary>
        /// <param name="disposing">Whether the function was called from Dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (null != _backgroundRemovedColorStream)
                {
                    _backgroundRemovedColorStream.Dispose();
                    _backgroundRemovedColorStream = null;
                }

                _disposed = true;
            }
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _sensorChooser.Stop();
            _sensorChooser = null;
        }
        
        /// <summary>
        /// Changes the status bar text to the measurements from the skeleton object
        /// </summary>
        private void LiveMeasure()
        {            
            if (_skelObj != null && _skelObj.isEmpty() == false)
            {
                statusBarText.Text = _allLengthsOutput;
                //this.statusBarText.Text = "Measurements Available to the Right";

                this.NeckMeasureBlock.Text = _neckStringOutput;
                this.ChestMeasureBlock.Text = _chestStringOutput;
                this.WaistMeasureBlock.Text = _waistStringOutput;

                // Left Arm
                this.LeftUpperArmMeasureBlock.Text = _armUpperLeftStringOutput;
                this.LeftLowerArmMeasureBlock.Text = _armLowerLeftStringOutput;

                // Right Arm
                this.RightUpperArmMeasureBlock.Text = _armUpperRightStringOutput;
                this.RightLowerArmMeasureBlock.Text = _armLowerRightStringOutput;

                // Left Leg
                this.LeftUpperLegMeasureBlock.Text = _legUpperLeftStringOutput;
                this.LeftLowerLegMeasureBlock.Text = _legLowerLeftStringOutput;

                // Right Leg
                this.RightUpperLegMeasureBlock.Text = _legUpperRightStringOutput;
                this.RightLowerLegMeasureBlock.Text = _legLowerRightStringOutput;
            }
            else
            {
                this.statusBarText.Text = "No skeleton ready yet";
            }
        }

        /// <summary>
        /// Event handler for when the Measure Button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonMeasureClick(object sender, RoutedEventArgs e)
        {
            LiveMeasure();
        }
        
        /// <summary>
        /// Event handler for Kinect sensor's DepthFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            // in the middle of shutting down, or lingering events from previous sensor, do nothing here.
            if (null == _sensorChooser || null == _sensorChooser.Kinect || _sensorChooser.Kinect != sender)
            {
                return;
            }

            try
            {
                _skelObj = new SkeletonLib();

                using (var depthFrame = e.OpenDepthImageFrame())
                {
                    if (null != depthFrame)
                    {
                        _backgroundRemovedColorStream.ProcessDepth(depthFrame.GetRawPixelData(), depthFrame.Timestamp);
                        _depthFrameIn = depthFrame;
                        _skelObj.setDepthFrame(depthFrame, 480, 640);
                    }
                }

                using (var colorFrame = e.OpenColorImageFrame())
                {
                    if (null != colorFrame)
                    {
                        _backgroundRemovedColorStream.ProcessColor(colorFrame.GetRawPixelData(), colorFrame.Timestamp);
                        _colourFrameIn = colorFrame;
                        _skelObj.setColourFrame(colorFrame, 480, 640);
                    }
                }

                using (var skeletonFrame = e.OpenSkeletonFrame())
                {
                    if (null != skeletonFrame)
                    {
                        skeletonFrame.CopySkeletonDataTo(_skeletons);
                        _backgroundRemovedColorStream.ProcessSkeleton(_skeletons, skeletonFrame.Timestamp);
                        _skelObj.setSkeletonFrame(skeletonFrame);
                    }
                }

                this.ChooseSkeleton();
            }
            catch (InvalidOperationException)
            {
                // Ignore the exception. 
            }
        }

        /// <summary>
        /// Handle the background removed color frame ready event. The frame obtained from the background removed
        /// color stream is in RGBA format.
        /// </summary>
        /// <param name="sender">object that sends the event</param>
        /// <param name="e">argument of the event</param>
        private void BackgroundRemovedFrameReadyHandler(object sender, BackgroundRemovedColorFrameReadyEventArgs e)
        {
            using (var backgroundRemovedFrame = e.OpenBackgroundRemovedColorFrame())
            {
                if (backgroundRemovedFrame != null)
                {
                    if (null == _foregroundBitmap || _foregroundBitmap.PixelWidth != backgroundRemovedFrame.Width
                        || _foregroundBitmap.PixelHeight != backgroundRemovedFrame.Height)
                    {
                        _foregroundBitmap = new WriteableBitmap(backgroundRemovedFrame.Width, backgroundRemovedFrame.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
                        
                    }                    

                    // Write the pixel data into our bitmap
                    _foregroundBitmap.WritePixels(
                        new Int32Rect(0, 0, _foregroundBitmap.PixelWidth, _foregroundBitmap.PixelHeight),
                        backgroundRemovedFrame.GetRawPixelData(),
                        _foregroundBitmap.PixelWidth * sizeof(int),
                        0);
                    
                    if (_skelObj.isEmpty() == false)
                    {

                        int stride = (int)((_foregroundBitmap.PixelWidth * _foregroundBitmap.Format.BitsPerPixel + 7) / 8);
                        byte[] bitmapArray = new byte[_foregroundBitmap.PixelHeight * stride];
                        _foregroundBitmap.CopyPixels(                             
                             bitmapArray, 
                             stride, 
                             0);
                        _skelObj.setMeasureDirection(_frontMeasure, _leftMeasure, _rightMeasure, _backMeasure);

                        _skelObj.setPixelArray(bitmapArray, stride);

                        _skelObj.setActiveKinectSensor(_sensorChooser.Kinect);

                        _skelObj.populateDepthAndSkelPoints(_colourFrameIn, _depthFrameIn, _ColorFormat, _DepthFormat, 480, 640, 480, 640);

                        _skelObj.setAveragePersonDepth(backgroundRemovedFrame.AverageDepth);
                        
                        _skelObj.SkeletonStart(_foregroundBitmap);
 
                        // Set the image we display to point to the bitmap where we'll put the image data
                        this.Image.Source = _skelObj.getOutputImage();

                        populateStrings(_skelObj.getMostRecentMeasure());

                        addView(_skelObj.getMostRecentMeasure());

                        if (_frontMeasure)
                        {
                            Measure tempMeas = new Measure();
                            _allLengthsOutput = "Left Arm: " + "\t" + tempMeas.formatToCm(_skelObj.getLeftArmLength()) + "cm"
                                                + "\n" + "Right Arm: " + "\t" + tempMeas.formatToCm(_skelObj.getRightArmLength()) + "cm"
                                                + "\n" + "Left Leg: " + "\t" + tempMeas.formatToCm(_skelObj.getLeftLegLength()) + "cm"
                                                + "\n" + "Right Leg: " + "\t" + tempMeas.formatToCm(_skelObj.getRightLegLength()) + "cm"
                                                + "\n" + "Torso: " + "\t" + tempMeas.formatToCm(_skelObj.getTorsoLength()) + "cm";
                        }

                        if (_circumferenceObj.allViewsReady())
                        {
                            buttonResults.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        // Set the image we display to point to the bitmap where we'll put the image data
                        this.Image.Source = _foregroundBitmap;
                    }
                }
            }
        }

        public void addView(Measure measObj)
        {
            if (_frontMeasure ==  true)
            {
                _circumferenceObj.addViewMeasurement(measObj, Circumference3D.FRONT);
            }
            else if (_leftMeasure == true)
            {
                _circumferenceObj.addViewMeasurement(measObj, Circumference3D.LEFT);
            }
            else if(_rightMeasure == true)
            {
                _circumferenceObj.addViewMeasurement(measObj, Circumference3D.RIGHT);
            }
            else if (_backMeasure == true)
            {
                _circumferenceObj.addViewMeasurement(measObj, Circumference3D.BACK);
            }
        }

        public void populateStrings(Measure measureObj)
        {
            _neckStringOutput = measureObj.toStringNeck();
            _waistStringOutput = measureObj.toStringWaist();
            _chestStringOutput = measureObj.toStringChest();

            if (_rightMeasure == false)
            {
                //Left Arm
                _armUpperLeftStringOutput = measureObj.toStringArmLeftUpper();
                _armLowerLeftStringOutput = measureObj.toStringArmLeftLower();

                //Left Leg
                _legUpperLeftStringOutput = measureObj.toStringLegLeftUpper();
                _legLowerLeftStringOutput = measureObj.toStringLegLeftLower();
            }
            else
            {
                //Left Arm
                _armUpperLeftStringOutput = "N/A in Right Mode";
                _armLowerLeftStringOutput = "N/A in Right Mode";

                //Left Leg
                _legUpperLeftStringOutput = "N/A in Right Mode";
                _legLowerLeftStringOutput = "N/A in Right Mode";
            }

            if (_leftMeasure == false)
            {
                //Right Arm
                _armUpperRightStringOutput = measureObj.toStringArmRightUpper();
                _armLowerRightStringOutput = measureObj.toStringArmRightLower();

                //Right Leg 
                _legUpperRightStringOutput = measureObj.toStringLegRightUpper();
                _legLowerRightStringOutput = measureObj.toStringLegRightLower();
            }
            else
            {
                //Right Arm
                _armUpperRightStringOutput = "N/A in Left Mode";
                _armLowerRightStringOutput = "N/A in Left Mode";

                //Right Leg 
                _legUpperRightStringOutput = "N/A in Left Mode";
                _legLowerRightStringOutput = "N/A in Left Mode";
            }
        }

        /// <summary>
        /// Use the sticky skeleton logic to choose a player that we want to set as foreground. This means if the app
        /// is tracking a player already, we keep tracking the player until it leaves the sight of the camera, 
        /// and then pick the closest player to be tracked as foreground.
        /// </summary>
        private void ChooseSkeleton()
        {
            var isTrackedSkeltonVisible = false;
            var nearestDistance = float.MaxValue;
            var nearestSkeleton = 0;

            foreach (var skel in _skeletons)
            {
                if (null == skel)
                {
                    continue;
                }

                if (skel.TrackingState != SkeletonTrackingState.Tracked)
                {
                    continue;
                }

                if (skel.TrackingId == _currentlyTrackedSkeletonId)
                {
                    isTrackedSkeltonVisible = true;
                    break;
                }

                if (skel.Position.Z < nearestDistance)
                {
                    nearestDistance = skel.Position.Z;
                    nearestSkeleton = skel.TrackingId;
                }
            }

            if (!isTrackedSkeltonVisible && nearestSkeleton != 0)
            {
                _backgroundRemovedColorStream.SetTrackedPlayer(nearestSkeleton);
                _currentlyTrackedSkeletonId = nearestSkeleton;
            }
        }
        
        /// <summary>
        /// Called when the KinectSensorChooser gets a new sensor
        /// </summary>
        /// <param name="sender">sender of the event</param>
        /// <param name="args">event arguments</param>
        private void SensorChooserOnKinectChanged(object sender, KinectChangedEventArgs args)
        {
            if (args.OldSensor != null)
            {
                try
                {
                    args.OldSensor.AllFramesReady -= this.SensorAllFramesReady;
                    args.OldSensor.DepthStream.Disable();
                    args.OldSensor.ColorStream.Disable();
                    args.OldSensor.SkeletonStream.Disable();

                    // Create the background removal stream to process the data and remove background, and initialize it.
                    if (null != _backgroundRemovedColorStream)
                    {
                        _backgroundRemovedColorStream.BackgroundRemovedFrameReady -= this.BackgroundRemovedFrameReadyHandler;
                        _backgroundRemovedColorStream.Dispose();
                        _backgroundRemovedColorStream = null;
                    }
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }

            if (args.NewSensor != null)
            {
                try
                {
                    args.NewSensor.DepthStream.Enable(_DepthFormat);
                    args.NewSensor.ColorStream.Enable(_ColorFormat);
                    args.NewSensor.SkeletonStream.Enable();

                    _backgroundRemovedColorStream = new BackgroundRemovedColorStream(args.NewSensor);
                    _backgroundRemovedColorStream.Enable(_ColorFormat, _DepthFormat);

                    // Allocate space to put the depth, color, and skeleton data we'll receive
                    if (null == _skeletons)
                    {
                        _skeletons = new Skeleton[args.NewSensor.SkeletonStream.FrameSkeletonArrayLength];
                    }

                    // Add an event handler to be called when the background removed color frame is ready, so that we can
                    // composite the image and output to the app
                    _backgroundRemovedColorStream.BackgroundRemovedFrameReady += this.BackgroundRemovedFrameReadyHandler;

                    // Add an event handler to be called whenever there is new depth frame data
                    args.NewSensor.AllFramesReady += this.SensorAllFramesReady;

                    try
                    {

                        /*
                        args.NewSensor.DepthStream.Range = this.checkBoxNearMode.IsChecked.GetValueOrDefault()
                                                    ? DepthRange.Near
                                                    : DepthRange.Default;
                        args.NewSensor.SkeletonStream.EnableTrackingInNearRange = true;
                        */
                        
                    }
                    catch (InvalidOperationException)
                    {
                        // Non Kinect for Windows devices do not support Near mode, so reset back to default mode.
                        //args.NewSensor.DepthStream.Range = DepthRange.Default;
                        //args.NewSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }

                    //this.statusBarText.Text = Properties.Resources.ReadyForScreenshot;
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }
        }

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ButtonScreenshotClick(object sender, RoutedEventArgs e)
        {
            if (null == _sensorChooser || null == _sensorChooser.Kinect)
            {
                this.statusBarText.Text = "Connect Kinect First";
                return;
            }

            int colorWidth = _foregroundBitmap.PixelWidth;
            int colorHeight = _foregroundBitmap.PixelHeight;

            // create a render target that we'll render our controls to
            var renderBitmap = new RenderTargetBitmap(colorWidth, colorHeight, 96.0, 96.0, PixelFormats.Pbgra32);

            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                // render the color image masked out by players
                var colorBrush = new VisualBrush(Image);
                dc.DrawRectangle(colorBrush, null, new Rect(new Point(), new Size(colorWidth, colorHeight)));
            }

            renderBitmap.Render(dv);

            // create a png bitmap encoder which knows how to save a .png file
            BitmapEncoder encoder = new PngBitmapEncoder();

            // create frame from the writable bitmap and add to encoder
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            var time = DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            var myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            var path = Path.Combine(myPhotos, "MeasurementResultPhoto-" + time + ".png");

            // write the new file to disk
            try
            {
                using (var fs = new FileStream(path, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                this.statusBarText.Text = string.Format(CultureInfo.InvariantCulture, "Image saved successfully", path);
            }
            catch (IOException)
            {
                this.statusBarText.Text = string.Format(CultureInfo.InvariantCulture, "Failed to save image", path);
            }
        }

        private void MeasureDirection(object sender, RoutedEventArgs e)
        {
            if ((Boolean)FrontMeasure.IsChecked)
            {
                _frontMeasure   = true;
                _leftMeasure    = false;
                _rightMeasure   = false;
                _backMeasure    = false;
                if(statusBarText != null)
                {
                    statusBarText.Text = "Front Measure On";
                }
                
            }             
            else if ((Boolean)LeftMeasure.IsChecked)
            {
                _frontMeasure   = false;
                _leftMeasure    = true;
                _rightMeasure   = false;
                _backMeasure    = false;
                if (statusBarText != null)
                {
                    statusBarText.Text = "Left Measure On";
                }
            }
            else if ((Boolean)RightMeasure.IsChecked)
            {
                _frontMeasure   = false;
                _leftMeasure    = false;
                _rightMeasure   = true;
                _backMeasure    = false;
                if (statusBarText != null)
                {
                    statusBarText.Text = "Right Measure On";
                }
            }
            else if ((Boolean)BackMeasure.IsChecked)
            {
                _frontMeasure   = false;
                _leftMeasure    = false;
                _rightMeasure   = false;
                _backMeasure    = true;
                if (statusBarText != null)
                {
                    statusBarText.Text = "Back Measure On";
                }
            }
        }

        private void ButtonResultsClick(object sender, RoutedEventArgs e)
        {
            All_Lengths.Text = _allLengthsOutput;
            All_Circumferences.Text = _circumferenceObj.toStringAllCircumferences();
            _sensorChooser.Kinect.AllFramesReady -= this.SensorAllFramesReady;
            ResultsPopup.IsOpen = true;
        }

        private void ButtonDoneClick(object sender, RoutedEventArgs e)
        {
            _allLengthsOutput = "";
            _circumferenceObj = new Circumference3D();
            FrontMeasure.IsChecked = true;
            _frontMeasure = true;
            _leftMeasure = false;
            _rightMeasure = false;
            _backMeasure = false;
            _sensorChooser.Kinect.AllFramesReady += this.SensorAllFramesReady;
            ResultsPopup.IsOpen = false;
        }

        /*
        /// <summary>
        /// Handles the checking or unchecking of the near mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxNearModeChanged(object sender, RoutedEventArgs e)
        {
            if (null == _sensorChooser || null == _sensorChooser.Kinect)
            {
                return;
            }

            // will not function on non-Kinect for Windows devices
            try
            {
                _sensorChooser.Kinect.DepthStream.Range = this.checkBoxNearMode.IsChecked.GetValueOrDefault()
                                                    ? DepthRange.Near
                                                    : DepthRange.Default;
            }
            catch (InvalidOperationException)
            {
            }
        }
        */
    }
}