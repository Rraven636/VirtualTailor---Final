﻿using System;
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

        // Left Arm
        private double _armLeftUpper;
        private double _armLeftLower;

        // Right Arm
        private double _armRightUpper;
        private double _armRightLower;

        // Left Leg
        private double _legLeftUpper;
        private double _legLeftLower;

        // Right Leg
        private double _legRightUpper;
        private double _legRightLower;

        private double _chest;

        public Measure()
        {
            _skeletonIn = null;

            // Left arm
            _armLeftUpper = 0;
            _armLeftLower = 0;

            // Right arm
            _armRightUpper = 0;
            _armRightLower = 0;

            // Left leg
            _legLeftUpper = 0;
            _legLeftLower = 0;

            // Right leg
            _legRightUpper = 0;
            _legRightLower = 0;
            
            _chest = 0;
        }

        public Measure(Skeleton tempSkel)
        {
            _skeletonIn = tempSkel;

            // Left arm
            _armLeftUpper = 0;
            _armLeftLower = 0;

            // Right arm
            _armRightUpper = 0;
            _armRightLower = 0;

            // Left leg
            _legLeftUpper = 0;
            _legLeftLower = 0;

            // Right leg
            _legRightUpper = 0;
            _legRightLower = 0;

            _chest = 0;
        }



        public Measure(Measure measureObj)
        {
            _skeletonIn = measureObj.getSkeletonOut();

            // Left Arm
            _armLeftUpper = measureObj.getArmLeftUpper();
            _armLeftLower = measureObj.getArmLeftLower();

            // Right Arm
            _armRightUpper = measureObj.getArmRightUpper();
            _armRightLower = measureObj.getArmRightLower();

            // Left Leg
            _legLeftUpper = measureObj.getLegLeftUpper();
            _legLeftLower = measureObj.getLegLeftLower();

            // Right Leg
            _legRightUpper = measureObj.getLegRightUpper();
            _legRightLower = measureObj.getLegRightLower();

            _chest = measureObj.getChest();
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

            // Left Arm Upper Check
            if (jointType1.Equals(JointType.ShoulderLeft))
            {
                if (jointType2.Equals(JointType.ElbowLeft))
                {
                    _armLeftUpper = measurement;
                }
            }

            // Left Arm Lower Check
            if (jointType1.Equals(JointType.ElbowLeft))
            {
                if (jointType2.Equals(JointType.WristLeft))
                {
                    _armLeftLower = measurement;
                }
            }

            // Right Arm Upper Check
            if (jointType1.Equals(JointType.ShoulderRight))
            {
                if (jointType2.Equals(JointType.ElbowRight))
                {
                    _armRightUpper = measurement;
                }
            }

            // Right Arm Lower Check
            if (jointType1.Equals(JointType.ElbowRight))
            {
                if (jointType2.Equals(JointType.WristRight))
                {
                    _armRightLower = measurement;
                }
            }

            // Left Leg Upper Check
            if (jointType1.Equals(JointType.HipLeft))
            {
                if (jointType2.Equals(JointType.KneeLeft))
                {
                    _legLeftUpper = measurement;
                }
            }

            // Left Leg Lower Check
            if (jointType1.Equals(JointType.KneeLeft))
            {
                if (jointType2.Equals(JointType.AnkleLeft))
                {
                    _legLeftLower = measurement;
                }
            }

            // Right Leg Upper Check
            if (jointType1.Equals(JointType.HipRight))
            {
                if (jointType2.Equals(JointType.KneeRight))
                {
                    _legRightUpper = measurement;
                }
            }

            // Right Leg Lower Check
            if (jointType1.Equals(JointType.KneeRight))
            {
                if (jointType2.Equals(JointType.AnkleRight))
                {
                    _legRightLower = measurement;
                }
            }

            // Right Leg Lower Check
            if (jointType1.Equals(JointType.ShoulderCenter))
            {
                if (jointType2.Equals(JointType.Spine))
                {
                    _chest = measurement;
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

        public double getArmRightUpper()
        {
            return _armRightUpper;
        }

        public double getArmRightLower()
        {
            return _armRightLower;
        }

        public double getLegLeftUpper()
        {
            return _legLeftUpper;
        }

        public double getLegLeftLower()
        {
            return _legLeftLower;
        }

        public double getLegRightUpper()
        {
            return _legRightUpper;
        }

        public double getLegRightLower()
        {
            return _legRightLower;
        }

        public double getChest()
        {
            return _chest;
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

        /////////////////////// Left Arm ////////////////////////////////////////
        public String toStringArmLeftUpper()
        {
            return "Upper Left Arm: " + formatToCm(getArmLeftUpper()) + "cm";
        }

        public String toStringArmLeftLower()
        {
            return "Lower Left Arm: " + formatToCm(getArmLeftLower()) + "cm";
        }
        /////////////////////////////////////////////////////////////////////////

        /////////////////////// Right Arm ////////////////////////////////////////
        public String toStringArmRightUpper()
        {
            return "Upper Right Arm: " + formatToCm(getArmRightUpper()) + "cm";
        }

        public String toStringArmRightLower()
        {
            return "Lower Right Arm: " + formatToCm(getArmRightLower()) + "cm";
        }
        /////////////////////////////////////////////////////////////////////////

        /////////////////////// Left Leg ////////////////////////////////////////
        public String toStringLegLeftUpper()
        {
            return "Upper Left Leg: " + formatToCm(getLegLeftUpper()) + "cm";
        }

        public String toStringLegLeftLower()
        {
            return "Lower Left Leg: " + formatToCm(getLegLeftLower()) + "cm";
        }
        /////////////////////////////////////////////////////////////////////////

        /////////////////////// Right Leg ////////////////////////////////////////
        public String toStringLegRightUpper()
        {
            return "Upper Right Leg: " + formatToCm(getLegRightUpper()) + "cm";
        }

        public String toStringLegRightLower()
        {
            return "Lower Right Leg: " + formatToCm(getLegRightLower()) + "cm";
        }
        /////////////////////////////////////////////////////////////////////////

        public String toStringChest()
        {
            return "Chest: " + formatToCm(getChest()) + "cm";
        }

        public String toStringAllMeaurements()
        {
            String output = "Measurements - ";
            output += toStringArmLeftUpper() + " " + toStringArmLeftLower();
            return output;
        }

    }
}
