using System;
using System.Xml.Linq;
using AE.Net.Mail;

namespace Sema4.MailHandlers
{
    /// <summary>
    /// En handler för mejl som nollställer alla tillstånd.
    /// Om ett mejl innehåller en speciell guid i ärenderaden så nollställs alla tillstånd.
    /// </summary>
    public class ResetMailHandler : IMailHandler
    {
        private Guid _magicGuid = new Guid("88032ce1-7cb0-409b-9279-1ff0ada29f27");

        private readonly StateManager _stateManager;

        public ResetMailHandler(StateManager stateManager)
        {
            _stateManager = stateManager;
        }

        public void StartUp(XDocument configuration)
        {
            ; // Gör ingenting
        }

        public bool HandleMail(MailMessage message)
        {
            Console.WriteLine("ResetMailHandler examining message subject: {0}", message.Subject);

            if (message.Subject.Contains(_magicGuid.ToString()))
            {
                Console.WriteLine("The subject did contain the magic guid: {0}", _magicGuid);

                Console.WriteLine("Clearing all the states!", _magicGuid);
                
                _stateManager.Clear();

                return true; // Sluta leta efter andra som vill hantera detta
            }
            else
            {
                Console.WriteLine("The subject did not contain the magic guid: {0}", _magicGuid);
            }

            return false; // Fortsätt anropa fler mailhandlers
        }
    }
}
