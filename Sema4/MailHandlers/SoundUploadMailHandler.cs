using System;
using System.Xml.Linq;
using AE.Net.Mail;

namespace Sema4.MailHandlers
{
    // TODO: Det kan nog behövas lite felhantering här...

    class FileUploadMailHandler : IMailHandler
    {
        private static Guid _magicGuid = new Guid("fc0839b8-d5ca-4cd3-b427-314aa05572e3");

        private readonly String _resourceDirectory;

        public FileUploadMailHandler(string resourceDirectory)
        {
            _resourceDirectory = resourceDirectory;
        }

        public void StartUp(XDocument configuration)
        {
            ; // Gör ingenting
        }

        public bool HandleMail(MailMessage message)
        {
            // Struna i meddelanden som inte har någon attachment
            if (message.Attachments.Count == 0)
                return false;

            Console.WriteLine("Received a message with an attachment");

            // Kolla magiska guiden i ärenderaden
            if (message.Subject.Contains(_magicGuid.ToString()))
            {
                Console.WriteLine("The subject did contain the magic guid: {0}", _magicGuid);

                foreach (var attachment in message.Attachments)
                {
                    Console.WriteLine("Examining attachment '{0}'", attachment.Filename);

                    // Ladda bara ned wav-filer, än så länge
                    if (attachment.Filename.EndsWith("wav"))
                    {
                        Console.WriteLine("Saving attachment to: {0}", _resourceDirectory + "/" + attachment.Filename);

                        attachment.Save(_resourceDirectory + "/" + attachment.Filename);
                    }
                }

                return true; // Sluta leta efter andra som vill hantera detta
            }
            else
            {
                Console.WriteLine("The subject did NOT contain the magic guid: {0}", _magicGuid);                
            }

            return false; // Fortsätt anropa fler mailhandlers
        }
    }
}
