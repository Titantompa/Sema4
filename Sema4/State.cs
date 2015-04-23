using System;
using System.Collections.Generic;

namespace Sema4
{
    public class State
    {
        /// <summary>
        /// Unik identifierare för tillståndet. Alla förändringar av ett och samma tillstånd ska
        /// använda samma Guid och överlagra tidigare .
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Vilken priority tillståndet har, relativt andra tillstånd
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Hur länge tillståndet ska existera. Om det inte har uppdaterats med ett nytt
        /// värde innan tiden löper ut raderas det (och nollställs därmed).
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Den sekvens med färger som ska presenteras när tillståndet är aktivt.
        /// Listan repeteras från början efter att alla element har gåtts igenom.
        /// Om det bara är ett element i listan så tas Duration för att vara
        /// oändligt.
        /// </summary>
        public List<ColorDuration> ColorSequence { get; set; }

        /// <summary>
        /// The name of a WAV file stored in the resource directory.
        /// </summary>
        public String SoundEffect { get; set; }
    }
}