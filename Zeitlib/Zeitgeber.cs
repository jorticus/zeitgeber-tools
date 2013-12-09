using System;
using System.Collections.Generic;
using System.Linq;
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
        private const string DeviceId = "VID_04D8&PID_003F";

        public bool Connected { get; private set; }
        private HidDevice HidDevice;

        public Zeitgeber()
        {
            HidDevice = new HidDevice(DeviceId);
        }

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

            // Note there is no Disconnect, as the connection is stateless.
            // Read/Write files are created as needed for communication.
        }


        #region Communication
        public void SendCommand()
        {

        }
        #endregion


        #region Commands
        public void Reset()
        {

        }
        #endregion
    }
}
