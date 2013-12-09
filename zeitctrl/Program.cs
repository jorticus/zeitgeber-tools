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


                Console.WriteLine("Connecting...");
                zeitgeber.Connect();

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
