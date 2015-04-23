using System.Runtime.InteropServices;

namespace Sema4
{
    class WiringPi
    {
        [DllImport("libwiringPi.so", BestFitMapping = true, EntryPoint = "wiringPiSetup", ExactSpelling = true,
               CallingConvention = CallingConvention.Cdecl)]
        internal static extern void wiringPiSetup();

        [DllImport("libwiringPi.so", BestFitMapping = true, EntryPoint = "pinMode", ExactSpelling = true,
           CallingConvention = CallingConvention.Cdecl)]
        internal static extern void pinMode(int pin, int mode);

        [DllImport("libwiringPi.so", BestFitMapping = true, EntryPoint = "digitalWrite", ExactSpelling = true,
           CallingConvention = CallingConvention.Cdecl)]
        internal static extern void digitalWrite(int pin, int value);

        [DllImport("libwiringPi.so", BestFitMapping = true, EntryPoint = "softPwmCreate", ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void softPwmCreate(int pin, int initialValue, int range);

        [DllImport("libwiringPi.so", BestFitMapping = true, EntryPoint = "softPwmWrite", ExactSpelling = true,
            CallingConvention = CallingConvention.Cdecl)]
        internal static extern void softPwmWrite(int pin, int value);
    }
}
