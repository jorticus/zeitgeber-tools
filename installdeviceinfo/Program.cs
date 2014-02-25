using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using ViscTronics.Zeitlib;

namespace installdeviceinfo
{
    class Program
    {
        /// <summary>
        /// Installs friendly information for any currently known drivers.
        /// Note: Windows installs a separate driver for each USB port you plug a device into for some bizzare reason,
        /// and this will only install info for currently known drivers.
        /// </summary>
        static void Main(string[] args)
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);

            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
                throw new ZeitgeberException("Installing device information requires administrator privileges");

            Console.WriteLine("Installing device information for all currently known devices...");

            Zeitgeber zeitgeber = new Zeitgeber();
            zeitgeber.InstallDriverInfo();

            Console.WriteLine("Done.");
        }
    }
}
