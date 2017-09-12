// *************************************************************************************
// $Id: Form1.cs 407 2017-07-19 13:04:19Z efranco $
// *************************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.IO.Ports;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Microsoft.Win32;
using InTheHand.Net.Sockets;
//using SerialHandler;
//using FTD2XX_NET;
using System.Management;
using System.Diagnostics;
using System.IO.Pipes;
using CommsLib;
using NUClass;
//using Utilities;
using System.Text;
using System.Text.RegularExpressions;
using System.Data.OleDb;
using MMark.Info;


namespace WindowsFormsApplication3
{
    /// <summary>
    /// A delegate used to contain functions that occur when a
    /// bluetooth device disconnects from the tablet
    /// </summary>
    public delegate void BluetoothDisconnect();

    public class Form1
    {

        //globalKeyboardHook gkh = new globalKeyboardHook();
        // BluetoothClient client;
        // BluetoothDeviceInfo[] devices;
        List<string> IMUS = new List<string>();
        List<NU> ConnectedIMUS = new List<NU>();
        //Handler HandPortHandler;
        Quaternion quatVal; // Quaternion data
        Quaternion quatVal2; // Quaternion data
        Quaternion quatVal3; // Quaternion data
        Quaternion quatVal4; // Quaternion data

        private BluetoothDisconnect disconnectEvent;
        
        // int dataCount = 0;
        // float[,] quatData; // 2D array to store grasp template trial data
        // float[,] mmgData; // MMG data
        float[] quatData1;
        // float[] quatData2;
        
        int countread1 = 0;
        int countread2 = 0;
        int countread3 = 0;
        int countread4 = 0;
        float temp1 = 0;
        float temp2 = 0;
        float temp3 = 0;
        float temp4 = 0;

        StringBuilder sb = new StringBuilder();
        StringBuilder sb1 = new StringBuilder();
        public int currentPosition = 0;
        string currentIMU;
        //Form6 HandDemoForm;
        //Form7 MATLABForm;
        //ServerUnity server;
        // int Timer3Counter = 0;
        TCPServer Server;
        // bool handStarted = false;
        NU DummyTestClass = new NU();
        List<Quaternion> QuatData = new List<Quaternion>();
        System.IO.StreamWriter file;
        // System.Timers.Timer timer3;
        Process q;
        // int unityslow = 0;
        // string startpath = "C:\\Test stroke";//"D:\\MMG\\IMU\\SamIMU\\biomechatronicslab-nuimu-385233f67da3\\biomechatronicslab-nuimu-385233f67da3\\WindowsFormsApplication3";
        // string savetpath = "C:\\Test stroke\\DataFiles\\test1.csv";//"D:\\MMG\\IMU\\SamIMU\\biomechatronicslab-nuimu-385233f67da3\\biomechatronicslab-nuimu-385233f67da3\\WindowsFormsApplication3\\DataFiles\\enrico1.csv";

        public string firstIMU;//upper arm
        public string secondIMU;//forearm
        public string thirdIMU;// trunk
        public string fourthIMU;// hand

        // System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();

        Stopwatch stopwatch = new Stopwatch();

        public Form1()
        {
            //HandPortHandler = new Handler();
            _Form1 = this;
            //richTextBox1.AppendText("Welcome\n\n For Assistance - Enter \"help\"\n\n", true);
            //richTextBox1.AppendText("Welcome\n\nTo connect an IMU, please enter it's three digit ID number\n\nFor Assistance - Enter \"help\"\n\n", true);
            IPHostEntry host;                                           //Code to get IP address of module for any TCP Applications
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());

            q = new Process();
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }

            Server = new TCPServer(5970, localIP);
        }

        private long dataRecieved = 0;

        public void closeAll()   // CLOSE BLUETOOTH CONNECTIONS
        {
            foreach (NU NU in ConnectedIMUS)
            {
                try
                {
                    NU.StopNU();
                    NU.saveData = false;
                    NU.Dispose();
                }
                catch(Exception)
                { }
            }

            try
            {
                file.Close();
            }
            catch (Exception) { }
            // Environment.Exit(Environment.ExitCode);
        }

        public static Form1 _Form1;

        public bool Savebt, Closebt, Stopbt, gobt;

        void DataAvailable(Flag e)
        {
            ++dataRecieved;
            //Quaternion quat = ConnectedIMUS[e.address].quat;
            if (e.Class == 2)
            {
                //try { file.WriteLine(e.data); } catch (Exception) { }
                try
                {
                    sb1.AppendLine(e.data);
                }
                catch (Exception)
                {
                    // richTextBox1.AppendText("Warning! IMU not transmitting", true);
                    // richTextBox1.ScrollToCaret();
                }

            }
            if (e.Class == 3)
            {
                Server.SendPacket(e.TCPdata);
            }
            if (e.Class == 4)
            {
                //ChannelSelect(e.result, e.name);
            }
            if (e.Class == 5)
            {
                /*
                try {
                    //richTextBox1.AppendText(e.StringData, true);
                   // richTextBox1.ScrollToCaret();
                    Invoke(new Action(() => System.Windows.Forms.Application.DoEvents()));
                }
                catch(Exception)
                {
                    //WriteToBoxXThread(e.StringData);
                }
                */
            }
            if (e.Class == 6)
            {
                //SendKeys.SendWait(" ");
                //handTriggered();
            }
            if (e.Class == 7)
            {
                //sendData2Unity(e.unityData, e.address);
            }
            if (e.Class == 9)
            {
                int positionFirst = getNU(firstIMU);
                int positionSecond = getNU(secondIMU);
                int positionThird = getNU(thirdIMU);
                int positionFourth = getNU(fourthIMU);

                quatData1 = new float[21];

                if (Stopbt == true)
                { // Records data if experiment is running

                    if (ConnectedIMUS[positionFirst].bluetoothConnection == true && ConnectedIMUS[positionSecond].bluetoothConnection == true && ConnectedIMUS[positionThird].bluetoothConnection == true && ConnectedIMUS[positionFourth].bluetoothConnection == true)
                    {

                        quatVal = ConnectedIMUS[positionFirst].quat;
                        quatVal2 = ConnectedIMUS[positionSecond].quat;
                        quatVal3 = ConnectedIMUS[positionThird].quat;
                        quatVal4 = ConnectedIMUS[positionFourth].quat;

                        quatData1[0] = quatVal.W;
                        quatData1[1] = quatVal.X;
                        quatData1[2] = quatVal.Y;
                        quatData1[3] = quatVal.Z;
                        quatData1[4] = ConnectedIMUS[positionFirst].rawmmg[2];

                        quatData1[5] = quatVal2.W;
                        quatData1[6] = quatVal2.X;
                        quatData1[7] = quatVal2.Y;
                        quatData1[8] = quatVal2.Z;
                        quatData1[9] = ConnectedIMUS[positionFirst].rawmmg[0];

                        quatData1[10] = quatVal3.W;
                        quatData1[11] = quatVal3.X;
                        quatData1[12] = quatVal3.Y;
                        quatData1[13] = quatVal3.Z;
                        quatData1[14] = ConnectedIMUS[positionFirst].rawmmg[1];

                        quatData1[15] = quatVal4.W;
                        quatData1[16] = quatVal4.X;
                        quatData1[17] = quatVal4.Y;
                        quatData1[18] = quatVal4.Z;
                        quatData1[19] = ConnectedIMUS[positionSecond].rawmmg[0];
                        quatData1[20] = ConnectedIMUS[positionSecond].rawmmg[1];


                        // Check transmission AND SEND MESSAGE IF IMU HAS STOPPED (ACCESSORY)

                        if (quatVal.W == temp1) { countread1 += 1; }
                        else { countread1 = 0; }
                        if (quatVal2.W == temp2) { countread2 += 1; }
                        else { countread2 = 0; }
                        if (quatVal3.W == temp3) { countread3 += 1; }
                        else { countread3 = 0; }
                        if (quatVal4.W == temp4) { countread4 += 1; }
                        else { countread4 = 0; }

                        temp1 = quatVal.W;
                        temp2 = quatVal2.W;
                        temp3 = quatVal3.W;
                        temp4 = quatVal4.W;


                        if (countread1 > 1000)   //RAISE A FLAG IF TRANSMISSIO STOPS FOR A COUPLE OF SECONDS
                        {
                            disconnectEvent();
                            // MessageBox.Show("Upper arm IMU not connected", "Warning!",
                            // MessageBoxButtons.OK, MessageBoxIcon.Error);
                            //label1.Text = "Upper arm IMU not connected";
                            countread1 = 0;
                        }
                        if (countread2 > 1000)
                        {
                            disconnectEvent();
                            // MessageBox.Show("Forearm IMU not connected", "Warning!",
                            // MessageBoxButtons.OK, MessageBoxIcon.Error);
                            //label1.Text = "Forearm IMU not connected";
                            countread2 = 0;
                        }
                        if (countread3 > 1000)
                        {
                            disconnectEvent();
                            // MessageBox.Show("Chest IMU not connected", "Warning!",
                            // MessageBoxButtons.OK, MessageBoxIcon.Error);
                            //label1.Text = "Chest IMU not connected";
                            countread3 = 0;
                        }
                        if (countread4 > 1000)
                        {
                            // USE DELEGATE HERE!
                            disconnectEvent();

                            // MessageBox.Show("Hand IMU not connected", "Warning!",
                            // MessageBoxButtons.OK, MessageBoxIcon.Error);
                            //label1.Text = "Hand IMU not connected";
                            countread4 = 0;
                        }
                        //

                        string dataStr = "";

                        for (int i = 0; i < 21; i++)
                        {
                            string temp = quatData1[i].ToString("0.000");
                            var values1 = temp.Split('.');
                            if (values1.Length == 2)
                            {
                                dataStr = dataStr + quatData1[i].ToString("0.000") + ",";
                            }
                            else
                            {
                                dataStr = "";
                            }
                        }
                        dataStr = dataStr.TrimEnd(',');

                        // check that each string contains 21 values otherwise discard it
                        if (!string.IsNullOrEmpty(dataStr) & !string.IsNullOrWhiteSpace(dataStr))
                        {
                            var values2 = dataStr.Split(',');
                            if (values2.Length == 21)
                            {
                                sb.AppendLine(dataStr);
                            }
                        }
                        dataStr = "";
                    }
                    else
                    {
                        // richTextBox1.AppendText("Warning! IMU not connected", true);
                        // richTextBox1.ScrollToCaret();
                    }
                }
            }

        }

        private int getNU(string name)
        {
            int position = 0;
            foreach (NU nu in ConnectedIMUS)
            {
                if (nu.name == name)
                {
                    return position;
                }
                position++;
            }
            return -1;
        }

        public int count1 = 0;
        public int count2 = 0;
        public int count3 = 0;
        public int count4 = 0;
        int repetitions = 0;
        //// flags are global (IMU connection)
        public int flag1 = 0;   
        public int flag2 = 0;
        public int flag3 = 0;
        public int flag4 = 0;
        private string FileLocation;

        public void BtConnect(BluetoothDisconnect btDisconnectEvent)  // CONNECT TO BLUETOOTH, START RECORDING IN STRING BUILDER
        {
            disconnectEvent = btDisconnectEvent;

            // TODO Verify this file exists
            string imuNames = MMarkInfo.ImuNamesFile; 
            string[] btnames = File.ReadAllLines(imuNames);
            firstIMU = btnames[0];
            secondIMU = btnames[1];
            thirdIMU = btnames[2];
            fourthIMU = btnames[3];

            if (count1 == 0)
            {
                NU NU_1 = new NU();
                NU_1.setflag += new NU.DataReadyEvent(DataAvailable);
                NU_1.initialise(firstIMU);

                if (NU_1.bluetoothConnection || NU_1.USBConnection)
                {
                    ConnectedIMUS.Add(NU_1);
                    // richTextBox1.AppendText("Device " + NU_1.name + " Connected Successfully\n\n", true);
                    currentIMU = NU_1.name;
                    NU_1.address = getNU(NU_1.name);
                    NU_1.initReg();
                    flag1 = 0;
                }
                else
                {
                    flag1 = 1;
                }

                System.Threading.Thread.Sleep(100);
            }

            if (count2 == 0)
            {
                NU NU_2 = new NU();
                NU_2.setflag += new NU.DataReadyEvent(DataAvailable);
                NU_2.initialise(secondIMU);

                if (NU_2.bluetoothConnection || NU_2.USBConnection)
                {
                    ConnectedIMUS.Add(NU_2);
                    // richTextBox1.AppendText("Device " + NU_2.name + " Connected Successfully\n\n", true);
                    currentIMU = NU_2.name;
                    NU_2.address = getNU(NU_2.name);
                    NU_2.initReg();
                    flag2 = 0;
                }
                else
                {
                    // richTextBox1.AppendText("Device Not Found\n\n", true);
                    flag2 = 1;
                }

                System.Threading.Thread.Sleep(100);
            }

            if (count3 == 0)
            {
                NU NU_3 = new NU();
                NU_3.setflag += new NU.DataReadyEvent(DataAvailable);
                NU_3.initialise(thirdIMU);

                if (NU_3.bluetoothConnection || NU_3.USBConnection)
                {
                    ConnectedIMUS.Add(NU_3);
                    // richTextBox1.AppendText("Device " + NU_3.name + " Connected Successfully\n\n", true);
                    currentIMU = NU_3.name;
                    NU_3.address = getNU(NU_3.name);
                    NU_3.initReg();
                    flag3 = 0;
                }
                else
                {
                    // richTextBox1.AppendText("Device Not Found\n\n", true);
                    flag3 = 1;
                }

                System.Threading.Thread.Sleep(100);
            }

            if (count4 == 0)
            {
                NU NU_4 = new NU();
                NU_4.setflag += new NU.DataReadyEvent(DataAvailable);
                NU_4.initialise(fourthIMU);

                if (NU_4.bluetoothConnection || NU_4.USBConnection)
                {
                    ConnectedIMUS.Add(NU_4);
                    // richTextBox1.AppendText("Device " + NU_4.name + " Connected Successfully\n\n", true);
                    currentIMU = NU_4.name;
                    NU_4.address = getNU(NU_4.name);
                    NU_4.initReg();
                    flag4 = 0;
                }
                else
                {
                    // richTextBox1.AppendText("Device Not Found\n\n", true);
                    flag4 = 1;
                }

                System.Threading.Thread.Sleep(100);
            }

            if (flag1 == 0)
            {
                count1++;
            }
            if (flag2 == 0)
            {
                count2++;
            }
            if (flag3 == 0)
            {
                count3++;
            }
            if (flag4 == 0)
            {
                count4++;
            }
            
            //button1.Focus();
        }

        private System.Timers.Timer DataStreamCheckTimer;

        public void StreamData(string outputDir)
        {
            //string FileLocation = @"C:\Users\mmark\Desktop\Log\"; //startpath + "\\DataFiles\\";  //SAVE PROCESSED DATA TO TEMP FILE, Can change to any function
            FileLocation = outputDir;

            File.Delete(FileLocation + "temp.csv");

            if (flag1 == 0 && flag2 == 0 && flag3 == 0 && flag4 == 0)  // IF ALL IMU CONNECTED START RECORDING (could go in another function that receives the flags as input and then calls M4Dg, which starts recording)
            {
                int positionFirst;          // LIST 4 IMU
                int positionSecond;
                int positionThird;
                int positionFourth;

                positionFirst = getNU(firstIMU); // Get first IMU position in list
                ConnectedIMUS[positionFirst].initfreq(); // Set FA IMU data ouput frequency    
                ConnectedIMUS[positionFirst].M4dg();  //sendData("M4dg"); // Tell FA IMU to start sending data

                positionSecond = getNU(secondIMU); // Get second IMU position in list
                ConnectedIMUS[positionSecond].initfreq(); // Set FA IMU data ouput frequency    
                ConnectedIMUS[positionSecond].M4dg();  //sendData("M4dg"); // Tell FA IMU to start sending data

                positionThird = getNU(thirdIMU); // Get first IMU position in list
                ConnectedIMUS[positionThird].initfreq(); // Set FA IMU data ouput frequency   
                ConnectedIMUS[positionThird].M4dg();  //sendData("M4dg"); // Tell FA IMU to start sending data

                positionFourth = getNU(fourthIMU); // Get second IMU position in list
                ConnectedIMUS[positionFourth].initfreq(); // Set FA IMU data ouput frequency  
                ConnectedIMUS[positionFourth].M4dg();  //sendData("M4dg"); // Tell FA IMU to start sending data


                Savebt = false;   // CONTROLS CONNECTION TO IMU
                gobt = true;      // CONTROLS REPETITION EVENT
                Stopbt = true;    // CONTROLS DATA SAVING
                                  //repetitions++;

                ConnectedIMUS[positionFirst].saveData = true;
                ConnectedIMUS[positionSecond].saveData = true;
                ConnectedIMUS[positionThird].saveData = true;
                ConnectedIMUS[positionFourth].saveData = true;
                //

                /*
                if (label5.Text == "0")
                {
                    label5.Text = repetitions.ToString();
                }
                */
                //t.Interval = 500;   //  REPETITION TIMER (disregard if not needed)
                //t.Tick += new EventHandler(this.t_Tick);
                //t.Start();
                stopwatch.Start();
                DataStreamCheckTimer = new System.Timers.Timer(1000);
                DataStreamCheckTimer.Elapsed += CheckDataRx;

                //labelwait.Text = "Start";

                DataStreamCheckTimer.Start();

            }
            else    // IF CANNOT CONNECT WITHIN 10 SECONDS  THEN SHUTS DOWN
            {
                //richTextBox1.AppendText("Connection failed. Please check that the IMUs are on, replace the batteries if necessary, and try again.\n\n Program closing in 10 seconds.");
                //richTextBox1.ScrollToCaret();
                //System.Threading.Thread.Sleep(10000);
                closeAll();
            }
        }

        /// <summary>
        /// A timer event that checks if data is being recived when it should be streaming.
        /// If no data has been recived, the function raises a disconnect event, else
        /// resets the data counter
        /// </summary>
        /// <param name="source">source of the timer event</param>
        /// <param name="e">Event arguments</param>
        private void CheckDataRx(object source, System.Timers.ElapsedEventArgs e)
        {
            if(dataRecieved == 0)
            {
                Stopbt_Click("patientnum", "taskname", "taskmode");
                disconnectEvent();
            }
            else
            {
                dataRecieved = 0;
            }
        }

        public void ForceReconnect(BluetoothDisconnect btDisconnectEvent)
        {
            closeAll();
            count1 = 0;
            count2 = 0;
            count3 = 0;
            count4 = 0;
            flag1 = 0; 
            flag2 = 0;
            flag3 = 0;
            flag4 = 0;
            BtConnect(disconnectEvent);
        }

        public string Stopbt_Click(string patientnum, string taskname, string taskmode)    //SAVES DATA AT THE END OF THE TASK AND STOPS TRANSMISSION (BLUETOOTH REMAINS OPEN)
        {
            string StringFileName;    // file path
            string filesavename;
            string datename;

            int positionFirst;
            int positionSecond;
            int positionThird;
            int positionFourth;

            if(DataStreamCheckTimer.Enabled == true)
            {
                DataStreamCheckTimer.Stop();
            }
            Stopbt = false;

            // save raw data
            try
            {
                positionFirst = getNU(firstIMU);
                positionSecond = getNU(secondIMU);
                positionThird = getNU(thirdIMU);
                positionFourth = getNU(fourthIMU);
                ConnectedIMUS[positionFirst].saveData = false;
                ConnectedIMUS[positionSecond].saveData = false;
                ConnectedIMUS[positionThird].saveData = false;
                ConnectedIMUS[positionFourth].saveData = false;
            }
            catch (Exception) { }


            // BUILD UP FILE NAMES AND PATH
            datename = DateTime.Now.ToString("s");
            datename = "-" + datename.Replace("\"", "-").Replace("/", "-").Replace(":", "-");

            //string FileLocation = startpath + "\\DataFiles\\";

            filesavename = "Patient" + patientnum + " - " + taskname + " - " + taskmode;

            StringFileName = FileLocation + filesavename + datename + ".csv";       // PROCESSED DATA

            string StringFileName1 = FileLocation + filesavename + datename + "_raw.csv";  // RAW DATA

            string datastring = Regex.Replace(sb.ToString(), @"^\s+$[\r\n]*", "", RegexOptions.Multiline);

            //System.IO.File.WriteAllText(StringFileName, datastring);

            System.IO.File.WriteAllText(FileLocation + "temp.csv", datastring);  //SAVE TO TEMPORARY FILE

            System.Threading.Thread.Sleep(100);

            // TIMER AND STOPWATCH (NOT NEEDED)
            //t.Stop();
            stopwatch.Stop();
            //labeltime.Text = "0";
            stopwatch.Reset();
            //labelwait.Text = "Wait";
            sb.Clear();


            // CHECK DATA INTEGRITY      (ACCESSORY)         
            var reader = new StreamReader(File.OpenRead(FileLocation + "temp.csv"));

            // parse csv file and remove incorrect lines
            List<string> listA = new List<string>();
            List<string> listB = new List<string>();
            List<string> listC = new List<string>();
            List<string> listD = new List<string>();
            List<string> listE = new List<string>();
            List<string> listF = new List<string>();
            List<string> listG = new List<string>();
            List<string> listH = new List<string>();
            List<string> listI = new List<string>();
            List<string> listJ = new List<string>();
            List<string> listK = new List<string>();
            List<string> listL = new List<string>();
            List<string> listM = new List<string>();
            List<string> listN = new List<string>();
            List<string> listO = new List<string>();
            List<string> listP = new List<string>();
            List<string> listQ = new List<string>();
            List<string> listR = new List<string>();
            List<string> listS = new List<string>();
            List<string> listT = new List<string>();
            List<string> listU = new List<string>();

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (!string.IsNullOrEmpty(line) & !string.IsNullOrWhiteSpace(line))
                {
                    var values = line.Split(',');
                    if (values.Length == 21)
                    {
                        int checkcount = 0;
                        for (int i = 0; i < 21; i++)
                        {
                            if (!string.IsNullOrEmpty(values[i]) & !string.IsNullOrWhiteSpace(values[i]))
                            {
                                string[] valuecell = values[i].Split('.');
                                if (valuecell.Length == 2)
                                {
                                    checkcount++;
                                }

                            }
                        }
                        if (checkcount == 21)
                        {
                            listA.Add(values[0]);
                            listB.Add(values[1]);
                            listC.Add(values[2]);
                            listD.Add(values[3]);

                            listE.Add(values[4]);       //MMG shoulder

                            listF.Add(values[5]);
                            listG.Add(values[6]);
                            listH.Add(values[7]);
                            listI.Add(values[8]);
                            if (taskmode == "Left")    //MMG upper arm
                            {
                                listJ.Add(values[9]);
                                listO.Add(values[14]);
                            }
                            else
                            {
                                listJ.Add(values[14]);
                                listO.Add(values[9]);
                            }
                            listK.Add(values[10]);
                            listL.Add(values[11]);
                            listM.Add(values[12]);
                            listN.Add(values[13]);

                            listP.Add(values[15]);
                            listQ.Add(values[16]);
                            listR.Add(values[17]);
                            listS.Add(values[18]);
                            if (taskmode == "Left")    //MMG forearm
                            {
                                listT.Add(values[19]);
                                listU.Add(values[20]);
                            }
                            else
                            {
                                listT.Add(values[20]);
                                listU.Add(values[19]);
                            }

                        }

                    }

                }


            }
            reader.Dispose();

            int length = listA.Count - 2;
            string datastr = "";

            // WRITE PROCESSED DATA TO FILE (AFTER INTERGRITY CHECK) - ACCESSORY
            for (int i = 0; i < length; i++)
            {

                if (listA[i] != "0.000" & listA[i + 1] == "0.000" & listA[i + 2] != "0.000")
                {
                    listA[i + 1] = listA[i];
                }
                if (listB[i] != "0.000" & listB[i + 1] == "0.000" & listB[i + 2] != "0.000")
                {
                    listB[i + 1] = listB[i];
                }
                if (listC[i] != "0.000" & listC[i + 1] == "0.000" & listC[i + 2] != "0.000")
                {
                    listC[i + 1] = listC[i];
                }
                if (listD[i] != "0.000" & listD[i + 1] == "0.000" & listD[i + 2] != "0.000")
                {
                    listD[i + 1] = listD[i];
                }
                if (listE[i] != "0.000" & listE[i + 1] == "0.000" & listE[i + 2] != "0.000")
                {
                    listE[i + 1] = listE[i];
                }
                if (listF[i] != "0.000" & listF[i + 1] == "0.000" & listF[i + 2] != "0.000")
                {
                    listF[i + 1] = listF[i];
                }
                if (listG[i] != "0.000" & listG[i + 1] == "0.000" & listG[i + 2] != "0.000")
                {
                    listG[i + 1] = listG[i];
                }
                if (listH[i] != "0.000" & listH[i + 1] == "0.000" & listH[i + 2] != "0.000")
                {
                    listH[i + 1] = listH[i];
                }
                if (listI[i] != "0.000" & listI[i + 1] == "0.000" & listI[i + 2] != "0.000")
                {
                    listI[i + 1] = listI[i];
                }
                if (listJ[i] != "0.000" & listJ[i + 1] == "0.000" & listJ[i + 2] != "0.000")
                {
                    listJ[i + 1] = listJ[i];
                }
                if (listK[i] != "0.000" & listK[i + 1] == "0.000" & listK[i + 2] != "0.000")
                {
                    listK[i + 1] = listK[i];
                }
                if (listL[i] != "0.000" & listL[i + 1] == "0.000" & listL[i + 2] != "0.000")
                {
                    listL[i + 1] = listL[i];
                }
                if (listM[i] != "0.000" & listM[i + 1] == "0.000" & listM[i + 2] != "0.000")
                {
                    listM[i + 1] = listM[i];
                }
                if (listN[i] != "0.000" & listN[i + 1] == "0.000" & listN[i + 2] != "0.000")
                {
                    listN[i + 1] = listN[i];
                }
                if (listO[i] != "0.000" & listO[i + 1] == "0.000" & listO[i + 2] != "0.000")
                {
                    listO[i + 1] = listO[i];
                }
                if (listP[i] != "0.000" & listP[i + 1] == "0.000" & listP[i + 2] != "0.000")
                {
                    listP[i + 1] = listP[i];
                }
                if (listQ[i] != "0.000" & listQ[i + 1] == "0.000" & listQ[i + 2] != "0.000")
                {
                    listQ[i + 1] = listQ[i];
                }
                if (listR[i] != "0.000" & listR[i + 1] == "0.000" & listR[i + 2] != "0.000")
                {
                    listR[i + 1] = listR[i];
                }
                if (listS[i] != "0.000" & listS[i + 1] == "0.000" & listS[i + 2] != "0.000")
                {
                    listS[i + 1] = listS[i];
                }

                if (listT[i] != "0.000" & listT[i + 1] == "0.000" & listT[i + 2] != "0.000")
                {
                    listT[i + 1] = listT[i];
                }
                if (listU[i] != "0.000" & listU[i + 1] == "0.000" & listU[i + 2] != "0.000")
                {
                    listU[i + 1] = listU[i];
                }

                datastr = listA[i] + "," + listB[i] + "," + listC[i] + "," + listD[i] + "," + listE[i] + "," + listF[i] + "," + listG[i] + "," + listH[i] + "," + listI[i] + "," + listJ[i] + "," + listK[i] + "," + listL[i] + "," + listM[i] + "," + listN[i] + "," + listO[i] + "," + listP[i] + "," + listQ[i] + "," + listR[i] + "," + listS[i] + "," + listT[i] + "," + listU[i];
                sb.AppendLine(datastr);
            }


            System.IO.File.WriteAllText(StringFileName, sb.ToString());
            var fileInfo = new FileInfo(StringFileName);


            // CHECK FILE SIZE LARGER THAN 0 AND STOP TRANSMISSION IF IT IS OR SEND ERROR MESSAGE (ACCESSORY)
            if (fileInfo.Length > 10000)
            {

                //MessageBox.Show("file size " + fileInfo.Length);

                Savebt = true;
                gobt = false;

                sb.Clear();

                positionFirst = getNU(firstIMU); // Get first IMU position in list  
                ConnectedIMUS[positionFirst].StopNU(); // 

                positionSecond = getNU(secondIMU); // Get first IMU position in list  
                ConnectedIMUS[positionSecond].StopNU(); // 

                positionThird = getNU(thirdIMU); // Get first IMU position in list  
                ConnectedIMUS[positionThird].StopNU(); // 

                positionFourth = getNU(fourthIMU); // Get first IMU position in list  
                ConnectedIMUS[positionFourth].StopNU(); // 

                repetitions = 0;

                //label5.Text = repetitions.ToString();

            }
            else
            {
                //MessageBox.Show("Save operation failed. Please save again ");
            }

            // SAVE PROCESSED DATA (NO CHECKS)
            System.IO.File.WriteAllText(StringFileName1, sb1.ToString());

            //button1.Focus();

            return StringFileName;
        }
        
        public void Closebt_Click(object sender, EventArgs e)  // CLOSES APPLICATION AND BLUETOOTH CONNECTION
        {
            // string message = "Is the test finished?";
            // string caption = "Confirm";

            closeAll();
            // MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            //DialogResult result;
            //result = MessageBox.Show(message, caption, buttons);
            /*
            if (result == System.Windows.Forms.DialogResult.Yes)

            {

                try
                {

                    if (Savebt == false)    // IF WE HAVE NOT STOPPED TRANSMISSION AND SAVED THEN DO IT NOW
                    {
                        labelwait.Text = "Wait";
                        Stopbt_Click(sender, e);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error " + ex);
                }
                

            }*/
        }
        
        DateTime latestHit = DateTime.UtcNow;

        public void GObt_Click()  // REPETITION EVENT . Can change to any function
        {
            if (Savebt == false & Stopbt == true & DateTime.UtcNow > latestHit.AddMilliseconds(700))
            {
                /*
                t.Stop();
                stopwatch.Stop();
                labeltime.Text = "0";
                stopwatch.Reset();
                */
                string dataStr = "";
                for (int i = 0; i < 21; i++)
                {
                    dataStr = dataStr + "10.000" + ",";
                }
                dataStr = dataStr.TrimEnd(',');
                try
                {
                    sb.AppendLine(dataStr);
                }
                catch (Exception ex)
                {
                    // MessageBox.Show("Error " + ex);
                }

                string dataStr2 = "";
                for (int i = 0; i < 25; i++)
                {
                    dataStr2 = dataStr2 + "-10.00" + ",";
                }
                dataStr2 = dataStr2.TrimEnd(',');
                try
                {
                    sb1.AppendLine(dataStr2);
                }
                catch (Exception ex2)
                {
                    // MessageBox.Show("Error " + ex2);
                }
                /*
                repetitions++;
                label5.Text = repetitions.ToString();
                latestHit = DateTime.UtcNow;

                t.Interval = 500;
                t.Tick += new EventHandler(this.t_Tick);
                t.Start();
                stopwatch.Start();
                */
            }
            // button1.Focus();
        }
    }
}
