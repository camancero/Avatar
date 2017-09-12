// *************************************************************************************
// $Id: IMUConnection.cs 546 2017-08-23 10:27:51Z efranco $
// *************************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using InTheHand.Net;
using InTheHand.Net.Sockets;
using System.Text;
using System.Windows.Media.Media3D;

namespace NUClass
{
    /// <summary>
    /// Central class for each connected IMU devices. Creates and handles the low-level Bluetooth controller and
    /// manages memory relating to Bluetooth MAC address, IMU configuration files and input data reads. Houses the
    /// Madgwick algorithm and MMG manipulations to process raw data. 
    /// </summary>
    public class IMUConnection
    {
        /// <summary>
        /// Unformatted IMU number associated with the connected IMU device.
        /// </summary>
        public string IMU_number;
        /// <summary>
        /// Memory of and underlying functions relating to read / write of the configuration file.
        /// </summary>
        public Calibration calibration;
        /// <summary>
        /// The processed quaternion produced from the latest raw data input.
        /// </summary>
        public System.Windows.Media.Media3D.Quaternion processed = new System.Windows.Media.Media3D.Quaternion(0, 0, 0,1);
        /// <summary>
        /// The underlying instance of the Bluetooth manager class.
        /// </summary>
        private BTManager bt_manager = null;
        /// <summary>
        /// Parent ConnectionManager instance.
        /// </summary>        
        ConnectionManager connection_manager;
        /// <summary>
        /// Name of the text file which acts as a record of a previously connected IMU device's MAC address.
        /// </summary>
        private string IMU_memory;
        /// <summary>
        /// Scaling factor used when performing gradient descent in the Madgwick algorithm.
        /// </summary>
        private float step_size = 0.005f;
        /// <summary>
        /// The raw data input from the last read from the IMU stream.
        /// </summary>
        public int[] raw { get; private set; } = { 0 };
        /// <summary>
        /// The processed IMU data created from the last raw data input.
        /// </summary>
        public float[] MMG { get; private set; } = { 0.0f, 0.0f, 0.0f };
        /// <summary>
        /// Flag used to indicate whether or not extensive debugging is on.
        /// </summary>
        private bool debug_connection_reports;
        /// <summary>
        /// Client instance managing Bluetooth connection and stream acquisition.
        /// </summary>
        private BluetoothClient client;
        /// <summary>
        /// MAC address of the IMU device attempting to be connnected with.
        /// </summary>
        private BluetoothAddress MAC_address = null;
        /// <summary>
        /// All devices discovered in Bluetooth range.
        /// </summary>
        private BluetoothDeviceInfo[] devices;
        /// <summary>
        /// Formatted IMU string name built from its number.
        /// </summary>
        public string IMU_device_name;
        /// <summary>
        /// A store of all the raw reads and processed data collected.
        /// </summary>
        public Records records;
        /// <summary>
        /// Boolean indicating whether to attempt to read existing calibration files or to create new ones.
        /// </summary>
        public bool use_calibration;

        // Connection Information
        /// <summary>
        /// Debug report collected from this IMU connection.
        /// </summary>
        public Dictionary<string, string> connection_information = new Dictionary<string, string>();
        /// <summary>
        /// Has the IMU device data been collected?
        /// </summary>
        public bool conn_info_set { get; private set; } = false;
        /// <summary>
        /// Has a corresponding device been found over Bluetooth? Will always flag false when using the MAC file.
        /// </summary>
        public bool device_found { get; private set; } = false;
        /// <summary>
        /// Was the connection attempt made using information from an existing record of the correspong MAC address?
        /// </summary>
        public bool MAC_file { get; private set; } = false;

        private int iteration = 0;

        /// <summary>
        /// Sets global variables based on those passed as parameters, creates an instance of the Calibration class and
        /// determines the formatted IMU device name and the path to the memory file. Creates a new Bluetooth client and
        /// checks for a coressponding MAC file.
        /// </summary>
        /// <param name="_IMU_number"> The unformatted IMU number.</param>
        /// <param name="_connection_manager">Parent ConnectionManager instance.</param>
        /// <param name="_debug_connection_reports"> Whether to acquire device connection data.</param>
        /// <param name="_use_calibration"> Whether to use calibration file if it exists.</param>
        public IMUConnection(string _IMU_number, ConnectionManager _connection_manager, bool _debug_connection_reports, bool _use_calibration)
        {
            IMU_number = _IMU_number;
            connection_manager = _connection_manager;
            debug_connection_reports = _debug_connection_reports;
            use_calibration = _use_calibration;
            calibration = new Calibration(IMU_number, this);
            // IMU_memory = MMark.Info.MMarkInfo.MMarkResources + "IMU_Memory/IMU_BT_" + IMU_number + ".txt";
            IMU_memory = @"C:\Users\Nathan\AppData\Local\M-Mark\M-Mark\" + "IMU_Memory/IMU_BT_" + IMU_number + ".txt";
            IMU_device_name = (IMU_number.Length == 4) ? "IMU_BT_" + IMU_number : "NU_BT_" + IMU_number;
            client = new BluetoothClient();
            Check_MAC_file(IMU_memory);
        }

        /// <summary>
        /// Attempts to read MAC file at the path specified if it exists. If not present or the read fails,
        /// attempts to discover the device over the local Bluetooth.
        /// </summary>
        /// <param name="imu_memory"> Path to the IMU's MAC file.</param>
        private void Check_MAC_file(string imu_memory)
        {
            if (File.Exists(imu_memory))
            {
                MAC_address = load_from_MAC_file(File.ReadAllLines(imu_memory)[0].ToCharArray());
                MAC_file = true;
            }
            else
            {
                devices = client.DiscoverDevices();
                MAC_address = check_for_device();
            }
        }

        /// <summary>
        /// Reads in the chars array passed and converts it to a byte array before using this to create a
        /// Bluetooth address instance.
        /// </summary>
        /// <param name="device_MAC_address"> Character array read from the MAC text file.</param>
        /// <returns></returns>
        private BluetoothAddress load_from_MAC_file(char[] device_MAC_address)
        {
            byte[] address = { 0, 0, 0, 0, 0, 0 };
            for (int i = 0; i < 6; i++)
            {
                address[i] = (byte)(Convert.ToInt32((device_MAC_address[10 - (2 * i)].ToString() + device_MAC_address[11 - (2 * i)].ToString()), 16));
            }
            return new BluetoothAddress(address);
        }

        /// <summary>
        /// Writes the MAC file for a device when discovered over Bluetooth saving the address for faster future connection attempts.
        /// </summary>
        private void create_MAC_file()
        {
            foreach (BluetoothDeviceInfo d in devices)
            {
                if (d.DeviceName == IMU_device_name)
                {
                    try
                    {
                        FileStream imuMemoryFile = File.Create(IMU_memory);
                        byte[] byte_MAC_address = new UTF8Encoding(true).GetBytes(d.DeviceAddress.ToString());
                        imuMemoryFile.Write(byte_MAC_address, 0, byte_MAC_address.Length);
                        imuMemoryFile.Close();
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }

        
        /// <summary>
        /// Checks the local Bluetooth devices searching for one with a corresponding name to the formatted
        /// IMU name.
        /// </summary>
        /// <returns>MAC address if found, null if not.</returns>
        private BluetoothAddress check_for_device()
        {
            foreach (BluetoothDeviceInfo d in devices)
            {
                if (d.DeviceName == IMU_device_name)
                {
                    MAC_address = d.DeviceAddress;
                    device_found = true;
                    create_MAC_file();

                    return MAC_address;
                }
            }
            return null;
        }

        /// <summary>
        /// Creates new record and Bluetooth manager classes before attempting to connect to the IMU device.
        /// If an instance of the Bluetooth manager is already in effect, will make a call to dispose of it first.
        /// </summary>
        /// <returns> Whether the connection attempt to the IMU device was successful.</returns>
        public bool connect()
        {
            if (bt_manager != null)
            {
                dispose_previous_connection();
            }
            records = new Records(IMU_number);
            bt_manager = new BTManager(this, MAC_address, debug_connection_reports, conn_info_set);
            return bt_manager.connect();
        }

        /// <summary>
        /// Performs safe termination of the Bluetooth manager's underlying data stream thread, disposes of
        /// the associated 32Feet Bluetooth client and then the Bluetooth manager itself.
        /// </summary>
        public void dispose_previous_connection()
        {
            if(bt_manager.get_data != null && bt_manager.safe_to_terminate)
            {
                bt_manager.get_data.Abort();
            }
            if(bt_manager.client != null)
            {
                bt_manager.client.Close();
            }
            bt_manager = null;
        }

        /// <summary>
        /// Adds the information from the Bluetooth and firmware versions to the connection information dictionary. Sets the fact that 
        /// the data has been collected in the 'conn_info_set' boolean.
        /// </summary>
        /// <param name="major_version"> Major version number of the firmware.</param>
        /// <param name="minor_version"> Minor version number of the firmware.</param>
        /// <param name="bluetooth_version"> Bluetooth version number.</param>
        public void set_connection_info(int major_version, int minor_version, string bluetooth_version)
        {
            connection_information.Add("Bluetooth Version", bluetooth_version);
            connection_information.Add("Firmware Version", major_version.ToString() + "." + minor_version.ToString());
            conn_info_set = true;
        }

        /// <summary>
        /// Closes the Bluetooth connection by terminating the associated thread, sending a stop command to the IMU device
        /// and closing the underlying Bluetooth connection.
        /// </summary>
        public void close()
        {
            if (bt_manager != null && bt_manager.client != null)
            {
                bt_manager.send_data("M0dg");
                bt_manager.stream_open = false;

                if (bt_manager.get_data != null)
                {
                    /*
                    while (!bt_manager.safe_to_terminate)
                    {
                        System.Diagnostics.Debug.WriteLine(IMU_number + "--" + "waiting");
                    }
                    */
                    //bt_manager.get_data.Abort();
                }
                bt_manager.client.Close();
                bt_manager = null;
            }
        }

        /// <summary>
        /// Method invoked by the underlying Bluetooth manager when a data packet is assembled from the stream.
        /// Triggers data processing and the addition of both raw and processed values to the records class.
        /// </summary>
        /// <param name="data"> Raw data input.</param>
        /// <param name="read_time">Tick count since stream started.</param>
        public void data_read_event(int[] data, long read_time)
        {
                process_data(data);
            //System.Diagnostics.Debug.WriteLine(DateTime.Now);
            //System.Diagnostics.Debug.WriteLine(iteration);
            if (iteration > 4000)
            {
                connection_manager.return_quaternion(processed, IMU_number);
            }
            else
            {
                connection_manager.return_quaternion(Quaternion.Identity, IMU_number);
            }
            
                //records.add_record(read_time,data, processed,MMG);   
        }

        /// <summary>
        /// Toggles whether the Bluetooth manager should be streaming data.
        /// </summary>
        /// <param name="stream"> Whether the stream should be open.</param>
        public void set_stream(bool stream)
        {
            if (stream)
            {
                records.flush();
                bt_manager.start_streaming();
            } else
            {
                System.Diagnostics.Debug.WriteLine(IMU_number + ":" + "END");
                bt_manager.send_data("M0dg");
                bt_manager.stream_open = false;  
            }  
        }

        /// <summary>
        /// Takes the raw input data and invokes the appropriate method to handle its processing depending on whether 
        /// the calibration phase is completed. Calls the MMG data processing.
        /// </summary>
        /// <param name="data"></param>
        private void process_data(int[] data)
        {
            iteration++;
            int[] IMU_data = new int[18];
            Array.Copy(data, 0, IMU_data, 0, 18);
            int[] MMG_data = new int[6];
            Array.Copy(data, 18, MMG_data, 0, 6);
            IMU_data = integrate_sensor_readings(IMU_data);
            short[] short_values = short_ints(IMU_data);
            if (calibration.is_calibrated())
            {
                step_size = 0.005f;
                adjust(short_values);
            }
            else
            {
                step_size = 1f;
                calibrate(short_values);
            }
            process_MMG(MMG_data);
        }

        /// <summary>
        /// Integrates the high and low reads from the gyroscope, magnetometer and accelerometer into singular values.
        /// </summary>
        /// <param name="raw_values"> High and low values from the IMU read.</param>
        /// <returns> Integrated gyro, mageto and accel data. </returns>
        private int[] integrate_sensor_readings(int[] raw_values)
        {
            return new int[]{
            raw_values[0] + ((raw_values[1]) << 8),
            raw_values[2] + ((raw_values[3]) << 8),
            (raw_values[4] + ((raw_values[5]) << 8)),
            raw_values[6] + ((raw_values[7]) << 8),
            raw_values[8] + ((raw_values[9]) << 8),
            (raw_values[10] + ((raw_values[11]) << 8)),
            raw_values[12] + ((raw_values[13]) << 8),
            raw_values[14] + ((raw_values[15]) << 8),
            (raw_values[16] + ((raw_values[17]) << 8))
        };
        }

        /// <summary>
        /// Converts the high/low integer bands of the MMG reads into individual float values.
        /// </summary>
        /// <param name="data"></param>
        private void process_MMG(int[] data)
        {
            int MMG_count = 3;
            lock (MMG)
            {
                MMG = new float[MMG_count];
                for (var i = 0; i < MMG_count; i++)
                {
                    MMG[i] = (((data[2*i] * 256) + data[(2*i)+1]) * 3.3f) / 1024;
                }
            }

        }

        /// <summary>
        /// Converts the integer raw data values into short values for more accurate processing.
        /// </summary>
        /// <param name="data"> Raw integer values.</param>
        /// <returns></returns>
        public short[] short_ints(int[] data)
        {
            short counter = 0;
            short[] accel_data_short = new short[data.Length];
            foreach (Int32 value in data)
            {
                accel_data_short[counter] = Convert.ToInt16((data[counter]) & 32767);
                if (((data[counter]) & 32768) != 0)
                {
                    accel_data_short[counter] = Convert.ToInt16(-32768 + accel_data_short[counter]);
                }
                counter++;
            }
            return accel_data_short;
        }

        /// <summary>
        /// Gets the memory / calibration of the IMU values after calibration is complete and invokes the 
        /// main Madwick algorithm.
        /// </summary>
        /// <param name="values"> Short values converted from the raw integer values.</param>
        public void adjust(short[] values)
        {
            float GX = ((((float)(values[0]) - (float)calibration.get("GYRO", "X")) * 500f * (float)Math.PI) / (32758f * 180f));
            float GY = ((((float)(values[1]) - (float)calibration.get("GYRO", "Y")) * 500f * (float)Math.PI) / (32758f * 180f));
            float GZ = ((((float)(values[2]) - (float)calibration.get("GYRO", "Z")) * 500f * (float)Math.PI) / (32758f * 180f));
            float AX = values[6] - calibration.get("ACCEL", "X");
            float AY = values[7] - calibration.get("ACCEL", "Y");
            float AZ = values[8] - calibration.get("ACCEL", "Z");
            float MX = values[3] - calibration.get("MAG", "X");
            float MY = values[4] - calibration.get("MAG", "Y");
            float MZ = -(values[5] - calibration.get("MAG", "Z"));
            update(GX, GY, GZ, AX, AY, AZ, MX, MY, MZ);
        }

        /// <summary>
        /// Updates the values saved for the IMU device during the calibration phase. Begins to call the
        /// Madgwick algorithm once the threshold has been met.
        /// </summary>
        /// <param name="values"> Short values converted from the raw integer values.</param>
        public void calibrate(short[] values)
        {
            calibration.countdown--;
            if (calibration.countdown < 600)
            {
                calibration.adjust("GYRO", "X", values[0]);
                calibration.adjust("GYRO", "Y", values[1]);
                calibration.adjust("GYRO", "Z", values[2]);
                float AX = values[6] - calibration.get("ACCEL", "X");
                float AY = values[7] - calibration.get("ACCEL", "Y");
                float AZ = values[8] - calibration.get("ACCEL", "Z");
                float MX = values[3] - calibration.get("MAG", "X");
                float MY = values[4] - calibration.get("MAG", "Y");
                float MZ = -(values[5] - calibration.get("MAG", "Z"));
                update(0, 0, 0, AX, AY, AZ, MX, MY, MZ);
            }

            if (calibration.countdown == 0)
            {
                calibration.set("GYRO", "X", calibration.get("GYRO", "X") / 600);
                calibration.set("GYRO", "Y", calibration.get("GYRO", "Y") / 600);
                calibration.set("GYRO", "Z", calibration.get("GYRO", "Z") / 600);
                calibration.set_calibrated(true);
            }
        }

        /// <summary>
        /// Implementation of the Magdewick algorithm, using IMU data to update orientation Quaternion.
        /// </summary>
        /// <param name="gx">Gyro X value</param>
        /// <param name="gy">Gyro Y value</param>
        /// <param name="gz">Gyro Z value</param>
        /// <param name="ax">Accel X value</param>
        /// <param name="ay">Accel Y value</param>
        /// <param name="az">Accel Z value</param>
        /// <param name="mx">Mag X value</param>
        /// <param name="my">Mag Y value</param>
        /// <param name="mz">Mag Z value</param>
        public void update(float gx, float gy, float gz, float ax, float ay, float az, float mx, float my, float mz)
        {
            float q1 = (float) processed.W, q2 = (float)processed.X, q3 = (float)processed.Y, q4 = (float)processed.Z;   // short name local variable for readability
            float norm;
            float hx, hy, _2bx, _2bz, _8bx, _8bz;
            float s1, s2, s3, s4;
            float qDot1, qDot2, qDot3, qDot4;

            // Auxiliary variables to avoid repeated arithmetic
            float _2q1mx;
            float _2q1my;
            float _2q1mz;
            float _2q2mx;
            float _4bx;
            float _4bz;
            float _2q1 = 2f * q1;
            float _2q2 = 2f * q2;
            float _2q3 = 2f * q3;
            float _2q4 = 2f * q4;
            float _2q1q3 = 2f * q1 * q3;
            float _2q3q4 = 2f * q3 * q4;
            float q1q1 = q1 * q1;
            float q1q2 = q1 * q2;
            float q1q3 = q1 * q3;
            float q1q4 = q1 * q4;
            float q2q2 = q2 * q2;
            float q2q3 = q2 * q3;
            float q2q4 = q2 * q4;
            float q3q3 = q3 * q3;
            float q3q4 = q3 * q4;
            float q4q4 = q4 * q4;

            // Normalise accelerometer measurement
            norm = (float)Math.Sqrt(ax * ax + ay * ay + az * az);
            if (norm == 0f) return; // handle NaN
            norm = 1 / norm;        // use reciprocal for division
            ax *= norm;
            ay *= norm;
            az *= norm;

            // Normalise magnetometer measurement
            norm = (float)Math.Sqrt(mx * mx + my * my + mz * mz);
            if (norm == 0f) return; // handle NaN
            norm = 1 / norm;        // use reciprocal for division
            mx *= norm;
            my *= norm;
            mz *= norm;

            // Reference direction of Earth's magnetic field
            _2q1mx = 2f * q1 * mx;
            _2q1my = 2f * q1 * my;
            _2q1mz = 2f * q1 * mz;
            _2q2mx = 2f * q2 * mx;
            hx = mx * q1q1 - _2q1my * q4 + _2q1mz * q3 + mx * q2q2 + _2q2 * my * q3 + _2q2 * mz * q4 - mx * q3q3 - mx * q4q4;
            hy = _2q1mx * q4 + my * q1q1 - _2q1mz * q2 + _2q2mx * q3 - my * q2q2 + my * q3q3 + _2q3 * mz * q4 - my * q4q4;
            _2bx = (float)Math.Sqrt(hx * hx + hy * hy);
            _2bz = -_2q1mx * q3 + _2q1my * q2 + mz * q1q1 + _2q2mx * q4 - mz * q2q2 + _2q3 * my * q4 - mz * q3q3 + mz * q4q4;
            _4bx = 2f * _2bx;
            _4bz = 2f * _2bz;
            _8bx = 2f * _4bx;
            _8bz = 2f * _4bz;
            // Gradient decent algorithm corrective step
            s1 = -_2q3 * (2f * q2q4 - _2q1q3 - ax) + _2q2 * (2f * q1q2 + _2q3q4 - ay) - _2bz * q3 * (_2bx * (0.5f - q3q3 - q4q4) + _2bz * (q2q4 - q1q3) - mx) + (-_2bx * q4 + _2bz * q2) * (_2bx * (q2q3 - q1q4) + _2bz * (q1q2 + q3q4) - my) + _2bx * q3 * (_2bx * (q1q3 + q2q4) + _2bz * (0.5f - q2q2 - q3q3) - mz);
            s2 = _2q4 * (2f * q2q4 - _2q1q3 - ax) + _2q1 * (2f * q1q2 + _2q3q4 - ay) - 4f * q2 * (1 - 2f * q2q2 - 2f * q3q3 - az) + _2bz * q4 * (_2bx * (0.5f - q3q3 - q4q4) + _2bz * (q2q4 - q1q3) - mx) + (_2bx * q3 + _2bz * q1) * (_2bx * (q2q3 - q1q4) + _2bz * (q1q2 + q3q4) - my) + (_2bx * q4 - _4bz * q2) * (_2bx * (q1q3 + q2q4) + _2bz * (0.5f - q2q2 - q3q3) - mz);
            s3 = -_2q1 * (2f * q2q4 - _2q1q3 - ax) + _2q4 * (2f * q1q2 + _2q3q4 - ay) - 4f * q3 * (1 - 2f * q2q2 - 2f * q3q3 - az) + (-_4bx * q3 - _2bz * q1) * (_2bx * (0.5f - q3q3 - q4q4) + _2bz * (q2q4 - q1q3) - mx) + (_2bx * q2 + _2bz * q4) * (_2bx * (q2q3 - q1q4) + _2bz * (q1q2 + q3q4) - my) + (_2bx * q1 - _4bz * q3) * (_2bx * (q1q3 + q2q4) + _2bz * (0.5f - q2q2 - q3q3) - mz);
            s4 = _2q2 * (2f * q2q4 - _2q1q3 - ax) + _2q3 * (2f * q1q2 + _2q3q4 - ay) + (-_4bx * q4 + _2bz * q2) * (_2bx * (0.5f - q3q3 - q4q4) + _2bz * (q2q4 - q1q3) - mx) + (-_2bx * q1 + _2bz * q3) * (_2bx * (q2q3 - q1q4) + _2bz * (q1q2 + q3q4) - my) + _2bx * q2 * (_2bx * (q1q3 + q2q4) + _2bz * (0.5f - q2q2 - q3q3) - mz);
            norm = 1f / (float)Math.Sqrt(s1 * s1 + s2 * s2 + s3 * s3 + s4 * s4);    // normalise step magnitude
            s1 *= norm;
            s2 *= norm;
            s3 *= norm;
            s4 *= norm;

            // Compute rate of change of quaternion
            qDot1 = 0.5f * (-q2 * gx - q3 * gy - q4 * gz) - 0.5f * s1;
            qDot2 = 0.5f * (q1 * gx + q3 * gz - q4 * gy) - 0.5f * s2;
            qDot3 = 0.5f * (q1 * gy - q2 * gz + q4 * gx) - 0.5f * s3;
            qDot4 = 0.5f * (q1 * gz + q2 * gy - q3 * gx) - 0.5f * s4;

            // Integrate to yield quaternion
            q1 += qDot1 * step_size;
            q2 += qDot2 * step_size;
            q3 += qDot3 * step_size;
            q4 += qDot4 * step_size;
            norm = 1f / (float)Math.Sqrt(q1 * q1 + q2 * q2 + q3 * q3 + q4 * q4);    // normalise quaternion
            processed = new Quaternion(q2 * norm, q3 * norm, q4 * norm, q1 * norm);
        }

        /// <summary>
        /// Compiles the connection debug information from this class and the underlying
        /// Bluetooth connection manager instance.
        /// </summary>
        /// <returns> Compiled connection debug dictionary.</returns>
        public Dictionary<string, bool> get_debug_information()
        {
            Dictionary<string, bool> dict = new Dictionary<string, bool>();
            dict.Add("Valid Calibration File", calibration.valid_config_file);
            dict.Add("Used MAC file", MAC_file);
            dict.Add("Device Found", device_found);
            dict.Add("Connection Established", bt_manager.connection_established);
            dict.Add("Stream Established", bt_manager.stream_established);
            dict.Add("Stream Live", bt_manager.connection_live);
            return dict;
        }

        /// <summary>
        /// Used to determine the current index within the records class when a repetition event occurs.
        /// </summary>
        /// <returns> Current index.</returns>
        public int get_currrent_index()
        {
            return records.record_sum;
        }

        /// <summary>
        /// Disconnection event thrown by underlying Bluetooth manager, inform connection manager.
        /// </summary>
        public void disconnection()
        {
            connection_manager.disconnection(IMU_number);
        }
    }
}
