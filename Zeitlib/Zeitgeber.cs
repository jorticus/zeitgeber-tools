using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ViscTronics;

namespace ViscTronics.Zeitlib
{
    #region Exceptions
    public class ZeitgeberException : Exception
    {
        public ZeitgeberException() { }
        public ZeitgeberException(string message) : base(message) { }
        public ZeitgeberException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ZeitgeberCommandException : Exception
    {
        // Error codes
        public const byte ERR_OK = 0x00;
        public const byte ERR_UNKNOWN = 0x01;
        public const byte ERR_OUT_OF_RAM = 0x10;
        public const byte ERR_NOT_IMPLEMENTED = 0x11;
        public const byte ERR_INVALID_INDEX = 0x12;
        public const byte ERR_INVALID_PARAM = 0x13;

        protected static string GetErrorMessage(int code)
        {
            switch (code)
            {
                case ERR_OK:
                    return "OK";
                case ERR_OUT_OF_RAM:
                    return "Out of RAM";
                case ERR_NOT_IMPLEMENTED:
                    return "Not implemented";
                case ERR_INVALID_INDEX:
                    return "Invalid index";
                case ERR_INVALID_PARAM:
                    return "Invalid parameter";
                default:
                    return String.Format("Unknown Error ({0})", code);
            }
        }
        protected static string FormatErrorMessage(string message, int code)
        {
            return message + " (" + GetErrorMessage(code) + ")";
        }

        public ZeitgeberCommandException(int code = ERR_UNKNOWN) : base(FormatErrorMessage("Command Exception", code)) { }
        public ZeitgeberCommandException(string message, int code = ERR_UNKNOWN) : base(FormatErrorMessage(message, code)) { }
        public ZeitgeberCommandException(string message, Exception innerException, int code = ERR_UNKNOWN) : base(FormatErrorMessage(message, code), innerException) { }
    }
    #endregion

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
        private const byte CMD_GET_NEXT_MESSAGE = 0x12;

        // Display interface
        private const byte CMD_QUERY_DISPLAY = 0x20;
        private const byte CMD_SET_DISPLAY_POWER = 0x21;
        private const byte CMD_DISPLAY_LOCK = 0x22;
        private const byte CMD_DISPLAY_UNLOCK = 0x23;
        private const byte CMD_DISPLAY_WRITEBUF = 0x24;
        private const byte CMD_DISPLAY_READBUF = 0x25;

        // Sensors
        private const byte CMD_QUERY_SENSORS = 0x25;
        private const byte CMD_SET_SENSOR_ENABLE = 0x31;
        private const byte CMD_GET_SENSOR_DATA = 0x32;

        // Time & Date
        private const byte CMD_GET_DATETIME = 0x40;
        private const byte CMD_SET_DATETIME = 0x41;

        // Calendar
        private const byte CMD_CLEAR_CALENDAR = 0x50;
        private const byte CMD_ADD_CALENDAR_EVT = 0x51;

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

        private const int DISPLAY_CHUNK_SIZE = 32;
        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = COMMAND_PACKET_SIZE)]
        public struct DisplayChunk
        {
            public byte WindowsReserved;
            public byte Command;

            public byte State;
            public UInt16 Offset;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = DISPLAY_CHUNK_SIZE)]
            public byte[] Buf;
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

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = COMMAND_PACKET_SIZE)]
        public struct DebugMessageQuery
        {
            public byte WindowsReserved;
            public byte Command;
            public byte Padding;

            public UInt16 Len;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
            public char[] Message;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = COMMAND_PACKET_SIZE, CharSet=CharSet.Ansi)]
        public struct CalendarEventPacket
        {
            public byte WindowsReserved;
            public byte Command;
            public byte Padding;

            public Int16 Index;
            public byte EventType;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public char[] Label;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public char[] Location;

            public UInt16 color;

            // Weekly calendar events
            public byte dow;
            public UInt16 hr;
            public UInt16 min;
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

                // Check if the device reported any errors
                if (bytes[2] != ZeitgeberCommandException.ERR_OK)
                    throw new ZeitgeberCommandException(bytes[2]);

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

        /// <summary>
        /// Returns the current DateTime of the watch, or null if not currently valid.
        /// </summary>
        public DateTime? GetDateTime()
        {
            var packet = (DateTimePacket)SendCommandPacket(new CommandStruct { Command = CMD_GET_DATETIME }, typeof(DateTimePacket));
            try
            {
                return new DateTime(packet.Year + 2000, packet.Month, packet.Day, packet.Hour, packet.Minute, packet.Second);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        public void SetDateTime(DateTime dt)
        {
            if (dt.Year < 2000)
                throw new ArgumentException("Datetime year cannot be less than 2000");

            DateTimePacket packet = new DateTimePacket();
            packet.Command = CMD_SET_DATETIME;

            packet.Year = (byte)(dt.Year-2000);
            packet.Month = (byte)dt.Month;
            packet.Day = (byte)dt.Day;

            packet.DayOfWeek = (byte)ConvertDOW(dt.DayOfWeek);

            packet.Hour = (byte)dt.Hour;
            packet.Minute = (byte)dt.Minute;
            packet.Second = (byte)dt.Second;

            SendCommandPacket(packet);
        }

        #endregion

        #region Calendar

        /// <summary>
        /// Clears the internal calendar table
        /// </summary>
        public void ClearCalendar()
        {
            SendCommandPacket(new CommandStruct() { Command = CMD_CLEAR_CALENDAR });
        }

        /// <summary>
        /// Add an event to the watch.
        /// May throw an exception if the watch cannot add any more events
        /// </summary>
        public void CalendarAddEvent(CalendarItem item)
        {
            CalendarEventPacket packet = new CalendarEventPacket();

            packet.Label = new char[20]; // TODO: Find a better way of doing this
            packet.Location = new char[20];
            CopyString(packet.Label, item.Label);
            CopyString(packet.Location, item.Location);

            packet.color = 0; // reserved for future use

            if (item is WeeklyTimetableItem) {
                WeeklyTimetableItem witem = (item as WeeklyTimetableItem);
                packet.EventType = 0;
                packet.dow = (byte)ConvertDOW(witem.DayOfWeek);
                packet.hr = (UInt16)witem.Time.Hour;
                packet.min = (UInt16)witem.Time.Minute;
                //TODO: length of time for the event??
            }

            packet.Command = CMD_ADD_CALENDAR_EVT;
            SendCommandPacket(packet);
        }

        /// <summary>
        /// Retrieve an event by the specified ID
        /// </summary>
        public void CalendarGetEvent(int id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieve a list of all events loaded onto the watch
        /// </summary>
        public void CalendarGetEvents()
        {
            throw new NotImplementedException();
        }


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

        /// <summary>
        /// Returns the latest debug message, or null if no debug messages available
        /// </summary>
        public string GetNextDebugMessage()
        {
            var query = (DebugMessageQuery)SendCommandPacket(new CommandStruct { Command = CMD_GET_NEXT_MESSAGE }, typeof(DebugMessageQuery));
            if (query.Len > 0)
            {
                return new String(query.Message.Take(query.Len).ToArray());
            }
            return null; // No more messages in the buffer
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

        private void DisplayLock()
        {
            SendCommandPacket(new CommandStruct { Command = CMD_DISPLAY_LOCK });
        }
        private void DisplayUnlock()
        {
            SendCommandPacket(new CommandStruct { Command = CMD_DISPLAY_UNLOCK });
        }

        private byte[] ReadDisplayChunk(UInt16 offset)
        {
            var chunk = (DisplayChunk)SendCommandPacket(
                new DisplayChunk { 
                    Command = CMD_DISPLAY_READBUF,
                    Offset = offset
                }, 
                typeof(DisplayChunk));

            return (chunk.State > 0) ? chunk.Buf : null;
        }

        public Image CaptureScreenImage()
        {
            DisplayQuery query = QueryDisplay();

            int stride = query.Width * query.BitsPerPixel / 8;
            int total_size = query.Width * query.Height * query.BitsPerPixel / 8;
            int chunks = total_size / DISPLAY_CHUNK_SIZE; // 32 bytes per chunk
            UInt16 offset = 0;

            // Lock the display
            DisplayLock();

            // Wait for the frame to be ready
            byte[] chunk;
            do
                chunk = ReadDisplayChunk(offset);
            while (chunk == null);

            // Read the complete frame buffer
            byte[] image_buffer = new byte[total_size];
            for (int i = 0; i < chunks; i++)
            {
                if (i > 0)
                    chunk = ReadDisplayChunk(offset);

                for (int j = 0; j < DISPLAY_CHUNK_SIZE; j++)
                    image_buffer[offset++] = chunk[j];
            }

            // Unlock the display
            DisplayUnlock();

            GCHandle image_handle = GCHandle.Alloc(image_buffer, GCHandleType.Pinned);
            try
            {
                Image img = new Bitmap(query.Width, query.Height, stride, System.Drawing.Imaging.PixelFormat.Format16bppRgb565, image_handle.AddrOfPinnedObject());
                return img;
            }
            finally
            {
                image_handle.Free();
            }
        }

        #endregion

        #region Sensors

        public SensorsQuery GetSensorsInfo()
        {
            return (SensorsQuery)SendCommandPacket(new CommandStruct { Command = CMD_QUERY_SENSORS }, typeof(SensorsQuery));
        }

        #endregion


        #region Util Methods

        private static int ConvertDOW(DayOfWeek dayOfWeek)
        {
            int dow = (int)dayOfWeek - 1;
            if (dow < 0) dow += 7;
            return (dow); // 0:Monday, 1:Tuesday, ...
        }

        private static void CopyString(char[] buf, string s)
        {
            int i;
            for (i = 0; i < Math.Min(buf.Length, s.Length); i++ )
            {
                buf[i] = s[i];
            }
            if (i < buf.Length)
            {
                buf[i] = '\0';
            }
        }

        #endregion

    }
}
