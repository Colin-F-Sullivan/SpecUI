using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using AIOUSBNet;

namespace Sample1
{
    public partial class FormDetect : Form
    {
        private bool[] DeviceOK = new bool[8];

        // For the ovreloaded contructor as a sub form:
        Form1 mainForm;

        public FormDetect()
        {
            InitializeComponent();
            
            listBoxDeviceList.Items.Clear();
        }

        public FormDetect(Form1 mainForm)
        {
            // Ovreloaded contructor as a parent aware sub form:

            // set access to mainform
            this.mainForm = mainForm;

            InitializeComponent();

            btnDetect_Click(this, null);
        }

        private void btnDetect_Click(object sender, EventArgs e)
        {
            // Detect all Boards: 
            UInt32 Status = 0;
            UInt32 locDeviceIndex = AIOUSB.diFirst;   // local version default to First
            UInt32 DeviceMask;
            bool Found = false;

            UInt32 uPID = 0;
            Int32 NameSize = 256;
            string strName = "name";
            UInt32 DIOBytes = 0;
            UInt32 Counters = 0;

            listBoxDeviceList.Items.Clear();

            DeviceMask = AIOUSB.GetDevices();
	        
            for (int i = 0; i <= 7; i++)
	        {
                DeviceOK[i] = false;

                if ((DeviceMask & (1 << i)) != 0)
                {
                    locDeviceIndex = (uint)i;

                    // Get device info:
                    Status = AIOUSB.QueryDeviceInfo(locDeviceIndex, ref uPID, ref NameSize, ref strName, out DIOBytes, out Counters);

                    if (uPID >= 0x8040 && uPID <= 0x815D) // AIO and AI
                    {
                        listBoxDeviceList.Items.Add(" " + strName); // DIO
                        DeviceOK[i] = true;
                        Found = true;
                    }
                    else
                    {
                        listBoxDeviceList.Items.Add(" Not for this sample.");
                        break;
                    }                 
                }
                else
                {
                    listBoxDeviceList.Items.Add(" No device at this index.");
                }
            }
            
            if (Found)
            {
                txtStatus.Text = " Select device and click Accept or Cancel.";
            }
            else
            {
                txtStatus.Text = " No compatible devices found!";
            }	        
        }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            // Pass new index to mainform
            UInt32 myuint = 0;
            int myint = 0;
            myint = listBoxDeviceList.SelectedIndex;
             
            myuint = (uint)myint;
            mainForm.DeviceIndex = myuint;

            // Clear the controls:
            listBoxDeviceList.Items.Clear();
            txtStatus.Clear();

            // Close this form:
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            // Reject changes:
            // Device Index defaults to First available:

            txtStatus.Text = "No change to device selection.";
            
            // Clear the controls:
            listBoxDeviceList.Items.Clear();
            txtStatus.Clear();

            this.Close();
        }
    }
}
