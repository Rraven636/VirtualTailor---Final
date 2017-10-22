using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace ColourSkel
{
    public class SkeletonLib
    {        
        /// <summary>
        /// Width of output drawing
        /// </summary>
        private float RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private Pen perpLineTrackedBone = new Pen(Brushes.Red, 3);

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private WriteableBitmap colourImageSource;

        /// <summary>
        /// Skeleton to be used to measure
        /// </summary>
        private Skeleton skeletonOut;

        /// <summary>
        /// Variable holding measurement reading
        /// </summary>
        private String measureOut;

        /// <summary>
        /// The skeletonframe sent from a skeleton ready event
        /// </summary>
        private SkeletonFrame skelFrame;

        private Skeleton[] skeletons = new Skeleton[0];

        private byte[] foregroundArray;

        private int foregroundStride;

        public SkeletonLib()
        {
            this.RenderWidth = 0;
            this.RenderHeight = 0;
            this.JointThickness = 0;
            this.BodyCenterThickness = 0;
            this.ClipBoundsThickness = 0;
            this.centerPointBrush = Brushes.Blue;
            this.trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
            this.inferredJointBrush = Brushes.Yellow;
            this.trackedBonePen = new Pen(Brushes.Green, 6);
            this.inferredBonePen = new Pen(Brushes.Gray, 1);
            this.sensor = null;
            this.skelFrame = null;
        }

        public SkeletonLib(KinectSensor sensor)
        {
            this.sensor = sensor;
            this.skelFrame = null;
            this.RenderWidth = 640.0f;
            this.RenderHeight = 480.0f;
            this.JointThickness = 3;
            this.BodyCenterThickness = 10;
            this.ClipBoundsThickness = 10;
            this.centerPointBrush = Brushes.Blue;
            this.trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
            this.inferredJointBrush = Brushes.Yellow;
            this.trackedBonePen = new Pen(Brushes.Green, 6);
            this.inferredBonePen = new Pen(Brushes.Gray, 1);
        }

        public SkeletonLib(SkeletonFrame skelFrame)
        {
            this.skelFrame = skelFrame;
            this.sensor = null;
            this.RenderWidth = 640.0f;
            this.RenderHeight = 480.0f;
            this.JointThickness = 3;
            this.BodyCenterThickness = 10;
            this.ClipBoundsThickness = 10;
            this.centerPointBrush = Brushes.Blue;
            this.trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
            this.inferredJointBrush = Brushes.Yellow;
            this.trackedBonePen = new Pen(Brushes.Green, 6);
            this.inferredBonePen = new Pen(Brushes.Gray, 1);

            using (SkeletonFrame skeletonFrame = skelFrame)
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }
        }

        /// <summary>
        /// Set the image parameter to use as background for skeleton
        /// </summary>
        /// <param name="img"></param>
        public void setColourImage(WriteableBitmap img)
        {
            this.colourImageSource = img;
        }

        public void setActiveKinectSensor(KinectSensor sensorIn)
        {
            this.sensor = sensorIn;
        }

        public void setPixelArray(byte[] Pixels, int strideIn)
        {
            this.foregroundArray = new byte[Pixels.Length];
            for(int i = 0; i < Pixels.Length; i++)
            {
                if (Pixels[i] == 0)
                {
                    int previousZero = i - 4;
                    int temp = i-1;
                    if (previousZero < 0)
                    {
                        while(temp >= 0)
                        {
                            foregroundArray[temp] = 0;
                            temp--;
                        }
                    }
                    else
                    {
                        if (Pixels[previousZero] == 0)
                        {
                            while (temp > previousZero)
                            {
                                foregroundArray[temp] = 0;
                                temp--;
                            }
                        }
                    }
                }
                else
                {
                    foregroundArray[i] = Pixels[i];
                }
            }
            this.foregroundStride = strideIn;
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        public void SkeletonStart()
        {
            if (sensor != null)
            {
                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();
            }
            else
            {
                return;
            }

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);
        }

        public void SkeletonStart (WriteableBitmap colourImg, KinectSensor sensorIn)
        {
            if (skelFrame == null)
            {
                return;
            }
            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            setActiveKinectSensor(sensorIn);

            // Set the input image to the passed in image parameter
            setColourImage(colourImg);

            // Run the code normally in the SkeletonFrameReady event 
            processSkeletonFrame();
        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            ColorImagePoint colourPoint = this.sensor.CoordinateMapper.MapSkeletonPointToColorPoint(skelpoint, ColorImageFormat.RgbResolution640x480Fps30);
            //DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(colourPoint.X, colourPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            Point startJointPos = this.SkeletonPointToScreen(joint0.Position);
            Point endJointPos = this.SkeletonPointToScreen(joint1.Position);
            drawingContext.DrawLine(drawPen, startJointPos, endJointPos);
            
            if(jointType0.Equals(JointType.ShoulderLeft) || jointType1.Equals(JointType.ShoulderLeft))
            {
                DrawPerpLine(skeleton, drawingContext, startJointPos, endJointPos);
            }
            
            //DrawPerpLine(skeleton, drawingContext, startJointPos, endJointPos);
        }

        /// <summary>
        /// Draws a perpendicular line to the line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start the reference line from</param>
        /// <param name="jointType1">joint to end the refernece line at</param>
        public void DrawPerpLine(Skeleton skeleton, DrawingContext drawingContext, Point startPoint, Point endPoint)
        {
            Measure measureObj = new Measure(skeleton);
            var perpGrad = measureObj.perpendicularGrad(startPoint, endPoint);
            var midPoint = measureObj.midpoint(startPoint, endPoint);
            //Point startPerpPoint = measureObj.getNewPoint(midPoint, perpGrad, (int)startPoint.X);
            //Point endPerpPoint = measureObj.getNewPoint(midPoint, perpGrad, (int)endPoint.X);
            Point startPerpPoint = getStartPoint(measureObj, midPoint, perpGrad);
            Point endPerpPoint = getEndPoint(measureObj, midPoint, perpGrad);
            Pen drawPen = this.perpLineTrackedBone;
            drawingContext.DrawLine(drawPen, startPerpPoint, endPerpPoint);
        }

        public Point getStartPoint(Measure measObj, Point midpoint, float perpGrad)
        {
            Point potentialStart = midpoint;
            int loopCount = 1;

            while (potentialStart.X > 0 && potentialStart.Y > 0 && potentialStart.X < colourImageSource.PixelWidth && potentialStart.Y < colourImageSource.PixelHeight)
            {
                //int xValue = (int)midpoint.X - loopCount * 10;
                int xValue = (int)midpoint.X - loopCount;
                potentialStart = measObj.getNewPoint(midpoint, perpGrad, xValue);
                byte pixelAtPoint = getPixelValue(potentialStart);
                if(pixelAtPoint == 0)
                {
                    int tempXValue = xValue;
                    Boolean check = true;
                    Point tempStart = potentialStart;
                    for (int i = 1; i <= 10; i++)
                    {
                        tempXValue--;
                        tempStart = measObj.getNewPoint(midpoint, perpGrad, tempXValue);
                        byte tempPixelAtPoint = getPixelValue(tempStart);
                        if (tempPixelAtPoint != 0)
                        {
                            check = false;
                            break;
                        }
                    }
                    if (check == true)
                    {
                        potentialStart = tempStart;
                        break;
                    }                    
                }
                loopCount++;
            }

            Point fineStart = potentialStart;
            /*
            int loopCount2 = 1;
            while (potentialStart.X > 0 && potentialStart.Y > 0 && potentialStart.X < colourImageSource.PixelWidth && potentialStart.Y < colourImageSource.PixelHeight)
            {
                int xValue = (int)potentialStart.X + loopCount2;
                fineStart = measObj.getNewPoint(midpoint, perpGrad, xValue);
                byte pixelAtPoint = getPixelValue(potentialStart);
                if (pixelAtPoint != 0)
                {
                    break;
                }
                loopCount2++;
            }
            */

            return fineStart;
        }

        public Point getEndPoint(Measure measObj, Point midpoint, float perpGrad)
        {
            Point potentialStart = midpoint;
            int loopCount = 1;

            while (potentialStart.X > 0 && potentialStart.Y > 0 && potentialStart.X < colourImageSource.PixelWidth && potentialStart.Y < colourImageSource.PixelHeight)
            {
                //int xValue = (int)midpoint.X + loopCount * 10;
                int xValue = (int)midpoint.X + loopCount;
                potentialStart = measObj.getNewPoint(midpoint, perpGrad, xValue);
                byte pixelAtPoint = getPixelValue(potentialStart);
                if (pixelAtPoint == 0)
                {
                    int tempXValue = xValue;
                    Boolean check = true;
                    Point tempStart = potentialStart;
                    for (int i = 1; i <= 10; i++)
                    {
                        tempXValue++;
                        tempStart = measObj.getNewPoint(midpoint, perpGrad, tempXValue);
                        byte tempPixelAtPoint = getPixelValue(tempStart);
                        if (tempPixelAtPoint != 0)
                        {
                            check = false;
                            break;
                        }
                    }
                    if (check == true)
                    {
                        potentialStart = tempStart;
                        break;
                    }
                }
                loopCount++;
            }

            Point fineStart = potentialStart;
            /*
            int loopCount2 = 1;
            while (potentialStart.X > 0 && potentialStart.Y > 0 && potentialStart.X < colourImageSource.PixelWidth && potentialStart.Y < colourImageSource.PixelHeight)
            {
                int xValue = (int)potentialStart.X - loopCount2;
                fineStart = measObj.getNewPoint(midpoint, perpGrad, xValue);
                byte pixelAtPoint = getPixelValue(potentialStart);
                if (pixelAtPoint != 0)
                {
                    break;
                }
                loopCount2++;
            }
            */

            return fineStart;
        }

        public byte getPixelValue(Point point)
        {
            int yValue = (int)point.Y;
            int xValue = (int)point.X;
            if (yValue > colourImageSource.PixelHeight || xValue > colourImageSource.PixelWidth || yValue < 0 || xValue < 0)
            {
                return 0;
            }
            int arrayVal = (int)point.Y * this.foregroundStride + (int)point.X;
            return this.foregroundArray[arrayVal];
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        public void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (DrawingContext dc = this.drawingGroup.Open())
            {
                if(this.colourImageSource != null)
                {
                    dc.DrawImage(this.colourImageSource, new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                }
                else
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                }                

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                            this.skeletonOut = skel;
                            this.measureJoints(JointType.ShoulderLeft, JointType.ElbowLeft);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }                        
                    }                    
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        public void processSkeletonFrame()
        {            
            using (DrawingContext dc = this.drawingGroup.Open())
            {
                if (this.colourImageSource != null)
                {
                    dc.DrawImage(this.colourImageSource, new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                }
                else
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                }

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                            this.skeletonOut = skel;
                            this.measureJoints(JointType.ShoulderLeft, JointType.ElbowLeft);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        public DrawingImage getOutputImage()
        {
            return this.imageSource;
        }

        public Skeleton getSkeletonOut()
        {
            return this.skeletonOut;
        }

        public SkeletonFrame getSkeletonFrameOut()
        {
            return this.skelFrame;
        }

        public void measureJoints(JointType jointType1, JointType jointType2)
        {
            Joint joint1 = this.skeletonOut.Joints[jointType1];
            Joint joint2 = this.skeletonOut.Joints[jointType2];

            String output = "Between: " + jointType1.ToString() + " and " + jointType2.ToString();
            Measure measureObj = new Measure(this.skeletonOut);
            output += " - " + measureObj.distanceJoints(joint1, joint2);

            this.measureOut = output;
        }

        public String getMeasurements()
        {
            return this.measureOut;
        }

        public Boolean isEmpty()
        {
            if (skeletons.Length != 0)
            {
                foreach (Skeleton skel in skeletons)
                {
                    if (skel.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
