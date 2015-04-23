using System.Xml.Linq;
using AE.Net.Mail;

namespace Sema4.MailHandlers
{
    public interface IMailHandler
    {
        /// <summary>
        /// Anropas av ramverket när demonen startar så att handlen kan återställa ett tillstånd efter t ex omboot
        /// </summary>
        void StartUp(XDocument configuration);

        /// <summary>
        /// Anropas för varje hämtat mejl. Ramverket fortsätter att ropa på denna metod på alla registrerade
        /// IMailHandler implementationer tills dess att någon av dem returnerar sant.
        /// </summary>
        /// <param name="message"></param>
        /// <returns>Sant om inga fler MailHandlers ska tillfrågas.</returns>
        bool HandleMail(MailMessage message);
    }
}
