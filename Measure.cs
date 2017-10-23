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
    public class Measure
    {
        /// <summary>
        /// Skeleton to be used to measure
        /// </summary>
        Skeleton _skeletonIn;

        private double _armLeftUpper;

        private double _armLeftLower;

        public Measure()
        {
            _skeletonIn = null;
            _armLeftUpper = 0;
            _armLeftLower = 0;
        }

        public Measure(Skeleton tempSkel)
        {
            _skeletonIn = tempSkel;
            _armLeftUpper = 0;
            _armLeftLower = 0;
        }

        public Measure(Measure measureObj)
        {
            _skeletonIn = measureObj.getSkeletonOut();
            _armLeftUpper = measureObj.getArmLeftUpper();
            _armLeftLower = measureObj.getArmLeftLower();
        }

        public double distanceSkelPoint(SkeletonPoint p1, SkeletonPoint p2)
        {
            var xdiff = p1.X - p2.X;
            var ydiff = p1.Y - p2.Y;
            var zdiff = p1.Z - p2.Z;
            var total = xdiff * xdiff + ydiff * ydiff + zdiff * zdiff;
            return Math.Sqrt(total);
        }

        public double distanceJoints(Joint joint1, Joint joint2)
        {
            SkeletonPoint p1 = joint1.Position;
            SkeletonPoint p2 = joint2.Position;
            return distanceSkelPoint(p1, p2);
        }

        public float perpendicularGrad(Point p1, Point p2)
        {
            var grad = ((float)(p2.Y - p1.Y)) / ((float)(p2.X - p1.X));
            var perpGrad = -1.0f / grad;
            return perpGrad;
        }

        public Point midpoint(Point p1, Point p2)
        {
            var xCo = (float)(p1.X + p2.X) / 2.0f;
            var yCo = (float)(p1.Y + p2.Y) / 2.0f;
            Point outPoint = new Point(((int)Math.Round(xCo)), ((int)Math.Round(yCo)));
            return outPoint;
        }

        public Point getNewPoint(Point midpoint, float perpGrad, int xPos)
        {
            var outY = perpGrad * ((float)(xPos - midpoint.X)) + (float)(midpoint.Y);
            var finalY = (int)Math.Round(outY);
            return new Point(xPos, finalY);
        }
        
        public void addMeasurement(SkeletonPoint skelPoint1, SkeletonPoint skelPoint2, JointType jointType1, JointType jointType2)
        {
            double measurement = distanceSkelPoint(skelPoint1, skelPoint2);
            if (jointType1.Equals(JointType.ShoulderLeft))
            {
                if (jointType2.Equals(JointType.ElbowLeft))
                {
                    _armLeftUpper = measurement;
                }
            }
            if (jointType1.Equals(JointType.ElbowLeft))
            {
                if (jointType2.Equals(JointType.WristLeft))
                {
                    _armLeftLower = measurement;
                }
            }
        }

        public double getArmLeftUpper()
        {
            return _armLeftUpper;
        }

        public double getArmLeftLower()
        {
            return _armLeftLower;
        }

        public double formatToCm(double measureIn)
        {
            double output = measureIn * 100;
            return Math.Round(output, 2);
        }

        public Skeleton getSkeletonOut()
        {
            return _skeletonIn;
        }

        public String toStringArmLeftUpper()
        {
            return "Upper Left Arm: " + formatToCm(getArmLeftUpper()) + "cm";
        }

        public String toStringArmLeftLower()
        {
            return "Lower Left Arm: " + formatToCm(getArmLeftLower()) + "cm";
        }

        public String toStringAllMeaurements()
        {
            String output = "Measurements - ";
            output += toStringArmLeftUpper() + " " + toStringArmLeftLower();
            return output;
        }

    }
}
