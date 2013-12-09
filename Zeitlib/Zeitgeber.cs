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

    public class Zeitgeber
    {
        //private const string DeviceId = "VID_04D8&PID_003F";  // Zeitgeber ID
        private const string DeviceId = "Vid_04d8&Pid_003c";  // Bootloader ID

        private HidDevice HidDevice;

        #region Command Constants

        private const byte CMD_PING = 0x02;
        private const byte CMD_RESET = 0x02;

        #endregion

        #region Structs

        private const int COMMAND_PACKET_SIZE = 65;

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = COMMAND_PACKET_SIZE)]
        public struct CommandStruct
        {
            public byte WindowsReserved;
            public byte Command;
        }

        #endregion


        public Zeitgeber()
        {
            HidDevice = new HidDevice(DeviceId);
        }

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
            }
            catch (HidDeviceException e)
            {
                throw new ZeitgeberException("Could not connect to the device", e);
            }

            // The PIC must be pinged every few seconds for it to show the USB connection icon.
        }

        /// <summary>
        /// Send a generic packet with no response
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="packet"></param>
        private void SendCommandPacket<T>(T packet)
        {
            // Copying this format from the HidBootloader source code.
            using (var WriteDevice = HidDevice.GetWriteFile())
                WriteDevice.WriteStructure<T>(packet);
        }

        private T GetCommandPacket<T>()
        {
            using (var ReadDevice = HidDevice.GetReadFile())
                return ReadDevice.ReadStructure<T>();
        }

        private void SendCommand(byte command)
        {
            SendCommandPacket<CommandStruct>(new CommandStruct {
                WindowsReserved = 0,
                Command = command
            });
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
            SendCommandPacket<CommandStruct>(new CommandStruct
            {
                WindowsReserved = 0,
                Command = CMD_PING
            });

            var packet = GetCommandPacket<CommandStruct>();
            if (packet.Command != CMD_PING)
                throw new ZeitgeberException("Invalid response to CMD_PING");
        }

        /// <summary>
        /// Reset the device
        /// </summary>
        public void Reset()
        {
            SendCommand(CMD_RESET);
        }

        #endregion

        #region Clock and Calendar

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

        #region Diagnostics

        /*public void GetDiagnosticInfo()
        {
            // Return a struct containing diagnostic info
            throw new NotImplementedException();
        }*/

        #endregion

        #region Display

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
    }
}
