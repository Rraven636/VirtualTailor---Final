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
    class Measure
    {
        /// <summary>
        /// Skeleton to be used to measure
        /// </summary>
        Skeleton skeletonIn;

        public Measure()
        {
            this.skeletonIn = null;
        }

        public Measure(Skeleton tempSkel)
        {
            this.skeletonIn = tempSkel;
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
            return this.distanceSkelPoint(p1, p2);
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
        /*
        public double distance(float num11, float num12, float num13, float num21, float num22, float num23)
        {

        }
        */
    }
}
