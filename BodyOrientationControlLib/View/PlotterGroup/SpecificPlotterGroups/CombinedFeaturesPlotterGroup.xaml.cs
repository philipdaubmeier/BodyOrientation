using BodyOrientationLib;

namespace BodyOrientationControlLib
{
    public class _CombinedFeaturesPlotterGroup : PlotterGroupBase<CombinedFeatureSet> { }

    public partial class CombinedFeaturesPlotterGroup : _CombinedFeaturesPlotterGroup
    {
        public CombinedFeaturesPlotterGroup()
            : base()
        {
            InitializeComponent();

            _diagram1 = diagrams.diagram1;
            _diagram2 = diagrams.diagram2;
            _diagram3 = diagrams.diagram3;
        }
    }
}
