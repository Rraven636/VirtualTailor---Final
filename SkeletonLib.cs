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
        private float _RenderWidth = 640.0f;

        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private float _RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private double _JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private double _BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private double _ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private Brush _centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private Brush _trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private Brush _inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private Pen _trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private Pen _inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private Pen _perpLineTrackedBone = new Pen(Brushes.Red, 3);

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor _sensor;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup _drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage _imageSource;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private WriteableBitmap _colourImageSource;

        /// <summary>
        /// Skeleton to be used to measure
        /// </summary>
        private Skeleton _skeletonOut;

        /// <summary>
        /// Variable holding measurement reading
        /// </summary>
        private String _measureOut;

        /// <summary>
        /// The skeletonframe sent from a skeleton ready event
        /// </summary>
        private SkeletonFrame _skelFrame;

        private ColorImageFrame _colourFrame;

        private DepthImageFrame _depthFrame;

        private Skeleton[] _skeletons = new Skeleton[0];

        private byte[] _foregroundArray;

        private byte[][] _filteredArray;

        private int _foregroundStride;

        private DepthImagePixel[] _depthImageMeasure;

        private SkeletonPoint[] _skelPointsMeasure;

        private int _colourFormatHeight, _colourFormatWidth, _depthFormatHeight, _depthFormatWidth;

        private Measure _totalMeasure;

        private Measure _lastTotalMeasure;


        public SkeletonLib()
        {
            _sensor = null;
            _skelFrame = null;
            _RenderWidth = 640.0f;
            _RenderHeight = 480.0f;
            _JointThickness = 3;
            _BodyCenterThickness = 10;
            _ClipBoundsThickness = 10;
            _centerPointBrush = Brushes.Blue;
            _trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
            _inferredJointBrush = Brushes.Yellow;
            _trackedBonePen = new Pen(Brushes.Green, 6);
            _inferredBonePen = new Pen(Brushes.Gray, 1);
        }

        public void setColourFrame(ColorImageFrame colourFrame, int colourHeight, int colourWidth)
        {
            _colourFrame = colourFrame;
            _colourFormatHeight = colourHeight;
            _colourFormatWidth = colourWidth;
        }

        public void setDepthFrame(DepthImageFrame depthFrame, int depthHeight, int depthWidth)
        {
            _depthFrame = depthFrame;
            _depthFormatHeight = depthHeight;
            _depthFormatWidth = depthWidth;

            _depthImageMeasure = new DepthImagePixel[_depthFormatHeight * _depthFormatWidth];
            depthFrame.CopyDepthImagePixelDataTo(_depthImageMeasure);
        }

        public void setSkeletonFrame(SkeletonFrame skelFrame)
        {
            _skelFrame = skelFrame;
            using (SkeletonFrame skeletonFrame = skelFrame)
            {
                if (skeletonFrame != null)
                {
                    _skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(_skeletons);
                }
            }
        }

        /// <summary>
        /// Set the image parameter to use as background for skeleton
        /// </summary>
        /// <param name="img"></param>
        public void setColourImage(WriteableBitmap img)
        {
            _colourImageSource = img;
        }

        public void setActiveKinectSensor(KinectSensor sensorIn)
        {
            _sensor = sensorIn;
        }

        public void setPixelArray(byte[] Pixels, int strideIn)
        {
            _foregroundArray = new byte[Pixels.Length/4];
            for(int i = 3; i < Pixels.Length; i+=4)
            {
                int foregroundIndex = ((i + 1) / 4) - 1;
                if (Pixels[i] == 0)
                {
                    _foregroundArray[foregroundIndex] = 255;
                }
                else
                {
                    _foregroundArray[foregroundIndex] = 0;
                }
            }
            _foregroundStride = strideIn;
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
                    new Rect(0, _RenderHeight - _ClipBoundsThickness, _RenderWidth, _ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, _RenderWidth, _ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, _ClipBoundsThickness, _RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(_RenderWidth - _ClipBoundsThickness, 0, _ClipBoundsThickness, _RenderHeight));
            }
        }

        public void SkeletonStart (WriteableBitmap colourImg)
        {
            if (_skelFrame == null)
            {
                return;
            }
            // Create the drawing group we'll use for drawing
            _drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            _imageSource = new DrawingImage(_drawingGroup);

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
            DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Waistline
            DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.HipRight);

            // Left Arm
            DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = _trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = _inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, SkeletonPointToScreen(joint.Position), _JointThickness, _JointThickness);
                }
            }
        }

        public void populateDepthAndSkelPoints(ColorImageFrame colourFrame, DepthImageFrame depthFrame, ColorImageFormat colourFormat, DepthImageFormat depthFormat, int depthHeight, int depthWidth, int colourHeight, int colourWidth)
        {
            if (_colourFormatHeight == 0 || _colourFormatWidth == 0)
            {
                setColourFrame(colourFrame, colourHeight, colourWidth);
            }

            if (_depthFormatHeight == 0 || _depthFormatWidth == 0)
            {
                setDepthFrame(depthFrame, depthHeight, depthWidth);
            }

            _skelPointsMeasure = new SkeletonPoint[_colourFormatHeight*_colourFormatWidth];

            _sensor.CoordinateMapper.MapColorFrameToSkeletonFrame(colourFormat, depthFormat, _depthImageMeasure, _skelPointsMeasure);            
        }

        private SkeletonPoint getSkelPointFromPoint(Point imagePoint)
        {
            SkeletonPoint skelPoint = new SkeletonPoint();
            int yValue = (int)imagePoint.Y - 1;
            int xValue = (int)imagePoint.X - 1;
            if (yValue > _colourFormatHeight || xValue > _colourFormatWidth || yValue < 0 || xValue < 0)
            {
                return skelPoint;
            }
            int arrayVal = yValue * _colourFormatWidth + xValue;
            skelPoint = _skelPointsMeasure[arrayVal];
            return skelPoint;
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
            ColorImagePoint colourPoint = _sensor.CoordinateMapper.MapSkeletonPointToColorPoint(skelpoint, ColorImageFormat.RgbResolution640x480Fps30);
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
            Pen drawPen = _inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = _trackedBonePen;
            }

            Point startJointPos = SkeletonPointToScreen(joint0.Position);
            Point endJointPos = SkeletonPointToScreen(joint1.Position);
            
            //Draw waist line and measure waistline
            if (jointType0.Equals(JointType.HipLeft) && jointType1.Equals(JointType.HipRight))
            {
                DrawWaistLine(skeleton, drawingContext, startJointPos, endJointPos, jointType0, jointType1);
            }
            else
            {
                drawingContext.DrawLine(drawPen, startJointPos, endJointPos);
            }

            //Draw perpendicular lines and measure arms and legs
            if (jointType0.Equals(JointType.ElbowLeft) || jointType1.Equals(JointType.ElbowLeft) 
                || jointType0.Equals(JointType.ElbowRight) || jointType1.Equals(JointType.ElbowRight) 
                || jointType0.Equals(JointType.KneeLeft) || jointType1.Equals(JointType.KneeLeft) 
                || jointType0.Equals(JointType.KneeRight) || jointType1.Equals(JointType.KneeRight))
            {
                DrawPerpLine(skeleton, drawingContext, startJointPos, endJointPos, jointType0, jointType1);
            }

            //Draw perpendicular line and measure chest
            if (jointType0.Equals(JointType.ShoulderCenter) && jointType1.Equals(JointType.Spine))
            {
                DrawPerpLine(skeleton, drawingContext, startJointPos, endJointPos, jointType0, jointType1);
            }

            //Draw perpendicular line and measure neck
            if (jointType0.Equals(JointType.Head) && jointType1.Equals(JointType.ShoulderCenter))
            {
                DrawPerpLine(skeleton, drawingContext, startJointPos, endJointPos, jointType0, jointType1);
            }
        }

        public void DrawWaistLine(Skeleton skeleton, DrawingContext drawingContext, Point startPoint, Point endPoint, JointType jointType1, JointType jointType2)
        {
            var lineGrad = _totalMeasure.lineGrad(startPoint, endPoint);
            Point startLinePoint = getStartPoint(_totalMeasure, startPoint, lineGrad);
            Point endLinePoint = getEndPoint(_totalMeasure, endPoint, lineGrad);
            addMeasurement(_totalMeasure, startLinePoint, endLinePoint, jointType1, jointType2);
            Pen waistPen = _perpLineTrackedBone;
            drawingContext.DrawLine(waistPen, startLinePoint, endLinePoint);
        }

        /// <summary>
        /// Draws a perpendicular line to the line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start the reference line from</param>
        /// <param name="jointType1">joint to end the refernece line at</param>
        public void DrawPerpLine(Skeleton skeleton, DrawingContext drawingContext, Point startPoint, Point endPoint, JointType jointType1, JointType jointType2)
        {
            //Measure measureObj = new Measure(skeleton);
            var perpGrad = _totalMeasure.perpendicularGrad(startPoint, endPoint);
            var midPoint = _totalMeasure.midpoint(startPoint, endPoint);
            //Point startPerpPoint = measureObj.getNewPoint(midPoint, perpGrad, (int)startPoint.X);
            //Point endPerpPoint = measureObj.getNewPoint(midPoint, perpGrad, (int)endPoint.X);
            Point startPerpPoint = getStartPoint(_totalMeasure, midPoint, perpGrad);
            Point endPerpPoint = getEndPoint(_totalMeasure, midPoint, perpGrad);
            addMeasurement(_totalMeasure, startPerpPoint, endPerpPoint, jointType1, jointType2);
            Pen drawPen = _perpLineTrackedBone;
            drawingContext.DrawLine(drawPen, startPerpPoint, endPerpPoint);
        }

        public void addMeasurement(Measure measureObj, Point startPoint, Point endPoint, JointType jointType1, JointType jointType2)
        {
            SkeletonPoint skelPoint1 = getSkelPointFromPoint(startPoint);
            SkeletonPoint skelPoint2 = getSkelPointFromPoint(endPoint);
            _totalMeasure.addMeasurement(skelPoint1, skelPoint2, jointType1, jointType2);
        }

        public SkeletonPoint refineStartPoint();

        public Point getStartPoint(Measure measObj, Point midpoint, float perpGrad)
        {
            Point potentialStart = midpoint;
            int loopCount = 1;

            while (potentialStart.X > 0 && potentialStart.Y > 0 && potentialStart.X < _colourImageSource.PixelWidth && potentialStart.Y < _colourImageSource.PixelHeight)
            {
                //int xValue = (int)midpoint.X - loopCount * 10;
                int xValue = (int)midpoint.X - loopCount;
                potentialStart = measObj.getNewPoint(midpoint, perpGrad, xValue);
                byte pixelAtPoint = getPixelValue(potentialStart);
                if(pixelAtPoint == 255)
                {
                    int tempXValue = xValue;
                    Boolean check = true;
                    Point tempStart = potentialStart;
                    for (int i = 1; i <= 5; i++)
                    {
                        tempXValue--;
                        tempStart = measObj.getNewPoint(midpoint, perpGrad, tempXValue);
                        byte tempPixelAtPoint = getPixelValue(tempStart);
                        if (tempPixelAtPoint != 255)
                        {
                            check = false;
                            break;
                        }
                    }
                    if (check == true)
                    {
                        potentialStart = measObj.getNewPoint(midpoint, perpGrad, xValue + 1);
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

            while (potentialStart.X > 0 && potentialStart.Y > 0 && potentialStart.X < _colourImageSource.PixelWidth && potentialStart.Y < _colourImageSource.PixelHeight)
            {
                //int xValue = (int)midpoint.X + loopCount * 10;
                int xValue = (int)midpoint.X + loopCount;
                potentialStart = measObj.getNewPoint(midpoint, perpGrad, xValue);
                byte pixelAtPoint = getPixelValue(potentialStart);
                if (pixelAtPoint == 255)
                {
                    int tempXValue = xValue;
                    Boolean check = true;
                    Point tempStart = potentialStart;
                    for (int i = 1; i <= 5; i++)
                    {
                        tempXValue++;
                        tempStart = measObj.getNewPoint(midpoint, perpGrad, tempXValue);
                        byte tempPixelAtPoint = getPixelValue(tempStart);
                        if (tempPixelAtPoint != 255)
                        {
                            check = false;
                            break;
                        }
                    }
                    if (check == true)
                    {
                        potentialStart = measObj.getNewPoint(midpoint, perpGrad, xValue - 1);
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
            int yValue = (int)point.Y - 1;
            int xValue = (int)point.X - 1;
            if (yValue > _colourImageSource.PixelHeight || xValue > _colourImageSource.PixelWidth || yValue < 0 || xValue < 0)
            {
                return 255;
            }
            int arrayVal = yValue * _colourImageSource.PixelWidth + xValue;
            return _foregroundArray[arrayVal];
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

            using (DrawingContext dc = _drawingGroup.Open())
            {
                if(_colourImageSource != null)
                {
                    dc.DrawImage(_colourImageSource, new Rect(0.0, 0.0, _RenderWidth, _RenderHeight));
                }
                else
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, _RenderWidth, _RenderHeight));
                }                

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            DrawBonesAndJoints(skel, dc);
                            _skeletonOut = skel;
                            measureJoints(JointType.ShoulderLeft, JointType.ElbowLeft);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            _centerPointBrush,
                            null,
                            SkeletonPointToScreen(skel.Position),
                            _BodyCenterThickness,
                            _BodyCenterThickness);
                        }                        
                    }                    
                }

                // prevent drawing outside of our render area
                _drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, _RenderWidth, _RenderHeight));
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        public void processSkeletonFrame()
        {            
            using (DrawingContext dc = _drawingGroup.Open())
            {
                if (_colourImageSource != null)
                {
                    dc.DrawImage(_colourImageSource, new Rect(0.0, 0.0, _RenderWidth, _RenderHeight));
                }
                else
                {
                    // Draw a transparent background to set the render size
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, _RenderWidth, _RenderHeight));
                }

                if (_skeletons.Length != 0)
                {
                    foreach (Skeleton skel in _skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            _skeletonOut = skel;
                            _totalMeasure = new Measure(_skeletonOut);
                            DrawBonesAndJoints(skel, dc);
                            measureJoints(JointType.ShoulderLeft, JointType.ElbowLeft);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            _centerPointBrush,
                            null,
                            SkeletonPointToScreen(skel.Position),
                            _BodyCenterThickness,
                            _BodyCenterThickness);
                        }
                    }
                }

                // prevent drawing outside of our render area
                _drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, _RenderWidth, _RenderHeight));
            }

            _lastTotalMeasure = new Measure(_totalMeasure);
        }

        public DrawingImage getOutputImage()
        {
            return _imageSource;
        }

        public Skeleton getSkeletonOut()
        {
            return _skeletonOut;
        }

        public SkeletonFrame getSkeletonFrameOut()
        {
            return _skelFrame;
        }

        public void measureJoints(JointType jointType1, JointType jointType2)
        {
            Joint joint1 = _skeletonOut.Joints[jointType1];
            Joint joint2 = _skeletonOut.Joints[jointType2];

            String output = "Between: " + jointType1.ToString() + " and " + jointType2.ToString();
            Measure measureObj = new Measure(_skeletonOut);
            output += " - " + measureObj.distanceJoints(joint1, joint2);

            _measureOut = output;
        }

        public String getBodyMeasurements()
        {
            String output = "";
            if (_lastTotalMeasure != null)
            {
                output += _lastTotalMeasure.toStringArmLeftLower() + " " + _lastTotalMeasure.toStringArmLeftUpper();
            }
            else
            {
                output += "Measurements - None available yet";
            }
            return output;
        }

        public Measure getMostRecentMeasure()
        {
            return _lastTotalMeasure;
        }

        public String getMeasurements()
        {
            return _measureOut;
        }

        public Boolean isEmpty()
        {
            if (_skeletons.Length != 0)
            {
                foreach (Skeleton skel in _skeletons)
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
