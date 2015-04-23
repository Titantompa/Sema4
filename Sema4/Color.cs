namespace Sema4
{
    public class Color
    {
        public static Color Off = new Color { Red = 0, Green = 0, Blue = 0 };
        public static Color Error = new Color { Red = 255, Green = 0, Blue = 255 };
        public static Color White = new Color { Red = 255, Green = 255, Blue = 255 };

        public byte Red { get; set; }
        public byte Green { get; set; }
        public byte Blue { get; set; }
    }
}