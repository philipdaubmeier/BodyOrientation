using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BodyOrientationLib
{
    public interface IRecordable
    {
        void WriteToRecorder(BinaryWriter writer);
        void ReadFromRecorder(BinaryReader reader);
    }
}
