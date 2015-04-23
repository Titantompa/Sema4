using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using Sema4.MailHandlers;

namespace Sema4
{
    public class LightsManager
    {
        private class ColorState
        {
            public ColorDuration ColorDuration { get; set; }
            public DateTime Expiration { get; set; }
        }

        private Queue<ColorState> _colorSequence = new Queue<ColorState>();

        private readonly Object _lock = new object();

        public LightsManager()
        {
            _colorTimer.Elapsed += ProcessColorSequence;
        }

        /// <summary>
        /// Ska anropas vid uppstart så att managern kan intialisera det som behövs innan
        /// <see cref="IMailHandler"/> instanserna börjar skicka in tillstånd.
        /// </summary>
        public void Initialize()
        {
#if __MonoCS__
            WiringPi.wiringPiSetup();

            WiringPi.softPwmCreate(25, 0, 75);
            WiringPi.softPwmCreate(23, 0, 75);
            WiringPi.softPwmCreate(24, 0, 75);

            // TODO: Egentligen borde det ju vara konfigurerbart vilka pinnarna är i Properties.Setting.Red etc
#endif

            // Stäng av allt ljus

            LightsOn(Color.Off);
        }

        /// <summary>
        /// Genväg för att sätta en ensam färg som ColorSequence som varar för evigt, typ
        /// </summary>
        /// <param name="color"></param>
        public void SetColor(Color color)
        {
            SetColorSequence(new List<ColorDuration> { new ColorDuration { Color = color, Duration = TimeSpan.FromDays(7) } });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="colorSequence"></param>
        public void SetColorSequence(List<ColorDuration> colorSequence)
        {
            if (colorSequence.Any(x => x.Duration <= TimeSpan.FromMilliseconds(100)))
                throw new ArgumentException("Duration of ColorDuration too short, minimum is 100ms");

            // Vi trådlåser här så att vi kan manipulera allt utan att behöva krocka med utgångstimern
            lock (_lock)
            {
                // Stoppa utgångstimern
                _colorTimer.Stop();

                _colorSequence = new Queue<ColorState>(colorSequence.Select(x =>
                    new ColorState
                    {
                        ColorDuration = x,
                        Expiration =
                            x.Duration ==
                            TimeSpan.MaxValue
                                ? DateTime.MaxValue
                                : DateTime.Now + x.Duration
                    }));

                // Städa listan över utgånga tillstånd och sätt upp timern på nytt
                ProcessColorSequence(this, null);
            }
        }

        readonly Timer _colorTimer = new Timer { AutoReset = false, Enabled = false, Interval = 1000000 };

        /// <summary>
        /// Anropas för att ta bort states som passerat sin duration. Metoden ställer också
        /// timern för nästa anrop.
        /// </summary>
        private void ProcessColorSequence(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            lock (_lock)
            {
                while (true)
                {
                    var colorState = _colorSequence.Peek();

                    if (colorState.Expiration <= DateTime.Now)
                    {
                        // Rotera kön
                        _colorSequence.Enqueue(_colorSequence.Dequeue());

                        colorState = _colorSequence.Peek();

                        colorState.Expiration = DateTime.Now +
                                                (colorState.ColorDuration.Duration == TimeSpan.MaxValue
                                                    ? TimeSpan.FromDays(7)
                                                    : colorState.ColorDuration.Duration);
                    }
                    else
                    {
                        try
                        {
                            // Set timer for next expiration
                            var nextTime = colorState.Expiration;
                            _colorTimer.Interval = (nextTime - DateTime.Now).TotalMilliseconds + 1;
                            _colorTimer.Start();

                            // LightsOn(colorState.ColorDuration.Color);
                            UpdateIndicators();

                            break;
                        }
                        catch (Exception)
                        {

                            throw;
                        }
                    }
                }
            }
        }

        private void UpdateIndicators()
        {
            var colorState = _colorSequence.Peek();

            LightsOn(colorState == null ? Color.Off : colorState.ColorDuration.Color);
        }

        private Color _currentColor = Color.Off;

        /// <summary>
        /// Slå på lamporna röd/grön/blå enligt parametern
        /// </summary>
        /// <param name="color"></param>
        private void LightsOn(Color color)
        {
            lock (_lock)
            {
                // TODO: I stället för att starta en massa processer vi kanske anropa lib:et direkt? Hur gör man det i MONO?

                //switch ((color.Red ? 1 : 0) << 2 | (color.Green ? 1 : 0) << 1 | (color.Blue ? 1 : 0))
                //{
                //    case 0:
                //        Console.ForegroundColor = ConsoleColor.White;
                //        break;
                //    case 1:
                //        Console.ForegroundColor = ConsoleColor.Blue;
                //        break;
                //    case 2:
                //        Console.ForegroundColor = ConsoleColor.Green;
                //        break;
                //    case 3:
                //        Console.ForegroundColor = ConsoleColor.Cyan;
                //        break;
                //    case 4:
                //        Console.ForegroundColor = ConsoleColor.Red;
                //        break;
                //    case 5:
                //        Console.ForegroundColor = ConsoleColor.Magenta;
                //        break;
                //    case 6:
                //        Console.ForegroundColor = ConsoleColor.Yellow;
                //        break;
                //    case 7:
                //        Console.ForegroundColor = ConsoleColor.White;
                //        break;
                //}

                Console.WriteLine("Setting lights to {0},{1},{2} ({3})", color.Red, color.Green, color.Blue, DateTime.Now);

                Console.ForegroundColor = ConsoleColor.White;

                // Kolla om lamporna redan lyser, om de gör det behöver vi inte slösa tid på att ändra dem.
                if (_currentColor.Red == color.Red && _currentColor.Green == color.Green && _currentColor.Blue == color.Blue)
                    return;

                // Lagra undan värdena som aktuella
                _currentColor = color;

#if __MonoCS__

                WiringPi.softPwmWrite(25, (_currentColor.Red>>4)*5);
                WiringPi.softPwmWrite(23, (_currentColor.Green>>4)*5);
                WiringPi.softPwmWrite(24, (_currentColor.Blue>>4)*5);

                //WiringPi.digitalWrite(25, _currentColor.Red ? 1 : 0);
                //WiringPi.digitalWrite(23, _currentColor.Green ? 1 : 0);
                //WiringPi.digitalWrite(24, _currentColor.Blue ? 1 : 0);

                Console.WriteLine("Set GPIO states using native library (wiringPi)");
#else
                //var info = new ProcessStartInfo
                //{
                //    FileName = @"C:\Windows\system32\notepad.exe",
                //    Arguments = "hejsan",
                //    UseShellExecute = false,
                //    CreateNoWindow = false,
                //    RedirectStandardOutput = true,
                //    RedirectStandardError = true
                //};

                ///* var p = */
                //Process.Start(info);

                //if (p != null)
                //    p.WaitForExit();
#endif
            }
        }
    }
}
