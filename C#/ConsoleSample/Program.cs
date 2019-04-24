using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading; // For testing Thread.Sleep() effects

using System.Runtime.InteropServices; // for callback
using AIOUSBNet;  // the namespace exposes the AIOUSB Class interface 


// This is a simple console App:
namespace ConsoleApplication1
{
    class Program
    {
        // Global static persistent pointer to callback function defined in Net wrapper dll:
        public static AIOUSB.ADCallback myCallBack;         
        
        static int TotalAmountOfDataReceived = 0;  // will eventually rollover reasonably at int.MaxValue
        static double[] VoltsPerChannel = new double[16];

        // Note: BufSize returned is actually total number of bytes, so the array of UInt16's lenghth is actually 1/2 of BufSize
        public static void ADCallbackReport(IntPtr pBuf, UInt32 BufSize, UInt32 Flags, UInt32 Context)
        {
            int LastChanInBuf, FirstChanInBuf, NumOfChansToCopyFromBuf;
            int BufLen = ((int)BufSize / 2);                // length of the buffer in 16 bit elements instead of 8-bit
            Int16[] marshallingArray = new Int16[BufLen];   // array for Int16 copy of buf for marshalling

            // Copy pBuf into an Int16 array then convert to UInt16 when used 
            // This is required due to CLR restraints on IntPtr and Marshal.Copy() for unmanaged code array pointers
            Marshal.Copy(pBuf, marshallingArray, 0, (marshallingArray.Length));

            TotalAmountOfDataReceived += BufLen;  // By default, C# arithmetic is done in an unchecked context, meaning values will roll over. So it will eventually rollover reasonably at int.MaxValue (+1 %16)
            NumOfChansToCopyFromBuf = Math.Min(16, BufLen);
            LastChanInBuf = (TotalAmountOfDataReceived + 15) % 16;   // channel # of actual last entry in this buf of data (ie 15)
            FirstChanInBuf = (16 + (LastChanInBuf - NumOfChansToCopyFromBuf + 1  )) % 16;  // zero based (ie 0)

            for (int i = 0; i < NumOfChansToCopyFromBuf; i++)
            {
                UInt16 counts = (UInt16)marshallingArray[BufLen - NumOfChansToCopyFromBuf + i];
                VoltsPerChannel[(FirstChanInBuf + i) % 16] = (double)(counts) / (double)(0xFFFF) * 20.0 - 10.0; 
            }

            // Display one sample of 16 channels of the data and total number sampled so far in console window:
             Console.SetCursorPosition(0, 0); // just reset cursor to avoid flicker

            for (int ch = 0; ch < 16; ch++)
                Console.Write( String.Format( "Ch: {0,2:D2}  Volts: {1,8:F3} \r\n", ch, VoltsPerChannel[ch] ) );

            Console.Write(String.Format("\n Total number of 16 Bit Data elements received: {0,2:D2} \r\n", TotalAmountOfDataReceived));

            Console.WriteLine("\r\n Onboard ADC Timing Started... Data collecting now... \r\n Press any key to stop program...");      
        }
  
      
        static void Main(string[] args)
        {

            myCallBack = new AIOUSB.ADCallback(ADCallbackReport);               // Instantiate delegate with named method
            UInt32 ConfigBufSize = 20;
            
            double Hz = 0.0;
            UInt32 DeviceIndex = AIOUSB.diOnly;
            UInt32 Status = 0;
            byte[] ConfigArray = new byte[ConfigBufSize];                       // array for ADC_SetConfig() dll calls

            Status = AIOUSB.CTR_StartOutputFreq(DeviceIndex, 0, ref Hz);        // Stop the counter, in case it was running   

            if (Status != 0)
            {
                Console.WriteLine(String.Format("\n Start Output Freq Status Error:{0,8:D8}", Status));
                Console.WriteLine("\n Possible card is not present ? \n\n Press any key to Exit Console App\n"); 
                Console.ReadKey(true); // this is a "while (not _kbhit()) do{/*nothing*/}  loop to close Console App
                return;
            }

            // Init all config values:
            for (int i = 0; i < 16; i++)
            {
                ConfigArray[i] = 0x01;                                          // range code 1 is ±10 volts, used for testing full range in this sample
            }
            ConfigArray[16] = 0x00;                                             // 0 Take actual data, not internal calibration sources.
            ConfigArray[17] = 0x05;                                             // 0x05 Select Scans of selected channels on counter rising edges.
            ConfigArray[18] = 0xF0;                                             // 0xF0 Select all 16 channels 0-15
            ConfigArray[19] = 0x00;                                             // 0 No oversamples.

            // Init Config:
            AIOUSB.ADC_SetConfig(DeviceIndex, ConfigArray,  ref ConfigBufSize);
            if (Status != 0) // !ERROR_SUCCESS 
            {
                Console.WriteLine(String.Format("Config Error:{0,8:D8}", Status));
                Console.ReadKey(true); // this is a "while (not _kbhit()) do{/*nothing*/}  loop to close Console App
                return;
            }


            // Status = AIOUSB.ADC_SetCal(DeviceIndex, ":AUTO:");                // if your card supports it ("A" models) you can use onboard calibration voltages, and perform an autocal with :AUTO:
            // Status = AIOUSB.ADC_SetCal(DeviceIndex, {PathToCalibrationFile}); // if your card supports it ("A" models) you could also pass a path to a file containing the necessary calibration values
            // Status = AIOUSB.ADC_SetCal(DeviceIndex, ":NONE:");                // :NONE: and :1TO1: are identical, they both set the calibration system to "uncalibated".  Use whichever constant feels intuitive to you
            Status = AIOUSB.ADC_SetCal(DeviceIndex, ":1TO1:");
            //if (Status != 0) //!ERROR_SUCCESS
            //{
            //    Console.WriteLine(String.Format("Calibration Error:{0,8:D8}", Status));
            //    Console.ReadKey(true); // this is a "while (not _kbhit()) do{/*nothing*/}  loop to close Console App
            //    return;
            //}


            Hz = 1000.0;  // Since we've put it in scan mode, this is 1kHz per channel, or 16kHz aggregate.  Each channel in the scan is taken 2usec apart, for 16 channels, then it waits for the next counter output pulse before taking another scan.
            Status = AIOUSB.ADC_BulkContinuousCallbackStartClocked(DeviceIndex, 1024, 64, 0, myCallBack, ref Hz);
            if (Status != 0) // !ERROR_SUCCESS 
            {
                Console.WriteLine(String.Format("Bulk Continuous Error:{0,8:D8}", Status));
                Console.ReadKey(true); // this is a "while (not _kbhit()) do{/*nothing*/}  loop to close Console App
                return;
            }

            Console.WriteLine("Callback Started!");

            // Code now runs in the callback's thread until this ReadKey() gets a keystroke.
            Console.WriteLine("Got Here 0");
            Console.ReadKey(true);  // this is a "while (not _kbhit()) do{/*nothing*/}  loop  for end callback
            Console.WriteLine("I got here");
            uint IOStatus = 0;
            AIOUSB.ADC_BulkContinuousEnd(DeviceIndex, IOStatus);
           
            Console.WriteLine("\n Continuous Callback Stopped... \n Press any key to Exit Console App\n");

            Console.ReadKey(true); // this is a "while (not _kbhit()) do{/*nothing*/}  loop to close Console App

        }
    }
}
