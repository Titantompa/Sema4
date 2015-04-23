using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AE.Net.Mail;

namespace Sema4.MailHandlers
{
    public class RegexMailHandler : IMailHandler
    {
        private class MatchInstruction
        {
            public Regex Expression;
            public Guid StateId;
            public int Priority;
            public TimeSpan Duration;
            public List<ColorDuration> ColorSequence;
            public String SoundEffect;
        }

        private readonly StateManager _stateManager;

        private readonly List<MatchInstruction> _matchInstructions = new List<MatchInstruction>();

        public RegexMailHandler(StateManager stateManager)
        {
            _stateManager = stateManager;
        }

        public void StartUp(XDocument configuration)
        {
            var matchInstructions = configuration.Element("configuration").Elements("handlers").Elements("regexmatcher").Elements("match");

            foreach (var match in matchInstructions)
            {
                var colorSequence = match.Element("colorsequence");
                var colors = new List<ColorDuration>();

                if (colorSequence != null)
                {
                    colors = colorSequence.Elements("color")
                        .Select(
                            x =>
                            {
                                var red = Byte.Parse(x.Attribute("red").Value);
                                var green = Byte.Parse(x.Attribute("green").Value);
                                var blue = Byte.Parse(x.Attribute("blue").Value);
                                return new ColorDuration
                                {
                                    Color = new Color {Red = red, Green = green, Blue = blue},
                                    Duration = TimeSpan.FromMilliseconds(Int32.Parse(x.Attribute("duration").Value))
                                };
                            }).ToList();
                }

                var soundEffect = match.Element("soundeffect");

                var matchInstruction = new MatchInstruction
                {
                    ColorSequence = colorSequence == null ? null : colors,
                    Duration = TimeSpan.FromMilliseconds(Int32.Parse(match.Attribute("duration").Value)),
                    Expression = new Regex(match.Attribute("regex").Value),
                    Priority = Int32.Parse(match.Attribute("priority").Value),
                    SoundEffect = soundEffect == null ? String.Empty : soundEffect.Attribute("filename").Value,
                    StateId = Guid.Parse(match.Attribute("state").Value)
                    
                };

                _matchInstructions.Add(matchInstruction);
            }


            // TODO: Ladda tillbaka det rådande tillståndet och kicka igång lightsManagern
        }

        /// <summary>
        /// Skicka ett tillstånd till den globala <see cref="StateManager"/> instansen.
        /// </summary>
        /// <param name="match"></param>
        private void SubmitState(MatchInstruction match)
        {
            var state = new State()
            {
                ColorSequence = match.ColorSequence,
                Duration = match.Duration,
                Id = match.StateId,
                Priority = match.Priority,
                SoundEffect = match.SoundEffect
            };

            _stateManager.SetState(state);
        }

        // Exempel på ärenderader (Subject)
        //
        // Mimer Build Mimer - Nightly Extended_20141210.1 succeeded
        //
        // Leaps Build Leaps - Nightly build and test_20141209.3 partially succeeded
        //
        // Mimer Build Mimer - Nightly_20141210.1 succeeded
        //
        // Leaps Build Leaps - Nightly build and test_20141210.1 succeeded
        //

        public bool HandleMail(MailMessage message)
        {
            Console.WriteLine("BuildMailHandler examining message subject: {0}", message.Subject);

            foreach (var match in _matchInstructions)
            {
                if (match.Expression.IsMatch(message.Subject))
                {
                    Console.WriteLine("DID match regex '{0}'", match.Expression);

                    SubmitState(match);
                }
                else
                {
                    Console.WriteLine("Did NOT match regex '{0}'", match.Expression);
                }
            }

            return false; // Fortsätt anropa fler mailhandlers
        }
    }
}
