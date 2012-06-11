using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Research.Kinect.Nui;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using BodyOrientationLib;

namespace BodyOrientationControlLib
{
    public class Skeleton3d
    {
        private static Dictionary<JointTrackingState, Material> _trackingStateMaterials = new Dictionary<JointTrackingState, Material>()
        {
            { JointTrackingState.Tracked, Model3DFactory.GetSurfaceMaterial(Color.FromRgb(128, 255, 128)) },
            { JointTrackingState.Inferred, Model3DFactory.GetSurfaceMaterial(Color.FromRgb(255, 255, 128)) },
            { JointTrackingState.NotTracked, Model3DFactory.GetSurfaceMaterial(Color.FromRgb(255, 128, 128)) }
        };
        private static Material defaultMaterial = _trackingStateMaterials[JointTrackingState.Tracked];

        private static Material rightShoulderIndicatorMaterial = Model3DFactory.GetSurfaceMaterial(Color.FromRgb(128, 128, 255));

        private GeometryModel3D[] _bones = new GeometryModel3D[(int)JointID.Count];

        private GeometryModel3D[] _joints = new GeometryModel3D[(int)JointID.Count];

        private Vector3D[] _jointPositions = new Vector3D[(int)JointID.Count];
        
        private Model3DGroup _environment;

        public Skeleton3d(Model3DGroup environment)
        {
            if (environment == null)
                throw new NullReferenceException("No 3d environment for skeleton given!");

            _environment = environment;
        }

        public void UpdateOrCreateJoints(JointsCollection joints)
        {
            if (joints != null)
                foreach (Joint joint in joints)
                    UpdateOrCreateJoint(joint);
        }

        public void UpdateOrCreateJoints(IEnumerable<Joint> joints)
        {
            if(joints != null)
                foreach (Joint joint in joints)
                    UpdateOrCreateJoint(joint);
        }

        public void UpdateOrCreateJoint(Joint joint)
        {
            int segments = 5;
            double jointRadius = 0.07;
            double boneRadius = 0.03;

            int jointId = (int)joint.ID;
            int connectedToJoint = SkeletonMetadata.BoneConnectionMapping[jointId];
            _jointPositions[jointId] = new Vector3D(joint.Position.X, joint.Position.Y, joint.Position.Z);

            // Create new 3d cube for the joint if not yet existing
            GeometryModel3D model;
            if (_joints[jointId] != null)
            {
                model = _joints[jointId];
            }
            else
            {
                model = Model3DFactory.CreateNormalizedSphere(defaultMaterial, segments);
                _environment.Children.Add(model);
                _joints[jointId] = model;

                if (connectedToJoint >= 0)
                {
                    GeometryModel3D cylinder = Model3DFactory.CreateNormalizedCylinder(defaultMaterial, segments);
                    _environment.Children.Add(cylinder);
                    _bones[jointId] = cylinder;
                }
            }

            // Performance improvement: not using a transformation group, but multiply 
            // matrices first and use a single MatrixTransform3D
            var matrix = new Matrix3D();
            matrix.Scale(new Vector3D(jointRadius, jointRadius, jointRadius));
            matrix.Translate(new Vector3D(joint.Position.X, joint.Position.Y, joint.Position.Z));

            // Update position and the material/color (based on current tracking state), color right shoulder blue
            if (joint.ID == JointID.ShoulderRight)
                model.Material = rightShoulderIndicatorMaterial;
            else
                model.Material = _trackingStateMaterials[joint.TrackingState];
            model.Transform = new MatrixTransform3D(matrix);
           
            if (connectedToJoint >= 0)
            {
                GeometryModel3D bone = _bones[jointId];
                Vector3D boneStart = _jointPositions[jointId];
                Vector3D boneEnd = _jointPositions[connectedToJoint];
                Vector3D boneCenter = (boneStart + boneEnd) / 2;
                Vector3D boneVector = boneEnd - boneStart;
                
                // Again, compute a single transformation matrix and apply it to each bone
                var boneMatrix = new Matrix3D();
                boneMatrix.Scale(new Vector3D(boneRadius, boneRadius, boneVector.Length));
                boneMatrix.Rotate(GetQuaternionFromVectors(new Vector3D(0, 0, 1), boneVector));
                boneMatrix.Translate(boneCenter);

                bone.Material = _trackingStateMaterials[joint.TrackingState];
                bone.Transform = new MatrixTransform3D(boneMatrix); ;
            }
        }

        private Quaternion GetQuaternionFromVectors(Vector3D origin, Vector3D destination)
        {
            Vector3D rotationAxis = Vector3D.CrossProduct(origin, destination);
            double angleInDegrees = Vector3D.AngleBetween(origin, destination);

            if (rotationAxis.X == 0 && rotationAxis.Y == 0 && rotationAxis.Z == 0)
                return Quaternion.Identity;
            else
                return new Quaternion(rotationAxis, angleInDegrees);
        }
    }
}
