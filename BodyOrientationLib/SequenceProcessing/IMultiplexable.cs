using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BodyOrientationLib
{
    public interface IMultiplexable : ICloneable
    {
        double[] ExtractValues();
        void InjectValues(double[] values);

        int NumMultiplexableItems { get; }
    }
}
