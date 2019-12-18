using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using FTD2XX_NET;


namespace EEPROM
{
    class Program
    {
        static void Main(string[] args)
        {
            UInt32 ftdiDeviceCount = 0;
            FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;

            // Create new instance of the FTDI device class
            FTDI myFtdiDevice = new FTDI();

            // Determine the number of FTDI devices connected to the machine
            ftStatus = myFtdiDevice.GetNumberOfDevices(ref ftdiDeviceCount);
            // Check status
            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                Console.WriteLine("Number of FTDI devices: " + ftdiDeviceCount.ToString());
                Console.WriteLine("");
            }
            else
            {
                // Wait for a key press
                Console.WriteLine("Failed to get number of devices (error " + ftStatus.ToString() + ")");
                Console.ReadKey();
                return;
            }

            // If no devices available, return
            if (ftdiDeviceCount == 0)
            {
                // Wait for a key press
                Console.WriteLine("Failed to get number of devices (error " + ftStatus.ToString() + ")");
                Console.ReadKey();
                return;
            }

            // Allocate storage for device info list
            FTDI.FT_DEVICE_INFO_NODE[] ftdiDeviceList = new FTDI.FT_DEVICE_INFO_NODE[ftdiDeviceCount];

            // Populate our device list
            ftStatus = myFtdiDevice.GetDeviceList(ftdiDeviceList);

            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                for (UInt32 i = 0; i < ftdiDeviceCount; i++)
                {
                    Console.WriteLine("Device Index: " + i.ToString());
                    Console.WriteLine("Flags: " + String.Format("{0:x}", ftdiDeviceList[i].Flags));
                    Console.WriteLine("Type: " + ftdiDeviceList[i].Type.ToString());
                    Console.WriteLine("ID: " + String.Format("{0:x}", ftdiDeviceList[i].ID));
                    Console.WriteLine("Location ID: " + String.Format("{0:x}", ftdiDeviceList[i].LocId));
                    Console.WriteLine("Serial Number: " + ftdiDeviceList[i].SerialNumber.ToString());
                    Console.WriteLine("Description: " + ftdiDeviceList[i].Description.ToString());
                    Console.WriteLine("");
                }
            }


            // Open first device in our list by serial number
            ftStatus = myFtdiDevice.OpenBySerialNumber(ftdiDeviceList[0].SerialNumber);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Wait for a key press
                Console.WriteLine("Failed to open device (error " + ftStatus.ToString() + ")");
                Console.ReadKey();
                return;
            }


            // Create our device EEPROM structure based on the type of device we have open
            if (ftdiDeviceList[0].Type == FTDI.FT_DEVICE.FT_DEVICE_232R)
            {
                // We have an FT232R or FT245R so use FT232R EEPROM structure
                FTDI.FT232R_EEPROM_STRUCTURE myEEData = new FTDI.FT232R_EEPROM_STRUCTURE();
                // Read the device EEPROM
                // This can throw an exception if trying to read a device type that does not 
                // match the EEPROM structure being used, so should always use a 
                // try - catch block when calling
                try
                {
                    ftStatus = myFtdiDevice.ReadFT232REEPROM(myEEData);
                }
                catch (FTDI.FT_EXCEPTION)
                {
                    Console.WriteLine("Exception thrown when calling ReadFT232REEPROM");
                }

                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    // Wait for a key press
                    Console.WriteLine("Failed to read device EEPROM (error " + ftStatus.ToString() + ")");
                    Console.ReadKey();
                    // Close the device
                    myFtdiDevice.Close();
                    return;
                }

                // Write common EEPROM elements to our console
                Console.WriteLine("EEPROM Contents for device at index 0:");
                Console.WriteLine("Vendor ID: " + String.Format("{0:x}", myEEData.VendorID));
                Console.WriteLine("Product ID: " + String.Format("{0:x}", myEEData.ProductID));
                Console.WriteLine("Manufacturer: " + myEEData.Manufacturer.ToString());
                Console.WriteLine("Manufacturer ID: " + myEEData.ManufacturerID.ToString());
                Console.WriteLine("Description: " + myEEData.Description.ToString());
                Console.WriteLine("Serial Number: " + myEEData.SerialNumber.ToString());
                Console.WriteLine("Max Power: " + myEEData.MaxPower.ToString() + "mA");
                Console.WriteLine("Self Powered: " + myEEData.SelfPowered.ToString());
                Console.WriteLine("Remote Wakeup Enabled: " + myEEData.RemoteWakeup.ToString());
                Console.WriteLine("");

                // Change our serial number to write back to device
                // By setting to an empty string, we allow the FTD2XX DLL 
                // to generate a serial number
                myEEData.SerialNumber = String.Empty;

                // Write our modified data structure back to the device EEPROM
                // This can throw an exception if trying to write a device type that does not 
                // match the EEPROM structure being used, so should always use a 
                // try - catch block when calling
                try
                {
                    ftStatus = myFtdiDevice.WriteFT232REEPROM(myEEData);
                }
                catch (FTDI.FT_EXCEPTION)
                {
                    Console.WriteLine("Exception thrown when calling WriteFT232REEPROM");
                }

                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    // Wait for a key press
                    Console.WriteLine("Failed to write device EEPROM (error " + ftStatus.ToString() + ")");
                    Console.ReadKey();
                    // Close the device
                    myFtdiDevice.Close();
                    return;
                }
            }
            else if (ftdiDeviceList[0].Type == FTDI.FT_DEVICE.FT_DEVICE_2232)
            {
                // We have an FT2232 so use FT2232 EEPROM structure
                FTDI.FT2232_EEPROM_STRUCTURE myEEData = new FTDI.FT2232_EEPROM_STRUCTURE();
                // Read the device EEPROM
                ftStatus = myFtdiDevice.ReadFT2232EEPROM(myEEData);
                // This can throw an exception if trying to read a device type that does not 
                // match the EEPROM structure being used, so should always use a 
                // try - catch block when calling
                try
                {
                    ftStatus = myFtdiDevice.ReadFT2232EEPROM(myEEData);
                }
                catch (FTDI.FT_EXCEPTION)
                {
                    Console.WriteLine("Exception thrown when calling ReadFT2232EEPROM");
                }

                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    // Wait for a key press
                    Console.WriteLine("Failed to read device EEPROM (error " + ftStatus.ToString() + ")");
                    Console.ReadKey();
                    // Close the device
                    myFtdiDevice.Close();
                    return;
                }

                // Write common EEPROM elements to our console
                Console.WriteLine("EEPROM Contents for device at index 0:");
                Console.WriteLine("Vendor ID: " + String.Format("{0:x}", myEEData.VendorID));
                Console.WriteLine("Product ID: " + String.Format("{0:x}", myEEData.ProductID));
                Console.WriteLine("Manufacturer: " + myEEData.Manufacturer.ToString());
                Console.WriteLine("Manufacturer ID: " + myEEData.ManufacturerID.ToString());
                Console.WriteLine("Description: " + myEEData.Description.ToString());
                Console.WriteLine("Serial Number: " + myEEData.SerialNumber.ToString());
                Console.WriteLine("Max Power: " + myEEData.MaxPower.ToString() + "mA");
                Console.WriteLine("Self Powered: " + myEEData.SelfPowered.ToString());
                Console.WriteLine("Remote Wakeup Enabled: " + myEEData.RemoteWakeup.ToString());
                Console.WriteLine("");

                // Change our serial number to write back to device
                // By setting to an empty string, we allow the FTD2XX DLL 
                // to generate a serial number
                myEEData.SerialNumber = String.Empty;

                // Write our modified data structure back to the device EEPROM
                // This can throw an exception if trying to write a device type that does not 
                // match the EEPROM structure being used, so should always use a 
                // try - catch block when calling
                try
                {
                    ftStatus = myFtdiDevice.WriteFT2232EEPROM(myEEData);
                }
                catch (FTDI.FT_EXCEPTION)
                {
                    Console.WriteLine("Exception thrown when calling WriteFT2232EEPROM");
                }

                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    // Wait for a key press
                    Console.WriteLine("Failed to write device EEPROM (error " + ftStatus.ToString() + ")");
                    Console.ReadKey();
                    // Close the device
                    myFtdiDevice.Close();
                    return;
                }
            }
            else if (ftdiDeviceList[0].Type == FTDI.FT_DEVICE.FT_DEVICE_BM)
            {
                // We have an FT232B or FT245B so use FT232B EEPROM structure
                FTDI.FT232B_EEPROM_STRUCTURE myEEData = new FTDI.FT232B_EEPROM_STRUCTURE();
                // Read the device EEPROM
                ftStatus = myFtdiDevice.ReadFT232BEEPROM(myEEData);
                // This can throw an exception if trying to read a device type that does not 
                // match the EEPROM structure being used, so should always use a 
                // try - catch block when calling
                try
                {
                    ftStatus = myFtdiDevice.ReadFT232BEEPROM(myEEData);
                }
                catch (FTDI.FT_EXCEPTION)
                {
                    Console.WriteLine("Exception thrown when calling ReadFT232BEEPROM");
                }

                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    // Wait for a key press
                    Console.WriteLine("Failed to read device EEPROM (error " + ftStatus.ToString() + ")");
                    Console.ReadKey();
                    // Close the device
                    myFtdiDevice.Close();
                    return;
                }

                // Write common EEPROM elements to our console
                Console.WriteLine("EEPROM Contents for device at index 0:");
                Console.WriteLine("Vendor ID: " + String.Format("{0:x}", myEEData.VendorID));
                Console.WriteLine("Product ID: " + String.Format("{0:x}", myEEData.ProductID));
                Console.WriteLine("Manufacturer: " + myEEData.Manufacturer.ToString());
                Console.WriteLine("Manufacturer ID: " + myEEData.ManufacturerID.ToString());
                Console.WriteLine("Description: " + myEEData.Description.ToString());
                Console.WriteLine("Serial Number: " + myEEData.SerialNumber.ToString());
                Console.WriteLine("Max Power: " + myEEData.MaxPower.ToString() + "mA");
                Console.WriteLine("Self Powered: " + myEEData.SelfPowered.ToString());
                Console.WriteLine("Remote Wakeup Enabled: " + myEEData.RemoteWakeup.ToString());
                Console.WriteLine("");

                // Change our serial number to write back to device
                // By setting to an empty string, we allow the FTD2XX DLL 
                // to generate a serial number
                myEEData.SerialNumber = String.Empty;

                // Write our modified data structure back to the device EEPROM
                // This can throw an exception if trying to write a device type that does not 
                // match the EEPROM structure being used, so should always use a 
                // try - catch block when calling
                try
                {
                    ftStatus = myFtdiDevice.WriteFT232BEEPROM(myEEData);
                }
                catch (FTDI.FT_EXCEPTION)
                {
                    Console.WriteLine("Exception thrown when calling WriteFT232BEEPROM");
                }

                if (ftStatus != FTDI.FT_STATUS.FT_OK)
                {
                    // Wait for a key press
                    Console.WriteLine("Failed to write device EEPROM (error " + ftStatus.ToString() + ")");
                    Console.ReadKey();
                    // Close the device
                    myFtdiDevice.Close();
                    return;
                }
            }


            // Use cycle port to force a re-enumeration of the device.  
            // In the FTD2XX_NET class library, the cycle port method also 
            // closes the open handle so no need to call the Close method separately.
            ftStatus = myFtdiDevice.CyclePort();

            UInt32 newFtdiDeviceCount = 0;
            do
            {
                // Wait for device to be re-enumerated
                // The device will have the same location since it has not been 
                // physically unplugged, so we will keep trying to open it until it succeeds
                ftStatus = myFtdiDevice.OpenByLocation(ftdiDeviceList[0].LocId);
                Thread.Sleep(1000);
            } while (ftStatus != FTDI.FT_STATUS.FT_OK);

            // Close the device
            myFtdiDevice.Close();

            // Re-create our device list
            ftStatus = myFtdiDevice.GetNumberOfDevices(ref newFtdiDeviceCount);
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Wait for a key press
                Console.WriteLine("Failed to get number of devices (error " + ftStatus.ToString() + ")");
                Console.ReadKey();
                return;
            }

            // Re-populate our device list
            ftStatus = myFtdiDevice.GetDeviceList(ftdiDeviceList);

            if (ftStatus == FTDI.FT_STATUS.FT_OK)
            {
                for (UInt32 i = 0; i < ftdiDeviceCount; i++)
                {
                    Console.WriteLine("Device Index: " + i.ToString());
                    Console.WriteLine("Flags: " + String.Format("{0:x}", ftdiDeviceList[i].Flags));
                    Console.WriteLine("Type: " + ftdiDeviceList[i].Type.ToString());
                    Console.WriteLine("ID: " + String.Format("{0:x}", ftdiDeviceList[i].ID));
                    Console.WriteLine("Location ID: " + String.Format("{0:x}", ftdiDeviceList[i].LocId));
                    Console.WriteLine("Serial Number: " + ftdiDeviceList[i].SerialNumber.ToString());
                    Console.WriteLine("Description: " + ftdiDeviceList[i].Description.ToString());
                    Console.WriteLine("");
                }
            }

            // Wait for a key press
            Console.WriteLine("Press any key to continue.");
            Console.ReadKey();
            return;

        }
    }
}
