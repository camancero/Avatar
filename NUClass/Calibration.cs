// *************************************************************************************
// $Id: Calibration.cs 531 2017-08-21 14:33:43Z tjones $
// *************************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NUClass
{
    /// <summary>
    /// Handles the data storage when using the Madgwick algorithm as well as providing functionality for 
    /// reading / writing configuration files.
    /// </summary>
    public class Calibration
    {
        /// <summary>
        /// Is the IMU device currently calibrated?
        /// </summary>
        public bool calibrated = false;
        /// <summary>
        /// variable used to keep track of the interations performed in the calibration stage.
        /// </summary>
        public int countdown = 1000;
        /// <summary>
        /// Axes used for the magnetometer,accelerometer and gyroscope.
        /// </summary>
        private readonly string[] axes = { "X", "Y", "Z" };
        /// <summary>
        /// Master dictionary of the values stored from the IMU device.
        /// </summary>
        private Dictionary<string, Dictionary<string, int>> variables = new Dictionary<string, Dictionary<string, int>>();
        /// <summary>
        /// Number associated with the IMU device.
        /// </summary>
        private string IMU_number;
        /// <summary>
        /// Path to the calibration memory file.
        /// </summary>
        private string path;
        /// <summary>
        /// Parent IMUConnection instance.
        /// </summary>
        private IMUConnection IMU_connection;
        /// <summary>
        /// Whether the configuration file is valid.
        /// </summary>
        public bool valid_config_file { get; private set; } = true;

        /// <summary>
        /// Constructs the Calibration class, passing the associated IMU number and parent IMUConnection instance.
        /// Looks for the associated calibration file and attempts to read it if it exists, else creates a new one.
        /// </summary>
        /// <param name="_IMU_number">IMU device number.</param>
        /// <param name="_IMU_connection"> Parent IMU connection instance.</param>
        public Calibration(string _IMU_number, IMUConnection _IMU_connection)
        {
            populate_variables();
            IMU_number = _IMU_number;
            IMU_connection = _IMU_connection;
            bool use_calibration = IMU_connection.use_calibration;

            path += "default.csv"; //MMark.Info.MMarkInfo.MMarkResources + "IMU_Memory\\MagCali\\" + IMU_number + ".txt";

            if (File.Exists(path) && use_calibration)
            {
                System.Diagnostics.Debug.WriteLine(IMU_number.ToString() + " ACTIVATED");
                try
                {
                    string[] CalibrationValues = File.ReadAllLines(path);

                    for (var i = 0; i < 3; i++)
                    {
                        variables["GYRO"][axes[i]] = int.Parse(CalibrationValues[i]);
                        variables["ACCEL"][axes[i]] = int.Parse(CalibrationValues[(i + 3)]);
                        variables["MAG"][axes[i]] = int.Parse(CalibrationValues[(i + 6)]);
                    }
                    valid_config_file = true;
                }
                catch (Exception)
                {
                    valid_config_file = false;
                }
            }
        }

        /// <summary>
        /// Formats the master dictionary to a string array to be printed to the calibration file.
        /// </summary>
        /// <returns>String array represnting IMU values.</returns>
        private string[] get_variables_array()
        {
            string[] data = new string[9];
            int index = 0;
            foreach (KeyValuePair<string, Dictionary<string, int>> sub_dictionary in variables)
            {
                foreach (KeyValuePair<string, int> entry in sub_dictionary.Value)
                {
                    data[index] = entry.Value.ToString();
                    index++;
                }
            }
            return data;
        }

        /// <summary>
        /// Gets the value from the master dictionary.
        /// </summary>
        /// <param name="variable"> GYRO / ACCEL / MAG </param>
        /// <param name="axis"> X / Y / Z</param>
        /// <returns>Integer value from passed keys.</returns>
        public int get(string variable, string axis)
        {
            return variables[variable][axis];
        }
        /// <summary>
        /// Sets the value in the master dictionary associated with the passed keys.
        /// </summary>
        /// <param name="variable"> GYRO / ACCEL / MAG </param>
        /// <param name="axis"> X / Y / Z</param>
        /// <param name="value"> Integer value to set in the master dictionary.</param>
        public void set(string variable, string axis, int value)
        {
            variables[variable][axis] = value;
        }
        /// <summary>
        /// Increments the value in the master dictionary associated with the passed keys.
        /// </summary>
        /// <param name="variable"> GYRO / ACCEL / MAG </param>
        /// <param name="axis"> X / Y / Z</param>
        /// <param name="value"> Integer value to increment the value in the master dictionary by.</param>
        public void adjust(string variable, string axis, int value)
        {
            variables[variable][axis] += value;
        }

        /// <summary>
        /// Is the IMU device calibrated?
        /// </summary>
        /// <returns> Is calibrated?</returns>
        public bool is_calibrated()
        {
            return calibrated;
        }

        /// <summary>
        /// Sets whether the IMU device is calibrated.
        /// </summary>
        /// <param name="calibrated">T - calibrated, F - not calibrated.</param>
        public void set_calibrated(bool calibrated)
        {
            this.calibrated = calibrated;
        }

        private void write_calibration_file()
        {
            string[] write_data = get_variables_array();
        }

        /// <summary>
        /// Builds the structure of the master dictionary.
        /// </summary>
        private void populate_variables()
        {
            Dictionary<string, int> gyro_dict = new Dictionary<string, int>();
            Dictionary<string, int> accel_dict = new Dictionary<string, int>();
            Dictionary<string, int> mag_dict = new Dictionary<string, int>();


            for (var i = 0; i < axes.Length; i++)
            {
                gyro_dict.Add(axes[i], 0);
                accel_dict.Add(axes[i], 0);
                mag_dict.Add(axes[i], 0);
            }

            variables.Add("GYRO", gyro_dict);
            variables.Add("ACCEL", accel_dict);
            variables.Add("MAG", mag_dict);

        }
    }
}