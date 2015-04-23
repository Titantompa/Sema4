using System;
using System.Collections.Generic;

namespace Sema4
{
    public class State
    {
        /// <summary>
        /// Unik identifierare f�r tillst�ndet. Alla f�r�ndringar av ett och samma tillst�nd ska
        /// anv�nda samma Guid och �verlagra tidigare .
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Vilken priority tillst�ndet har, relativt andra tillst�nd
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Hur l�nge tillst�ndet ska existera. Om det inte har uppdaterats med ett nytt
        /// v�rde innan tiden l�per ut raderas det (och nollst�lls d�rmed).
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Den sekvens med f�rger som ska presenteras n�r tillst�ndet �r aktivt.
        /// Listan repeteras fr�n b�rjan efter att alla element har g�tts igenom.
        /// Om det bara �r ett element i listan s� tas Duration f�r att vara
        /// o�ndligt.
        /// </summary>
        public List<ColorDuration> ColorSequence { get; set; }

        /// <summary>
        /// The name of a WAV file stored in the resource directory.
        /// </summary>
        public String SoundEffect { get; set; }
    }
}