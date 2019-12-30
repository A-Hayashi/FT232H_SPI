using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using FTD2XX_NET;
using static FTD2XX_NET.FTDI;

namespace EEPROM
{
    class Program
    {
        private const int MemSize = 1024;

        private static FTDI myFtdiDevice;
        private static byte[] DataOutBuffer = new byte[MemSize];
        private static byte[] DataInBuffer = new byte[MemSize];
        private static int dwNumBytesToSend = 0;

        private static bool FT232H_Initial()
        {

            UInt32 ftdiDeviceCount = 0;
            FTDI.FT_STATUS ftStatus = FTDI.FT_STATUS.FT_OK;

            // Create new instance of the FTDI device class
            myFtdiDevice = new FTDI();

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
                return false;
            }

            // If no devices available, return
            if (ftdiDeviceCount == 0)
            {
                // Wait for a key press
                Console.WriteLine("Failed to get number of devices (error " + ftStatus.ToString() + ")");
                Console.ReadKey();
                return false;
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
            ftStatus = myFtdiDevice.OpenByDescription("FT232H");
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Wait for a key press
                Console.WriteLine("Failed to open device (error " + ftStatus.ToString() + ")");
                Console.ReadKey();
                return false;
            }

            return true;
        }

        private static void SPI_CSEnable()
        {
            for (int i = 0; i < 5; i++)
            {
                DataOutBuffer[dwNumBytesToSend++] = 0x80;
                DataOutBuffer[dwNumBytesToSend++] = 0x08;
                DataOutBuffer[dwNumBytesToSend++] = 0x0b;
            }
        }

        private static void SPI_CSDisnable()
        {
            for (int i = 0; i < 5; i++)
            {
                DataOutBuffer[dwNumBytesToSend++] = 0x80;
                DataOutBuffer[dwNumBytesToSend++] = 0x00;
                DataOutBuffer[dwNumBytesToSend++] = 0x0b;
            }
        }

        private static bool SPI_Initial()
        {
            uint dwNumInputBuffer = 0;
            uint dwNumBytesRead = 0;
            uint dwNumBytesSent = 0;
            byte[] InputBuffer = new byte[512];
            FT_STATUS ftStatus;

            ftStatus = myFtdiDevice.ResetDevice();      //USBデバイスをリセットする


            {   //USB受信バッファを空にする
                ftStatus |= myFtdiDevice.GetRxBytesAvailable(ref dwNumInputBuffer);

                if ((ftStatus == FT_STATUS.FT_OK) && (dwNumInputBuffer > 0))
                {
                    ftStatus |= myFtdiDevice.Read(InputBuffer, dwNumInputBuffer, ref dwNumBytesRead);
                }
            }

            //ftStatus |= myFtdiDevice.FT_SetUSBParameters(ftHandle, 65535, 65535); //Set USB
            //request transfer size

            ftStatus |= myFtdiDevice.SetCharacters(0, false, 0, false);

            ftStatus |= myFtdiDevice.SetTimeouts(3000, 3000);//readタイムアウト=3s, writeタイムアウト=3s

            ftStatus |= myFtdiDevice.SetLatency(1);          //レイテンシタイマ=1ms

            ftStatus |= myFtdiDevice.SetBitMode(0x00, FT_BIT_MODES.FT_BIT_MODE_RESET);  //リセット

            ftStatus |= myFtdiDevice.SetBitMode(0xfb, FT_BIT_MODES.FT_BIT_MODE_MPSSE);  //MPSSEモード有効

            if (ftStatus != FT_STATUS.FT_OK)
            {
                Console.WriteLine("fail on initialize FT2232H device !");

                return false;
            }

            Thread.Sleep(50);

            {   //bad command:0xaaによりMPSSEと同期をとる
                int dwCount = 0;
                bool bCommandEchod = false;

                dwNumBytesToSend = 0;
                DataOutBuffer[dwNumBytesToSend++] = 0xaa;
                ftStatus = myFtdiDevice.Write(DataOutBuffer, dwNumBytesToSend, ref dwNumBytesSent);
                do
                {
                    ftStatus = myFtdiDevice.GetRxBytesAvailable(ref dwNumInputBuffer);
                } while ((dwNumInputBuffer == 0) && (ftStatus == FT_STATUS.FT_OK));

                ftStatus = myFtdiDevice.Read(InputBuffer, dwNumInputBuffer, ref dwNumBytesRead);

                for (dwCount = 0; dwCount < (dwNumBytesRead - 1); dwCount++)
                {
                    if ((InputBuffer[dwCount] == 0xfa) && (InputBuffer[dwCount + 1] == 0xaa))
                    {
                        bCommandEchod = true;
                        break;
                    }
                }
                if (bCommandEchod == false)
                {
                    Console.WriteLine("fail to synchronize MPSSE with command '0xAA'");
                    return false;
                }
            }

            {   //bad command:0xabによりMPSSEと同期をとる
                int dwCount = 0;
                bool bCommandEchod = false;

                dwNumBytesToSend = 0;
                DataOutBuffer[dwNumBytesToSend++] = 0xab;
                ftStatus = myFtdiDevice.Write(DataOutBuffer, dwNumBytesToSend, ref dwNumBytesSent);
                do
                {
                    ftStatus = myFtdiDevice.GetRxBytesAvailable(ref dwNumInputBuffer);
                } while ((dwNumInputBuffer == 0) && (ftStatus == FT_STATUS.FT_OK));

                ftStatus = myFtdiDevice.Read(InputBuffer, dwNumInputBuffer, ref dwNumBytesRead);

                for (dwCount = 0; dwCount < (dwNumBytesRead - 1); dwCount++)
                {
                    if ((InputBuffer[dwCount] == 0xfa) && (InputBuffer[dwCount + 1] == 0xab))
                    {
                        bCommandEchod = true;
                        break;
                    }
                }
                if (bCommandEchod == false)
                {
                    Console.WriteLine("fail to synchronize MPSSE with command '0xAB'");
                    return false;
                }
            }

            {
                dwNumBytesToSend = 0;
                DataOutBuffer[dwNumBytesToSend++] = 0x8b;   //クロック5分周有効
                DataOutBuffer[dwNumBytesToSend++] = 0x97;   //adaptive clocking無効
                DataOutBuffer[dwNumBytesToSend++] = 0x8d; 　//3 phase data clocking無効
                ftStatus = myFtdiDevice.Write(DataOutBuffer, dwNumBytesToSend, ref dwNumBytesSent);
            }

            {
                //freq[MHz] = 12[MHz] / (( 1 +[(0xValueH * 256) OR 0xValueL] ) * 2)
                //freq[MHz] = 12[MHz] / ( (1 +value) * 2)
                //(1 +value) * 2 = 12[MHz]/freq[MHz]
                //1 +value = 6[MHz]/freq[MHz]
                //value = 6[MHz]/freq[MHz]-1
                //value = 6000[kHz]/freq[kHz]-1

                uint kHz = 3000;
                uint dwClockDivisor = 6000/kHz-1;
                dwNumBytesToSend = 0;
                DataOutBuffer[dwNumBytesToSend++] = 0x80;          //下位バイトの方向・出力設定
                DataOutBuffer[dwNumBytesToSend++] = 0x00;          //Value:0b0000
                                                                   //D3:CS(0), D2:MISO(0), D1:MOSI(0), D0:SCK(0) 
                DataOutBuffer[dwNumBytesToSend++] = 0x0b;          //Direction:0x1011 
                                                                   //D3:CS(out), D2:MISO(in), D1:MOSI(out), D0:SCK(in) 

                DataOutBuffer[dwNumBytesToSend++] = 0x86;          //ボーレート設定:3MHz
                DataOutBuffer[dwNumBytesToSend++] = (byte)(dwClockDivisor & 0xff);  //ValueL
                DataOutBuffer[dwNumBytesToSend++] = (byte)(dwClockDivisor >> 8);    //ValueH

                ftStatus = myFtdiDevice.Write(DataOutBuffer, dwNumBytesToSend, ref dwNumBytesSent);
            }


            Thread.Sleep(20);

            {
                dwNumBytesToSend = 0;
                DataOutBuffer[dwNumBytesToSend++] = 0x85;       //ループバック無効
                ftStatus = myFtdiDevice.Write(DataOutBuffer, dwNumBytesToSend, ref dwNumBytesSent);
            }

            Thread.Sleep(30);

            Console.WriteLine("SPI initial successful");

            return true;

        }

        private static bool SPI_Write(byte[] wdata, int size)
        {
            FT_STATUS ftStatus;
            uint dwNumBytesSent = 0;
            dwNumBytesToSend = 0;

            SPI_CSEnable();
            //Clock Data Bytes Out on -ve clock edge MSB first (no read)
            DataOutBuffer[dwNumBytesToSend++] = 0x11;
            DataOutBuffer[dwNumBytesToSend++] = (byte)((size - 1) & 0xff);
            DataOutBuffer[dwNumBytesToSend++] = (byte)(((size - 1) >> 8) & 0xff);
            for (int i = 0; i < size; i++)
            {
                DataOutBuffer[dwNumBytesToSend++] = wdata[i];
            }
            SPI_CSDisnable();

            ftStatus = myFtdiDevice.Write(DataOutBuffer, dwNumBytesToSend, ref dwNumBytesSent);

            if (ftStatus != FT_STATUS.FT_OK)
            {
                Console.WriteLine("SPI write failed");
                return false;
            }

            return true;
        }

        private static bool SPI_Read(byte[] rdata, uint size)
        {
            FT_STATUS ftStatus;
            uint dwNumBytesSent = 0;
            uint dwNumBytesRead = 0;
            dwNumBytesToSend = 0;

            SPI_CSEnable();
            //Clock Data Bytes In on -ve clock edge MSB first (no write)
            DataOutBuffer[dwNumBytesToSend++] = 0x24;
            DataOutBuffer[dwNumBytesToSend++] = (byte)((size - 1) & 0xff);
            DataOutBuffer[dwNumBytesToSend++] = (byte)(((size - 1) >> 8) & 0xff);
            SPI_CSDisnable();

            ftStatus = myFtdiDevice.Write(DataOutBuffer, dwNumBytesToSend, ref dwNumBytesSent);
            ftStatus |= myFtdiDevice.Read(rdata, size, ref dwNumBytesRead);

            if (ftStatus != FT_STATUS.FT_OK)
            {
                Console.WriteLine("SPI read failed");
                return false;
            }

            return true;
        }


        static void Main(string[] args)
        {
            bool result;

            if (FT232H_Initial())
            {
                result = SPI_Initial();
            }
            else
            {
                result = false;
            }

            if (result == true)
            {
                byte[] wdata = new byte[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
                while (true)
                {
                    SPI_Write(wdata, wdata.Length);

                    Thread.Sleep(1);
                }
            }
        }
    }
}
