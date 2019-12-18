using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using FTD2XX_NET;



namespace EEPROM
{
    class Program
    {
        public const byte FT232R_TXD = 0x01;
        public const byte FT232R_RXD = 0x02;
        public const byte FT232R_RTSn = 0x04;
        public const byte FT232R_CTSn = 0x08;
        public const byte FT232R_DTRn = 0x10;
        public const byte FT232R_DSRn = 0x20;
        public const byte FT232R_DCDn = 0x40;
        public const byte FT232R_RIn = 0x80;

        // SPI pin mapping
        public const byte SPI_CSn = (FT232R_RTSn);
        public const byte SPI_CLK = (FT232R_CTSn);
        public const byte SPI_MOSI = (FT232R_TXD);
        public const byte SPI_MISO = (FT232R_RXD);

        // read/write port
        public const byte READ_PORT = (SPI_MISO);
        public const byte WRITE_PORT = (SPI_MOSI | SPI_CSn | SPI_CLK);

        public static FTDI myFtdiDevice;

        static void SPI_Initialize(uint baudrate)
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
            ftStatus = myFtdiDevice.OpenByDescription("FT232H");
            if (ftStatus != FTDI.FT_STATUS.FT_OK)
            {
                // Wait for a key press
                Console.WriteLine("Failed to open device (error " + ftStatus.ToString() + ")");
                Console.ReadKey();
                return;
            }

            myFtdiDevice.SetBitMode(0x00, FTDI.FT_BIT_MODES.FT_BIT_MODE_RESET);

            myFtdiDevice.SetBitMode(unchecked((byte)~(READ_PORT)), FTDI.FT_BIT_MODES.FT_BIT_MODE_SYNC_BITBANG);

            myFtdiDevice.SetBaudRate(baudrate);

            myFtdiDevice.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);

            SPI_Stop();
        }

        static void SPI_Start()
        {
            byte[] bits = new byte[1];
            uint bytes = 0;

            myFtdiDevice.GetPinStates(ref bits[0]);

            // rise CS#
            bits[0] &= unchecked((byte)~(SPI_CSn));

            myFtdiDevice.Write(bits, 1, ref bytes);
            myFtdiDevice.Read(bits, 1, ref bytes);
        }

        static void SPI_Stop()
        {
            byte[] bits = new byte[1];
            uint bytes = 0;

            myFtdiDevice.GetPinStates(ref bits[0]);

            // rise CS#
            bits[0] |= (SPI_CSn);

            myFtdiDevice.Write(bits, 1, ref bytes);
            myFtdiDevice.Read(bits, 1, ref bytes);
        }



        public static int SPI_WRITE_BLOCK_SIZE = 1024;
        static void SPI_Write(int len, byte[] wBuf, int idx)
        {
            byte bits = 0;
            uint bytes = 0;

            byte[] writeBuf = new byte[16 * SPI_WRITE_BLOCK_SIZE];
            byte[] readBuf = new byte[16 * SPI_WRITE_BLOCK_SIZE];

            if (len > SPI_WRITE_BLOCK_SIZE)
            {
                // divide
                for (int i = 0; i < len; i += SPI_WRITE_BLOCK_SIZE)
                {
                    int size = (len - i > SPI_WRITE_BLOCK_SIZE) ? (SPI_WRITE_BLOCK_SIZE) : (len - i);
                    SPI_Write(size, wBuf, i);
                }
                return;
            }

            myFtdiDevice.GetPinStates(ref bits);

            // clear MOSI and CLK bit
            bits &= unchecked((byte)~(SPI_MOSI | SPI_CLK));

            for (int i = 0; i < len; i++)
            {
                byte data = wBuf[idx + i];

                // generate signal
                for (int j = 0; j < 8; j++)
                {
                    // fall CLK and set MOSI
                    if ((data & 0x80) != 0x00)
                    {
                        writeBuf[2 * (i * 8 + j) + 0] = (byte)(bits | SPI_MOSI);
                    }
                    else
                    {
                        writeBuf[2 * (i * 8 + j) + 0] = bits;
                    }

                    // rise CLK
                    if ((data & 0x80) != 0x00)
                    {
                        writeBuf[2 * (i * 8 + j) + 1] = (byte)(bits | SPI_MOSI | SPI_CLK);
                    }
                    else
                    {
                        writeBuf[2 * (i * 8 + j) + 1] = (byte)(bits | SPI_CLK);
                    }

                    // shift
                    data = (byte)(data << 1);
                }
            }

            myFtdiDevice.Write(writeBuf, 16 * len, ref bytes);
            myFtdiDevice.Read(readBuf, (uint)(16 * len), ref bytes);

            if (bytes != 16 * len)
            {
                Console.WriteLine("Error! FT_Write reported value too short.");
            }
        }


        public static int SPI_READ_BLOCK_SIZE = 4096;

        static void SPI_Read(int len, byte[] rBuf, int idx)
        {
            byte bits = 0;
            uint bytes = 0;
            byte[] writeBuf = new byte[16 * SPI_READ_BLOCK_SIZE];
            byte[] readBuf = new byte[16 * SPI_READ_BLOCK_SIZE];

            if (len > SPI_READ_BLOCK_SIZE)
            {
                // divide
                for (int i = 0; i < len; i += SPI_READ_BLOCK_SIZE)
                {
                    int size = (len - i > SPI_READ_BLOCK_SIZE) ? (SPI_READ_BLOCK_SIZE) : (len - i);
                    SPI_Read(size, rBuf, i);
                }
                return;
            }

            myFtdiDevice.GetPinStates(ref bits);

            bits &= unchecked((byte)~(SPI_MOSI | SPI_CLK));

            // generate clk
            for (int i = 0; i < 8 * len; i++)
            {
                // fall CLK
                writeBuf[2 * i + 0] = bits;
                // rise CLK
                writeBuf[2 * i + 1] = (byte)(bits | SPI_CLK);
            }

            myFtdiDevice.Write(writeBuf, 16 * len, ref bytes);
            myFtdiDevice.Read(readBuf, (uint)(16 * len), ref bytes);

            if (bytes != 16 * len)
            {
                Console.WriteLine("Error! FT_Read reported value too short.");
            }

            for (int i = 0; i < len; i++)
            {
                byte data = 0;
                for (int j = 0; j < 8; j++)
                {
                    if ((readBuf[2 * (8 * i + j) + 1] & SPI_MISO) != 0)
                    {
                        data = (byte)((data << 1) | 1);
                    }
                    else
                    {
                        data = (byte)((data << 1) | 0);
                    }
                }
                rBuf[idx + i] = data;
            }
        }


        static void Main(string[] args)
        {
            int len = 256;
            byte[] wBuf = new byte[len];

            for (int i = 0; i < len; i++)
            {
                wBuf[i] = (byte)i;
            }

            SPI_Initialize(4*10^7);

            while (true)
            {
                SPI_Write(len, wBuf, 0);
                Thread.Sleep(500);
            }
        }
    }
}
