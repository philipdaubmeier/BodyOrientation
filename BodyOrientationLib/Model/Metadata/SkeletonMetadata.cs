using Microsoft.Research.Kinect.Nui;
using System.Collections.Generic;

namespace BodyOrientationLib
{
    public static class SkeletonMetadata
    {
        public static int[] BoneConnectionMapping = new int[]{
            -1,
            (int)JointID.HipCenter,
            (int)JointID.Spine,
            (int)JointID.ShoulderCenter,
            (int)JointID.ShoulderCenter,
            (int)JointID.ShoulderLeft,
            (int)JointID.ElbowLeft,
            (int)JointID.WristLeft,
            (int)JointID.ShoulderCenter,
            (int)JointID.ShoulderRight,
            (int)JointID.ElbowRight,
            (int)JointID.WristRight,
            (int)JointID.HipCenter,
            (int)JointID.HipLeft,
            (int)JointID.KneeLeft,
            (int)JointID.AnkleLeft,
            (int)JointID.HipCenter,
            (int)JointID.HipRight,
            (int)JointID.KneeRight,
            (int)JointID.AnkleRight
        };

        public static Dictionary<JointID, JointID> SymmetricJoints = new Dictionary<JointID, JointID>()
        {
            { JointID.AnkleLeft, JointID.AnkleRight },
            { JointID.ElbowLeft, JointID.ElbowRight },
            { JointID.FootLeft, JointID.FootRight },
            { JointID.HandLeft, JointID.HandRight },
            { JointID.HipLeft, JointID.HipRight },
            { JointID.KneeLeft, JointID.KneeRight },
            { JointID.ShoulderLeft, JointID.ShoulderRight },
            { JointID.WristLeft, JointID.WristRight }
        };

        public static IEnumerable<JointID> CenteredJoints = new List<JointID>()
        {
            JointID.Head,
            JointID.Spine,
            JointID.ShoulderCenter,
            JointID.HipCenter
        };
    }
}
