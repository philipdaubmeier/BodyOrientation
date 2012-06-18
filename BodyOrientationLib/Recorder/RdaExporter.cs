using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Globalization;

namespace BodyOrientationLib
{
    public class RdaExporter<TData> : IDisposable
    {
        private TextWriter _writer = null;

        private List<Tuple<List<PropertyInfo>, int>> _properties = new List<Tuple<List<PropertyInfo>, int>>();

        private CultureInfo NumericDotCulture = CultureInfo.GetCultureInfo("en-US");

        public RdaExporter(string filename)
        {
            _writer = new StreamWriter(filename);

            TraverseProperties(typeof(TData));

            _writer.WriteLine("");
            _writer.Flush();
        }

        private void TraverseProperties(Type type)
        {
            TraverseProperties(type, new List<PropertyInfo>(), type.Namespace, 0);
        }
        private void TraverseProperties(Type type, List<PropertyInfo> path, string nameSpace, int depth)
        {
            foreach (var property in type.GetProperties())
            {
                string name = property.Name.ToLowerInvariant();
                
                Type ptype = property.PropertyType;
                TypeCode code = Type.GetTypeCode(ptype);

                // Only export doubles
                if (code == TypeCode.Double)
                {
                    _writer.Write(name + " ");

                    var newpath = path.ToList();
                    newpath.Add(property);
                    _properties.Add(new Tuple<List<PropertyInfo>, int>(newpath, depth));
                }
                else if (code == TypeCode.Object)
                {
                    // Only traverse objects of the same namespace
                    if (property.PropertyType.Namespace == nameSpace)
                    {
                        var newpath = path.ToList();
                        newpath.Add(property);
                        TraverseProperties(property.PropertyType, newpath, nameSpace, depth + 1);
                    }
                }
            }
        }

        private double GetValue(object data, Tuple<List<PropertyInfo>, int> nestedProperty)
        {
            return GetValue(data, nestedProperty.Item1, nestedProperty.Item2);
        }
        private double GetValue(object data, List<PropertyInfo> propertyPath, int depth)
        {
            object value = propertyPath[propertyPath.Count - depth - 1].GetValue(data, null);

            if (depth == 0)
                return (double)value;
            else
                return GetValue(value, propertyPath, depth - 1);
        }

        public void WriteData(TData data)
        {
            _writer.WriteLine(string.Join(" ", _properties.Select(type => GetValue(data, type).ToString(NumericDotCulture))));
        }

        public void Close()
        {
            _writer.Close();
        }

        public void Dispose()
        {
            _writer.Dispose();
        }
    }
}
