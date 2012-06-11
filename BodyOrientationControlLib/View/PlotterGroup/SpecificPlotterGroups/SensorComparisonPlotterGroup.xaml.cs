using BodyOrientationLib;

namespace BodyOrientationControlLib
{
    public class _SensorComparisonPlotterGroup : PlotterGroupBase<SensorComparisonFeatureSet> { }

    public partial class SensorComparisonPlotterGroup : _SensorComparisonPlotterGroup
    {
        public SensorComparisonPlotterGroup()
            : base()
        {
            InitializeComponent();

            _diagram1 = diagrams.diagram1;
            _diagram2 = diagrams.diagram2;
            _diagram3 = diagrams.diagram3;
        }
    }
}
