using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ViscTronics;

namespace ViscTronics.Zeitlib
{
    public class ZeitgeberException : Exception
    {
        public ZeitgeberException() { }
        public ZeitgeberException(string message) : base(message) { }
        public ZeitgeberException(string message, Exception innerException) : base(message, innerException) { }
    }

    #region Enums

    public enum PowerStatus { Battery, Charged, Charging }

    #endregion

    public class Zeitgeber
    {
        public bool isConnected = false;
        private const string DeviceId = "VID_04D8&PID_003F";  // Zeitgeber ID
        //private const string DeviceId = "Vid_04d8&Pid_003c";  // Bootloader ID

        public String DeviceDescription { get { return HidDevice.DeviceDescription; } }

        private HidDevice HidDevice;

        #region Command Constants

        // Basic system commands
        private const byte CMD_PING = 0x01;
        private const byte CMD_RESET = 0x02;
        private const byte CMD_SET_LED = 0x03;

        // Diagnostics
        private const byte CMD_GET_BATTERY_INFO = 0x10;
        private const byte CMD_GET_CPU_INFO = 0x11;

        // Display interface
        private const byte CMD_QUERY_DISPLAY = 0x20;
        private const byte CMD_SET_DISPLAY_POWER = 0x21;
        private const byte CMD_DISPLAY_OVERRIDE = 0x22;
        private const byte CMD_UPDATE_DISPLAY_BUF = 0x23;
        private const byte CMD_READ_DISPLAY_BUF = 0x24;

        // Sensors
        private const byte CMD_QUERY_SENSORS = 0x25;
        private const byte CMD_SET_SENSOR_ENABLE = 0x31;
        private const byte CMD_GET_SENSOR_DATA = 0x32;

        // Time & Date
        private const byte CMD_GET_DATETIME = 0x40;
        private const byte CMD_SET_DATETIME = 0x41;

        #endregion

        #region Structs

        private const int COMMAND_PACKET_SIZE = 65;

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = COMMAND_PACKET_SIZE)]
        public struct CommandStruct
        {
            public byte WindowsReserved;
            public byte Command;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = COMMAND_PACKET_SIZE-2)]
            public byte[] Data;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = COMMAND_PACKET_SIZE)]
        public struct SetLedStruct
        {
            public byte WindowsReserved;
            public byte Command;
            public byte Led;
            public byte Value;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = COMMAND_PACKET_SIZE)]
        public struct BatteryInfo
        {
            public byte WindowsReserved;
            public byte Command;
            public byte Padding;

            public UInt16 Level;
            public UInt16 Voltage;

            public UInt16 ChargeStatus;
            public UInt16 PowerStatus;
            public UInt16 BatteryStatus;

            public UInt16 bq25010_status;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = COMMAND_PACKET_SIZE)]
        public struct CpuInfo
        {
            public byte WindowsReserved;
            public byte Command;
            public byte Padding;

            public UInt16 systick;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = COMMAND_PACKET_SIZE)]
        public struct DisplayQuery
        {
            public byte WindowsReserved;
            public byte Command;
            public byte Padding;

            public UInt16 Width;
            public UInt16 Height;
            public UInt16 BitsPerPixel;

            public UInt16 DisplayOn;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = COMMAND_PACKET_SIZE)]
        public struct SensorsQuery
        {
            public byte WindowsReserved;
            public byte Command;
            public byte Padding;

            public UInt16 Count;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] Sensors;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = COMMAND_PACKET_SIZE)]
        public struct DateTimePacket
        {
            public byte WindowsReserved;
            public byte Command;
            public byte Padding;

            public byte Hour;
            public byte Minute;
            public byte Second;

            public byte DayOfWeek;
            public byte Day;
            public byte Month;
            public byte Year;
        }

        #endregion


        public Zeitgeber()
        {
            HidDevice = new HidDevice(DeviceId);
        }

        #region Setup

        /// <summary>
        /// Install additional driver information after the HID device has been installed.
        /// This is optional but adds a better description to the device manager.
        /// REQUIRES ADMINISTRATOR PRIVILEGE
        /// </summary>
        public void InstallDriverInfo()
        {
            HidDevice.SetFriendlyName("ViscTronics OLED Watch");
        }

        #endregion

        #region Communication

        /// <summary>
        /// Try and connect to the device.
        /// Note there is no Disconnect, as the connection is stateless.
        /// </summary>
        public void Connect()
        {
            try
            {
                HidDevice.Connect();
                isConnected = true;
            }
            catch (HidDeviceException e)
            {
                isConnected = false;
                throw new ZeitgeberException("Could not connect to the device", e);
            }

            // The PIC must be pinged every few seconds for it to show the USB connection icon.
        }

        /// <summary>
        /// Send a generic packet with no response
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packet"></param>
        /*private void SendCommandPacket<T>(T packet)
        {
            // Copying this format from the HidBootloader source code.
            using (var WriteDevice = HidDevice.GetWriteFile())
                WriteDevice.WriteStructure<T>(packet);
        }

        private T GetCommandPacket<T>()
        {
            using (var ReadDevice = HidDevice.GetReadFile())
                return ReadDevice.ReadStructure<T>();
        }*/

        /// <summary>
        /// Send a command and receive a response
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        private object SendCommandPacket(object packet, Type response_type = null)
        {
            try
            {
                // Convert structure to bytes
                byte[] bytes = new byte[COMMAND_PACKET_SIZE];
                GCHandle txhandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                try
                {
                    Marshal.StructureToPtr(packet, txhandle.AddrOfPinnedObject(), false);
                }
                finally
                {
                    txhandle.Free();
                }
                bytes[0] = 0; // WindowsReserved
                byte command = bytes[1];

                // Write
                using (var WriteDevice = HidDevice.GetWriteFile())
                    WriteDevice.Write(bytes, COMMAND_PACKET_SIZE);

                // NOTE: If debugging, make sure you don't step over these or the read will time out.

                // Read
                using (var ReadDevice = HidDevice.GetReadFile())
                    ReadDevice.Read(bytes, COMMAND_PACKET_SIZE);

                // Validate
                if (command != bytes[1])
                    throw new ZeitgeberException("Received invalid response to command");

                // Convert bytes to structure
                GCHandle rxhandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                try
                {
                    return Marshal.PtrToStructure(rxhandle.AddrOfPinnedObject(), (response_type != null) ? response_type : typeof(CommandStruct));
                }
                finally
                {
                    rxhandle.Free();
                }

                /*using (var WriteDevice = HidDevice.GetWriteFile())
                    WriteDevice.WriteStructure(packet, COMMAND_PACKET_SIZE);

                using (var ReadDevice = HidDevice.GetReadFile())
                {
                    if (response_type == null)
                        response_type = typeof(CommandStruct);

                    var response = ReadDevice.ReadStructure(response_type);
                
                    if (response.Command != packet.Command)
                    {
                        throw new ZeitgeberException("Received invalid response to command");
                    }

                    return response;
                }*/
            }
            catch (Exception)
            {
                isConnected = false;
                throw;
            }
        }

        #endregion

        #region Actions

        /// <summary>
        /// Ping the device to make sure it's still connected,
        /// and to tell it that we're still doing stuff.
        /// Call this every few seconds to tell the device we're still alive.
        /// </summary>
        public void Ping()
        {
            SendCommandPacket(new CommandStruct { Command = CMD_PING });
        }

        /// <summary>
        /// Reset the device
        /// </summary>
        public void Reset()
        {
            // Note that we don't wait for a response since the MCU will 
            // be reset immediately after calling this.
            using (var WriteDevice = HidDevice.GetWriteFile())
                WriteDevice.WriteStructure<CommandStruct>(new CommandStruct { Command = CMD_RESET });
        }

        public void SetLed(byte led, byte value)
        {
             SendCommandPacket(new SetLedStruct
             {
                 Command = CMD_SET_LED,
                 Led = led,
                 Value = value
             });
        }

        #endregion

        #region Time & Date

        public DateTime GetDateTime()
        {
            var packet = (DateTimePacket)SendCommandPacket(new CommandStruct { Command = CMD_GET_DATETIME }, typeof(DateTimePacket));
            return new DateTime(packet.Year + 2000, packet.Month, packet.Day, packet.Hour, packet.Minute, packet.Second);
        }

        public void SetDateTime(DateTime dt)
        {
            if (dt.Year < 2000)
                throw new Exception("Datetime year cannot be less than 2000");

            DateTimePacket packet = new DateTimePacket();
            packet.Command = CMD_SET_DATETIME;

            packet.Year = (byte)(dt.Year-2000);
            packet.Month = (byte)dt.Month;
            packet.Day = (byte)dt.Day;
            packet.DayOfWeek = (byte)dt.DayOfWeek; // 0:Sunday, 1:Monday, ...

            packet.Hour = (byte)dt.Hour;
            packet.Minute = (byte)dt.Minute;
            packet.Second = (byte)dt.Second;

            SendCommandPacket(packet);
        }

        /*public void UpdateDateTime(DateTime time)
        {
            throw new NotImplementedException();
        }

        public DateTime GetDateTime()
        {
            throw new NotImplementedException();
        }

        public void AddAppointment()
        {
            throw new NotImplementedException();
        }

        public void RemoveAppointment()
        {
            throw new NotImplementedException();
        }

        public void UpdateAppointment()
        {

        }

        public void GetAppointments()
        {
            // Return a list of appointments stored in the device
            throw new NotImplementedException();
        }*/

        #endregion

        #region System Info

        public BatteryInfo GetBatteryInfo()
        {
            return (BatteryInfo)SendCommandPacket(new CommandStruct { Command = CMD_GET_BATTERY_INFO }, typeof(BatteryInfo));
        }

        public CpuInfo GetCpuInfo()
        {
            return (CpuInfo)SendCommandPacket(new CommandStruct { Command = CMD_GET_CPU_INFO }, typeof(CpuInfo));
        }

        #endregion

        #region Display

        public DisplayQuery QueryDisplay()
        {
            return (DisplayQuery)SendCommandPacket(new CommandStruct { Command = CMD_QUERY_DISPLAY }, typeof(DisplayQuery));
        }

        /*public void DisableScreenUpdates()
        {
            throw new NotImplementedException();
        }

        public void EnableScreenUpdates()
        {
            throw new NotImplementedException();
        }

        public void DisplayImage(byte[] image_data)
        {
            // Display the given image bytes on the device's OLED screen.
            // You must de-activate the display first, to prevent the
            // device from updating the screen.
            throw new NotImplementedException();
        }

        public byte[] CaptureScreenImage()
        {
            // Capture whatever is currently in the screen buffer
            throw new NotImplementedException();
        }*/
        #endregion

        #region Sensors

        public SensorsQuery GetSensorsInfo()
        {
            return (SensorsQuery)SendCommandPacket(new CommandStruct { Command = CMD_QUERY_SENSORS }, typeof(SensorsQuery));
        }

        #endregion
    }
}
