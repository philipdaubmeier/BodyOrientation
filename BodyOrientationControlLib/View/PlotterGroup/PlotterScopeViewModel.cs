using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows;

namespace BodyOrientationControlLib
{
    public class PlotterScopeViewModel<T> : AbstractViewModel
    {
        private PlottableValue<T>[] valueMapping;
        private PlottableValueGroup[] valueGroupMapping;

        public PlotterScopeViewModel(PlottableValue<T>[] valueMapping, PlottableValueGroup[] valueGroupMapping, int defaultValueGroup, int[] defaultValues)
        {
            this.valueMapping = valueMapping;
            this.valueGroupMapping = valueGroupMapping;

            // Fill the group list according to the mapping
            var groups = new List<KeyValuePair<int, string>>();
            int i = 0;
            foreach (var info in valueGroupMapping)
                groups.Add(new KeyValuePair<int, string>(i++, info.Name));

            // Fill the custom lists according to the mapping
            var custom1 = new List<KeyValuePair<int, string>>();
            var custom2 = new List<KeyValuePair<int, string>>();
            var custom3 = new List<KeyValuePair<int, string>>();
            i = 0;
            foreach (var info in valueMapping)
            {
                custom1.Add(new KeyValuePair<int, string>(i, info.Name));
                custom2.Add(new KeyValuePair<int, string>(i, info.Name));
                custom3.Add(new KeyValuePair<int, string>(i++, info.Name));
            }

            // Assign the lists to the public properties
            Groups = groups;
            Custom1 = custom1;
            Custom2 = custom2;
            Custom3 = custom3;

            // Set default selected items; this in turn sets the 
            // corresponding custom default values (in the setter of this property)
            GroupId = defaultValueGroup;

            if (defaultValues != null && defaultValues.Length >= 3)
            {
                // Set group to 'custom'
                GroupId = groups.Count - 1;
                Custom1Id = defaultValues[0];
                Custom2Id = defaultValues[1];
                Custom3Id = defaultValues[2];
            }
        }

        public List<KeyValuePair<int, string>> Groups { get; private set; }
        private bool _groupEnabled;
        public bool GroupEnabled
        {
            get
            {
                return _groupEnabled;
            }
            set
            {
                if (value != _groupEnabled)
                {
                    _groupEnabled = value;
                    NotifyPropertyChanged("GroupEnabled");
                }
            }
        }
        private int _groupId;
        public int GroupId
        {
            get
            {
                return _groupId;
            }
            set
            {
                if (value != _groupId && value >= 0 && value < valueGroupMapping.Length)
                {
                    var groupInfo = valueGroupMapping[value];

                    var custom = groupInfo.Custom;
                    GroupEnabled = true;
                    Custom1Enabled = custom;
                    Custom2Enabled = custom;
                    Custom3Enabled = custom;

                    if (!custom)
                    {
                        var subIndices = groupInfo.SensorValueIndices;
                        Custom1Id = subIndices[0];
                        Custom2Id = subIndices[1];
                        Custom3Id = subIndices[2];
                    }

                    _groupId = value;

                    NotifyPropertyChanged("GroupId");
                }
            }
        }

        public List<KeyValuePair<int, string>> Custom1 { get; private set; }
        private bool _custom1Enabled;
        public bool Custom1Enabled
        {
            get
            {
                return _custom1Enabled;
            }
            set
            {
                if (value != _custom1Enabled)
                {
                    _custom1Enabled = value;
                    NotifyPropertyChanged("Custom1Enabled");
                }
            }
        }
        private int _custom1Id;
        public int Custom1Id
        {
            get
            {
                return _custom1Id;
            }
            set
            {
                if (value != _custom1Id)
                {
                    _custom1Id = value;
                    NotifyPropertyChanged("Custom1Id");
                }
            }
        }

        public List<KeyValuePair<int, string>> Custom2 { get; private set; }
        private bool _custom2Enabled;
        public bool Custom2Enabled
        {
            get
            {
                return _custom2Enabled;
            }
            set
            {
                if (value != _custom2Enabled)
                {
                    _custom2Enabled = value;
                    NotifyPropertyChanged("Custom2Enabled");
                }
            }
        }
        private int _custom2Id;
        public int Custom2Id
        {
            get
            {
                return _custom2Id;
            }
            set
            {
                if (value != _custom2Id)
                {
                    _custom2Id = value;
                    NotifyPropertyChanged("Custom2Id");
                }
            }
        }

        public List<KeyValuePair<int, string>> Custom3 { get; private set; }
        private bool _custom3Enabled;
        public bool Custom3Enabled
        {
            get
            {
                return _custom3Enabled;
            }
            set
            {
                if (value != _custom3Enabled)
                {
                    _custom3Enabled = value;
                    NotifyPropertyChanged("Custom3Enabled");
                }
            }
        }
        private int _custom3Id;
        public int Custom3Id
        {
            get
            {
                return _custom3Id;
            }
            set
            {
                if (value != _custom3Id)
                {
                    _custom3Id = value;
                    NotifyPropertyChanged("Custom3Id");
                }
            }
        }
    }
}
