using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Research.Kinect.Nui;
using System.Windows.Media.Media3D;
using System.IO;

namespace BodyOrientationLib
{
    public class KinectFeatureSet : AbstractFeatureSet
    {
        public const int NumValues = 3;
        public override int NumMultiplexableItems { get { return NumValues; } }

        public double ShoulderOrientation { get; set; }
        public double RightLegToTorsoAngle { get; set; }
        public double LeftLegToTorsoAngle { get; set; }

        public KinectFeatureSet() : this(null) { }
        public KinectFeatureSet(IEnumerable<Joint> joints)
        {
            CalculateFeatures(joints);
        }

        public void ReadFromSkeletonJoints(IEnumerable<Joint> joints)
        {
            CalculateFeatures(joints);
        }

        public override double[] ExtractValues()
        {
            double[] values = new double[NumMultiplexableItems];
            values[0] = ShoulderOrientation;
            values[1] = RightLegToTorsoAngle;
            values[2] = LeftLegToTorsoAngle;
            return values;
        }

        public override void InjectValues(double[] values)
        {
            ShoulderOrientation = values[0];
            RightLegToTorsoAngle = values[1];
            LeftLegToTorsoAngle = values[2];
        }

        public override object Clone() { return base.CloneByValues<KinectFeatureSet>(); }

        private void CalculateFeatures(IEnumerable<Joint> joints)
        {
            if (joints == null)
                return;

            var vectors = joints.ToDictionary(x => x.ID, x => x.Position.ToVector3D());
            var neededJoints = new HashSet<JointID>() { JointID.ShoulderCenter, JointID.ShoulderLeft, JointID.ShoulderRight, 
                                                        JointID.HipLeft, JointID.HipRight, JointID.KneeLeft, JointID.KneeRight };

            // Check if we have all joints we need
            if (neededJoints.Any(x => !vectors.ContainsKey(x)))
                return;

            // Check if all needed joints are tracked
            if (joints.Where(x => neededJoints.Contains(x.ID)).Any(x => x.TrackingState == JointTrackingState.NotTracked))
                return;

            // Get the orientation relative to the camera
            CalculateShoulderOrientation(vectors[JointID.ShoulderLeft], vectors[JointID.ShoulderRight]);

            // Often used vectors
            var hipRight = vectors[JointID.HipRight];
            var hipLeft = vectors[JointID.HipLeft];

            // Get the angles between the torso and both legs.
            // Therefore calculate the angles between the corresponding planes.
            // The angle between planes is equal to the angle between the planes' normal vectors.
            var torsoPlaneNormal = PlaneNormal(vectors[JointID.ShoulderCenter], hipLeft, hipRight).NormalizeVector();
            var legLeftPlaneNormal = PlaneNormal(hipLeft, vectors[JointID.KneeLeft], hipRight).NormalizeVector();
            var legRightPlaneNormal = PlaneNormal(hipLeft, vectors[JointID.KneeRight], hipRight).NormalizeVector();
            RightLegToTorsoAngle = CCWAngleBetweenPlanes(torsoPlaneNormal, legRightPlaneNormal, hipLeft, hipRight);
            LeftLegToTorsoAngle = CCWAngleBetweenPlanes(torsoPlaneNormal, legLeftPlaneNormal, hipLeft, hipRight);
        }

        /// <summary>
        /// Calculates the counterclockwise angle from the upper plane to the lower plane. Both 
        /// planes must be described by 4 points, two of them describing the intersecting line,
        /// with the other two describing the distinct planes each. As an angle in 3D space can
        /// be seen clockwise and counterclockwise at the same time, only depending on the observers
        /// point of view, the two points that descibe the intersection have to be given, with
        /// one beeing the distinctive farther point (from the observers point of view) and the other
        /// beeing the nearer point.
        /// </summary>
        /// <param name="normalUpperPlane">The normal of the upper plane. Has to be normalized to a length of 1!</param>
        /// <param name="normalLowerPlane">The normal of the lower plane. Has to be normalized to a length of 1!</param>
        /// <param name="nearPoint">The nearer point from the observers point of view.</param>
        /// <param name="farPoint">The farther point from the observers point of view.</param>
        /// <returns>The angle between the planes, measured counterclockwise from the upper plane, in radians between 0 and 2*Pi.</returns>
        private double CCWAngleBetweenPlanes(Vector3D normalUpperPlane, Vector3D normalLowerPlane, Vector3D nearPoint, Vector3D farPoint)
        {
            // Calculate the dot product, used for calculating the angle via the arccos function.
            var dotprod = Vector3D.DotProduct(normalUpperPlane, normalLowerPlane);

            // Calculate the cross product. This results in a vector orthogonal to both plane 
            // normals and therefore parallel to the axis of rotation. As this vector is directed
            // differently if the angle is greater than Pi (180°), it helps to distinguish between 
            // a sharp and a bevelled angle (See right-hand-rule of cross product).
            var crossprod = Vector3D.CrossProduct(normalUpperPlane, normalLowerPlane);

            // To decide whether to take the larger or smaller angle, look in which direction the
            // cross product vector is facing: Add it to both points and look which of the sums is
            // nearer to the respective other point. This changes the sign of the angle between 0 and Pi.
            var distanceToNearPoint = (farPoint - (nearPoint + crossprod)).LengthSquared;
            var distanceToFarPoint = (nearPoint - (farPoint + crossprod)).LengthSquared;
            var sign = (distanceToNearPoint < distanceToFarPoint ? -1 : 1);

            // Now put the angle (0-Pi) and the sign together to get an angle between -Pi and Pi.
            // Finally, add Pi to get an angle between 0 and 2*Pi.
            return Math.PI + sign * Math.Acos(dotprod);
        }

        /// <summary>
        /// Returns the normal vector of a plane that is described by three points
        /// </summary>
        private Vector3D PlaneNormal(Vector3D p1, Vector3D p2, Vector3D p3)
        {
            // The cross product of two arbitrary disjunct lines inside the plane is 
            // orthogonal to both and therefore the normal of the plane.
            return Vector3D.CrossProduct(p2 - p1, p3 - p1);
        }

        private void CalculateShoulderOrientation(Vector3D shoulderLeft, Vector3D shoulderRight)
        {
            // Project the vector pointing from the left to right shoulder onto the ground
            var shoulderVector = (shoulderRight - shoulderLeft).SetToZero(NuiVectorExtensions.Component.Y);

            // Get the angle where the 2D-vector (lying on the ground) is pointing at
            ShoulderOrientation = -Math.Atan2(shoulderVector.Z, shoulderVector.X);
        }
    }
}
