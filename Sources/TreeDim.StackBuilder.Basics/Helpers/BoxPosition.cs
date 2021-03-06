﻿#region Using directives
using System;
using System.Collections.Generic;
using Sharp3D.Math.Core;
#endregion

namespace treeDiM.StackBuilder.Basics
{
    /// <summary>
    /// Box position
    /// </summary>
    [Serializable]
    public struct BoxPosition : ICloneable
    {
        #region Constructor
        public BoxPosition(Vector3D vPosition, HalfAxis.HAxis dirLength = HalfAxis.HAxis.AXIS_X_P, HalfAxis.HAxis dirWidth = HalfAxis.HAxis.AXIS_Y_P)
        {
            if (dirLength == dirWidth)
                throw new Exception("Can not create BoxPosition"); 
            Position = vPosition;
            DirectionLength = dirLength;
            DirectionWidth = dirWidth;
        }
        public BoxPosition(BoxPosition bpos)
        {
            Position = bpos.Position;
            DirectionLength = bpos.DirectionLength;
            DirectionWidth = bpos.DirectionWidth;
        }
        #endregion

        #region Public properties
        public Vector3D Position { get; set; }
        public HalfAxis.HAxis DirectionLength { get; set; }
        public HalfAxis.HAxis DirectionWidth { get; set; }
        public HalfAxis.HAxis DirectionHeight
        {
            get
            {
                if (!IsValid)
                    throw new Exception("Invalid position -> Can not compute DirectionHeight.");
                return HalfAxis.ToHalfAxis(Vector3D.CrossProduct(HalfAxis.ToVector3D(DirectionLength), HalfAxis.ToVector3D(DirectionWidth)));
            }
        }
        public bool IsValid => DirectionLength != DirectionWidth;
        public BBox3D BBox(Vector3D dimensions)
        {
            BBox3D bbox = new BBox3D();
            Vector3D vI = HalfAxis.ToVector3D(DirectionLength);
            Vector3D vJ = HalfAxis.ToVector3D(DirectionWidth);
            Vector3D vK = Vector3D.CrossProduct(vI, vJ);
            Vector3D[] pts = new Vector3D[8];
            pts[0] = Position;
            pts[1] = Position + dimensions.X * vI;
            pts[2] = Position + dimensions.Y * vJ;
            pts[3] = Position + dimensions.X * vI + dimensions.Y * vJ;
            pts[4] = Position + dimensions.Z * vK;
            pts[5] = Position + dimensions.Y * vJ + dimensions.Z * vK;
            pts[6] = Position + dimensions.Y * vJ;
            pts[7] = Position + dimensions.X * vI + dimensions.Y * vJ;
            foreach (Vector3D pt in pts)
                bbox.Extend(pt);
            return bbox;
        }
        public Vector3D Center(Vector3D dimensions)
        {
            Vector3D vI = HalfAxis.ToVector3D(DirectionLength);
            Vector3D vJ = HalfAxis.ToVector3D(DirectionWidth);
            Vector3D vK = Vector3D.CrossProduct(vI, vJ);
            return Position + (0.5 * dimensions.X * vI) + (0.5 * dimensions.Y * vJ) + (0.5 * dimensions.Z * vK);
        }
        #endregion

        #region Static properties
        public static BoxPosition Zero => new BoxPosition(Vector3D.Zero, HalfAxis.HAxis.AXIS_X_P, HalfAxis.HAxis.AXIS_Y_P);
        #endregion

        #region Translate / Rotate
        public BoxPosition Translate(HalfAxis.HAxis axis, double value)
        {
            Vector3D v = Position;
            v += value * HalfAxis.ToVector3D(axis);
            return new BoxPosition(v, DirectionLength, DirectionWidth);
        }
        public BoxPosition RotateZ90(Vector3D dim)
        {
            Vector3D centerOfRotation = Center(dim);
            return Transform(this, Transform3D.Translation(centerOfRotation) * Transform3D.RotationZ(90.0) * Transform3D.Translation(-centerOfRotation));
        }
        public BoxPosition RotateZ180(Vector3D dim)
        {
            Vector3D v = Position;
            v += dim.X * HalfAxis.ToVector3D(DirectionLength) + dim.Y * HalfAxis.ToVector3D(DirectionWidth);
            return new BoxPosition(v, HalfAxis.Opposite(DirectionLength), HalfAxis.Opposite(DirectionWidth));
        }
        #endregion

        #region Transformation
        public Transform3D Transformation
        {
            get
            {
                // build 4D matrix
                Vector3D vAxisLength = HalfAxis.ToVector3D(DirectionLength);
                Vector3D vAxisWidth = HalfAxis.ToVector3D(DirectionWidth);
                Vector3D vAxisHeight = Vector3D.CrossProduct(vAxisLength, vAxisWidth);
                Matrix4D mat = Matrix4D.Identity;
                mat.M11 = vAxisLength.X; mat.M12 = vAxisWidth.X; mat.M13 = vAxisHeight.X; mat.M14 = Position.X;
                mat.M21 = vAxisLength.Y; mat.M22 = vAxisWidth.Y; mat.M23 = vAxisHeight.Y; mat.M24 = Position.Y;
                mat.M31 = vAxisLength.Z; mat.M32 = vAxisWidth.Z; mat.M33 = vAxisHeight.Z; mat.M34 = Position.Z;
                return new Transform3D(mat);
            }
        }
        public static BoxPosition Transform(BoxPosition boxPosition, Transform3D transform)
        {
            if (!boxPosition.IsValid)
                throw new Exception("Invalid box position : can not transform");
            return new BoxPosition(
                transform.transform(boxPosition.Position)
                , HalfAxis.Transform(boxPosition.DirectionLength, transform)
                , HalfAxis.Transform(boxPosition.DirectionWidth, transform)
                );
        }
        public BoxPosition Transform(Transform3D transform)
        {
            return Transform(this, transform);
        }
        public BoxPosition Adjusted(Vector3D dimensions)
        {
            var boxPosTemp = new BoxPosition(Position, DirectionLength, DirectionWidth);
            // reverse if oriented to Z- (AXIS_Z_N)
            if (DirectionHeight == HalfAxis.HAxis.AXIS_Z_N)
            {
                if (DirectionLength == HalfAxis.HAxis.AXIS_X_P)
                {
                    boxPosTemp.DirectionWidth = HalfAxis.HAxis.AXIS_Y_P;
                    boxPosTemp.Position += new Vector3D(0.0, -dimensions.Y, -dimensions.Z);
                }
                else if (DirectionLength == HalfAxis.HAxis.AXIS_Y_P)
                {
                    boxPosTemp.DirectionWidth = HalfAxis.HAxis.AXIS_X_N;
                    boxPosTemp.Position += new Vector3D(dimensions.Y, 0.0, -dimensions.Z);
                }
                else if (DirectionLength == HalfAxis.HAxis.AXIS_X_N)
                {
                    boxPosTemp.DirectionLength = HalfAxis.HAxis.AXIS_X_P;
                    boxPosTemp.Position += new Vector3D(-dimensions.X, 0.0, -dimensions.Z);
                }
                else if (DirectionLength == HalfAxis.HAxis.AXIS_Y_N)
                {
                    boxPosTemp.DirectionWidth = HalfAxis.HAxis.AXIS_X_P;
                    boxPosTemp.Position += new Vector3D(-dimensions.Y, 0.0, -dimensions.Z);
                }
            }
            return boxPosTemp;
        }
        public bool IsPointInside(Vector3D dim, Vector3D pt)
        {
            var bbox = BBox(dim);
            var ptMin = bbox.PtMin;
            var ptMax = bbox.PtMax;
            return (ptMin.X <= pt.X) && (pt.X <= ptMax.X)
                && (ptMin.Y <= pt.Y) && (pt.Y <= ptMax.Y)
                && (ptMin.Z <= pt.Z) && (pt.Z <= ptMax.Z);
        }
        public Vector3D[] TopFacePoints(Vector3D dim)
        {
            Vector3D[] points = Points(dim);
            int[,] faceIndexes = {
                { 3, 0, 4, 7},
                { 1, 2, 6, 5},
                { 0, 1, 5, 4},
                { 2, 3, 7, 6},
                { 3, 2, 1, 0},
                { 4, 5, 6, 7}
            };
            Vector3D[] vFace = new Vector3D[4];
            for (int i = 0; i < 6; ++i)
            {
                
                for (int j = 0; j < 4; ++j)
                    vFace[j] = points[faceIndexes[i, j]];
                Vector3D faceNormal = Vector3D.CrossProduct(vFace[1] - vFace[0], vFace[2] - vFace[0]);
                faceNormal.Normalize();
                if (Math.Abs(Vector3D.DotProduct(faceNormal, Vector3D.ZAxis) - 1) < MathFunctions.EpsilonD)
                    return vFace;
            }
            throw new Exception("No top face found?");
        }
        private Vector3D[] Points(Vector3D dim)
        {
            Vector3D position = Position;
            Vector3D vI = HalfAxis.ToVector3D(DirectionLength);
            Vector3D vJ = HalfAxis.ToVector3D(DirectionWidth);
            Vector3D vK = Vector3D.CrossProduct(vI, vJ);

            var points = new Vector3D[8];
            points[0] = position;
            points[1] = position + dim[0] * vI;
            points[2] = position + dim[0] * vI + dim[1] * vJ;
            points[3] = position + dim[1] * vJ;

            points[4] = position + dim[2] * vK;
            points[5] = position + dim[2] * vK + dim[0] * vI;
            points[6] = position + dim[2] * vK + dim[0] * vI + dim[1] * vJ;
            points[7] = position + dim[2] * vK + dim[1] * vJ;

            return points;
        }
        public void MinMax2D(double boxLength, double boxWidth, out Vector2D vMin, out Vector2D vMax)
        {
            Vector3D[] pts = new Vector3D[4];
            pts[0] = new Vector3D(Position.X, Position.Y, 0.0);
            pts[1] = new Vector3D(Position.X, Position.Y, 0.0) + HalfAxis.ToVector3D(DirectionLength) * boxLength;
            pts[2] = new Vector3D(Position.X, Position.Y, 0.0) + HalfAxis.ToVector3D(DirectionWidth) * boxWidth;
            pts[3] = new Vector3D(Position.X, Position.Y, 0.0) + HalfAxis.ToVector3D(DirectionLength) * boxLength + HalfAxis.ToVector3D(DirectionWidth) * boxWidth;

            vMin = new Vector2D(double.MaxValue, double.MaxValue);
            vMax = new Vector2D(double.MinValue, double.MinValue);
            foreach (Vector3D v in pts)
            {
                vMin.X = Math.Min(v.X, vMin.X);
                vMin.Y = Math.Min(v.Y, vMin.Y);
                vMax.X = Math.Max(v.X, vMax.X);
                vMax.Y = Math.Max(v.Y, vMax.Y);
            }
        }
        #endregion

        #region Static members

        public static bool Intersect(BoxPosition p1, BoxPosition p2, double boxLength, double boxWidth)
        {
            Vector2D v1Min, v1Max, v2Min, v2Max;
            p1.MinMax2D(boxLength, boxWidth, out v1Min, out v1Max);
            p2.MinMax2D(boxLength, boxWidth, out v2Min, out v2Max);
            if (v1Max.X <= v2Min.X || v2Max.X <= v1Min.X || v1Max.Y <= v2Min.Y || v2Max.Y <= v1Min.Y)
                return false;
            return true;
        }

        /// <summary>
        /// This method will be used to build 
        /// </summary>
        /// <param name="pos">Position of left/front/down corner of box</param>
        /// <param name="dimOriented">Projections of box on axes X/Y/Z</param>
        /// <param name="dimOriginal">Box length, width, height</param>
        /// <returns></returns>
        public static BoxPosition FromPositionDimension(Vector3D pos, Vector3D dimOriented, Vector3D dimOriginal)
        {
            BoxPositionIndexed bpi = BoxPositionIndexed.Zero;
            // search for length (dimOriginal[0])
            if (MostlyEqual(dimOriented[0], dimOriginal[0])) 
            {
                // L W H
                if (MostlyEqual(dimOriented[1], dimOriginal[1]))
                    bpi = new BoxPositionIndexed(pos, 1);
                // L H W
                else if (MostlyEqual(dimOriented[1], dimOriginal[2]))
                    bpi = new BoxPositionIndexed(pos, 5);
            }
            else if (MostlyEqual(dimOriented[0], dimOriginal[1]))
            {
                // W L H
                if (MostlyEqual(dimOriented[1], dimOriginal[0]))
                    bpi = new BoxPositionIndexed(pos, 2);
                // W H L
                else if (MostlyEqual(dimOriented[1], dimOriginal[2]))
                    bpi = new BoxPositionIndexed(pos, 3);
            }
            else if (MostlyEqual(dimOriented[0], dimOriginal[2]))
            {
                // H L W
                if (MostlyEqual(dimOriented[1], dimOriginal[0]))
                    bpi = new BoxPositionIndexed(pos, 6);
                // H W L
                else if (MostlyEqual(dimOriented[1], dimOriginal[1]))
                    bpi = new BoxPositionIndexed(pos, 4);
            }
            return bpi.ToBoxPosition(dimOriginal);
        }
        private static bool MostlyEqual(double val0, double val1) => Math.Abs(val1 - val0) < 1.0e-03;
        public static BoxPosition Parse(string s)
        {
            string[] sArray = s.Split('|');
            var v = Vector3D.Parse(sArray[0]);
            string sOrientation = sArray[1];
            sOrientation = sOrientation.Trim();
            sOrientation = sOrientation.TrimStart('(');
            sOrientation = sOrientation.TrimEnd(')');
            string[] vOrientation = sOrientation.Split(',');
            return new BoxPosition(v, HalfAxis.Parse(vOrientation[0]), HalfAxis.Parse(vOrientation[1]));
        }
        #endregion

        #region Object method overrides
        public override string ToString() => $"{Position} | ({HalfAxis.ToString(DirectionLength)},{HalfAxis.ToString(DirectionWidth)})";
        public object Clone() => new BoxPosition(Position, DirectionLength, DirectionWidth);
        public override bool Equals(object obj)
        {
            BoxPosition bPos = (BoxPosition)obj;
            return Position.Equals(bPos.Position)
                && bPos.DirectionLength.Equals(DirectionLength)
                && bPos.DirectionWidth.Equals(DirectionWidth); 
        }
        public override int GetHashCode()
        {
            return Position.GetHashCode() ^ DirectionLength.GetHashCode() ^ DirectionWidth.GetHashCode();
        }
        public static bool operator ==(BoxPosition left, BoxPosition right) { return left.Equals(right); }
        public static bool operator !=(BoxPosition left, BoxPosition right) { return !(left == right); }
        #endregion
    }

    public struct BoxPositionIndexed
    {
        #region Constructor
        public BoxPositionIndexed(Vector3D vPosition, int orientation)
        {
            if (orientation < 1 || orientation > 6)
                throw new Exception($"BoxPositionIndexed : cannot have orientation = {orientation}");

            Position = vPosition;
            Orientation = orientation;
        }
        #endregion

        #region Public properties
        public Vector3D Position { get; set; }
        public int Orientation { get; set; }
        public BoxPosition ToBoxPosition(Vector3D dimensions)
        {
            // 1 -> L W H
            // 2 -> W L H
            // 3 -> W H L
            // 4 -> H W L
            // 5 -> L H W
            // 6 -> H L W
            switch (Orientation)
            {
                case 1: return new BoxPosition(Position, HalfAxis.HAxis.AXIS_X_P, HalfAxis.HAxis.AXIS_Y_P);
                case 2: return new BoxPosition(Position + dimensions.Y * Vector3D.XAxis, HalfAxis.HAxis.AXIS_Y_P, HalfAxis.HAxis.AXIS_X_N);
                case 3: return new BoxPosition(Position, HalfAxis.HAxis.AXIS_Z_P, HalfAxis.HAxis.AXIS_X_P);
                case 4: return new BoxPosition(Position + dimensions.Y * Vector3D.YAxis, HalfAxis.HAxis.AXIS_Z_P, HalfAxis.HAxis.AXIS_Y_N);
                case 5: return new BoxPosition(Position + dimensions.Z * Vector3D.YAxis, HalfAxis.HAxis.AXIS_X_P, HalfAxis.HAxis.AXIS_Z_P);
                case 6: return new BoxPosition(Position, HalfAxis.HAxis.AXIS_Y_P, HalfAxis.HAxis.AXIS_Z_P);
                default: throw new Exception("BoxPositionIndexed : Invalid orientation!");
            }
        }
        #endregion

        #region Static members
        public static BoxPositionIndexed Zero = new BoxPositionIndexed(Vector3D.Zero, 1);
        #endregion

        #region Object method override
        public override string ToString() => $"{Position} | {Orientation}";
        #endregion
    }

    public class BoxInteraction
    {
        public static bool BoxIsInside(BoxPosition bPos, Vector3D dim, Vector2D ptMin, Vector2D ptMax)
        {
            var bbox = bPos.BBox(dim);
            return ptMin.X <= bbox.PtMin.X && ptMin.Y <= bbox.PtMin.Y
                && bbox.PtMax.X <= ptMax.X && bbox.PtMax.Y <= ptMax.Y;
        }
        public static bool BoxCanMoveInside(BoxPosition bPos, Vector3D dim, Vector2D ptMin, Vector2D ptMax, HalfAxis.HAxis axis)
        {
            var bbox = bPos.BBox(dim);
            switch (axis)
            {
                case HalfAxis.HAxis.AXIS_X_N: return bbox.PtMin.X > ptMin.X;
                case HalfAxis.HAxis.AXIS_X_P: return bbox.PtMax.X < ptMax.X;
                case HalfAxis.HAxis.AXIS_Y_N: return bbox.PtMin.Y > ptMin.Y;
                case HalfAxis.HAxis.AXIS_Y_P: return bbox.PtMax.Y < ptMax.Y;
                default: return false;
            }
        }
        public static bool BoxesAreInside(List<BoxPosition> list, Vector3D dim, Vector2D ptMin, Vector2D ptMax)
        {
            foreach (var bpos in list)
            {
                if (!BoxIsInside(bpos, dim, ptMin, ptMax))
                    return false;
            }
            return true;
        }
        public static bool PointIsInside(BoxPosition bPos, Vector3D dim, Vector2D pt)
        {
            var bbox = bPos.BBox(dim);
            return pt.X >= bbox.PtMin.X && pt.X <= bbox.PtMax.X
                && pt.Y >= bbox.PtMin.Y && pt.Y <= bbox.PtMax.Y;
        }
        public static int SelectedPosition(List<BoxPosition> list, Vector3D dim, Vector2D pt)
        {
            for (int i = 0; i < list.Count; ++i)
                if (PointIsInside(list[i], dim, pt))
                    return i;
            return -1;
        }
        public static bool HaveIntersection(List<BoxPosition> list, Vector3D dim, int selectedIndex, BoxPosition bPosition)
        {
            for (int i = 0; i < list.Count; ++i)
            {
                if (i != selectedIndex && HaveIntersection(list[i], dim, bPosition, dim))
                    return true;
            }
            return false;
        }
        public static bool HaveIntersection(BoxPosition bPos1, Vector3D dim1, BoxPosition bPos2, Vector3D dim2)
        {
            var bbox1 = bPos1.BBox(dim1);
            var bbox2 = bPos2.BBox(dim2);
            Vector3D pt1 = bbox1.PtMin, pt2 = bbox1.PtMax;
            Vector3D pt3 = bbox2.PtMin, pt4 = bbox2.PtMax;

            Vector3D pt5 = new Vector3D(Math.Max(pt1.X, pt3.X), Math.Max(pt1.Y, pt3.Y), Math.Max(pt1.Z, pt3.Z));
            Vector3D pt6 = new Vector3D(Math.Min(pt2.X, pt4.X), Math.Min(pt2.Y, pt4.Y), Math.Min(pt2.Z, pt4.Z));

            // no intersections ?
            if ((pt5.X - pt6.X > -EPSILON)
                || (pt5.Y - pt6.Y > -EPSILON)
                || (pt5.Z - pt6.Z > -EPSILON))
                return false;
            else
                return true;
        }
        public static bool MinDistance(List<BoxPosition> list, Vector3D dim, int selectedIndex, HalfAxis.HAxis axis, ref double distance)
        {
            if (selectedIndex < 0 || selectedIndex > list.Count - 1)
                return false;
            var bPos = list[selectedIndex];
            distance = double.MaxValue;
            bool found = false;
            for (int i = 0; i < list.Count; ++i)
            {
                if (i == selectedIndex)
                    continue;
                double dTemp = double.MaxValue;
                if (MinDistance(bPos, dim, list[i], dim, axis, ref dTemp))
                {
                    found = true;
                    distance = Math.Min(distance, dTemp);
                }
            }
            if (!found) distance = 0.0;
            return found;
        }
        public static bool MinDistance(
            BoxPosition bPos1, Vector3D dim1
            , BoxPosition bPos2, Vector3D dim2
            , HalfAxis.HAxis axis
            , ref double distance)
        {
            var bbox1 = bPos1.BBox(dim1);
            var bbox2 = bPos2.BBox(dim2);
            Vector3D pt1 = bbox1.PtMin, pt2 = bbox1.PtMax;
            Vector3D pt3 = bbox2.PtMin, pt4 = bbox2.PtMax;

            Vector3D pt5 = new Vector3D(Math.Max(pt1.X, pt3.X), Math.Max(pt1.Y, pt3.Y), Math.Max(pt1.Z, pt3.Z));
            Vector3D pt6 = new Vector3D(Math.Min(pt2.X, pt4.X), Math.Min(pt2.Y, pt4.Y), Math.Min(pt2.Z, pt4.Z));

            if (axis == HalfAxis.HAxis.AXIS_X_N || axis == HalfAxis.HAxis.AXIS_X_P)
            {
                if ((pt5.Y - pt6.Y > -EPSILON) || (pt5.Z - pt6.Z > -EPSILON))
                    return false;
                if (axis == HalfAxis.HAxis.AXIS_X_N)
                {
                    distance = pt1.X - pt4.X;
                    return distance >= -EPSILON;
                }
                else if (axis == HalfAxis.HAxis.AXIS_X_P)
                {
                    distance = pt3.X - pt2.X;
                    return distance >= -EPSILON;
                }
            }
            else if (axis == HalfAxis.HAxis.AXIS_Y_N || axis == HalfAxis.HAxis.AXIS_Y_P)
            {
                if ((pt5.X - pt6.X > -EPSILON) || (pt5.Z - pt6.Z > -EPSILON))
                    return false;
                if (axis == HalfAxis.HAxis.AXIS_Y_N)
                {
                    distance = pt1.Y - pt4.Y;
                    return distance >= -EPSILON;
                }
                else if (axis == HalfAxis.HAxis.AXIS_Y_P)
                {
                    distance = pt3.Y - pt2.Y;
                    return distance >= -EPSILON;
                }
            }
            else if (axis == HalfAxis.HAxis.AXIS_Z_N || axis == HalfAxis.HAxis.AXIS_Z_P)
            {
                if ((pt5.X - pt6.X > -EPSILON) || (pt5.Y - pt6.Z > -EPSILON))
                    return false;
                if (axis == HalfAxis.HAxis.AXIS_Z_N)
                {
                    distance = pt1.Z - pt4.Z;
                    return distance >= -EPSILON;
                }
                else if (axis == HalfAxis.HAxis.AXIS_Z_P)
                {
                    distance = pt3.Z - pt2.Z;
                    return distance >= -EPSILON;
                }
            }
            distance = 0.0;
            return false;
        }
        static readonly double EPSILON = 1.0E-03;
    }
}
