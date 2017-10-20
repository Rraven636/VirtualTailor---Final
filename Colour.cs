using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace ColourSkel
{
    public class Colour
    {
        /// <summary>
        /// Sensor being used - Passed from main application
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Bitmap that will hold color information to be outputted
        /// </summary>
        private WriteableBitmap outputBitmap;

        /// <summary>
        /// Intermediate storage for the color data received from the camera
        /// </summary>
        private byte[] colorPixels;

        public Colour()
        {
            this.sensor = null;
        }

        public Colour(KinectSensor sensor)
        {
            this.sensor = sensor;
            startUpColour();
        }

        public Colour(Colour copy)
        {
            this.sensor = copy.getSensor();
            this.outputBitmap = copy.getImage();
        }

        /// <summary>
        /// Handles the startup initialisations for outputting the colour image
        /// </summary>
        public void startUpColour()
        {
            // Turn on the colour stream to receive colour frames from the sensor
            this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

            // Creating an array to store all the individual pixels received
            this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

            // Formats bitmap to be outputted to screen
            //  - Pixels in RGB
            //  - Dimensions according to sensor stream 
            //  - DPI = 96.0 (Standard for Windows) 
            this.outputBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

        }

        /// <summary>
        /// Prepares the output bitmap whenever a colour frame is sent to it
        /// </summary>
        /// <param name="cFrame"></param>
        public void readStream(ColorImageFrame cFrame)
        {
            // Copy data from inputted ColourImageFrame to array for pixel data
            cFrame.CopyPixelDataTo(this.colorPixels);

            //Write the copied data to the Bitmap to be outputted
            this.outputBitmap.WritePixels(
                new Int32Rect(0, 0, this.outputBitmap.PixelWidth, this.outputBitmap.PixelHeight),
                this.colorPixels,
                this.outputBitmap.PixelWidth * sizeof(int),
                0);
        }

        /// <summary>
        /// Creates an event handler for when a new colour frame is available
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    readStream(colorFrame);
                }
            }
        }

        /// <summary>
        /// Returns the output Bitmap
        /// </summary>
        /// <returns></returns>
        public WriteableBitmap getImage()
        {
            return this.outputBitmap;
        }

        /// <summary>
        /// Returns the sensor of the object
        /// </summary>
        /// <returns></returns>
        public KinectSensor getSensor()
        {
            return this.sensor;
        }

    }
}
