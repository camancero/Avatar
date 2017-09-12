// *************************************************************************************
// $Id: ConnectionManager.cs 593 2017-09-06 09:49:34Z efranco $
// *************************************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InTheHand.Net.Bluetooth;
using System.Text.RegularExpressions;
using System.Windows.Media.Media3D;

/// <summary>
/// This project provides and interface with which to connect with and control several IMU devices simulataneously. 
/// The project stores the raw data from each IMU as well as quaternion and MMG data processed from each read.
/// It outputs a processed and raw CSV file detailing the collected data.
/// </summary>
namespace NUClass
{
    /// <summary>
    /// A delegate used to contain functions that occur when a
    /// bluetooth device disconnects from the tablet.
    /// </summary>
    public delegate void BluetoothDisconnect();

    /// <summary>
    /// Main interface of the NUClass project - instantiates other classes and handles event calls from connected GarmentInterface instance.
    /// Provides high-level debug information relating to system configuration and compiles final output when an exercise is complete.
    /// </summary>
    public class ConnectionManager
    {
        // Manually set variables
        /// <summary>
        /// Statically set amount of IMU devices to be connected.
        /// </summary>
        private static int IMU_COUNT = 4;
        /// <summary>
        /// Manualy set boolean for increased debugging output.
        /// </summary>
        private static bool DEBUG = true;
        /// <summary>
        /// Manually set boolean indicating whether or not to use an existing calibration file.
        /// </summary>
        private static bool USE_CALIBRATION = false;
        /// <summary>
        /// Include a repetition string on the end of the processed CSVs.
        /// </summary>
        public static bool END_REPETITION_STRING = false;
        

        // Variables
        /// <summary>
        /// Amount of repetition events triggered whilst conducting the current exercise.
        /// </summary>
        private int repetition_count = 0;
        /// <summary>
        /// Disconnect event for triggering interuption of main thread.
        /// </summary>
        private BluetoothDisconnect disconnect_event;
        /// <summary>
        /// Boolean array of whether IMU devices are currently connected.
        /// </summary>
        public bool[] IMU_connected = new bool[IMU_COUNT];
        /// <summary>
        /// Internal list of the unformatted IMU number read from the 'imunames.txt' file.
        /// </summary>
        public string[] IMU_numbers = new string[IMU_COUNT];
        /// <summary>
        /// List of the IMU locations on the garment. Corresponds with the order of the IMU_numbers.
        /// </summary>
        public string[] IMU_locations = { "Upper Arm", "Forearm", "Chest", "Hand" };
        /// <summary>
        /// Mapping of IMU numbers to instance of IMUConnection.
        /// </summary>
        public Dictionary<string, IMUConnection> IMU_connections = new Dictionary<string, IMUConnection>();
        /// <summary>
        /// Formatting string added to the processed CSV when a repetition event occurs.
        /// </summary>
        private string processed_rep_string = "10.000,10.000,10.000,10.000,10.000,10.000,10.000,10.000,10.000,10.000,10.000,10.000,10.000,10.000,10.000,10.000,10.000,10.000,10.000,10.000,";
        /// <summary>
        /// Formatting string added to the raw CSV when a repetition event occurs
        /// </summary>
        private string raw_rep_string = "-10.00,-10.00,-10.00,-10.00,-10.00,-10.00,-10.00,-10.00,-10.00,-10.00,-10.00,-10.00,-10.00,-10.00,-10.00,-10.00,-10.00,-10.00,-10.00,-10.00,-10.00,-10.00,-10.00,-10.00,-10.00,";
        /// <summary>
        /// List of formatted strings detailing the IMU number and raw integer reads of the IMU devices ready to be written to the raw CSV output.
        /// </summary>
        private List<string> raw_data = new List<string>();
        /// <summary>
        /// List of formatted strings detailing the processed quaternions and MMG readings of the IMU devices ready to be written to the processed CSV output.
        /// </summary>
        private List<string> processed_data = new List<string>();
        /// <summary>
        /// Location of the output directory passed from the GarmentInterface.
        /// </summary>
        private string file_location;
        /// <summary>
        /// Report on the Bluetooth version and IMU firmware version of each IMU device connected, compiled at the end of each connect event.
        /// </summary>
        public Dictionary<string, Dictionary<string, string>> device_information { get; private set; }
        /// <summary>
        /// Time stamp of when the stream starts.
        /// </summary>
        private DateTime stream_start;
        /// <summary>
        /// Time stamp of when stream ends.
        /// </summary>
        private DateTime stream_end;
        /// <summary>
        /// List of the indices when repetion events occurred.
        /// </summary>
        private List<int> repetitions;

        // Error variables
        /// <summary>
        /// Report on the connection configuration and attempts produced when making a connection attempt. Details where a connection attempt
        /// failed.
        /// </summary>
        public Dictionary<string, Dictionary<string, bool>> debug_information { get; private set; }
        /// <summary>
        /// Are the IMU numbers provided unique?
        /// </summary>
        public bool unique_device_numbers { get; private set; } = true;
        /// <summary>
        /// Does the names file have to correct amount of entries?
        /// </summary>
        public bool valid_names_file { get; private set; } = true;
        /// <summary>
        /// Is Bluetooth enabled on this device?
        /// </summary>
        public bool bluetooth_enabled { get; private set; } = true;

        public Quaternion[] current_quaternions = new Quaternion[4];
        

        /// <summary>
        /// Constructor establishes the IMU numbers from the 'imunames.txt' file before validating them. 
        /// Checks whether Bluetooth is enabled.
        /// If Bluetooth is enabled and a valid list of IMU numbers is provided, begins connection attempts.
        /// </summary>
        public ConnectionManager()
        {
            set_IMU_names();
            check_bluetooth_enabled();
            if (unique_device_numbers && bluetooth_enabled)
            {
                set_connections();
            }
        }

        /// <summary>
        ///  Attempts to read the 'imunames.txt' file, calls 'validate_names_file' and checks for unique entries.
        /// </summary>
        private void set_IMU_names()
        {
            try
            {
                //string imuNames = MMarkInfo.ImuNamesFile;
                string imuNames = @"C:\Users\Nathan\AppData\Local\M-Mark\M-Mark\IMUNames\avatar_imu.txt";
                IMU_numbers = File.ReadAllLines(imuNames);
                validate_names_file(IMU_numbers);

                string[] unique_names = IMU_numbers.Distinct().ToArray();
                if (IMU_numbers.Length > unique_names.Length)
                {
                    unique_device_numbers = false;
                }
            }
            catch (IOException)
            {
                valid_names_file = false;
            }
        }


        /// <summary>
        /// Checks IMU_numbers for the right amount of correctly formatted IMU numbers. 
        /// </summary>
        /// <param name="IMU_numbers"> List of IMU numbers provided by 'imunames.txt'.</param>
        private void validate_names_file(string[] IMU_numbers)
        {
            if (IMU_numbers.Length == IMU_COUNT)
            {
                foreach (string IMU_name in IMU_numbers)
                {
                    if (!Regex.IsMatch(IMU_name, "^[0 - 9] + $"))
                    {
                        valid_names_file = false;
                    }
                }
                valid_names_file = true;
            } else
            {
                valid_names_file = false;
            }
        }


        /// <summary>
        /// Is Bluetooth enabled on this device?
        /// </summary>
        private void check_bluetooth_enabled()
        {
            if (BluetoothRadio.PrimaryRadio == null)
            {
                this.bluetooth_enabled = false;
            }
            else
            {
                this.bluetooth_enabled = true;
            }
        }


        /// <summary>
        /// Populates the connections array with a new instance of IMUConnection for each IMU.
        /// </summary>
        private void set_connections()
        {
            foreach (string IMU_name in IMU_numbers)
            {
                IMU_connections.Add(IMU_name, new IMUConnection(IMU_name, this, DEBUG, USE_CALIBRATION));
            }
        }


        /// <summary>
        /// Takes the processed quaternions from all IMU devices and their corresponding MMG reads
        /// and formats them ready to be written to the processed CSV file.
        /// </summary>
        /// <param name="entries">Processed quaternions from each IMU.</param>
        /// <param name="MMG_entries">Processed MMG data from each IMU.</param>
        /// <returns></returns>
        private string compile_processed_entry(Quaternion[] entries, float[] MMG_entries)
        {
            string ans = "";
            int index = 0;
            foreach (Quaternion q in entries)
            {
               //ans += q.stringify() + MMG_entries[index].ToString("0.000") + ",";
                index++;
            }
            return ans + MMG_entries[index].ToString("0.000") + ",";
        }

        /// <summary>
        /// Takes the raw data input from all IMU devices and formats them ready to be
        /// written to the raw CSV file.
        /// </summary>
        /// <param name="values"></param>
        private void compile_raw_entry(int[][] values)
        {
            for(var x = 0; x < IMU_COUNT; x++)
            {
                string str = IMU_numbers[x] + ",";
                for (var i = 0; i < values[x].Length; i++)
                {
                    str+= values[x][i].ToString() +",";
                }
                raw_data.Add(str);
            }
        }

        /// <summary>
        /// Starts parallel threads for each IMU listed in IMU_numbers which attempt to establish a connection to the specified IMU. Updates local connection statuses and debug info. 
        /// </summary>
        /// <param name="_disconnect_event"> Bluetooth disconnect event</param>
        public void connection_event()
        {
            if (DEBUG)
            {
                System.Diagnostics.Debug.WriteLine("Connection Event");
            }
            //disconnect_event = _disconnect_event;
            IMU_connected = new bool[IMU_COUNT];
            int index = 0;
            foreach(KeyValuePair<string,IMUConnection> connection in IMU_connections){
                bool connected = connection.Value.connect();
                if (!connected)
                {
                  //  TODO: break loop if one has already failed. 
                }
                IMU_connected[index] = connected;
                index++;
            }
            build_debug_report();
            build_device_report();
        }

        /// <summary>
        /// Loops the IMU connections and invokes the call to close the underlying connection.
        /// </summary>
        public void close_connections()
        {
            if (DEBUG)
            {
                System.Diagnostics.Debug.WriteLine("Disconnection Event");
            }
            foreach (KeyValuePair<string, IMUConnection> connection in IMU_connections)
            {
                connection.Value.close();
            }
        }

        /// <summary>
        /// Called at the end of each connection event to compile the connection debug report from local
        /// variables as well as aggregating from each IMU connection.
        /// </summary>
        private void build_debug_report()
        {
            debug_information = new Dictionary<string, Dictionary<string, bool>>();
            Dictionary<string, bool> global = new Dictionary<string, bool>();
            global.Add("Bluetooth Enabled", bluetooth_enabled);
            global.Add("Valid Names File", valid_names_file);
            global.Add("Unique IMU Names", unique_device_numbers);
            debug_information.Add("GLOBAL", global);

            for (var i = 0; i < IMU_COUNT; i++)
            {
                string name = IMU_numbers[i];
                string location = IMU_locations[i];
                debug_information.Add(name + " - " + location, IMU_connections[name].get_debug_information());
            }
        }

        /// <summary>
        /// Called at the end of each connection event to compile the high-level debug information
        /// on the system and its configuration.
        /// </summary>
        private void build_device_report()
        {
            device_information = new Dictionary<string, Dictionary<string, string>>();
            for (var i = 0; i < IMU_COUNT; i++)
            {
                string name = IMU_numbers[i];
                string location = IMU_locations[i];
                device_information.Add(name + " - " + location, IMU_connections[name].connection_information);
            }
        }

        
        /// <summary>
        /// Start streaming event. Removes old data and triggers streaming from each IMU connection. Timestamps the start of streaming.
        /// </summary>
        /// <param name="output_dir"> The target directory for the processed and raw CSV.</param>
        public void stream_data(string output_dir)
        {

            //connection_test();
            clear_data();
            stream_toggle(true);
            stream_start = DateTime.Now;
            file_location = output_dir;
        }

        public void return_quaternion(Quaternion q, string IMU_number)
        {
            int x = Array.IndexOf(IMU_numbers, IMU_number);
            current_quaternions[x] = q;
            //System.Diagnostics.Debug.WriteLine("E" + q);
        }

        /// <summary>
        /// Method invoked by the start and end streaming events to loop through the IMU connections and toggle their underlying
        /// data streams on or off.
        /// </summary>
        /// <param name="stream_bool"> Whether to open or close the data streams, T-open / F-close.</param>
        private void stream_toggle(bool stream_bool)
        {
            foreach (KeyValuePair<string, IMUConnection> connection in IMU_connections)
            {
                connection.Value.set_stream(stream_bool);
            }
        }

        /// <summary>
        /// Resets all the collections and values stored from the previous read when starting a new stream event.
        /// </summary>
        private void clear_data()
        {
            raw_data = new List<string>();
            processed_data = new List<string>();
            repetitions = new List<int>();
            repetition_count = 0;
        }
    
        /// <summary>
        /// Takes the MMG readings from all connected IMUs and extracts the important reads.
        /// </summary>
        /// <param name="MMG_readings"> Processed MMG reads from each IMU.</param>
        /// <returns></returns>
        private float[] get_MMG_readings(float[][] MMG_readings)
        {
            return new float[] { MMG_readings[0][2], MMG_readings[0][0], MMG_readings[0][1], MMG_readings[1][0], MMG_readings[1][1] };
        }
     
        /// <summary>
        /// End of exercise event invoked from the GarmentInterface. Pauses the IMU connection streams and determines the path for each file to be output before aggregating 
        /// the IMU data, formatting it and then printing it to CSV files.
        /// </summary>
        /// <param name="patient_num"> Patient number.</param>
        /// <param name="task_name"> Exercise being performed.</param>
        /// <param name="task_mode"> Left or Right handed attempt.</param>
        /// <returns></returns>
        public string end_event(string patient_num, string task_name, string task_mode)
        {
            string date_str = "-" + DateTime.Now.ToString("s").Replace("\"", "-").Replace("/", "-").Replace(":", "-");
            string info_str = "Patient" + patient_num + " - " + task_name + " - " + task_mode;
            string raw_output_path = file_location + info_str + date_str + "_raw.csv";
            string processed_output_path = file_location + info_str + date_str + ".csv";
            stream_toggle(false);
            stream_end = DateTime.Now;
            aggregate_results();
            if (END_REPETITION_STRING)
            {
                processed_data.Add(processed_rep_string);
            }
            print_results(raw_data, raw_output_path);
            print_results(processed_data, processed_output_path);
            return processed_output_path;
        }

        /// <summary>
        /// Collects all the reads from each IMU connections  and restuctures it into formatted strings which are appended
        /// to the raw and processed data lists. Also inserts the repetition event strings at the appropriate instances 
        /// based on earlier recording of when they occurred.
        /// </summary>
        private void aggregate_results()
        {
            Records[] records = new Records[IMU_COUNT];
            int current_rep = 0;
            int next_repetion = int.MaxValue;
            if(repetition_count > 0) //If there was a repetion event...
            {
                next_repetion = repetitions[0]; // ...assign it.
            }
            for (var x = 0; x < IMU_COUNT; x++)
            {
                records[x] = IMU_connections[IMU_numbers[x]].records;
            }
                int[] ranges = new int[IMU_COUNT];
            // Get the amount of reads from each IMU device.
            for(var x = 0; x < IMU_COUNT; x++)
            {
                ranges[x] = records[x].record_sum;
            }
            // Get the lowest amount.
            int range = ranges.Min()-1;
            for(var x = 0; x < range; x++)//Loop within the lowest range...
            {
                if (x > next_repetion) // If it is time for a repetion event...
                {   // ... add the appropriate strings to the data lists.
                    raw_data.Add(raw_rep_string);
                    processed_data.Add(processed_rep_string);
                    // If there are more repetion events, assign them.
                    if (current_rep < repetition_count - 1)
                    {
                        current_rep++;
                        next_repetion = repetitions[current_rep];
                    }
                    // Else if there are no repetiton events invalidate it.
                    else if (current_rep == repetition_count - 1)
                    {
                        next_repetion = int.MaxValue;
                    }
                }

            Object[][] latest_reads = new Object[IMU_COUNT][];
                // Loop the IMU devices getting the next processed and raw data.
                for (var y = 0; y < IMU_COUNT; y++)
                {
                    latest_reads[y] = records[y].get_record(x);
                }
                process_reads(latest_reads, x);
            }
        }

        /// <summary>
        /// Breaks down the data from a data recording from each of the IMU devices.
        /// </summary>
        /// <param name="reads"> All the data from all the IMUs.</param>
        /// <param name="current_index"> Read number.</param>
        private void process_reads(Object[][] reads,int current_index)
        {
            long[] datetimes = new long[IMU_COUNT];
            int[][] raw_inputs = new int[IMU_COUNT][];
            Quaternion[] quaternions = new Quaternion[IMU_COUNT];
            float[][] MMG = new float[IMU_COUNT][];
            
            for(var x = 0; x < IMU_COUNT; x++) // For each IMU...
            {   //.. cast objects.
                datetimes[x] = (long) reads[x][0];
                raw_inputs[x] = (int[])reads[x][1];
                quaternions[x] = (Quaternion)reads[x][2];
                MMG[x] = (float[])reads[x][3];
            }
            processed_data.Add(compile_processed_entry(quaternions, get_MMG_readings(MMG)));
            compile_raw_entry(raw_inputs);
        }
        
        /// <summary>
        /// Outputs a list of strings to the desired file path.
        /// </summary>
        /// <param name="data"> A list of  strings to be written to a file.</param>
        /// <param name="path"> Path as to where to output the file.</param>
        private void print_results(List<string> data, string path)
        {
            List<string> report_strings = new List<string>();
            foreach (string kv in data)
            {
                report_strings.Add(kv);
            }
            System.IO.File.WriteAllLines(path, report_strings);
        }

        /// <summary>
        /// Event fired when calling a repetition from the GarmentInterface. Checks the current read count of each
        /// IMU device and makes a note of the latest index for the repetition event to occur at.
        /// </summary>
        public void repetition_event()
        {
            repetition_count++;
            int[] tar = new int[IMU_COUNT];
            int index = 0;
            foreach(KeyValuePair<string, IMUConnection> connection in IMU_connections)
            {
                tar[index] = connection.Value.get_currrent_index();
                index++;
            }
            repetitions.Add(tar.Max());
        }

        /// <summary>
        /// Invoke the 'disconnection_event' to interupt the main thread if one of the IMUs disconnects.
        /// </summary>
        /// <param name="IMU_number">IMU number of the IMU device which has disconnected.</param>
        public void disconnection(string IMU_number)
        {
            int index = Array.IndexOf(IMU_numbers, IMU_number);
            System.Diagnostics.Debug.WriteLine("Passed Number: " + IMU_number + ", List-matched number: " + IMU_numbers[index] +", Index: " + index + ", Location: " + IMU_locations[index]);
            IMU_connected[index] = false;
            disconnect_event();
        }
    }
}
