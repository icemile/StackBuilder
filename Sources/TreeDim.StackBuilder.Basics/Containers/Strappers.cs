﻿#region Using directives
using System.Collections.Generic;
using System.Drawing;
#endregion

namespace treeDiM.StackBuilder.Basics
{
    #region Strapper
    public class Strapper
    {
        public int Axis { get; set; }
        public double Abscissa { get; set; }
        public double Width { get; set; }
        public Color Color { get; set; }
    }
    #endregion

    #region StrapperSet
    public class StrapperSet
    {
        #region Constructors
        public StrapperSet(double[] dims) { dimensions = dims; }
        public StrapperSet(PackableBrick packable) { Packable = packable;}
        #endregion

        #region Public properties
        public PackableBrick Packable { get; private set; }
        public Color Color { get; set; }
        public double Width { get; set; }
        public int[] Number { get; set; }
        private double[] Dimensions => Packable is null ? dimensions : Packable.Dimensions;
        public IEnumerable<Strapper> Strappers
        {
            get
            {
                List<Strapper> strappers = new List<Strapper>();
                for (int dir = 0; dir < 3; ++dir)
                {
                    foreach (double d in ActualAbscissa(dir))
                        strappers.Add(new Strapper() { Abscissa = d, Axis = dir, Color = Color, Width = Width });
                }
                return strappers;
            }
        }
        #endregion

        #region Setting / getting number and space
        public bool SetEvenlySpaced(int dir, int number, double space)
        {
            if ((number-1) * space >= Dimensions[dir])
                return false;
            evenlySpaced[dir] = true;
            spaces[dir] = space;
            return true;
        }
        public void SetUnevenlySpaced(int dir)
        {
            evenlySpaced[dir] = false;
            abscissa[dir] = EvenlySpacedAbscissa(dir);
        }
        public void SetUnevenlySpaced(int dir, List<double> abs)
        {
            evenlySpaced[dir] = false;
            Number[dir] = abs.Count;
            abscissa[dir] = abs;
        }
        public void GetStrapperByDirection(int dir, ref int number, ref bool evenSpaced, ref double space, ref List<double> abs)
        {
            number = Number[dir];
            evenSpaced = evenlySpaced[dir];
            space = spaces[dir];
            abs = ActualAbscissa(dir);
        }
        public double MaximumEvenSpace(int dir)
        {
            if (Number[dir] > 1)
                return Dimensions[dir] / (Number[dir] - 1);
            else
                return 0.5 * Dimensions[dir];
        }
        public double IdealEvenSpace(int dir)
        {
            if (Number[dir] > 1)
                return Dimensions[dir] / Number[dir];
            else
                return 0.5 * Dimensions[dir]; 
        }
        #endregion

        #region Private helpers
        private List<double> ActualAbscissa(int dir)
        {
            if (evenlySpaced[dir])
                return EvenlySpacedAbscissa(dir);
            else
                return abscissa[dir];
        }

        private List<double> EvenlySpacedAbscissa(int dir)
        {
            List<double> abs = new List<double>();
            int no = numbers[dir];
            if (0 != no)
            {
                double sp = spaces[dir];
                double length = Packable.Dim(0);
                double first = (length - (no - 1) * sp) / (no + 1);

                for (int i = 0; i < no; ++i)
                    abs.Add(first + i * sp);
            }
            return abs;
        }
        #endregion

        #region Data members
        private readonly double[] dimensions = new double[3];
        private readonly int[] numbers = new int[3];
        private readonly double[] spaces = new double[3];
        private readonly bool[] evenlySpaced = new bool[3];
        private readonly List<double>[] abscissa = new List<double>[3];
        #endregion
    }
    #endregion
}
