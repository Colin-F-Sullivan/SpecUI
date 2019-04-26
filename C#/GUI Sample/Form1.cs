using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices; // for the callback Marshaling
using AIOUSBNet;  // the namespace exposes the AIOUSB Class interface 
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Threading;

// You must also add a reference to the AIOUSBNet.dll in the project settings
// (preferably from the system32 directory or equivelant)


namespace Sample1
{

    public partial class Form1 : Form
    {
        // Callback definition etc is in the dll.

        // Declaration of callback function at Form Class level scope
        // so no Garbage collection will interfere with processing until the end of Form and App:
        // This could also be on its own worker thread not in the UI thread (Form).
        public static AIOUSB.ADCallback myCallBack;

        public UInt32 DeviceIndex = AIOUSB.diOnly;  // One and only Device Index
        bool bCal = true;                           // Does board have calibration
        public int numChannelsUsed = 16;            // Num channels to be used ie 16 or 8 if in Differential mode
        public const int numAnalogInputs = 16;      // Num analog inputs on the board:

        // Array of static controls for GUI updates used by callback for Asycronous BeginInvoke of delegates:
        public static TrackBar[] VoltTrackBar = new TrackBar[numAnalogInputs];
        public static Label[] VoltLabel = new Label[numAnalogInputs];
        public static CheckBox DiffCheckBox;
        public static ComboBox RangeComboBox;
        public static TextBox StatusTextBox;

        // New Array to accumulate Time
        string[] timeArray = new string[10000000];
        Stopwatch timeStart = new Stopwatch();
        Stopwatch Timestamp = Stopwatch.StartNew();


        // SEAN CHANGE: Place to store timestamps and data as we go, so that we can speed up the callback
        // Note: The List<> object can expand into memory limits, and by setting the initial capacity you can make this
        //     pretty fast, but once this initial capacity is reached the program will start to slow down as it has to
        //     dynamically allocate more memory as it goes. As they are set now, they can record data from 1,000,000 
        //     callbacks before they have to start allocating more memory for themselves. If this limit is reached, the
        //     program will start to slow down noticably.
        List<TimeSpan> timeStamps = new List<TimeSpan>(1_000_000); // Stores the timestamps for each step
        List<byte> dioValues = new List<byte>(2_000_000); // Stores the 0/1 values from WriteDIO() function
        List<Int16> marshallValues = new List<Int16>(16_000_000); // Stores the signed short values that are marshalled in in the callback

        // Copy rangecode and pass it
        byte passRangeCode;
        //string begin = "";

        public UInt32 numPins = 16;  //Change the number of channels to read (in increasing order) here.
        byte[] Data = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };


        // Config Buf size required for ADC_SetConfig() dll calls:
        public static UInt32 ConfigBufSize = numAnalogInputs + 4;

        // Configuration Information Array for ADC_SetConfig() dll calls:
        public byte[] ConfigArray = new byte[ConfigBufSize];

        // Array for AD Data the counts are unsigned 16 bit numbers (ushort)
        public UInt16[] ADData = new UInt16[128 * 1024]; //8K scans of 16 channels, or 16K scans of 8 differential channels.
        static int TotalAmountOfDataReceived = 0;   // Total number of 16 bit elements recieved

        public enum ADGainCode
        {
            AD_GAIN_CODE_0_10V = 0x0, // 0-10V 
            AD_GAIN_CODE_10V = 0x1, // ±10V
            AD_GAIN_CODE_0_5V = 0x2, // 0-5V
            AD_GAIN_CODE_5V = 0x3, // ±5V
            AD_GAIN_CODE_0_2V = 0x4, // 0-2V
            AD_GAIN_CODE_2V = 0x5, // ±2V
            AD_GAIN_CODE_0_1V = 0x6, // 0-1V
            AD_GAIN_CODE_1V = 0x7  // ±1V
        };


        public Form1()
        {
            InitializeComponent();
            Timestamp.Start(); ///////////////////////////////stuff
            // Set the Arrays of controls etc:
            VoltTrackBar[0] = trackBar1;
            VoltTrackBar[1] = trackBar2;
            VoltTrackBar[2] = trackBar3;
            VoltTrackBar[3] = trackBar4;
            VoltTrackBar[4] = trackBar5;
            VoltTrackBar[5] = trackBar6;
            VoltTrackBar[6] = trackBar7;
            VoltTrackBar[7] = trackBar8;
            VoltTrackBar[8] = trackBar9;
            VoltTrackBar[9] = trackBar10;
            VoltTrackBar[10] = trackBar11;
            VoltTrackBar[11] = trackBar12;
            VoltTrackBar[12] = trackBar13;
            VoltTrackBar[13] = trackBar14;
            VoltTrackBar[14] = trackBar15;
            VoltTrackBar[15] = trackBar16;

            VoltLabel[0] = lblVolt0;
            VoltLabel[1] = lblVolt1;
            VoltLabel[2] = lblVolt2;
            VoltLabel[3] = lblVolt3;
            VoltLabel[4] = lblVolt4;
            VoltLabel[5] = lblVolt5;
            VoltLabel[6] = lblVolt6;
            VoltLabel[7] = lblVolt7;
            VoltLabel[8] = lblVolt8;
            VoltLabel[9] = lblVolt9;
            VoltLabel[10] = lblVolt10;
            VoltLabel[11] = lblVolt11;
            VoltLabel[12] = lblVolt12;
            VoltLabel[13] = lblVolt13;
            VoltLabel[14] = lblVolt14;
            VoltLabel[15] = lblVolt15;

            DiffCheckBox = checkBoxDIFF;
            RangeComboBox = comboBoxRange;
            StatusTextBox = textBoxStatus;

        }

        /// <summary>
        ///
        /// </summary>


        private void Form1_Load(object sender, EventArgs e)
        {
            // Called before Form is displayed Initialize resources:

            // Single instantiation here of callback function at Form Class level scope
            // so no Garbage collection will interfere with processing until the end of Form scope and App:
            myCallBack = new AIOUSB.ADCallback(ADCallbackReport);

            // Initialize default Device Only:
            DeviceIndex = AIOUSB.diOnly;


            // Device data:
            UInt32 Status;
            UInt32 PID = 0;
            Int32 NameSize = 256;
            string strName = "name";
            UInt32 DIOBytes = 0;
            UInt32 Counters = 0;

            UInt32 ERROR_SUCCESS = 0;
            bool deviceIndexValid = false;

            // Get The Device Information test for valid device found AI and AIO boards:
            Status = AIOUSB.QueryDeviceInfo(DeviceIndex, ref PID, ref NameSize, ref strName, out DIOBytes, out Counters);
            if ((Status == ERROR_SUCCESS) && (((PID >= 0x8040) && (PID <= 0x8044)) || ((PID >= 0x8140) && (PID <= 0x8144))))// AIO and AI
            {
                deviceIndexValid = true;
            }
            else
            {
                // If Only device is not valid then Launch connect device dialog box:
                // New parent aware subform:
                FormDetect DetectSubForm = new FormDetect(this);
                DetectSubForm.ShowDialog();

                Status = AIOUSB.QueryDeviceInfo(DeviceIndex, ref PID, ref NameSize, ref strName, out DIOBytes, out Counters);
                if (Status == ERROR_SUCCESS && PID >= 0x8040 && PID <= 0x815D)
                    deviceIndexValid = true;
            }

            if (!deviceIndexValid)
            {
                // No valid device found could just exit:
                // this.Close();
            }


            if (AIOUSB.ADC_QueryCal(DeviceIndex) != ERROR_SUCCESS)
            {
                // this board doesn't have calibration (so dont try and cal it)
                bCal = false;
                lblCal.Visible = false;
                comboBoxCalibration.Visible = false;
            }

            //Stop the counter, in case it was running.
            double Hz = 0;
            UInt32 BlockIndex = 0;

            AIOUSB.CTR_StartOutputFreq(DeviceIndex, BlockIndex, ref Hz);

            comboBoxRange.SelectedIndex = 0;

            int i;
            byte RangeCode;

            RangeCode = (byte)comboBoxRange.SelectedIndex;
            if (checkBoxDIFF.Checked)
                RangeCode |= 0x08;

            // Copy range code to array for dll config call:
            for (i = 0; i < numAnalogInputs; i++)
                ConfigArray[i] = RangeCode;

            if (checkBoxDIFF.Checked)
            {
                numChannelsUsed = 8;
                ConfigArray[18] = 0x70; //Select only 8 differential channels, from 0 to 7.
            }
            else
            {
                numChannelsUsed = 16;
                ConfigArray[18] = 0xF0; //Select all 16 channels, from 0 to 15.
            }


            // Init all config values:     
            for (i = 0; i < numAnalogInputs; i++)
            {
                ConfigArray[i] = 1; // Range code 1 is ±10 volts, used for testing full range in this sample
            }
            ConfigArray[16] = 0x00; // 0x00 Take actual data, not internal calibration sources
            ConfigArray[17] = 0x05; // 0x05 Scan selected channels each counter rising edge
            ConfigArray[18] = 0xF0; // 0xf0 Scan all 16 channels. 0-15
            ConfigArray[19] = 0x00; // 0x00 No oversample

            AIOUSB.ADC_SetConfig(DeviceIndex, ConfigArray, ref ConfigBufSize);

            for (i = 0; i < 16; i++)
            {
                VoltTrackBar[i].Value = 0;
            }

            // Set reasonable default values of choice:
            //comboBoxCalibration.SelectedItem = ":AUTO:"; // if your card supports it ("A" models) you can use onboard calibration voltages, and perform an autocal with :AUTO:
            //comboBoxCalibration.SelectedItem = ":NONE:"; // :NONE: and :1TO1: are identical, they both set the calibration system to "uncalibated".  Use whichever constant feels intuitive to you
            comboBoxCalibration.SelectedItem = ":1TO1:";
            comboBoxRange.SelectedItem = "±10 Volts";
        }

        // SEAN CHANGE: Set the values to be placed in the List<> instead of having string operations
        public void WriteDIO()
        {
            //string DIOData;
            // int DIOValue;

            // DIOWrite += Timestamp.Elapsed + "\t\t";

            // Get data from board update display states:
            AIOUSB.DIO_ReadAll(DeviceIndex, Data);

            for (int z = 0; z < numPins; z++)
            {
                if ((Data[z / 8] & (1 << (z % 8))) != 0) // looks complicated but efficient
                {
                    dioValues.Add(0);
                    //DIOValue = 0;
                } // High Level
                else
                {
                    dioValues.Add(1);
                    //DIOValue = 1;
                } // Low Level

                //DIOWrite += DIOValue.ToString() + "\t\t";

            }

            //DIOWrite += "\r\n";
        }

        private void btnDevice_Click(object sender, EventArgs e)
        {
            // Launch connect device dialog box
            // Switch between multiple devices or reconnect

            // New parent aware subform:
            FormDetect DetectSubForm = new FormDetect(this);
            DetectSubForm.ShowDialog();
        }

        private void comboBoxRange_SelectedIndexChanged(object sender, EventArgs e)
        {
            // The ranges in the combo box are listed in order, 
            // so that the index is equal to the range code.

            int i;
            byte RangeCode;

            RangeCode = (byte)comboBoxRange.SelectedIndex;
            if (checkBoxDIFF.Checked)
                RangeCode |= 0x08;

            // Copy to array for dll config call:
            for (i = 0; i < numAnalogInputs; i++)
                ConfigArray[i] = RangeCode;

            // Set up Diff mode, Toggle display:
            if (checkBoxDIFF.Checked)
            {
                numChannelsUsed = 8;
                ConfigArray[18] = 0x70; //Select 8 differential channels, from 0 to 7.

                for (i = 8; i < numAnalogInputs; i++)
                {
                    VoltTrackBar[i].Visible = false;
                    VoltLabel[i].Visible = false;
                }
            }
            else
            {
                numChannelsUsed = 16;
                ConfigArray[18] = 0xF0; //Select all 16 channels, from 0 to 15.

                for (i = 8; i < numAnalogInputs; i++)
                {
                    VoltTrackBar[i].Visible = true;
                    VoltLabel[i].Visible = true;
                }
            }

            // Adjust track bar range:
            // Its easier to fine tune the track bars for range in a switch
            for (i = 0; i < numAnalogInputs; i++)
            {
                switch (comboBoxRange.SelectedIndex)
                {
                    case 0: // 0-10
                        VoltTrackBar[i].Minimum = 0;
                        VoltTrackBar[i].Maximum = 10000;
                        lblVMin.Text = "0";
                        lblVMid.Text = "+5";
                        lblVMax.Text = "+10";
                        VoltTrackBar[i].TickFrequency = 1000;
                        break;
                    case 1: // +-10
                        VoltTrackBar[i].Minimum = -10000;
                        VoltTrackBar[i].Maximum = 10000;
                        lblVMin.Text = "-10";
                        lblVMid.Text = "0";
                        lblVMax.Text = "+10";
                        VoltTrackBar[i].TickFrequency = 1000;
                        break;
                    case 2: // 0-5
                        VoltTrackBar[i].Minimum = 0;
                        VoltTrackBar[i].Maximum = 5000;
                        lblVMin.Text = "0";
                        lblVMid.Text = "+2.5";
                        lblVMax.Text = "+5";
                        VoltTrackBar[i].TickFrequency = 1000;
                        break;
                    case 3: // +-5
                        VoltTrackBar[i].Minimum = -5000;
                        VoltTrackBar[i].Maximum = 5000;
                        lblVMin.Text = "-5";
                        lblVMid.Text = "0";
                        lblVMax.Text = "+5";
                        VoltTrackBar[i].TickFrequency = 1000;
                        break;
                    case 4: // 0-2
                        VoltTrackBar[i].Minimum = 0;
                        VoltTrackBar[i].Maximum = 2000;
                        lblVMin.Text = "0";
                        lblVMid.Text = "+1";
                        lblVMax.Text = "+2";
                        VoltTrackBar[i].TickFrequency = 100;
                        break;
                    case 5:// +-2
                        VoltTrackBar[i].Minimum = -2000;
                        VoltTrackBar[i].Maximum = 2000;
                        lblVMin.Text = "-2";
                        lblVMid.Text = "0";
                        lblVMax.Text = "+2";
                        VoltTrackBar[i].TickFrequency = 1000;
                        break;
                    case 6: // 0-1
                        VoltTrackBar[i].Minimum = 0;
                        VoltTrackBar[i].Maximum = 1000;
                        lblVMin.Text = "0";
                        lblVMid.Text = "+.5";
                        lblVMax.Text = "+1";
                        VoltTrackBar[i].TickFrequency = 100;
                        break;
                    case 7: // +-1
                        VoltTrackBar[i].Minimum = -1000;
                        VoltTrackBar[i].Maximum = 1000;
                        lblVMin.Text = "-1";
                        lblVMid.Text = "0";
                        lblVMax.Text = "+1";
                        VoltTrackBar[i].TickFrequency = 100;
                        break;
                    default: // use max spread
                        VoltTrackBar[i].Minimum = -10000;
                        VoltTrackBar[i].Maximum = 10000;
                        lblVMin.Text = "-10";
                        lblVMid.Text = "0";
                        lblVMax.Text = "+10";
                        VoltTrackBar[i].TickFrequency = 1000;
                        break;
                }
                VoltTrackBar[i].Value = 0; // re init to 0                               
            }

        }

        private void checkBoxDIFF_CheckedChanged(object sender, EventArgs e)
        {
            comboBoxRange_SelectedIndexChanged(sender, e);
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            UInt32 Status = 1;
            int ERROR_SUCCESS = 0;

            textBoxStatus.Text = " Acquiring...";

            //Stop the counter, in case it was running.
            double Hz = 0;
            AIOUSB.CTR_StartOutputFreq(DeviceIndex, 0, ref Hz);

            // Array was set above
            Status = AIOUSB.ADC_SetConfig(DeviceIndex, ConfigArray, ref ConfigBufSize);
            passRangeCode = ConfigArray[0];

            //begin += passRangeCode.ToString() + "\r\n";
            // If cal is avail then set calibration:
            if (bCal)
            {
                Status = AIOUSB.ADC_SetCal(DeviceIndex, comboBoxCalibration.SelectedItem.ToString());
                if (Status != 0) //ERROR_SUCCESS )
                {
                    MessageBox.Show(" Calibration Error!");
                    return;
                }
            }
            // Since we've put it in scan mode, this is 1kHz per channel, or 16kHz aggregate.  
            // Each channel in the scan is taken 2usec apart, for 16 channels, then it waits for the next counter output pulse before taking another scan.
            // Register the callback and start acqusition:
            // Note: 1024 bytes requested so 512 Uint16 elements should be returned

            Status = AIOUSB.ADC_BulkContinuousCallbackStart(DeviceIndex, 1024, 64, 0, myCallBack);
            if (Status != ERROR_SUCCESS)
                textBoxStatus.Text = " Error " + Status.ToString("00") + " starting acquisition.";
            /*
             Hz is the speed of the board and how fast data is coming in. This number is not reflected on the gui as it is capped.
             */
            timeStart.Start();

            // Change speed of aquisition here.
            Hz = 250;
            AIOUSB.CTR_StartOutputFreq(DeviceIndex, 0, ref Hz);

            comboBoxCalibration.Enabled = false;
            comboBoxRange.Enabled = false;
            btnExecute.Enabled = false; // prevent unintentional multiple callbacks otherwise may need serialization etc
            btnStop.Enabled = true;
            checkBoxDIFF.Enabled = false;
        }
        /// <summary>
        /// </summary>
       // Must use threadsafe delegate across threads for GUI updates:
        private delegate void UpdateGUIFormDel(UInt16[] Counts);

        // Updates all the GUI Voltage controls:


        string headers = "Time\t\t\t\tDIO0\t\tDIO1\t\tDIO2\t\tDIO3\t\tDIO4\t\tDIO5\t\tDIO6\t\tDIO7\t\tDIO8\t\tDIO9\t\tDIO10\t\tDIO11\t\tDIO12\t\tDIO13\t\tDIO14\t\tDIO15\r\n";
        //string DIOWrite = "";

        //Header for CountTest Files. Add more channels if needed.
        //string countheaders = "Time\t\t\t\tCh0\t\tCh1\r\n";

        private void UpdateGUIFormDisplay(UInt16[] Counts)
        {
            // Note:
            // Executes a delegate asynchronously on the thread that the control's underlying handle was created on
            // So even though we invoke this on one cotrol we are in correct thread context to update all controls on the one form safely

            // Convert counts to volts and display:
            byte RangeCode;
            double Volts;

            // We're going to use the 1 range code to convert to volts.  Get it from array not GUI for efficiency
            RangeCode = ConfigArray[0];

            int Channels = 16;

            for (int i = 0; i < Channels; ++i)
            {

                Volts = (double)(Counts[i]) / (double)(0xFFFF); //"Volts" now holds a value from 0 to 1.
                //Volts = (double)(1) / (double)(0xFFFF) * Counts[i]; //"Volts" now holds a value from 0 to 1.
                //Volts = 1 / 65536 * Counts[i]; //"Volts" now holds a value from 0 to 1.

                if ((RangeCode & 0x01) != 0) //Bit 0(mask 01 hex) indicates bipolar.
                    Volts = Volts * 2 - 1;   //"Volts" now holds a value from -1 to +1.
                if ((RangeCode & 0x02) == 0) //Bit 1(mask 02 hex) indicates x2 gain.
                    Volts *= 2;
                if ((RangeCode & 0x04) == 0) //Bit 2(mask 04 hex) indicates x5 gain.
                    Volts *= 5;

                // Format and Set the GUI Volt text data.
                string strVolt;
                string strFormat = "Ch {0,2:D2}: {1,8:F4} V";  //D=integer, F=Float, change for more precision.
                strVolt = String.Format(strFormat, i, Volts);



                if (i < numChannelsUsed)
                {
                    VoltLabel[i].Text = strVolt;

                    // Set the GUI voltage task bar data +-10 V:
                    //VoltTrackBar[i].Value = 10000 + (int)(1000 * Volts);
                    //VoltTrackBar[i].Value =  (int)(1000 * Volts);
                    VoltTrackBar[i].Value = (int)Math.Round(1000 * Volts);
                }
            }
        }

        // Must use threadsafe delegate across threads for GUI updates:
        private delegate void UpdateStatusDel(string strText);

        // Updates the GUI Status Text:
        private void UpdateStatusDisplay(string strText)
        {
            textBoxStatus.Text += strText;  // Note: This just adds to existing messages
        }

        // Method that matches delegate signature to recieve data buffer:
        // Note: BufSize returned is actually total number of bytes, so the array of UInt16's lenghth is actually 1/2 of BufSize
        // IntPtr used to pass array
        public void ADCallbackReport(IntPtr pBuf, UInt32 BufSize, UInt32 Flags, UInt32 Context)
        {
            // This flag (2) tells us we've got the last continuous callback packet:
            if (((Flags & 2) != 0))
            {
                String strEnd = "\r\n Stream ended...";  // Be sure this isnt blcoked by stop button func on UI thread
                StatusTextBox.BeginInvoke(new UpdateStatusDel(UpdateStatusDisplay), new object[] { strEnd });
                //System.Threading.Thread.Sleep(100); // let last ui update occurs since were on UI thread (not needed but here just in case)
            }

            int LastChanInBuf, FirstChanInBuf, NumOfChansToCopyFromBuf;
            int BufLen = ((int)BufSize / 2);                // length of the buffer in 16 bit elements instead of 8-bit
            Int16[] marshallingArray = new Int16[BufLen];   // array for Int16 copy of buf for marshalling
            UInt16[] ChannelCounts = new UInt16[BufLen];    // buffer for final UInt16 count data

            // Copy pBuf into an Int16 array then convert to UInt16 when used: 
            // This is required due to CLR restraints on IntPtr and Marshal.Copy() for unmanaged code array pointers
            if (BufLen != 0) // if theres data to copy ie not end
            {

                Marshal.Copy(pBuf, marshallingArray, 0, (marshallingArray.Length)); // copy

                TotalAmountOfDataReceived += BufLen;  // By default, C# arithmetic is done in an unchecked context, meaning values will roll over. So it will eventually rollover reasonably at int.MaxValue (+1 %16)
                NumOfChansToCopyFromBuf = Math.Min(16, BufLen); // Note: Just getting 16 for display here
                LastChanInBuf = (TotalAmountOfDataReceived + 15) % 16;   // channel # of actual last entry in this buf of data (ie 15)
                FirstChanInBuf = (16 + (LastChanInBuf - NumOfChansToCopyFromBuf + 1)) % 16;  // zero based (ie 0)

                for (int i = 0; i < NumOfChansToCopyFromBuf; i++)
                {
                    ChannelCounts[(FirstChanInBuf + i) % 16] = (UInt16)marshallingArray[BufLen - NumOfChansToCopyFromBuf + i];
                }
            }

            /* This is used to append marshallingArray and move it to another array
                * The ADC callback is being refreshed
                * Change number of channels here. Change designated channel here.
                */

            /*change the numbe rof channels below. If all channels are needed, change "i+=7" in loop to "i++" and uncomment lines
                * 585 through 600 and comment out 578 to 584.
                *
            */
            for (int i = 0; i < marshallingArray.Length; i += 8)
            {
                // This will only intake Data from channels 00 and 01.
                // ============== JOANNA CHANGE ==============
                // To add more channels uncomment the desired amount of marshalling values.
                // Then go to the Generate counts for loop and change the range for i.
                // You can also add more channels other than the 8 listed.

                // =============== SEAN CHANGE ===============
                // Old version of making strings out of everything, new version stores the values for later
                //Int16 temp = 0;
                //TimeSpan currentTime = timeStart.Elapsed;                    
                //begin += currentTime.ToString() + "\r\n";
                //temp = marshallingArray[i];
                //begin += temp.ToString() + "\r\n";
                //++i;
                //temp = marshallingArray[i];
                //begin += temp.ToString() + "\r\n";
                //DIOWrite += currentTime.ToString() + "\t\t";
                timeStamps.Add(timeStart.Elapsed);
                marshallValues.Add(marshallingArray[i]);
                marshallValues.Add(marshallingArray[i + 1]);
                //marshallValues.Add(marshallingArray[i + 2]);
                //marshallValues.Add(marshallingArray[i + 3]);
                /*marshallValues.Add(marshallingArray[i + 4]);
                marshallValues.Add(marshallingArray[i + 5]);
                marshallValues.Add(marshallingArray[i + 6]);
                marshallValues.Add(marshallingArray[i + 7]);*/
                // ===========================================

                /*
                    * 8 Channels - 8 Data - 1 Time Input
                if( i == 0 || i % 8 == 0)
                {
                    TimeSpan currentTime = timeStart.Elapsed;
                    begin += currentTime.ToString() + "\r\n";
                    temp = marshallingArray[i];
                    begin += temp.ToString() + "\r\n";
                }
                else
                {
                    temp = marshallingArray[i];
                    begin += temp.ToString() + "\r\n";
                }
                */
                WriteDIO();
            }

            // Because the display can be slow, and this is called from a worker
            // thread, we just load each channel's last reading for display:
            // Any direct processing of the data, like a Fourier transform etc, would go here instead.
            if (BufLen >= 16) // if enough data to display not end
            {
                // Must be run asynchronously on the UI thread (BeginInvoke vs. Invoke) (members of the ISynchronizeInvoke interface)
                // ...asynchronous version of Invoke, which returns immediately and arranges for the method to run on the UI thread at some point in the future
                // Similar to but not the same as Asyncronous delegate Invocation to prevent thread blocking of the UI thread

                //  Display all the new data through a single delegate Invoke on one control:
                VoltLabel[0].BeginInvoke(new UpdateGUIFormDel(UpdateGUIFormDisplay), new object[] { ChannelCounts });
            }

            // SEAN CHANGE: The files are now written when the last packet is received, instead of when the application is closed
            if ((Flags & 2) != 0)
            {
                StatusTextBox.BeginInvoke(new UpdateStatusDel(UpdateStatusDisplay), new object[] { "\r\nWriting Data Files..." });
                writeDataFiles();
            }
        }

        // SEAN CHANGE: Wrote new function to convert all data to strings at the end, and then write out all at once
        private void writeDataFiles()
        {
            // Using SpecialFolders to grab the current user's desktop, instead of a hardcoded path
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string currentPath = System.AppDomain.CurrentDomain.BaseDirectory;
            string testPath = Path.Combine(currentPath,"SpecUI_DAQ");
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            string countUNIX = unixTimestamp.ToString();
            string countPath = Path.Combine(testPath, countUNIX+".txt"); // "CountTest.txt");
            string opdioPath = Path.Combine(testPath, countUNIX+".OPDIO");

            // Check if the Test folder exists, and create it if needed
            if (!Directory.Exists(testPath))
            {
                Directory.CreateDirectory(testPath);
            }

            // StringBuilder is a class made for doing rapid string manipulation much faster than the base string class
            StringBuilder countStr = new StringBuilder(10_000_000);
            countStr.Append(passRangeCode.ToString() + "\r\n");
            StringBuilder dioStr = new StringBuilder(10_000_000);
            dioStr.Append(headers);

            // Go through each timestamp, and generate the strings for both files
            // This seems to be necessary, as there are consistently 3 more timestamps than recoded values, no idea why...
            int pinCount = (int)numPins;
            int datalimit = Math.Min(Math.Min(timeStamps.Count, (marshallValues.Count / 2)), (dioValues.Count / pinCount));
            for (int scount = 0; scount < datalimit; ++scount)
            {
                string timeStr = timeStamps[scount].ToString();
                countStr.Append(timeStr);
                countStr.Append("\r\n");
                dioStr.Append(timeStr);
                dioStr.Append("\t\t");
                // Generate the counts
                for (int i = 0; i < 2; ++i)
                {
                    short counts = marshallValues[scount * 2 + i];
                    countStr.Append(counts);
                    countStr.Append("\r\n");
                }

                // Generate the dio values
                for (int i = 0; i < numPins; ++i)
                {
                    byte dioval = dioValues[scount * pinCount + i];
                    dioStr.Append(dioval);
                    dioStr.Append("\t\t");
                }
                dioStr.Append("\r\n");
            }

            // Use the new paths
            File.WriteAllText(countPath, countStr.ToString());
            File.WriteAllText(opdioPath, dioStr.ToString());
        }

        /// <summary>
        /// Stop the acquisition in the AIOUSB.dll, verify and display status of the shutdown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStop_Click(object sender, EventArgs e)
        {
            // Stop the acquisition verify return values:
            UInt32 EndStatus, IOStatus = 1;
            UInt32 ERROR_SUCCESS = 0;

            // Stop Continuous acquisition:
            EndStatus = AIOUSB.ADC_BulkContinuousEnd(DeviceIndex, ref IOStatus);

            // Make sure we get the "Stream ended" flagged message in callback first.
            Application.DoEvents();
            System.Threading.Thread.Sleep(500); // Since were on this UI thread (not necesary)

            // Thread safe msg display:
            if ((EndStatus == ERROR_SUCCESS) && (IOStatus == ERROR_SUCCESS))
                textBoxStatus.BeginInvoke(new UpdateStatusDel(UpdateStatusDisplay), new object[] { "\r\n Acquisition complete." });
            else if (EndStatus == ERROR_SUCCESS)
                textBoxStatus.BeginInvoke(new UpdateStatusDel(UpdateStatusDisplay), new object[] { " Acquisition ended with I/O error " + IOStatus.ToString("00") + "." });
            else
                textBoxStatus.BeginInvoke(new UpdateStatusDel(UpdateStatusDisplay), new object[] { " Error " + EndStatus.ToString("00") + " ending acquisition." });

            //            comboBoxCalibration.Enabled = true;
            //            comboBoxRange.Enabled = true;
            //            btnExecute.Enabled = true;
            //            btnStop.Enabled = false;
            //            checkBoxDIFF.Enabled = true;

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            UInt32 EndStatus, IOStatus = 1;
            timeStart.Stop();
            // Stop Continuous acquisition:
            EndStatus = AIOUSB.ADC_BulkContinuousEnd(DeviceIndex, ref IOStatus);

            // SEAN CHANGE: Moved the file IO to a different function

            // Old hardcoded paths
            //File.WriteAllText(@"C:\Users\abku6744\Desktop\Test\CountTest.txt", begin);
            //File.WriteAllText(@"C:\Users\abku6744\Desktop\Test\OPDIO.txt", headers + DIOWrite.ToString());
        }
    }
}
