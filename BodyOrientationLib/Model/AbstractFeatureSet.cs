using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BodyOrientationLib
{
    public abstract class AbstractFeatureSet : IRecordable, IMultiplexable
    {
        public virtual void WriteToRecorder(BinaryWriter writer)
        {
            foreach (double value in ExtractValues())
                writer.Write(value);
        }

        public virtual void ReadFromRecorder(BinaryReader reader)
        {
            double[] values = new double[NumMultiplexableItems];
            for (int i = 0; i < values.Length; i++)
                values[i] = reader.ReadDouble();
            InjectValues(values);
        }

        public virtual int NumMultiplexableItems { get { return 0; } }

        public virtual double[] ExtractValues()
        {
            return new double[NumMultiplexableItems];
        }

        public virtual void InjectValues(double[] values)
        {
            return;
        }

        public abstract object Clone();

        protected object CloneByValues<T>() where T : AbstractFeatureSet, new()
        {
            var newObj = new T();
            newObj.InjectValues(this.ExtractValues());
            return newObj;
        }
    }
}
