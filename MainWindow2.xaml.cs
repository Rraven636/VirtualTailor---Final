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
using System.Windows.Shapes;
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
        private const DepthImageFormat DepthFormat = DepthImageFormat.Resolution320x240Fps30;

        /// <summary>
        /// Format we will use for the color stream
        /// </summary>
        private const ColorImageFormat ColorFormat = ColorImageFormat.RgbResolution640x480Fps30;

        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap foregroundBitmap;

        /// <summary>
        /// Our core library which does background 
        /// </summary>
        private BackgroundRemovedColorStream backgroundRemovedColorStream;

        /// <summary>
        /// Intermediate storage for the skeleton data received from the sensor
        /// </summary>
        private Skeleton[] skeletons;

        /// <summary>
        /// the skeleton that is currently tracked by the app
        /// </summary>
        private int currentlyTrackedSkeletonId;

        /// <summary>
        /// Track whether Dispose has been called
        /// </summary>
        private bool disposed;
        /////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensorChooser sensorChooser;

        /// <summary>
        /// Object to handle colour image operations
        /// </summary>
        private Colour colourObj;

        /// <summary>
        /// Variable to hold colour image that will be sent to SkeletonLib for skeleton mapping
        /// </summary>
        private WriteableBitmap streamImg;

        /// <summary>
        /// Object to handle skeleton operations
        /// </summary>
        private SkeletonLib skelObj;

        /// <summary>
        /// Object to handle background removal operations
        /// </summary>
        private BackgroundRemovalLib backgroundObj;

        private SkeletonFrame skelFrame;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize the sensor chooser and UI
            this.sensorChooser = new KinectSensorChooser();
            this.sensorChooserUi.KinectSensorChooser = this.sensorChooser;
            this.sensorChooser.KinectChanged += this.SensorChooserOnKinectChanged;
            this.sensorChooser.Start();
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
            if (!this.disposed)
            {
                if (null != this.backgroundRemovedColorStream)
                {
                    this.backgroundRemovedColorStream.Dispose();
                    this.backgroundRemovedColorStream = null;
                }

                this.disposed = true;
            }
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.sensorChooser.Stop();
            this.sensorChooser = null;
        }

        /// <summary>
        /// Starts the Colour Reading Process
        /// </summary>
        private void InitiateColour(KinectSensor newSensor)
        {
            //Create Colour Object with active sensor and initiate StartUp
            colourObj = new Colour(newSensor);
            colourObj.startUpColour();

            //Set the global variable to hold the RGB image from the Colour Object
            this.streamImg = colourObj.getImage();

            // Adds event handler for whenever a new colour frame is ready
            newSensor.ColorFrameReady += colourObj.SensorColorFrameReady;
        }

        /// <summary>
        /// Starts the skeleton reading process and sends the colour image to be used for skeleton mapping
        /// </summary>
        private void InitiateSkel(KinectSensor newSensor)
        {
            //Create SkeltonLib Obj with active sensor
            skelObj = new SkeletonLib(newSensor);
            skelObj.SkeletonStart();
            skelObj.setColourImage(this.streamImg);

            //Tie image source to output of object
            this.Image.Source = skelObj.getOutputImage();

            // Add an event handler to be called whenever there is new skeleton frame data
            newSensor.SkeletonFrameReady += skelObj.SensorSkeletonFrameReady;
        }

        /// <summary>
        /// Starts the background removal process
        /// </summary>
        private void InitiateBackgroundRemoval(KinectSensor newSensor)
        {
            //Create BackgroundRemovalLib Obj with active sensor
            this.backgroundRemovedColorStream = new BackgroundRemovedColorStream(newSensor);
            backgroundObj = new BackgroundRemovalLib(this.sensorChooser, this.backgroundRemovedColorStream);
            backgroundObj.BackgroundStart();

            //this.backgroundRemovedColorStream = backgroundObj.getBackgroundRemovedColorStream();

            //Tie image source to output of object
            //this.Image.Source = backgroundObj.getBackgroundRemovedImage();

            backgroundObj.setImageSource(this.Image.Source);

            // Add an event handler to be called when the background removed color frame is ready, so that we can
            // composite the image and output to the app
            this.backgroundRemovedColorStream.BackgroundRemovedFrameReady += backgroundObj.BackgroundRemovedFrameReadyHandler;

            // Add an event handler to be called whenever there is new depth frame data
            newSensor.AllFramesReady += backgroundObj.SensorAllFramesReady;
        }

        /// <summary>
        /// Disables the colour streaming functionality
        /// </summary>
        /// <param name="OldSensor"></param>
        private void DisableColour(KinectSensor OldSensor)
        {
            OldSensor.ColorFrameReady -= colourObj.SensorColorFrameReady;
            OldSensor.ColorStream.Disable();
            colourObj = null;
        }

        /// <summary>
        /// Disables the skeleton tracking
        /// </summary>
        /// <param name="OldSensor"></param>
        private void DisableSkel(KinectSensor OldSensor)
        {
            OldSensor.SkeletonFrameReady -= skelObj.SensorSkeletonFrameReady;
            OldSensor.SkeletonStream.Disable();
            skelObj = null;
        }

        private void DisableBackgroundRemoval(KinectSensor OldSensor)
        {
            if (backgroundObj != null)
            {
                backgroundObj.BackgroundStop(OldSensor);
                backgroundObj = null;
            }

        }

        /// <summary>
        /// Changes the status bar text to the measurements from the skeleton object
        /// </summary>
        private void LiveMeasure()
        {
            this.measureBarText.Text = skelObj.getMeasurements();
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
        /*
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
                    DisableColour(args.OldSensor);
                    DisableSkel(args.OldSensor);
                    //DisableBackgroundRemoval(args.OldSensor);
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
                    this.InitiateColour(args.NewSensor);
                    this.InitiateSkel(args.NewSensor);
                    //this.InitiateBackgroundRemoval(args.NewSensor);                    
                }
                catch (InvalidOperationException ex)
                {
                    measureBarText.Text = ex.HelpLink;
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }
        }
        */

        
        /// <summary>
        /// Event handler for Kinect sensor's DepthFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            // in the middle of shutting down, or lingering events from previous sensor, do nothing here.
            if (null == this.sensorChooser || null == this.sensorChooser.Kinect || this.sensorChooser.Kinect != sender)
            {
                return;
            }

            try
            {
                using (var depthFrame = e.OpenDepthImageFrame())
                {
                    if (null != depthFrame)
                    {
                        this.backgroundRemovedColorStream.ProcessDepth(depthFrame.GetRawPixelData(), depthFrame.Timestamp);
                    }
                }

                using (var colorFrame = e.OpenColorImageFrame())
                {
                    if (null != colorFrame)
                    {
                        this.backgroundRemovedColorStream.ProcessColor(colorFrame.GetRawPixelData(), colorFrame.Timestamp);
                    }
                }

                using (var skeletonFrame = e.OpenSkeletonFrame())
                {
                    if (null != skeletonFrame)
                    {
                        skeletonFrame.CopySkeletonDataTo(this.skeletons);
                        this.backgroundRemovedColorStream.ProcessSkeleton(this.skeletons, skeletonFrame.Timestamp);
                        skelObj = new SkeletonLib(skeletonFrame);
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
                    if (null == this.foregroundBitmap || this.foregroundBitmap.PixelWidth != backgroundRemovedFrame.Width
                        || this.foregroundBitmap.PixelHeight != backgroundRemovedFrame.Height)
                    {
                        this.foregroundBitmap = new WriteableBitmap(backgroundRemovedFrame.Width, backgroundRemovedFrame.Height, 96.0, 96.0, PixelFormats.Bgra32, null);
                        
                    }

                    // Write the pixel data into our bitmap
                    this.foregroundBitmap.WritePixels(
                        new Int32Rect(0, 0, this.foregroundBitmap.PixelWidth, this.foregroundBitmap.PixelHeight),
                        backgroundRemovedFrame.GetRawPixelData(),
                        this.foregroundBitmap.PixelWidth * sizeof(int),
                        0);

                    // Set the image we display to point to the bitmap where we'll put the image data
                    //this.Image.Source = this.foregroundBitmap;
                    
                    if (skelObj.isEmpty() == false)
                    {

                        int stride = (int)((foregroundBitmap.PixelWidth * foregroundBitmap.Format.BitsPerPixel + 7) / 8);
                        byte[] bitmapArray = new byte[foregroundBitmap.PixelHeight * stride];
                        this.foregroundBitmap.CopyPixels(                             
                             bitmapArray, 
                             stride, 
                             0);
                        skelObj.setPixelArray(bitmapArray, stride);
                        
                        skelObj.SkeletonStart(this.foregroundBitmap, this.sensorChooser.Kinect);

                        // Set the image we display to point to the bitmap where we'll put the image data
                        this.Image.Source = skelObj.getOutputImage();
                    }
                    else
                    {
                        // Set the image we display to point to the bitmap where we'll put the image data
                        this.Image.Source = this.foregroundBitmap;
                    }
                    

                }
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

            foreach (var skel in this.skeletons)
            {
                if (null == skel)
                {
                    continue;
                }

                if (skel.TrackingState != SkeletonTrackingState.Tracked)
                {
                    continue;
                }

                if (skel.TrackingId == this.currentlyTrackedSkeletonId)
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
                this.backgroundRemovedColorStream.SetTrackedPlayer(nearestSkeleton);
                this.currentlyTrackedSkeletonId = nearestSkeleton;
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
                    if (null != this.backgroundRemovedColorStream)
                    {
                        this.backgroundRemovedColorStream.BackgroundRemovedFrameReady -= this.BackgroundRemovedFrameReadyHandler;
                        this.backgroundRemovedColorStream.Dispose();
                        this.backgroundRemovedColorStream = null;
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
                    args.NewSensor.DepthStream.Enable(DepthFormat);
                    args.NewSensor.ColorStream.Enable(ColorFormat);
                    args.NewSensor.SkeletonStream.Enable();

                    this.backgroundRemovedColorStream = new BackgroundRemovedColorStream(args.NewSensor);
                    this.backgroundRemovedColorStream.Enable(ColorFormat, DepthFormat);

                    // Allocate space to put the depth, color, and skeleton data we'll receive
                    if (null == this.skeletons)
                    {
                        this.skeletons = new Skeleton[args.NewSensor.SkeletonStream.FrameSkeletonArrayLength];
                    }

                    // Add an event handler to be called when the background removed color frame is ready, so that we can
                    // composite the image and output to the app
                    this.backgroundRemovedColorStream.BackgroundRemovedFrameReady += this.BackgroundRemovedFrameReadyHandler;

                    // Add an event handler to be called whenever there is new depth frame data
                    args.NewSensor.AllFramesReady += this.SensorAllFramesReady;

                    try
                    {
                        
                        //args.NewSensor.DepthStream.Range = this.checkBoxNearMode.IsChecked.GetValueOrDefault()
                        //                            ? DepthRange.Near
                        //                            : DepthRange.Default;
                        //args.NewSensor.SkeletonStream.EnableTrackingInNearRange = true;
                        
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
        

    }
}
