using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Research.Kinect.Nui;

namespace BodyOrientationLib
{
    public class KinectRawFeatureSet : AbstractFeatureSet
    {
        public const int NumValues = (int)JointID.Count * 3;
        public override int NumMultiplexableItems { get { return NumValues; } }

        public IEnumerable<Joint> SkeletonJoints { get; set; }

        public KinectRawFeatureSet()
        {
            SkeletonJoints = null;
        }

        public KinectRawFeatureSet(JointsCollection joints)
        {
            SkeletonJoints = joints.OfType<Joint>();
        }

        public KinectRawFeatureSet(IEnumerable<Joint> joints)
        {
            SkeletonJoints = joints;
        }

        public override void WriteToRecorder(BinaryWriter writer)
        {
            writer.Write(SkeletonJoints);
        }

        public override void ReadFromRecorder(BinaryReader reader)
        {
            SkeletonJoints = reader.ReadJoints().ToList();
        }

        public override double[] ExtractValues()
        {
            double[] values = new double[NumMultiplexableItems];

            int index = 0;
            foreach (Joint joint in SkeletonJoints)
            {
                if (joint.Position.W == 1d)
                {
                    values[index++] = joint.Position.X;
                    values[index++] = joint.Position.Y;
                    values[index++] = joint.Position.Z;
                }
                else
                {
                    values[index++] = joint.Position.X / joint.Position.W;
                    values[index++] = joint.Position.Y / joint.Position.W;
                    values[index++] = joint.Position.Z / joint.Position.W;
                }
            }

            return values;
        }

        public override void InjectValues(double[] values)
        {
            int index = 0;
            foreach (Joint joint in SkeletonJoints)
            {
                var j = joint;
                j.Position = new Vector()
                {
                    W = 1f,
                    X = (float)values[index++],
                    Y = (float)values[index++],
                    Z = (float)values[index++]
                };
            }
        }

        public override object Clone()
        {
            // Deep copy all Joints
            var newObj = new KinectRawFeatureSet();
            var newList = new List<Joint>(this.SkeletonJoints.Count());
            foreach (Joint joint in this.SkeletonJoints)
            {
                newList.Add(new Joint()
                {
                    ID = joint.ID,
                    TrackingState = joint.TrackingState,
                    Position = new Vector()
                    {
                        W = joint.Position.W,
                        X = joint.Position.X,
                        Y = joint.Position.Y,
                        Z = joint.Position.Z
                    }
                });
            }
            newObj.SkeletonJoints = newList;
            return newObj;
        }
    }

    public static class BinaryReaderWriterKinectExtensions
    {
        public static void Write(this BinaryWriter bw, Vector vector)
        {
            bw.Write(vector.X);
            bw.Write(vector.Y);
            bw.Write(vector.Z);
            bw.Write(vector.W);
        }

        public static Vector ReadVector(this BinaryReader br)
        {
            return new Vector()
            {
                X = br.ReadSingle(),
                Y = br.ReadSingle(),
                Z = br.ReadSingle(),
                W = br.ReadSingle()
            };
        }

        public static void Write(this BinaryWriter bw, Joint joint)
        {
            bw.Write((int)joint.ID);
            bw.Write((int)joint.TrackingState);
            bw.Write(joint.Position);
        }

        public static Joint ReadJoint(this BinaryReader br)
        {
            return new Joint()
            {
                ID = (JointID)br.ReadInt32(),
                TrackingState = (JointTrackingState)br.ReadInt32(),
                Position = br.ReadVector()
            };
        }

        public static void Write(this BinaryWriter bw, IEnumerable<Joint> joints)
        {
            int i = 0, expectedNumberOfJoints = (int)JointID.Count;

            if (joints == null)
            {
                // Write default empty skeleton, if no joint collection was given
                bw.Write(expectedNumberOfJoints);
                for (int j = 0; j < expectedNumberOfJoints; j++)
                {
                    bw.Write(new Joint()
                    {
                        ID = (JointID)j,
                        Position = new Vector() { X = 0, Y = 0, Z = 0, W = 1 },
                        TrackingState = JointTrackingState.NotTracked
                    });
                }
            }
            else
            {
                // Write the skeleton joints
                bw.Write(expectedNumberOfJoints);
                foreach (Joint joint in joints)
                {
                    bw.Write(joint);
                    i++;
                }

                // Check, if the expected number of joints was written
                if (expectedNumberOfJoints != i)
                    throw new ArgumentException("The given joint collection does not hold the " +
                        "expected number of joints ('" + expectedNumberOfJoints.ToString() + "').");
            }
        }

        public static IEnumerable<Joint> ReadJoints(this BinaryReader br)
        {
            int count = br.ReadInt32();
            for (int i = 0; i < count; i++)
                yield return br.ReadJoint();
        }
    }
}
