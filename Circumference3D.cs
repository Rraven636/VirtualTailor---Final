using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ColourSkel
{
    class Circumference3D
    {
        private Measure[] _differentViewMeasurements;

        public const int FRONT = 0;

        public const int LEFT = 1;

        public const int BACK = 2;

        public const int RIGHT = 3;

        private double _upperLeftArmCircum, _lowerLeftArmCircum;
        private double _upperRightArmCircum, _lowerRightArmCircum;

        private double _upperLeftLegCircum, _lowerLeftLegCircum;
        private double _upperRightLegCircum, _lowerRightLegCircum;

        private double _waistCircum, _chestCircum;

        public Circumference3D()
        {
            _differentViewMeasurements = new Measure[4];
        }

        public void addViewMeasurement(Measure measureObj, int measureView)
        {
            if (measureView < FRONT || measureView > RIGHT)
            {
                return;
            }
            Measure tempMeasure = new Measure(measureObj);

            _differentViewMeasurements[measureView] = tempMeasure;
        }

        public Boolean allViewsReady()
        {
            for (int i = 0; i < _differentViewMeasurements.Length; i++)
            {
                if (_differentViewMeasurements[i] == null)
                {
                    return false;
                }
            }

            measureAllLimbs();

            return true;
        }

        public void measureAllLimbs()
        {
            measureLeftLimbs();
            measureRightLimbs();
            measureMajors();
        }

        public void measureLeftLimbs()
        {
            // Upper Left Arm
            double upperLeftAverage = getAverage(_differentViewMeasurements[FRONT].getArmLeftUpper(), _differentViewMeasurements[BACK].getArmLeftUpper());
            double upperLeftSide = _differentViewMeasurements[LEFT].getArmLeftUpper();

            _upperLeftArmCircum = measureCircum(upperLeftAverage, upperLeftSide);

            // Lower Left Arm
            double lowerLeftAverage = getAverage(_differentViewMeasurements[FRONT].getArmLeftLower(), _differentViewMeasurements[BACK].getArmLeftLower());
            double lowerLeftSide = _differentViewMeasurements[LEFT].getArmLeftLower();

            _lowerLeftArmCircum = measureCircum(lowerLeftAverage, lowerLeftSide);

            // Upper Left Leg
            upperLeftAverage = getAverage(_differentViewMeasurements[FRONT].getLegLeftUpper(), _differentViewMeasurements[BACK].getLegLeftUpper());
            upperLeftSide = _differentViewMeasurements[LEFT].getLegLeftUpper();

            _upperLeftLegCircum = measureCircum(upperLeftAverage, upperLeftSide);

            // Lower Left Leg
            lowerLeftAverage = getAverage(_differentViewMeasurements[FRONT].getLegLeftLower(), _differentViewMeasurements[BACK].getLegLeftLower());
            lowerLeftSide = _differentViewMeasurements[LEFT].getLegLeftLower();

            _lowerLeftLegCircum = measureCircum(lowerLeftAverage, lowerLeftSide);
        }

        public void measureRightLimbs()
        {
            // Upper Right Arm
            double upperRightAverage = getAverage(_differentViewMeasurements[FRONT].getArmRightUpper(), _differentViewMeasurements[BACK].getArmRightUpper());
            double upperRightSide = _differentViewMeasurements[RIGHT].getArmRightUpper();

            _upperRightArmCircum = measureCircum(upperRightAverage, upperRightSide);

            // Lower Right Arm
            double lowerRightAverage = getAverage(_differentViewMeasurements[FRONT].getArmRightLower(), _differentViewMeasurements[BACK].getArmRightLower());
            double lowerRightSide = _differentViewMeasurements[RIGHT].getArmRightLower();

            _lowerRightArmCircum = measureCircum(lowerRightAverage, lowerRightSide);

            // Upper Right Leg
            upperRightAverage = getAverage(_differentViewMeasurements[FRONT].getLegRightUpper(), _differentViewMeasurements[BACK].getLegRightUpper());
            upperRightSide = _differentViewMeasurements[RIGHT].getLegRightUpper();

            _upperRightLegCircum = measureCircum(upperRightAverage, upperRightSide);

            // Lower Right Leg
            lowerRightAverage = getAverage(_differentViewMeasurements[FRONT].getLegRightLower(), _differentViewMeasurements[BACK].getLegRightLower());
            lowerRightSide = _differentViewMeasurements[RIGHT].getLegRightLower();

            _lowerRightLegCircum = measureCircum(lowerRightAverage, lowerRightSide);
        }

        public void measureMajors()
        {
            // Chest
            double frontAndBackAverage = getAverage(_differentViewMeasurements[FRONT].getChest(), _differentViewMeasurements[BACK].getChest());
            double leftAndRightAverage = getAverage(_differentViewMeasurements[LEFT].getChest(), _differentViewMeasurements[RIGHT].getChest());

            _chestCircum = measureCircum(frontAndBackAverage, leftAndRightAverage);

            // Waist
            frontAndBackAverage = getAverage(_differentViewMeasurements[FRONT].getWaist(), _differentViewMeasurements[BACK].getWaist());
            leftAndRightAverage = getAverage(_differentViewMeasurements[LEFT].getWaist(), _differentViewMeasurements[RIGHT].getWaist());

            _waistCircum = measureCircum(frontAndBackAverage, leftAndRightAverage);
        }

        public double getAverage(double measure1, double measure2)
        {
            double average = 0;

            if (measure1 <= 0)
            {
                average = measure2;
            }
            else if (measure2 <= 0)
            {
                average = measure1;
            }
            else
            {
                average = (measure1 + measure2) / 2;
            }

            return average;
        }

        public double measureCircum(double frontReading, double sideReading)
        {
            if (frontReading <= 0 || sideReading <= 0)
            {
                return -1;
            }

            double major, minor;

            if (frontReading > sideReading)
            {
                major = frontReading;
                minor = sideReading;
            }
            else
            {
                major = sideReading;
                minor = frontReading;
            }

            double eSquareRoot = (major * major - minor * minor) / (major * major);
            double tempTerm = ((2 * (major - minor)) / Math.PI) + (minor/Math.Sqrt(2));

            double circum = 2 * Math.PI * Math.Sqrt((minor * minor) + eSquareRoot * (tempTerm * tempTerm));

            return circum;
        }

        public double getUpperLeftArmCircum()
        {
            return _upperLeftArmCircum;
        }

        public double getLowerLeftArmCircum()
        {
            return _lowerLeftArmCircum;
        }

        public double getUpperRightArmCircum()
        {
            return _upperRightArmCircum;
        }

        public double getLowerRightArmCircum()
        {
            return _lowerRightArmCircum;
        }

        public double getUpperLeftLegCircum()
        {
            return _upperLeftLegCircum;
        }

        public double getLowerLeftLegCircum()
        {
            return _lowerLeftLegCircum;
        }

        public double getUpperRightLegCircum()
        {
            return _upperRightLegCircum;
        }

        public double getLowerRightLegCircum()
        {
            return _lowerRightLegCircum;
        }

        public double getWasitCircum()
        {
            return _waistCircum;
        }

        public double getChestCircum()
        {
            return _chestCircum;
        }

        public String toStringSingleCircum(double circum)
        {
            String output = "";
            if (circum <= 0)
            {
                output = "Not enough information";
            }
            else
            {
               output = _differentViewMeasurements[0].formatToCm(circum) + "cm";
            }
            return output;
        }

        public String toStringAllCircumferences()
        {
            String output = "";

            output = "Chest: \t\t" + toStringSingleCircum(_chestCircum)
                    + "\nWaist: \t\t" + toStringSingleCircum(_waistCircum)

                    + "\nUpper Left Arm: \t" + toStringSingleCircum(_upperLeftArmCircum)
                    + "\nLower Left Arm: \t" + toStringSingleCircum(_lowerLeftArmCircum)
                    + "\nUpper Right Arm: \t" + toStringSingleCircum(_upperRightArmCircum)
                    + "\nLower Right Arm: \t" + toStringSingleCircum(_lowerRightArmCircum)

                    + "\nUpper Left Leg: \t" + toStringSingleCircum(_upperLeftLegCircum)
                    + "\nLower Left Leg: \t" + toStringSingleCircum(_lowerLeftLegCircum)
                    + "\nUpper Right Leg: \t" + toStringSingleCircum(_upperRightLegCircum)
                    + "\nLower Right Leg: \t" + toStringSingleCircum(_lowerRightLegCircum);

            return output;
        }

    }
}
