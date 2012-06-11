using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Research.Kinect.Nui;
using System.Globalization;

namespace BodyOrientationLib
{
    public static class SkeletonExtensions
    {
        public static IEnumerable<Joint> NormalizeSkeleton(this IEnumerable<Joint> skeleton)
        {
            if (skeleton == null)
                throw new ArgumentException("No skeleton given!");

            // Test for correct count of joints. Look if there is any JointID that is not used by any joint in this skeleton.
            if (skeleton.Count() != (int)JointID.Count ||
                Enumerable.Range(0, (int)JointID.Count).Select(x => (JointID)x).Any(x => !skeleton.Any(y => y.ID == x)))
                throw new ArgumentException("Skeleton incomplete! A complete skeleton has to have exactly " +
                    "20 joints and exactly one of each possible JointID.");

            // Store into list for easy accessability of items
            var skeletonList = skeleton.ToList();

            // Center horizontically at spine, vertically at right hand, depth at the spine at value 2
            float spineX = skeletonList.FirstOrDefault(x => x.ID == JointID.Spine).Position.X;
            float spineOffsetZ = skeletonList.FirstOrDefault(x => x.ID == JointID.Spine).Position.Z - 2;
            float handY = skeletonList.FirstOrDefault(x => x.ID == JointID.HandRight).Position.Y;

            // 1. Iterate every joint for centering and aligning
            for (int i = 0; i < (int)JointID.Count; i++)
            {
                // Fetch reference to struct to write into it
                Joint joint = skeletonList[i];
                Vector vec = joint.Position;

                // Move the whole skeleton to center it. Round numbers.
                vec.X = (float)Math.Round(vec.X - spineX, 2, MidpointRounding.AwayFromZero);
                vec.Y = (float)Math.Round(vec.Y - handY, 2, MidpointRounding.AwayFromZero);
                vec.Z = (float)Math.Round(vec.Z - spineOffsetZ, 2, MidpointRounding.AwayFromZero);

                // All centered joints are aligned exactly at 0
                if (SkeletonMetadata.CenteredJoints.Contains(joint.ID))
                    vec.X = 0;

                // Write structs back into the skeleton
                joint.Position = vec;
                skeletonList[i] = joint;
            }

            // 2. Iterate every joint for mirroring
            for (int i = 0; i < (int)JointID.Count; i++)
            {
                // Fetch reference to struct to write into it
                Joint joint = skeletonList[i];
                Vector vec = joint.Position;

                // All right hand joints are mirrored to the left ones
                if (SkeletonMetadata.SymmetricJoints.ContainsKey(joint.ID))
                {
                    Joint j = skeletonList.FirstOrDefault(x => x.ID == SkeletonMetadata.SymmetricJoints[joint.ID]);
                    Vector symmetric = j.Position;
                    vec.X = -symmetric.X;
                    vec.Y = symmetric.Y;
                    vec.Z = symmetric.Z;
                }

                // Write structs back into the skeleton
                joint.Position = vec;
                skeletonList[i] = joint;
            }

            return skeletonList;
        }

        public static string ToHardCodeSource(this IEnumerable<Joint> skeleton)
        {
            StringBuilder code = new StringBuilder("public static List<Joint> ExampleSkeleton\r\n{\r\n\tget\r\n\t{\r\n\t\tList<Joint> skeleton = new List<Joint>((int)JointID.Count);\r\n\r\n");

            foreach (var joint in skeleton)
                code.AppendFormat(new CultureInfo("en-US", false), "\t\tskeleton.Add(new Joint() {{ ID = JointID.{0}, Position = new Vector() {{ W = {1}f, X = {2:0.00}f, Y = {3:0.00}f, Z = {4:0.00}f }}, TrackingState = JointTrackingState.Tracked }});\r\n", 
                    joint.ID.ToString("G"), joint.Position.W, joint.Position.X, joint.Position.Y, joint.Position.Z);

            code.Append("\r\n\t\treturn skeleton;\r\n\t}\r\n}");

            return code.ToString();
        }

        public static List<Joint> ExampleSkeleton
        {
            get
            {
                List<Joint> skeleton = new List<Joint>((int)JointID.Count);

                skeleton.Add(new Joint() { ID = JointID.HipCenter, Position = new Vector() { W = 1f, X = 0.00f, Y = 0.27f, Z = 1.95f }, TrackingState = JointTrackingState.Tracked });
                skeleton.Add(new Joint() { ID = JointID.Spine, Position = new Vector() { W = 1f, X = 0.00f, Y = 0.32f, Z = 2.00f }, TrackingState = JointTrackingState.Tracked });
                skeleton.Add(new Joint() { ID = JointID.ShoulderCenter, Position = new Vector() { W = 1f, X = 0.00f, Y = 0.62f, Z = 2.01f }, TrackingState = JointTrackingState.Tracked });
                skeleton.Add(new Joint() { ID = JointID.Head, Position = new Vector() { W = 1f, X = 0.00f, Y = 0.87f, Z = 2.01f }, TrackingState = JointTrackingState.Tracked });
                skeleton.Add(new Joint() { ID = JointID.ShoulderLeft, Position = new Vector() { W = 1f, X = -0.18f, Y = 0.59f, Z = 2.02f }, TrackingState = JointTrackingState.Tracked });
                skeleton.Add(new Joint() { ID = JointID.ElbowLeft, Position = new Vector() { W = 1f, X = -0.28f, Y = 0.34f, Z = 2.01f }, TrackingState = JointTrackingState.Tracked });
                skeleton.Add(new Joint() { ID = JointID.WristLeft, Position = new Vector() { W = 1f, X = -0.26f, Y = 0.09f, Z = 1.85f }, TrackingState = JointTrackingState.Tracked });
                skeleton.Add(new Joint() { ID = JointID.HandLeft, Position = new Vector() { W = 1f, X = -0.25f, Y = 0.00f, Z = 1.82f }, TrackingState = JointTrackingState.Tracked });
                skeleton.Add(new Joint() { ID = JointID.ShoulderRight, Position = new Vector() { W = 1f, X = 0.18f, Y = 0.59f, Z = 2.02f }, TrackingState = JointTrackingState.Tracked });
                skeleton.Add(new Joint() { ID = JointID.ElbowRight, Position = new Vector() { W = 1f, X = 0.28f, Y = 0.34f, Z = 2.01f }, TrackingState = JointTrackingState.Tracked });
                skeleton.Add(new Joint() { ID = JointID.WristRight, Position = new Vector() { W = 1f, X = 0.26f, Y = 0.09f, Z = 1.85f }, TrackingState = JointTrackingState.Tracked });
                skeleton.Add(new Joint() { ID = JointID.HandRight, Position = new Vector() { W = 1f, X = 0.25f, Y = 0.00f, Z = 1.82f }, TrackingState = JointTrackingState.Tracked });
                skeleton.Add(new Joint() { ID = JointID.HipLeft, Position = new Vector() { W = 1f, X = -0.08f, Y = 0.19f, Z = 1.93f }, TrackingState = JointTrackingState.Tracked });
                skeleton.Add(new Joint() { ID = JointID.KneeLeft, Position = new Vector() { W = 1f, X = -0.12f, Y = -0.33f, Z = 1.95f }, TrackingState = JointTrackingState.Tracked });
                skeleton.Add(new Joint() { ID = JointID.AnkleLeft, Position = new Vector() { W = 1f, X = -0.13f, Y = -0.75f, Z = 1.94f }, TrackingState = JointTrackingState.Tracked });
                skeleton.Add(new Joint() { ID = JointID.FootLeft, Position = new Vector() { W = 1f, X = -0.13f, Y = -0.78f, Z = 1.84f }, TrackingState = JointTrackingState.Tracked });
                skeleton.Add(new Joint() { ID = JointID.HipRight, Position = new Vector() { W = 1f, X = 0.08f, Y = 0.19f, Z = 1.93f }, TrackingState = JointTrackingState.Tracked });
                skeleton.Add(new Joint() { ID = JointID.KneeRight, Position = new Vector() { W = 1f, X = 0.12f, Y = -0.33f, Z = 1.95f }, TrackingState = JointTrackingState.Tracked });
                skeleton.Add(new Joint() { ID = JointID.AnkleRight, Position = new Vector() { W = 1f, X = 0.13f, Y = -0.75f, Z = 1.94f }, TrackingState = JointTrackingState.Tracked });
                skeleton.Add(new Joint() { ID = JointID.FootRight, Position = new Vector() { W = 1f, X = 0.13f, Y = -0.78f, Z = 1.84f }, TrackingState = JointTrackingState.Tracked });

                return skeleton;
            }
        }
    }
}
