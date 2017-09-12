// *************************************************************************************
// $Id: BTManager.cs 531 2017-08-21 14:33:43Z tjones $
// *************************************************************************************

using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Text;
using System.Threading;

namespace NUClass
{
    /// <summary>
    /// Handles the low-level connection to the IMU device through Bluetooth.
    /// </summary>
    class BTManager
    {
        /// <summary>
        /// 32Feet library client to handle Bluetooth connection.
        /// </summary>
        public BluetoothClient client = null;
        /// <summary>
        /// Port address used to handle Bluetooth connection.
        /// </summary>
        private Guid port;
        /// <summary>
        /// Debug report from connection attempt to the specified IMU device.
        /// </summary>
        private Dictionary<string, string> connection_details = new Dictionary<string, string>();
        /// <summary>
        /// Data stream acquired from the IMU device.
        /// </summary>
        private System.Net.Sockets.NetworkStream stream = null;
        /// <summary>
        /// 32Feet library representation of the IMU device's MAC address.
        /// </summary>
        private BluetoothAddress bt_address;
        /// <summary>
        /// 32Feet library representation of the IMU device.
        /// </summary>
        private BluetoothEndPoint end_point;
        /// <summary>
        /// Desired data packet size. [0:2] - start sequence, [3] - packet size, [4] - id, [5:22] - data, [23] - packet size.
        /// </summary>
        private int packet_size = 24;
        /// <summary>
        /// Parent IMUConnection instance.
        /// </summary>
        private IMUConnection IMU_connection;
        /// <summary>
        /// Boolean flagging whether to produces debug report for this IMU device.
        /// </summary>
        private bool debug_connection_reports = false;
        /// <summary>
        /// Integers used to represent start of a new packet from the data stream.
        /// </summary>
        private int[] sequence_start = { 221, 170, 85 };
        /// <summary>
        /// Thread instance which handles the asynchronous streaming of IMU data.
        /// </summary>
        public Thread get_data;
        /// <summary>
        /// Whether the stream should currently being read.
        /// </summary>
        public bool stream_open = false;
        /// <summary>
        /// Whether the streaming thread can be safely aborted.
        /// </summary>
        public bool safe_to_terminate = true;
        /// <summary>
        /// Current connection attempt being made.
        /// </summary>
        private int attempt_number = 1;
        /// <summary>
        /// Maximu connection attempts that can be made.
        /// </summary>
        private int max_attempts = 5;
        /// <summary>
        /// Whether or not to collect IMU firmware information.
        /// </summary>
        private bool connection_info;

        //Connection variables
        /// <summary>
        /// Is this the first attempt?
        /// </summary>
        public bool initial_connection { get; private set; } = true;
        /// <summary>
        /// Has a connection been made to the Bluetooth end point?
        /// </summary>
        public bool connection_established { get; private set; } = false;
        /// <summary>
        /// Has a stream instance been pulled from the Bluetooth connection?
        /// </summary>
        public bool stream_established { get; private set; } = false;
        /// <summary>
        /// Have the appropriate bytes been collected from the stream instance?
        /// </summary>
        public bool connection_live { get; private set; } = false;

        /// <summary>
        /// Constructs the class and assigns global variables from the passed parameters, then sets up
        /// basic connection variables.
        /// </summary>
        /// <param name="_IMU_connection"> Parent IMUConnection instance.</param>
        /// <param name="_bt_address"> Bluetooth address for IMU device.</param>
        /// <param name="_debug_connection_reports"> Produce debug report?</param>
        /// <param name="_connection_info"> Get IMU firmware information? </param>
        public BTManager(IMUConnection _IMU_connection, BluetoothAddress _bt_address, bool _debug_connection_reports, bool _connection_info)
        {
            IMU_connection = _IMU_connection;
            debug_connection_reports = _debug_connection_reports;
            bt_address = _bt_address;
            connection_info = _connection_info;
            db_report("Bluetooth Manager Created.");
            establish_bluetooth_connection();
        }

        /// <summary>
        /// Gets the port address and establishs the 32Feet end point for making a connection
        /// to the IMU device over Bluetooth.
        /// </summary>
        public void establish_bluetooth_connection()
        {
            port = BluetoothService.SerialPort;
            end_point = new BluetoothEndPoint(bt_address, port);
            db_report("End point defined.");
        }

        /// <summary>
        /// Attempts to connection over Bluetooth to the IMU device and establish a data stream from it. Provides several attempts
        /// at a connection and gets the IMU firmware number if required.
        /// </summary>
        /// <returns> T - Connection success, F - Connection failure.</returns>
        public bool connect()
        {
            while (!connection_established && attempt_number < max_attempts+1)
            {
                try
                {
                    client = new BluetoothClient();
                    db_report("Connection Attempt: " + attempt_number + "/" + max_attempts + "." );
                    client.Connect(end_point);
                    connection_established = true;
                    db_report("Connected.");
                }
                catch
                {
                    db_report("Connection Failed. " + (max_attempts - attempt_number) + " remaining.");
                    attempt_number++;
                   
                }
            }
            if (!connection_established)
            {
                db_report("Out of Connection Attempts.");
                return false;
            }
            else
            {
                stream = client.GetStream();
                stream_established = true;
                db_report("Stream Established");
                byte[] buf = new byte[50];
                int readLen = stream.Read(buf, 0, 15);
                var welcome = Encoding.ASCII.GetString(buf);
                db_report(welcome);

                if (welcome.Contains("Connection Open"))
                {
                    connection_live = true;
                    if (!connection_info)
                    {
                        send_data("RegR");
                        stream.ReadTimeout = 5000;
                        try
                        {
                            while (!stream.DataAvailable)
                            {
                                // Waiting for IMU to begin streaming 
                            }
                            int i = 0;
                            int[] info_data = new int[42];
                            while (i < 42)
                            {
                                int val = stream.ReadByte();
                                info_data[i] = val;
                                i++;
                            }
                            IMU_connection.set_connection_info(info_data[39], info_data[40], establish_device_details(IMU_connection.IMU_device_name));
                        }
                        catch (System.IO.IOException)
                        {
                            return false;
                        }
                    }
                    
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Creates the thread to asynchronously handle the data stream and starts it.
        /// </summary>
        public void start_streaming()
        {
            get_data = new Thread(() => stream_data());
            get_data.Start();
        }

        /// <summary>
        /// Function used to handle the data stream. Send the initiate command, waits for data to become available and then compiles
        /// the stream into correct data packets. Continues while 'stream_open' is true, throws a a disconnect event if no data read
        /// for two seconds.
        /// </summary>
        public void stream_data()
        {
            stream_open = true;
            safe_to_terminate = false;
            send_data("M4dg");
            int[] stream_cache = new int[packet_size];
            Stopwatch wait = Stopwatch.StartNew();
            while (!stream.DataAvailable)
            {
                if (wait.ElapsedMilliseconds > 2000)
                {
                    stream_open = false;
                    IMU_connection.disconnection();
                    break;
                }
                // Waiting for IMU to begin streaming 
            }
            Stopwatch sw = Stopwatch.StartNew();
            stream.ReadTimeout = 2000;
          
            while (stream_open)
            {
                try
                {
                    int read = stream.ReadByte();
                    
                    while (read != sequence_start[0]) // Loop until match first marker
                    {
                        read = stream.ReadByte();
                    }
                    read = stream.ReadByte();
                    if (read == sequence_start[1]) // Check second marker
                    {
                        read = stream.ReadByte();
                        if (read == sequence_start[2]) // Check third marker
                        {
                            int init_len = stream.ReadByte(); //Len
                            read = stream.ReadByte(); //ID
                            for (var i = 0; i < packet_size; i++)
                            {
                                stream_cache[i] = stream.ReadByte();
                            }
                            int end_len = stream.ReadByte();
                            if (init_len == end_len)
                            {
                                long elapsed = sw.ElapsedTicks;
                                IMU_connection.data_read_event(stream_cache, elapsed);
                                //db_report("BOOP");
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    // DC
                    IMU_connection.disconnection();
                }
            }
            if (!stream_open)
            {
                safe_to_terminate = true;
            }
            
        }

        /// <summary>
        /// Writes the passed byte to the IMU device.
        /// </summary>
        /// <param name="new_data">Byte to send to IMU device.</param>
        public void send_data(byte new_data)
        {
            stream.WriteByte(new_data);
        }

        /// <summary>
        /// Send a string to the IMU device, breaking it down into the corresponding bytes.
        /// </summary>
        /// <param name="new_data">String message to be converted into bytes.</param>
        public void send_data(string new_data)
        {
            try
            {
                stream.WriteByte(0x07);
                foreach (char c in new_data)
                {
                    stream.WriteByte((byte)c);
                }
                stream.WriteByte(0x0B);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Console debugging output if toggled on.
        /// </summary>
        /// <param name="message"> Debug message.</param>
        private void db_report(string message)
        {
            if (debug_connection_reports)
            {
                System.Diagnostics.Debug.WriteLine(IMU_connection.IMU_number + ": " + message);
            }
        }

        /// <summary>
        /// Acquires the Bluetooth version number being used to connect to the Bluetooth device.
        /// </summary>
        /// <param name="IMU_device_name"> Formatted string name of the IMU device.</param>
        /// <returns> String representation of the Bluetooth version number.</returns>
        private string establish_device_details(string IMU_device_name)
        {
            connection_details = new Dictionary<string, string>();
            ManagementObjectSearcher objSearcher = new ManagementObjectSearcher("Select * from Win32_PnPSignedDriver");
            ManagementObjectCollection objCollection = objSearcher.Get();

            foreach (ManagementObject obj in objCollection)
            {
                if ((string)obj["FriendlyName"] == IMU_device_name)
                {
                    return (string)obj["DriverVersion"];
                }
            }
            return null;
        }
    }
}


