using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using Microsoft.Research.Kinect.Nui;

namespace BodyOrientationLib
{
    public static class NuiVectorExtensions
    {
        public static Vector3D ToVector3D(this Vector vector)
        {
            if(vector.W == 1)
                return new Vector3D(vector.X, vector.Y, vector.Z);
            else
                return new Vector3D(vector.X / vector.W, vector.Y / vector.W, vector.Z / vector.W);
        }

        public static Vector ToNuiVector(this Vector3D vector)
        {
            return new Vector() { W = 1, X = (float)vector.X, Y = (float)vector.Y, Z = (float)vector.Z };
        }

        public static Vector3D NormalizeVector(this Vector3D vector)
        {
            vector.Normalize();
            return vector;
        }

        public enum Component
        {
            X,
            Y,
            Z
        }

        public static Vector3D SetToZero(this Vector3D vector, Component component)
        {
            if (component == Component.X)
                return new Vector3D(0, vector.Y, vector.Z);
            else if (component == Component.Y)
                return new Vector3D(vector.X, 0, vector.Z);
            else
                return new Vector3D(vector.X, vector.Y, 0);
        }

        public static double ToRadians(this double degrees)
        {
            return degrees / 180 * Math.PI;
        }

        public static double ToDegrees(this double radians)
        {
            return radians / Math.PI * 180;
        }

        public static Vector3D ProjectToGround(this Vector3D vector)
        {
            return vector.SetToZero(Component.Z);
        }

        public static double AngleOnGround(this Vector3D vector)
        {
            return Math.Atan2(vector.Y, vector.X);
        }
    }
}
