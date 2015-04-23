using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Timers;

namespace Sema4
{
    public class StateManager
    {
        // TODO: Det vore nuttigt om man kunde ha severitys som växlar mellan två färger

        /// <summary>
        /// Det här är en guid som används för tillståndet som skapas när alla tillstånd nollas genom <see cref="Clear"/>.
        /// </summary>
        private static readonly Guid OverrideGuid = new Guid("5e333599-1548-42d8-b3eb-0737e8b963d6");

        private readonly Dictionary<Guid, State> _currentStates = new Dictionary<Guid, State>();

        private readonly Dictionary<Guid, DateTime> _stateExpirations = new Dictionary<Guid, DateTime>();

        private readonly Object _lock = new object();

        private readonly String _resourceDirectory;

        private readonly LightsManager _lightsManager;

        public StateManager(LightsManager lightsManager, String resourceDirectory)
        {
            _resourceDirectory = resourceDirectory;
            _lightsManager = lightsManager;
            _expirationTimer.Elapsed += ProcessStateExpirations;
        }

        /// <summary>
        /// Ska anropas vid uppstart så att managern kan intialisera det som behövs innan
        /// <see cref="IMailHandler"/> instanserna börjar skicka in tillstånd.
        /// </summary>
        public void Initialize()
        {
            ScanAudioDirectory();
        }

        private readonly List<String> _audioFiles = new List<string>();

        /// <summary>
        /// Ett gränssnitt som bara är till för den <see cref="IMailHandler"/> som tar emot
        /// uppladdning av nya ljudfiler.
        /// </summary>
        public void ScanAudioDirectory()
        {
            if (!Directory.Exists(_resourceDirectory))
                return;

            var regex = new Regex(@"^.*/[0-9abcdef]{8}-[0-9abcdef]{4}-[0-9abcdef]{4}-[0-9abcdef]{4}-[0-9abcdef]{12}_(Error|Failure|Notice|Partial|Success|Override|Transient)\.wav$");

            _audioFiles.Clear();

            var files = Directory.EnumerateFiles(_resourceDirectory);

            _audioFiles.AddRange(files.Where(x => regex.IsMatch(x)));

            files.ToList().ForEach(x => Console.WriteLine("Listed audiofile {0}", x));

            _audioFiles.ForEach(x=> Console.WriteLine("Added audiofile {0}", x));
        }

        public void SetState(State state)
        {
            // TODO: Spela upp ljud vid tillståndsförändringar, borde kunna göras med ljudfil som ligger i en speciell mapp och heter <guid>_<severity>.wav

            // Vi trådlåser här så att vi kan manipulera allt utan att behöva krocka med utgångstimern
            lock (_lock)
            {
                // Stoppa utgångstimern
                _expirationTimer.Stop();

                // Ta bort utgångstiden för det gamla tillståndet
                _stateExpirations.Remove(state.Id);

                State oldState;
                if (_currentStates.TryGetValue(state.Id, out oldState))
                {
                    if (oldState.Priority != state.Priority)
                        PlayStateTransitionAudio(state);
                }
                else
                {
                    PlayStateTransitionAudio(state);
                }

                _currentStates[state.Id] = state;

                // Lägg till en utgångstid för tillståndet
                if (state.Duration == TimeSpan.MaxValue)
                    _stateExpirations.Add(state.Id, DateTime.MaxValue);
                else
                    _stateExpirations.Add(state.Id, DateTime.Now + state.Duration);

                // Städa listan över utgånga tillstånd och sätt upp timern på nytt
                ProcessStateExpirations(this, null);
            }

            UpdateIndicators();
        }

        /// <summary>
        /// Mohahahaha
        /// </summary>
        /// <param name="state"></param>
        private void PlayStateTransitionAudio(State state)
        {
            // TODO: Check if there is an audio file for the guid and severity, build a static dictionary
            // TODO: The user should be able to upload new files - how do we handle updating the dictionary?

            var filename = _resourceDirectory + "/" + state.SoundEffect;

            Console.WriteLine("Checking if we have audio file: {0}", filename);

            if (_audioFiles.Contains(filename))
            {
                Console.WriteLine("Playing audiofile {0}/{1}", _resourceDirectory, filename);

#if __MonoCS__
                var info = new ProcessStartInfo
                {
                    FileName = "/usr/bin/aplay",
                    Arguments = filename,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true
                };

                // Just run this and continue
                Process.Start(info);
#else
                var player = new System.Media.SoundPlayer(filename);

                player.Play();
#endif                
            }
        }

        readonly Timer _expirationTimer = new Timer { AutoReset = false, Enabled = false, Interval = 100000 };

        /// <summary>
        /// Anropas för att ta bort states som passerat sin duration. Metoden ställer också
        /// timern för nästa anrop.
        /// </summary>
        private void ProcessStateExpirations(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            lock (_lock)
            {
                while (true)
                {
                    var staleStates = _stateExpirations.Where(x => x.Value <= DateTime.Now).ToList();

                    if (!staleStates.Any())
                    {
                        if (_stateExpirations.Any())
                        {
                            var nextTime = _stateExpirations.Values.Min();
                            _expirationTimer.Interval = (nextTime - DateTime.Now).TotalMilliseconds;
                            _expirationTimer.Start();
                        }

                        break;
                    }

                    // Ta bort gamla tillstånd från både listan över aktuella tillstånd och till utgångslistan.
                    staleStates.ForEach(x =>
                    {
                        _currentStates.Remove(x.Key);
                        _stateExpirations.Remove(x.Key);
                    });
                }
            }

            UpdateIndicators();
        }

        /// <summary>
        /// Kollar igenom tillgängliga tillstånd efter någon som har en severity som innebär att en lampa ska tändas.
        /// </summary>
        private void UpdateIndicators()
        {
            var state =
                _currentStates.Values.Where(x => x.ColorSequence != null && x.ColorSequence.Count > 0)
                    .OrderByDescending(x => x.Priority)
                    .FirstOrDefault();

            if (state == null)
            {
                // Turn off the lights
                _lightsManager.SetColor(Color.Off);
            }
            else
            {
                // Set the color sequence to the selected one!
                _lightsManager.SetColorSequence(state.ColorSequence);
            }
        }

        /// <summary>
        /// Radera alla tillstånd.
        /// Skapar även ett nytt tillstånd som signalerar att tillstånden raderats.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _currentStates.Clear();
                _stateExpirations.Clear();

                SetState(new State
                {
                    Id = OverrideGuid,
                    Priority = Int32.MaxValue,
                    Duration = new TimeSpan(0, 1, 0)
                });
            }

            UpdateIndicators();
        }
    }
}
