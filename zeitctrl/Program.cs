using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NDesk.Options;
using ViscTronics.Zeitlib;

namespace ViscTronics.ZeitCtrl
{
    class Program
    {
        private static Args args;
        private static OptionSet opts;
        private static Zeitgeber zeitgeber;

        public enum CommandLineAction { Unspecified, Ping, Reset, Query, SyncTime };

        struct Args
        {
            public bool showhelp;
            public bool verbose;
            public CommandLineAction action;
        }

        static void Main(string[] argv)
        {
            try
            {
                args = new Args();
                opts = new OptionSet() {
                    { "h|help", v => args.showhelp = (v != null) },
                    { "v|verbose", v => args.verbose = (v != null) },
                };

                List<string> subargs = opts.Parse(argv);

                if (args.showhelp) // --help
                {
                    ShowHelp();   
                    return;
                }

                // Un-named parameters
                if (subargs.Count < 1)
                {
                    Console.WriteLine("No action specified");
                    Console.WriteLine("");
                    ShowHelp();
                    return;
                }
                string action_str = subargs[0];
                try
                {
                    args.action = (CommandLineAction)Enum.Parse(typeof(CommandLineAction), action_str, true);
                }
                catch (System.ArgumentException e)
                {
                    throw new ZeitgeberException(String.Format("Invalid action '{0}'", action_str), e);
                }
         


                zeitgeber = new Zeitgeber();
                zeitgeber.Connect();

                if (args.verbose)
                    Console.WriteLine(String.Format("Connected to '{0}'", zeitgeber.DeviceDescription));

                switch (args.action)
                {
                    case CommandLineAction.Ping:
                        PingDevice();
                        break;

                    case CommandLineAction.Reset:
                        ResetDevice();
                        break;

                    case CommandLineAction.Query:
                        QueryDevice();
                        break;

                    case CommandLineAction.SyncTime:
                        SyncTime();
                        break;

                    default:
                        throw new NotImplementedException("The action is not yet implemented");

                }

            }
            catch (Exception e)
            {
                //Console.Error.WriteLine();
#if DEBUG
                Console.Error.WriteLine(e);
#else
                Console.Error.WriteLine("Error: " + e.Message);
#endif
                //Console.Error.WriteLine();
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

        private static void ShowHelp()
        {
            Console.WriteLine("Zeitctrl v1.0");
            Console.WriteLine("Author: Jared Sanson");
            Console.WriteLine();

            Console.WriteLine("Usage: zeitctrl action [OPTIONS]");
            Console.WriteLine("Where action can be: ping, reset");
            Console.WriteLine();

            Console.WriteLine("Options:");
            opts.WriteOptionDescriptions(Console.Out);
            Console.WriteLine();

            Console.WriteLine("Actions:");
            Console.WriteLine("        ping                 Tests that we can communicate with the watch");
            Console.WriteLine("        reset                Resets the device (into bootloader mode)");
            Console.WriteLine("        query                Query the watch for information");
            Console.WriteLine("        synctime             Update the watch's time and date to the computer's time");
        }

        /// <summary>
        /// Pings the device to make sure it's alive and responding
        /// </summary>
        private static void PingDevice()
        {
            if (args.verbose)
                Console.WriteLine("Pinging device...");

            zeitgeber.Ping();

            if (args.verbose)
                Console.WriteLine("Ping Success");
        }

        /// <summary>
        /// Resets the device, will normally reboot into bootloader mode
        /// </summary>
        private static void ResetDevice()
        {
            if (args.verbose)
                Console.WriteLine("Resetting device...");

            zeitgeber.Reset();
        }


        private static void QueryDevice()
        {
            // Simple program for resetting the device
            /*

            //zeitgeber.InstallDriverInfo();
            //return;

            // Other system tests
            /*Console.WriteLine("Connecting...");
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
            Console.WriteLine();*/
        }

        /// <summary>
        /// Update the watch's date and time to the computer's clock.
        /// </summary>
        private static void SyncTime()
        {
            if (args.verbose)
                Console.WriteLine("Syncing watch time to PC...");

            DateTime new_dt = DateTime.Now;
            DateTime old_dt = DateTime.Now;

            DateTime? curr_dt = zeitgeber.GetDateTime();
            if (curr_dt.HasValue)
                old_dt = curr_dt.Value;
            else
            {
                if (args.verbose)
                    Console.WriteLine("DateTime not yet set");
            }

            zeitgeber.SetDateTime(new_dt);

            
            TimeSpan diff = new_dt - old_dt;
            int seconds = (int)Math.Round(diff.TotalSeconds);

            if (seconds == 0)
            {
                Console.WriteLine("Watch time matches current time");
            }
            else
            {
                TimeSpan ts = new TimeSpan(0, 0, seconds);
                Console.WriteLine("Updated time to {0}");

                if (args.verbose)
                    Console.WriteLine("Difference: {1}", new_dt.ToString(), ts.ToString());
            }
        }
    }
}
