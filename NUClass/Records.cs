// *************************************************************************************
// $Id: Records.cs 531 2017-08-21 14:33:43Z tjones $
// *************************************************************************************

using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace NUClass
{
    /// <summary>
    /// Class recording all the raw, processed and MMG readings from an IMU connection.
    /// </summary>
    public class Records
    {
        /// <summary>
        /// Underlying list to store data.
        /// </summary>
        private List<object[]> records = new List<object[]>();
        /// <summary>
        /// Current index within the data list.
        /// </summary>
        private int current_index = 0;
        /// <summary>
        /// Total count of records stored.
        /// </summary>
        public int record_sum = 0;
        /// <summary>
        /// IMU number of the associated IMU device.
        /// </summary>
        public string IMU = "";

        /// <summary>
        /// Constructs the class, assigning the passed IMU number.
        /// </summary>
        /// <param name="_IMU"></param>
        public Records(String _IMU)
        {
            IMU = _IMU;
        }

        /// <summary>
        /// Adds an entry as a collection of objects to the underlying records list.
        /// </summary>
        /// <param name="ticks">Count in ticks from start of streaming until this read was made.</param>
        /// <param name="raw"> Integer array representation of the raw input.</param>
        /// <param name="processed"> Quaternion produced from the raw input.</param>
        /// <param name="MMG"> Processed MMG data from the raw input.</param>
        public void add_record(long ticks, int[] raw, Quaternion processed, float[] MMG)
        {
            int[] rawc = (int[])raw.Clone();
            //Quaternion processedc = processed.
            float[] MMGc = (float[])MMG.Clone();
            int record_sumc = record_sum;
            long ticksc = ticks;
            //object[] entry = { ticksc, rawc, processedc, MMGc, record_sumc};
            //records.Add(entry);
            record_sum++;
        }

        /// <summary>
        /// Get the objects at the specified index from the records list.
        /// </summary>
        /// <param name="index"> Target index to read.</param>
        /// <returns> Object array of raw,processed and MMG data.</returns>
        public object[] get_record(int index)
        {
            return records[index];
        }

        /// <summary>
        /// Invoked on new exercise attempt, empties the records list and resets the positional variables.
        /// </summary>
        public void flush()
        {
            records = new List<object[]>();
            current_index = 0;
            record_sum = 0;
        }
    }
}
