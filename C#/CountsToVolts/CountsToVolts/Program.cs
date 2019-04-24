using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Threading;
// SEAN CHANGE: Added a reference to Windows.Forms for the MessageBox class, so the solution changed as well,
// not just the code
using WinForms = System.Windows.Forms;

/* This program will read in the file containing the Counts-Int16 and convert it to voltages
 * The file will be saved in a separate file for analysis
 */

namespace CountsToVolts
{
    class ReadFromFile
    {
        static void Main(string[] filename)
        {
            // Creates a temporary variable to store line by line
            // Rangecode is the CONSTANT from the ConfigArray
            // Output is used to append all of the data and send it to a new file later
            // Current is used to seperate the channels and count the number of lines
            string line = "";
            string output = "";
            int current = -1;
            byte RangeCode = 0;
            int n = 2; //number of channels

            // ==================== SEAN CHANGE =====================
            // Using SpecialFolders to grab the current user's desktop, instead of a hardcoded path
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string currentPath = System.AppDomain.CurrentDomain.BaseDirectory;
            // System.Console.WriteLine(filename[0]);

            //foreach (Object obj in args)
            //{
            //    Console.WriteLine(obj);
            //}
            //string TEST = args.ToString();
            Console.WriteLine("Successfully Found File:");
            Console.WriteLine(filename[0]);
            Console.WriteLine("Please wait while this operation finishes, program will exit when complete");
            string testPath = Path.Combine(currentPath, "SpecUI_DAQ");
            string countPath = Path.Combine(testPath,filename[0]);
            string tempname = filename[0];
            int found = tempname.IndexOf(".");
            string convertedPath = Path.Combine(testPath,"Converted_"+tempname.Substring(0,found)+".csv");
            Console.WriteLine(convertedPath);
            // Check if the Test folder exists, and complain otherwise
            if (!Directory.Exists(testPath))
            {
                WinForms.MessageBox.Show("Could not find the output files from the sampler.",
                    "File Error", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
                return;
            }
            // ======================================================

            // Old hardcoded path
            // System.IO.StreamReader file = new System.IO.StreamReader(@"C:\Users\abku6744\Desktop\Test\CountTest.txt");

            // Iterating through every line and converting the string into double values
            // Then conversion between counts into voltages
            // The new voltages will go into a new file
            output += "TimeStamp\t\t Channel 00\t Channel 01";
            // \t Channel 02\t Channel 03\t Channel 04\t Channel 05\t Channel 06\t Channel 07

            // ==================== SEAN CHANGE =====================
            // Wrapped the file in a using statement, so the file gets closed correctly in the event of an error.
            // Without this, the file has a chance to be corrupted or otherwise held open by the OS if the program crashes.
            // ======================================================
            using (StreamReader file = new StreamReader(countPath))
            {
                while ((line = file.ReadLine()) != null)
                {
                    /* The first if loop will extract the RangeCode from the incoming file (as found on the first line of text).
                     * The RangeCode is the conversion factor for the differential mode
                     * i.e if the differential was +-10 the rangecode is 9. Different modes will have different RangeCodes.
                     */
                    if (current == -1)
                    {
                        // System.Globalization.CultureInfo.InvariantCulture
                        // This is forcing the program to take the incoming string from the text file
                        // To be stored into line, by forcing it to go through the main
                        byte range = byte.Parse(line, System.Globalization.CultureInfo.InvariantCulture);
                        RangeCode = range;
                        current++;
                    }
                    // This if loop will be used to change the incoming channels that needs to be converted
                    // i.e if you are using n channels, current % n; n+1 being channels.
                    
                    else if (current == 0 || current % (n+1) == 0)
                    {
                        output += "\r\n" + line + "\t ";
                        current++;
                    }
                    else
                    {
                        /* This is the conversion code between Counts to Voltages. DO NOT CHANGE
                         */
                        double voltTemp = double.Parse(line, System.Globalization.CultureInfo.InvariantCulture);

                        if (voltTemp < 0)
                        {
                            voltTemp += 65536;
                        }

                        voltTemp = (voltTemp) / ((double)(0xFFFF)); //"Volts" now holds a value from 0 to 1.

                        if ((RangeCode & 0x01) != 0)        //Bit 0(mask 01 hex) indicates bipolar.
                            voltTemp = (voltTemp * 2) - 1;  //"Volts" now holds a value from -1 to +1.
                        if ((RangeCode & 0x02) == 0)        //Bit 1(mask 02 hex) indicates x2 gain.
                            voltTemp *= 2;
                        if ((RangeCode & 0x04) == 0)        //Bit 2(mask 04 hex) indicates x5 gain.
                            voltTemp *= 5;

                        // Formatting the Voltages to have 4 decimal places - Can change to whatever is needed
                        // The mod 8 is used to signify that after 8 channels has been displayed, jump to the next line
                        // and repeat the process
                        string s = String.Format(",{0:0.0000}", voltTemp);
                        output += s + "  V\t";
                        current++;

                    }
                }
            }

            // Check how much data is in file
            int checkData = (current+1) * 2;
            output += "\r\n" + checkData.ToString();

            // SEAN CHANGE: Write ALL data from the output string into the new file
            File.WriteAllText(convertedPath, output);
            
            // Old hardcoded path
            //File.WriteAllText(@"C:\Users\abku6744\Desktop\Test\ConvertedCount.txt", output);
        }
    }
}