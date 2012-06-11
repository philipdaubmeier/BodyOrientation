using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BodyOrientationLib;
using System.Windows.Media.Media3D;
using Microsoft.Research.Kinect.Nui;

namespace BodyOrientationControlLib
{
    public partial class SkeletonModel : UserControl
    {
        private Skeleton3d skeletonModel3D = null;

        public SkeletonModel()
        {
            InitializeComponent();

            skeletonModel3D = new Skeleton3d(GlobalSystem);
        }

        public void UpdateSkeleton(IEnumerable<Joint> joints)
        {
            if (skeletonModel3D != null)
                skeletonModel3D.UpdateOrCreateJoints(joints);
        }
    }
}
