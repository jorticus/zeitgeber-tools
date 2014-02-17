using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViscTronics.Zeitlib;

namespace ViscTronics.ZeitCtrl
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Zeitgeber zeitgeber = new Zeitgeber();
                
                // Simple program for resetting the device
                /*Console.WriteLine("Resetting Zeitgeber Unit");
                zeitgeber.Connect();
                zeitgeber.Reset();

                System.Threading.Thread.Sleep(1000);*/

                zeitgeber.InstallDriverInfo();
                //return;

                // Other system tests
                Console.WriteLine("Connecting...");
                zeitgeber.Connect();

                Console.Write("Connected to ");
                Console.WriteLine(zeitgeber.DeviceDescription);

                 Console.WriteLine("Ping()");
                zeitgeber.Ping();

                //zeitgeber.SetLed(2, 1);

                Console.WriteLine("GetBatteryInfo()");
                var battery = zeitgeber.GetBatteryInfo();
                Console.WriteLine(String.Format("\tlevel: {0}%", battery.Level));
                Console.WriteLine(String.Format("\tvoltage: {0}mV", battery.Voltage));
                Console.WriteLine(String.Format("\tpower status: {0}", battery.PowerStatus));
                Console.WriteLine();

                Console.WriteLine("GetCpuInfo()");
                var cpu = zeitgeber.GetCpuInfo();
                Console.WriteLine(String.Format("\tsystick: {0}", cpu.systick));
                Console.WriteLine();

                Console.WriteLine("QueryDisplay()");
                var display = zeitgeber.QueryDisplay();
                Console.WriteLine(String.Format(
                    "\tsize: {0}x{1}x{2}bpp ({3} bytes)", 
                    display.Width, display.Height, display.BitsPerPixel, 
                    display.Width*display.Height*(display.BitsPerPixel/8)
                ));
                Console.WriteLine();

                zeitgeber.SetDateTime(DateTime.Now);

                Console.WriteLine("GetDateTime()");
                var dt = zeitgeber.GetDateTime();
                Console.WriteLine(dt.ToString());
                Console.WriteLine("Actual:");
                Console.WriteLine(DateTime.Now.ToString());
                Console.WriteLine();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine();
#if DEBUG
                Console.Error.WriteLine(e);
#else
                Console.Error.WriteLine("Error: " + e.Message);
#endif
                Console.Error.WriteLine();
            }
            finally
            {
                Console.WriteLine("Done.");
#if DEBUG
                // Pause the console so we can see the output
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
#endif
            }
        }
    }
}
