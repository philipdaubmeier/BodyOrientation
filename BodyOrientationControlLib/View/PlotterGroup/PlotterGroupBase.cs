using System;
using System.Collections.Generic;
using BodyOrientationLib;
using System.Windows.Controls;
using System.Windows;

namespace BodyOrientationControlLib
{
    public class PlotterGroupBase<T> : UserControl
    {
        protected Dictionary<string, Tuple<DiagramPlotter, Func<int>>> propertyDiagramMapping;
        protected PlotterScopeViewModel<T> viewmodel;

        protected DiagramPlotter _diagram1 = null;
        protected DiagramPlotter _diagram2 = null;
        protected DiagramPlotter _diagram3 = null;

        protected bool inited = false;

        protected PlottableValue<T>[] valueMapping;
        protected PlottableValueGroup[] valueGroupMapping;

        internal PlotterGroupBase()
        {
            // Get the mappings for the specific generic type parameter
            var mappings = PlottingMetadata.GetValueMapping<T>();
            var defaultValueGroup = mappings.Item3;
            var defaultValues = mappings.Item4;
            valueMapping = mappings.Item1;
            valueGroupMapping = mappings.Item2;

            // Create the viewmodel, pass it the mappings
            viewmodel = new PlotterScopeViewModel<T>(valueMapping, valueGroupMapping, defaultValueGroup, defaultValues);

            // Set the viewmodel as the datacontext for this user control.
            this.DataContext = viewmodel;

            // Listen for changes in the viewmodel (selected items in the comboboxes change)
            viewmodel.PropertyChanged += (s, e) => { PlotterScopeViewModelPropertyChanged(e.PropertyName); };

            this.Loaded += new RoutedEventHandler(ControlInitialized);
        }

        protected void ControlInitialized(object sender, RoutedEventArgs e)
        {
            inited = true;

            // Create a mapping from viewmodel property names to their corresponding diagrams and PlottingMapping indices
            propertyDiagramMapping = new Dictionary<string, Tuple<DiagramPlotter, Func<int>>>() 
            { 
                { "Custom1Id", new Tuple<DiagramPlotter, Func<int>>(_diagram1, () => viewmodel.Custom1Id) }, 
                { "Custom2Id", new Tuple<DiagramPlotter, Func<int>>(_diagram2, () => viewmodel.Custom2Id) }, 
                { "Custom3Id", new Tuple<DiagramPlotter, Func<int>>(_diagram3, () => viewmodel.Custom3Id) } 
            };

            // Init diagrams
            foreach (var item in propertyDiagramMapping)
                PlotterScopeViewModelPropertyChanged(item.Key);
        }

        protected void PlotterScopeViewModelPropertyChanged(string propertyName)
        {
            if (!propertyDiagramMapping.ContainsKey(propertyName) || !inited)
                return;

            // Get the reference to the diagram and the current selected value via the propertyDiagramMapping.
            // Then, in turn, get the corresponding diagramInfo from the selected id via the static PlottingMapping.
            var info = propertyDiagramMapping[propertyName];
            var diagram = info.Item1;
            var diagramInfo = valueMapping[info.Item2()];

            // Reset the diagram and give it the new diagram info.
            if(diagram != null)
                diagram.Reset(diagramInfo.Label, diagramInfo.Default, diagramInfo.Minimum, diagramInfo.Maximum);
        }

        /// <summary>
        /// Updates all diagrams by plotting a new value to it. The corresponding values the user 
        /// selected are picked from the given CombinedFeatureSet and passed to the respective diagram.
        /// </summary>
        /// <param name="reading">The current readings that shall be plotted.</param>
        public void Plot(T reading)
        {
            if (!inited)
                return;

            foreach (var diagram in propertyDiagramMapping.Values)
                diagram.Item1.PlotValue(valueMapping[diagram.Item2()].GetValue(reading));
        }

        /// <summary>
        /// Clears all diagrams.
        /// </summary>
        public void Clear()
        {
            if (!inited)
                return;

            foreach (var diagram in propertyDiagramMapping.Values)
                diagram.Item1.Reset();
        }
    }
}
