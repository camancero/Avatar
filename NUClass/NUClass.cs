// *************************************************************************************
// $Id: NUClass.cs 407 2017-07-19 13:04:19Z efranco $
// *************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using Communications;
using InTheHand.Net.Sockets;
using InTheHand.Net;
using System.Windows.Media.Media3D;

namespace NUClass
{
    public class Flag:EventArgs
    {
        public readonly int address;
        public readonly string data;
        public readonly byte [] TCPdata;
        public readonly int Class;
        public readonly string name;
        public readonly int result;
        public readonly bool triggered;
        public readonly string StringData;
        public readonly Quaternion quaternion;
        public readonly byte[] unityData; 
        public readonly bool unity; 

        public Flag(int _address)
        {
            this.Class = 1;
            this.address = _address;
        }
        public Flag(int _address, string _data)
        {
            this.Class = 2;
            this.data = _data;
            this.address = _address;
        }
        // 160516 EF
        public Flag(int _address, Quaternion _quaternion)
        {
            this.Class = 9;
            this.address = _address;
            this.quaternion = _quaternion;
        }
        //
        public Flag(int _address, byte[] _TCPdata)
        {
            this.Class = 3;
            this.TCPdata = _TCPdata;
            this.address = _address;
        }
        public Flag(string _name, int _result)
        {
            this.Class = 4;
            this.name = _name;
            this.result = _result;
        }
        public Flag(string _StringData, bool diff)
        {
            this.Class = 5;
            this.StringData = _StringData;
        }
        public Flag(bool _triggered)
        {
            this.Class = 6;
            this.triggered = _triggered;
        }
        public Flag(bool _unity, byte[] _data, int _address)
        {
            this.Class = 7;
            this.address = _address;
            this.unityData = _data;
        }
    }
    public class NU
    {

        int PERIOD;
        int FREQ;
        int ADC;
        int PS;
        int gscale;
        int grate;
        int ascale;
        int arate;
        int mscale;
        int mrate;
        int freq;
        public bool Kat = false;
        public string name;
        public int address;
        public bool saveData = false;
        Calibration cali;
        public Quaternion quat = new Quaternion(0, 0, 0, 1);
        public float[] mmg = new float[] { 0, 0, 0 };
        public float[] rawmmg = new float[] { 0, 0, 0};
        float samplePeriod = 0.005f;
        CommunicationsClass Comms;
        public bool bluetoothConnection = false;
        public bool USBConnection = false;
        int slow = 0;
        byte[] unitydata = new byte[] {0,0,0,0,0,0,0,0 };
        float sample_period;

        public byte openclose = 0;
        public List<int>[] MMGChannel = { new List<int> { }, new List<int> { }, new List<int> { }, new List<int> { }, new List<int> { }, new List<int> { }, new List<int> { }, new List<int> { } };
        public List<double>[] HandControlProcessorMemory = { new List<double> { }, new List<double> { }, new List<double> { }, new List<double> { }, new List<double> { }, new List<double> { }, new List<double> { }, new List<double> { } };
        public int[] Debouncer = { 0, 0, 0, 0, 0, 0, 0, 0 };
        ManualResetEvent[] doneEvents = new ManualResetEvent[7];

        protected virtual void DataReady()
        {
            if (setflag != null) setflag(new Flag(address));
        }  
        protected virtual void DataReady(string data)
        {
            if (setflag != null) setflag(new Flag(address, data));
        }
        protected virtual void DataReady(byte[] data)
        {
            if (setflag != null) setflag(new Flag(address, data));
        }
        protected virtual void WriteDataToBox(string _StringData)
        {
            if (setflag != null) setflag(new Flag(_StringData, true));
        }
        protected virtual void HandTriggered()
        {
            if (setflag != null) setflag(new Flag(true));
        }
        protected virtual void DataReadyUnity(bool unity, byte[] _data)
        {
            if (setflag != null) setflag(new Flag(unity, _data, address));
        }
        // change 160516 EF
        protected virtual void DataReady(Quaternion quat)
        {
            if (setflag != null) setflag(new Flag(address, quat));
        }
        //
        public delegate void DataReadyEvent(Flag e);
        public event DataReadyEvent setflag;

        public static NU NUClass;
        
        public void initialise(string _name) {
            name = _name;
            string imuBTName = "IMU_BT_" + name;
            if (name.Length == 4)
            {
                imuBTName = "IMU_BT_" + name;
            }
            else
            {
                 imuBTName = "NU_BT_" + name;
            }
            
            string imuUSBName = "IMU_USB_" + name;
            // TODO debug this directory!
            string imuMemory = MMark.Info.MMarkInfo.MMarkResources + "IMU_Memory/IMU_BT_" + name + ".txt";

            NUClass = this;
            bool bluetooth = true;
            try
            {
                Comms = new CommunicationsClass();                      //Attempt to create bluetooth object (Fails if computer does not support bluetooth)
            }
            catch (Exception)
            {
                WriteDataToBox("The computer does not appear to have bluetooth capabilities\n");
                bluetooth = false;
            }

            bluetoothConnection = BluetoothConnect(imuMemory, bluetooth, imuBTName);
            if (bluetoothConnection && USBConnection)
            {
                
            }
            if (bluetoothConnection)
            {
                cali = new Calibration(name);
            }
            
        }
        public void ChannelSelect(int channel)
        {
            if (channel == 0)
            {
                USBConnection = false;
            }
            else if (channel == 1)
            {
                bluetoothConnection = false;
            }
        }

        public void Dispose()
        {
            Comms.Dispose();
        }
        
        public void setAllReg(int[] Variables)
        {
            PERIOD = Variables[0];
            FREQ = Variables[1];
            ADC = Variables[2];
            PS = Variables[3];
            gscale = Variables[4];
            grate = Variables[5];
            ascale = Variables[6];
            arate = Variables[7];
            mscale = Variables[8];
            mrate = Variables[9];

            byte p1 = (byte)(PERIOD >> 8);

            sendClearData(7);
            sendClearData(67);
            sendClearData(1);
            sendClearData((byte)(PERIOD >> 8));
            sendClearData((byte)(PERIOD));
            sendClearData(11);

            System.Threading.Thread.Sleep(100);
            sendClearData(7);
            sendClearData(67);
            sendClearData(2);
            sendClearData((byte)(FREQ >> 8));
            sendClearData((byte)(FREQ));
            sendClearData(11);
            System.Threading.Thread.Sleep(100);
            sendClearData(7);
            sendClearData(67);
            sendClearData(3);
            sendClearData((byte)(ADC >> 8));
            sendClearData((byte)(ADC));
            sendClearData(11);
            System.Threading.Thread.Sleep(100);
            sendClearData(7);
            sendClearData(67);
            sendClearData(4);
            sendClearData((byte)(PS >> 8));
            sendClearData((byte)(PS));
            sendClearData(11);
            System.Threading.Thread.Sleep(100);
            sendClearData(7);
            sendClearData(67);
            sendClearData(5);
            sendClearData((byte)(gscale >> 8));
            sendClearData((byte)(gscale));
            sendClearData(11);
            System.Threading.Thread.Sleep(100);
            sendClearData(7);
            sendClearData(67);
            sendClearData(6);
            sendClearData((byte)(grate >> 8));
            sendClearData((byte)(grate));
            sendClearData(11);
            System.Threading.Thread.Sleep(100);
            sendClearData(7);
            sendClearData(67);
            sendClearData(7);
            sendClearData((byte)(ascale >> 8));
            sendClearData((byte)(ascale));
            sendClearData(11);
            System.Threading.Thread.Sleep(100);
            sendClearData(7);
            sendClearData(67);
            sendClearData(8);
            sendClearData((byte)(arate >> 8));
            sendClearData((byte)(arate));
            sendClearData(11);
            System.Threading.Thread.Sleep(100);
            sendClearData(7);
            sendClearData(67);
            sendClearData(9);
            sendClearData((byte)(mscale >> 8));
            sendClearData((byte)(mscale));
            sendClearData(11);
            System.Threading.Thread.Sleep(100);
            sendClearData(7);
            sendClearData(67);
            sendClearData(10);
            sendClearData((byte)(mrate >> 8));
            sendClearData((byte)(mrate));
            sendClearData(11);
            System.Threading.Thread.Sleep(100);
            sendClearData(7);
            sendClearData(67);
            sendClearData(11);
            sendClearData((byte)(mrate >> 8));
            sendClearData((byte)(mrate));
            sendClearData(11);

            freq = getFrequency() / (FREQ+1);
            samplePeriod = 1f / (float)freq;

        }
        public void initfreq()
        {
            freq = getFrequency() / (FREQ + 1);
            samplePeriod = 1f / (float)freq;
        }
        
        private bool BluetoothConnect(string imuMemory, bool bluetooth, string imuBTName)
        {
            BluetoothDeviceInfo[] devices;
            WriteDataToBox("Connecting to IMU\n");
            String[] deviceaddress = new String[] { "" };
            if (File.Exists(imuMemory))
            {
                deviceaddress = File.ReadAllLines(imuMemory);
            }
            if (deviceaddress[0].Length > 0 && bluetooth == true)
            {

                WriteDataToBox("Attempting Quick Connect\n");
                byte[] address = { 0, 0, 0, 0, 0, 0 };

                for (int i = 0; i < 6; i++)
                {
                    address[i] = (byte)(Convert.ToInt32((deviceaddress[0][10 - (2 * i)].ToString() + deviceaddress[0][11 - (2 * i)].ToString()), 16));
                }
                BluetoothAddress btaddress = new BluetoothAddress(address);
                if (Comms.connectthroughbluetoothaddress(btaddress))
                {
                    WriteDataToBox("Connection Successful\n");
                    Comms.DataIn += new CommunicationsClass.EventHandler(Comms_OnDataIn);
                    return true;
                }
                else
                {


                    if (bluetooth == true)
                    {
                        WriteDataToBox("Searching for Paired Bluetooth Device...\n");
                        BluetoothClient client = new BluetoothClient();
                        devices = client.DiscoverDevices();
                        foreach (BluetoothDeviceInfo d in devices)
                        {
                            if (d.DeviceName == imuBTName)
                            {
                                File.WriteAllText(imuMemory, d.DeviceAddress.ToString());
                                WriteDataToBox("Device Found\nAttempting Connection...\n");
                                if (Comms.connectthroughbluetooth(d))
                                {
                                    WriteDataToBox("Connection Successful\n");
                                    Comms.DataIn += new CommunicationsClass.EventHandler(Comms_OnDataIn);
                                    return true;
                                }
                                else
                                {
                                    WriteDataToBox("Connection Failed\n");
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            else
            {
                if (bluetooth == true)
                {
                    WriteDataToBox("Searching for Paired Bluetooth Device...\n");
                    BluetoothClient client = new BluetoothClient();
                    devices = client.DiscoverDevices();
                    foreach (BluetoothDeviceInfo d in devices)
                    {
                        if (d.DeviceName == imuBTName)
                        {
                            try
                            {
                                if (!File.Exists(imuMemory))
                                {
                                    FileStream imuMemoryFile = File.Create(imuMemory);
                                    Byte[] imuAddress = new UTF8Encoding(true).GetBytes(d.DeviceAddress.ToString());
                                    imuMemoryFile.Write(imuAddress, 0, imuAddress.Length);
                                    imuMemoryFile.Close();
                                }
                                else
                                {
                                    File.AppendAllText(imuMemory, d.DeviceAddress.ToString());
                                }
                                WriteDataToBox("Device Found\nAttempting Connection...\n");
                                if (Comms.connectthroughbluetooth(d))
                                {
                                    WriteDataToBox("Connection Successful\n");
                                    Comms.DataIn += new CommunicationsClass.EventHandler(Comms_OnDataIn);
                                    return true;
                                }
                                else
                                {
                                    WriteDataToBox("Connection Failed\n");
                                    return false;
                                }
                            }
                            catch(Exception)
                            {
                                    
                            }
                        }
                    }
                }
            }
            return false;
        }


        void Comms_OnDataIn(Communications.Message_EventArgs e)
        {
            OnDataIn(e.RawMessage);
            try { e.RawMessage.RemoveRange(0, e.RawMessage.Count); }
            catch (Exception)
            {
            }
        }
        public void sendData(string data)
        {
            if (bluetoothConnection)
            {
                Comms.senddata(data);
            }
         
        }
        public void sendClearData(byte data)
        {
            if (bluetoothConnection)
            {
                Comms.senddata(data);
            }
        }

        public void StopNU()
        {
            cali.caliCountDown = 150;
            sendData("M0dg");
            Comms.getStream();
        }

        public void initReg()
        {
            sendData("RegR");
        }
       
        public void M1dg()
        {
            sendData("M1dg");
        }
        
       
        private double RadianToDegree(double angle)
        {
            return angle * (180.0 / Math.PI);
        }
        /// <summary>
        /// Implementation of the Magdewick Algorithm, using IMU data to update orientation Quaternion.
        /// Variables determined in processData()
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
        public void Update(float gx, float gy, float gz, float ax, float ay, float az, float mx, float my, float mz)
        {
            samplePeriod = 0.005f;
            float qw = quat.W,
                qx = quat.X,
                qy = quat.Y,
                qz = quat.Z; // short name local variable for readability

            // Normalise accelerometer
            float[] normalised_accel_values = normalise(ax, ay, az);
            ax = normalised_accel_values[0];
            ay = normalised_accel_values[1];
            az = normalised_accel_values[2];

            // Normalise magnetometer
            float[] normalised_mag_values = normalise(mx, my, mz);
            mx = normalised_mag_values[0];
            my = normalised_mag_values[1];
            mz = normalised_mag_values[2];

            // Reference direction of Earth's magnetic field
            Quaternion mag_quat = new Quaternion(0, mx, my, mz);
            Quaternion q_conj = quat.conjugate();
            Quaternion refer = mag_quat.product(q_conj);
            Quaternion refer_b = quat.product(refer);
            Quaternion reference = new Quaternion(0, (float)Math.Sqrt(Math.Pow(refer_b.X, 2) + Math.Pow(refer_b.Y, 2)), 0, refer_b.Z);

            List<double[]> F_arrays = new List<double[]>();
            F_arrays.Add(new double[] { 2 * (qx * qz - qw * qy) - ax });
            F_arrays.Add(new double[] { 2 * (qw * qx + qy * qz) - ay });
            F_arrays.Add(new double[] { 2 * (0.5 - Math.Pow(qx, 2) - Math.Pow(qy, 2)) - az });
            F_arrays.Add(new double[] { 2 * reference.X * (0.5 - Math.Pow(qy, 2) - Math.Pow(qz, 2)) + 2 * reference.Z * (qx * qz - qw * qy) - mx });
            F_arrays.Add(new double[] { 2 * reference.X * (qx * qy - qw * qz) + 2 * reference.Z * (qw * qx + qy * qz) - my });
            F_arrays.Add(new double[] { 2 * reference.X * (qw * qy + qx * qz) + 2 * reference.Z * (0.5 - Math.Pow(qx, 2) - Math.Pow(qy, 2)) - mz });
            Matrix F = new Matrix(F_arrays);

            List<double[]> J_arrays = new List<double[]>();
            J_arrays.Add(new double[] { -2 * qy, 2 * qz, -2 * qw, 2 * qx });
            J_arrays.Add(new double[] { 2 * qx, 2 * qw, 2 * qz, 2 * qy });
            J_arrays.Add(new double[] { 0, -4 * qx, -4 * qy, 0 });
            J_arrays.Add(new double[] { -2 * reference.Z * qy, 2 * reference.Z * qz, -4 * reference.X * qy - 2 * reference.Z * qw, -4 * reference.X * qz + 2 * reference.Z * qx });
            J_arrays.Add(new double[] { -2 * reference.X * qz + 2 * reference.Z * qx, 2 * reference.X * qy + 2 * reference.X * qw, 2 * reference.X * qx + 2 * reference.Z * qz, -2 * reference.X * qw + 2 * reference.Z * qy });
            J_arrays.Add(new double[] { 2 * reference.X * qy, 2 * reference.X * qz - 4 * reference.Z * qx, 2 * reference.X * qw - 4 * reference.Z * qy, 2 * reference.X * qx });
            Matrix J = new Matrix(J_arrays);

            Matrix step = J.transpose().multiply(F);
            double[] st = step.get_vector();
            float w = (float)st[0];
            float x = (float)st[1];
            float y = (float)st[2];
            float z = (float)st[3];
            float[] norm_step = normalise(w, x, y, z);
            w = norm_step[0];
            x = norm_step[1];
            y = norm_step[2];
            z = norm_step[3];
            float beta = 0.5f;
            float step_size = 0.005f;

            List<double[]> step_array = new List<double[]>();
            step_array.Add(new double[] { w, x, y, z });
            Matrix ST = new Matrix(step_array).transpose().multipy(beta);

            Quaternion gyro_quot = new Quaternion(0, gx, gy, gz);
            Quaternion quat_prod = quat.product(gyro_quot).scalar_muliply(0.5f);

            w = (quat_prod.W - (float)ST.matrix[0, 0]) * step_size + qw;
            x = (quat_prod.X - (float)ST.matrix[1, 0]) * step_size + qx;
            y = (quat_prod.Y - (float)ST.matrix[2, 0]) * step_size + qy;
            z = (quat_prod.Z - (float)ST.matrix[3, 0]) * step_size + qz;

            float[] normalised_quaternion_values = normalise(w, x, y, z);
            quat.W = normalised_quaternion_values[0];
            quat.X = normalised_quaternion_values[1];
            quat.Y = normalised_quaternion_values[2];
            quat.Z = normalised_quaternion_values[3];
            DataReady(quat);
        }

        public float[] normalise(float x, float y, float z, float fourth = Single.MinValue)
        {
            float norm;

            if (fourth == Single.MinValue)
            {
                norm = (float)Math.Sqrt(x * x + y * y + z * z);
            }
            else
            {
                norm = (float)Math.Sqrt(x * x + y * y + z * z + fourth * fourth);
            }

            if (norm == 0f) return (float[])null; // handle NaN
            norm = 1f / norm; // use reciprocal for division
            x *= norm;
            y *= norm;
            z *= norm;

            if (fourth != Single.MinValue)
            {
                fourth *= norm;
                return new float[] { x, y, z, fourth };
            }
            else
            {
                return new float[] { x, y, z };
            }
        }
        public void M3dg()
        {
            sendData("M3dg");
        }

        public void M4dg()
        {
            sendData("M4dg");
        }


        public void Save()
        {
            sendData("M4dg");
        }
        public void MATLAB()
        {
            sendData("M2dg");
        }

        void MMGdatain(List<int> message, int GyroValue, bool docked)
        {
            if (saveData == true)
            {
                int[] testval = new int[8];
                if (slow < -10) { slow = 10; }
                float val1 = (message[0] * 256) + message[1];
                testval[0] = (int)val1;
                val1 = (val1 * 3.3f) / 1024;
                float val2 = (message[2] * 256) + message[3];
                testval[1] = (int)val2;
                val2 = (val2 * 3.3f) / 1024;
                float val3 = (message[4] * 256) + message[5];
                testval[2] = (int)val3;
                val3 = (val3 * 3.3f) / 1024;
                float val4 = (message[6] * 256) + message[7];
                testval[3] = (int)val4;
                val4 = 0;// (val4 * 3.3f) / 1024;
                float val5 = (message[8] * 256) + message[9];
                testval[4] = (int)val5;
                val5 = 0;// (val5 * 3.3f) / 1024;
                float val6 = (message[10] * 256) + message[11];
                testval[5] = (int)val6;
                val6 = 0;// (val6 * 3.3f) / 1024;
                float val7 = (message[12] * 256) + message[13];
                testval[6] = (int)val7;
                val7 = 0;// (val7 * 3.3f) / 1024;
                float val8 = (message[14] * 256) + message[15];
                testval[7] = (int)val8;
                val8 = 0;// (val8 * 3.3f) / 1024;
                slow--;

                rawmmg[0] = val1; rawmmg[1] = val2; rawmmg[2] = val3;
  
                for (int i = 0; i < 7; i++)
                {
                    doneEvents[i] = new ManualResetEvent(false);
                }

                if (slow == 0)
                {
                    try
                    {
                        slow = 10;
                    }

                    catch (Exception)
                    {
                        if (!Kat)
                        {
                            sendData("M0dg");
                        }
                    }
                }
            }
            if (Kat)
            {
                DataReadyUnity(true, unitydata);
            }
        }

        void UpdateSD()
        {
            sendClearData(7);
            sendClearData(67);
            sendClearData(12);
            sendClearData((byte)(cali.get("MAG","X") >> 8));
            sendClearData((byte)(cali.get("MAG", "X")));
            sendClearData(11);
            System.Threading.Thread.Sleep(100);
            sendClearData(7);
            sendClearData(67);
            sendClearData(13);
            sendClearData((byte)(cali.get("MAG", "Y") >> 8));
            sendClearData((byte)(cali.get("MAG", "Y")));
            sendClearData(11);
            System.Threading.Thread.Sleep(100);
            sendClearData(7);
            sendClearData(67);
            sendClearData(14);
            sendClearData((byte)(cali.get("MAG", "Z") >> 8));
            sendClearData((byte)(cali.get("MAG", "Z")));
            sendClearData(11);
            System.Threading.Thread.Sleep(100);
            sendClearData(7);
            sendClearData(67);
            sendClearData(11);
            sendClearData(0);
            sendClearData(0);
            sendClearData(11);
        }
        void ShowConfig(List<int> message)
        {
            int id = (message[0]<<8)|message[1];
            PERIOD = (message[2] << 8) | message[3];
            FREQ = (message[4] << 8) | message[5];
            ADC = (message[6] << 8) | message[7];
            PS = (message[8] << 8) | message[9];
            gscale = message[10];
            grate = message[11];
            ascale = message[12];
            arate = message[13];
            mscale = message[14];
            mrate = message[15];
            if (cali.setData(message.GetRange(16, 6))){ UpdateSD(); }
            freq = getFrequency() / (FREQ + 1);
            samplePeriod = 1f / (float)freq;
        }
        
        int getFrequency()
        {
            if(PERIOD == 0)
            {
                sendData("RegR");
                return 1000;
            }
            int frequency;
            int[] prescaler = { 1, 8, 64, 256 };
            int clock = 30000000;
            frequency = (clock) / (PERIOD * prescaler[PS] * 2);

            return frequency;
        }
        
        int getFrequency(int ps, int period)
        {
            int frequency;
            int[] prescaler = { 1, 8, 64, 256 };
            int clock = 30000000;
            frequency = (clock) / (period * prescaler[ps] * 2);

            return frequency;
        }
        int[] setFrequency(int frequency)
        {
            int[] variables = new int[2] {65536,0 };
            int ps = 0;

            int[] prescaler = { 1, 8, 64, 256 };
            int clock = 30000000;
            while ((variables[0] >= 65536) && (variables[1] <= 4))
            {
                variables[0] = (clock ) / (frequency * prescaler[variables[1]] * 2);
                variables[1]++;
            }

            if (ps == 5)
            {
                return null;
            }

            return variables;

        }
        void OnDataIn(List<int> message)
        {
            if (message.Count > 2)
            {
                int data = message[0];
                int mode = message[1];
                message.RemoveRange(0, 2);
                if (data - 1 != message.Count())
                {
                    return;
                }
                if (mode == 0)
                {
                    foreach (byte i in message)
                    {
                        WriteDataToBox((Convert.ToChar(i)).ToString());
                    }
                }
                if (mode == 1)
                {
                    WriteDataToBox(string.Join(",", message.ToArray()));
                }

                if (mode == 2 || mode == 3)
                {
                    processData(message, mode, false);
                    return;
                }
                if (mode == 4)
                {
                    return;
                }
                if (mode == 5)
                {
                    MMGdatainShort(message, 0);
                    return;
                }
                if (mode == 6)
                {
                    
                    if (saveData == true)
                    {
                        StringBuilder messageString = new StringBuilder();
                        messageString.Append(name.ToString()).Append(',');
                        foreach (int Data in message)
                        {
                            messageString.Append(Data).Append(',');
                        }
                        try {
                            DataReady(messageString.ToString());
                        }
                        catch { }

                        if (message.Count <= 16)
                        {
                            while (message.Count != 16)
                            {
                                message.Add(0);
                            }
                            MMGdatain(message,0,true);
                        }
                        else {
                            while (message.Count != 34)
                            {
                                message.Add(0);
                            }

                            MMGdatain(message.GetRange(18, 16), processData(message.GetRange(0, 18), 2, true), true);
                        }
                        
                        }


                    return;
                }

                if (mode == 9)
                {
                    ShowConfig(message);
                }
               
                else if (mode != 0 && mode != 1 && mode != 2 && mode != 3 && mode != 4 && mode != 5 && mode != 6 && mode != 7 && mode != 8)
                {
                    {
                        WriteDataToBox("\n");

                        Comms.getStream();
                    }
                }
            }
        }
        
        void MMGdatainShort(List<int> message, int GyroValue)
        {
            
            
                int[] testval = new int[8];
                if (slow < -10) { slow = 10; }
                float val1 = (message[0] * 256) + message[1];
                testval[0] = (int)val1;
                val1 = (val1 * 3.3f) / 1024;
                slow--;

            if (slow == 0)
            {
                try
                {
                    slow = 10;
                }

                catch (Exception)
                {
                    sendData("M0dg");
                }
            }

        }
        public int int32toint16(int value)
        {
            int newvalue = Convert.ToInt16((value) & 32767);
            if (((value) & 32768) != 0)
            {
                newvalue = Convert.ToInt16(-32768 + newvalue);
            }
            return newvalue;
        }

        public int processData(List<int> message, int mode, bool docked)
        {

            // accel_data is array of the data from the accelerometer in the following format: G_X, G_Y, G_Z, M_X, M_Y, M_Z, A_X, A_Y, A_Z
            int[] accel_data = new int[]{
            message[0] + ((message[1]) << 8),
            message[2] + ((message[3]) << 8),
            (message[4] + ((message[5]) << 8)),
            message[6] + ((message[7]) << 8),
            message[8] + ((message[9]) << 8),
            (message[10] + ((message[11]) << 8)),
            message[12] + ((message[13]) << 8),
            message[14] + ((message[15]) << 8),
            (message[16] + ((message[17]) << 8))
        };
            long GyroValue = ((Math.Abs(accel_data[0]) + Math.Abs(accel_data[1]) + Math.Abs(accel_data[2])) / 3);
            if (mode == 2)
            {
                short counter = 0;
                short[] accel_data_short = new short[accel_data.Count()];
                foreach (Int32 value in accel_data)
                {
                    accel_data_short[counter] = Convert.ToInt16((accel_data[counter]) & 32767);
                    if (((accel_data[counter]) & 32768) != 0)
                    {
                        accel_data_short[counter] = Convert.ToInt16(-32768 + accel_data_short[counter]);
                    }
                    counter++;
                }
                try
                {
                    GyroValue = ((Math.Abs(accel_data_short[0]) + Math.Abs(accel_data_short[1]) + Math.Abs(accel_data_short[2])) / 3);
                }
                catch (Exception)
                {

                }
                if (cali == null)
                {
                    sendData("M0dg");
                    return 0;
                }
                if (!cali.calibrated)
                {
                    sample_period = samplePeriod;
                    samplePeriod = 0.2f;
                    cali.caliCountDown--;

                    if (cali.caliCountDown < 150)
                    {
                        cali.adjust("GYRO", "X",accel_data_short[0]);
                        cali.adjust("GYRO", "Y", accel_data_short[1]);
                        cali.adjust("GYRO", "Z", accel_data_short[2]);
                        float AX = accel_data_short[6] - cali.get("ACCEL", "X");
                        float AY = accel_data_short[7] - cali.get("ACCEL", "Y");
                        float AZ = accel_data_short[8] - cali.get("ACCEL", "Z");
                        float MX = accel_data_short[3] - cali.get("MAG", "X");
                        float MY = accel_data_short[4] - cali.get("MAG", "Y");
                        float MZ = -(accel_data_short[5] - cali.get("MAG", "Z"));
                        Update(0, 0, 0, AX, AY, AZ, MX, MY, MZ);
                        samplePeriod = sample_period;                       
                    }
                        

                    // samplePeriod = sample_period;
                    cali.caliCountDown--;
                    cali.adjust("GYRO", "X", accel_data_short[0]);
                    cali.adjust("GYRO", "Y", accel_data_short[1]);
                    cali.adjust("GYRO", "Z", accel_data_short[2]);

                    if (cali.caliCountDown == 0)
                    {
                        cali.set("GYRO", "X", cali.get("GYRO", "X") / 100);
                        cali.set("GYRO", "Y", cali.get("GYRO", "Y") / 100);
                        cali.set("GYRO", "Z", cali.get("GYRO", "Z") / 100);
                        cali.set_calibrated(true);
                    }
                }
                else
                {
                    cali.set("GYRO", "X", 0);
                    cali.set("GYRO", "Y", 0);
                    cali.set("GYRO", "Z", 0);
                    float GX = ((((float)(accel_data_short[0]) - (float)cali.get("GYRO", "X") * 500f * (float)Math.PI) /
                                 (32758f * 180f)));
                    float GY = ((((float)(accel_data_short[1]) - (float)cali.get("GYRO", "Y") * 500f * (float)Math.PI) /
                                 (32758f * 180f)));
                    float GZ = ((((float)(accel_data_short[2]) - (float)cali.get("GYRO", "Z")) * 500f * (float)Math.PI) /
                                (32758f * 180f));
                    float AX = accel_data_short[6] - cali.get("ACCEL", "X");
                    float AY = accel_data_short[7] - cali.get("ACCEL", "Y");
                    float AZ = accel_data_short[8] - cali.get("ACCEL", "Z");
                    float MX = accel_data_short[3] - cali.get("MAG", "X");
                    float MY = accel_data_short[4] - cali.get("MAG", "Y");
                    float MZ = -(accel_data_short[5] - cali.get("MAG", "Z"));
                    Update(GX, GY, GZ, AX, AY, AZ, MX, MY, MZ);
                }
                if (Kat)  ///FIND
                {
                    Quaternion quat2 = quat.inverse();
                    Quaternion quat3 = Quaternion.roatation_YPR(0, (((float)Math.PI) / 2), 0).product(quat2);
                    Quaternion quat4 = Quaternion.roatation_YPR(0, -(((float)Math.PI) / 2), 0).inverse();
                    Quaternion quat5 = quat3.product(quat4);

                    byte W = (byte)((quat5.W * 125) + 125);
                    byte X = (byte)((quat5.X * 125) + 125);
                    byte Y = (byte)((quat5.Y * 125) + 125);
                    byte Z = (byte)((quat5.Z * 125) + 125);
                    //Z = Z * -1;
                    byte M = 0;
                    unitydata = new byte[] { 255,(byte)(address),W,X,Y,Z,M,(byte)(address),};
                    
                }
                if (docked)
                {
                    try {
                    }
                    catch(Exception e)
                    {
                        if(e.HResult == -2147467261)
                        {
                            StopNU();
                        }
                    }
                }
            }
            if (mode == 3)
            {


                short counter = 0;
                short[] accel_data_short = new short[accel_data.Count()];
                foreach (Int32 value in accel_data)
                {
                    accel_data_short[counter] = Convert.ToInt16((accel_data[counter]) & 32767);
                    if (((accel_data[counter]) & 32768) != 0)
                    {
                        accel_data_short[counter] = Convert.ToInt16(-32768 + accel_data_short[counter]);
                    }
                    counter++;
                }
                GyroValue = ((Math.Abs(accel_data_short[0]) + Math.Abs(accel_data_short[1]) + Math.Abs(accel_data_short[2])) / 3);

                float GX = ((((float)accel_data_short[0] - (float)cali.get("GYRO","X")) * 500f * (float)Math.PI) / (32758f * 180f));
                float GY = ((((float)accel_data_short[1] - (float)cali.get("GYRO","Y")) * 500f * (float)Math.PI) / (32758f * 180f ));
                float GZ = ((((float)accel_data_short[2] - (float)cali.get("GYRO", "Z")) * 500f * (float)Math.PI) / (32758f * 180f ));
                int AX = (int)accel_data_short[6] - cali.get("ACCEL", "X") ;
                int AY = (int)accel_data_short[7] - cali.get("ACCEL", "Y");
                int AZ = accel_data_short[8] - cali.get("ACCEL", "Z");
                int MX = ((int)accel_data_short[3] - (int)cali.get("MAG","X"));
                int MY = ((int)accel_data_short[4] - (int)cali.get("MAG","Y"));
                int MZ = -((int)accel_data_short[5] - (int)cali.get("MAG","Z"));

                Update(GX, GY, GZ, AX, AY, AZ, MX, MY, MZ);
                Vector3D mag = new Vector3D(accel_data_short[3], accel_data_short[4], accel_data_short[5]);
                Vector3D accel = new Vector3D(accel_data_short[6], accel_data_short[7], accel_data_short[8]);
                //mag = Vector3.Normalize(mag);
                accel.Normalize();
                mag.Normalize();
                float angle = (float)RadianToDegree(Vector3D.DotProduct(mag, accel));
                byte[] byteArrayW = BitConverter.GetBytes(quat.W);
                byte[] byteArrayX = BitConverter.GetBytes(quat.X);
                byte[] byteArrayY = BitConverter.GetBytes(quat.Y);
                byte[] byteArrayZ = BitConverter.GetBytes(quat.Z);
                byte[] byteArrayMX = BitConverter.GetBytes(MX);
                byte[] byteArrayMY = BitConverter.GetBytes(MY);
                byte[] byteArrayMZ = BitConverter.GetBytes(MZ);

                WriteDataToBox(string.Join(",", accel_data.ToArray()));
                WriteDataToBox("\n");
                DataReady(new byte[] { 6,
                   (byte)message[0],
                   (byte)message[1],
                   (byte)message[2],
                   (byte)message[3],
                   (byte)message[4],
                   (byte)message[5],
                    (byte)message[6],
                    (byte)message[7],
                    (byte)message[8],
                    (byte)message[9],
                    (byte)message[10],
                    (byte)message[11],


                   (byte)message[12],
                   (byte)message[13],
                   (byte)message[14],
                   (byte)message[15],
                   (byte)message[16],
                   (byte)message[17],
                    byteArrayW[0],
                    byteArrayW[1],
                    byteArrayW[2],
                    byteArrayW[3],
                    byteArrayX[0],
                    byteArrayX[1],
                    byteArrayX[2],
                    byteArrayX[3],
                    byteArrayY[0],
                    byteArrayY[1],
                    byteArrayY[2],
                    byteArrayY[3],
                    byteArrayZ[0],
                    byteArrayZ[1],
                    byteArrayZ[2],
                    byteArrayZ[3],

                });
            }
            return (int)GyroValue;
        }

        public void loadCalifiles(string name)
        {
            cali = new Calibration(name);
        }
    }
}
