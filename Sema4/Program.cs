using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

// http://stackoverflow.com/questions/670183/accessing-imap-in-c-sharp
using System.Xml.Linq;
using AE.Net.Mail;
using Sema4.MailHandlers;

namespace Sema4
{
    internal class Program
    {
        // TODO: Configurable names of mailboxes
        private static readonly List<String> Mailboxes = new List<string> {"INBOX", "Spam"};

        private static readonly List<IMailHandler> MailHandlers = new List<IMailHandler>();

        private static StateManager _stateManager;

        private static void Main(string[] args)
        {
            var lightsManager = new LightsManager();

            lightsManager.Initialize();

            // Read configuration file

#if __MonoCS__
            var config = XDocument.Load(Properties.Settings.Default.ResourcePath + "/Sema4.xml");
#else
            var config = XDocument.Load(@".\Sema4.xml");
#endif

            _stateManager = new StateManager(lightsManager, Properties.Settings.Default.ResourcePath);

            _stateManager.Initialize();

            MailHandlers.Add(new RegexMailHandler(_stateManager));
            MailHandlers.Add(new ResetMailHandler(_stateManager));
            MailHandlers.Add(new FileUploadMailHandler(Properties.Settings.Default.ResourcePath));

            foreach (var mailHandler in MailHandlers)
                mailHandler.StartUp(config);

            var bgWorker = new Thread(CheckMail) {IsBackground = true, Priority = ThreadPriority.AboveNormal};

            bgWorker.Start();

            bgWorker.Join();

            // Connect to the IMAP server. The 'true' parameter specifies to use SSL
            // which is important (for Gmail at least)
        }

        private static void CheckMail(object state)
        {
            while (true)
            {
                try
                {
                    using (
                        var ic = new ImapClient("imap.aim.com", "lighten_up@aim.com", "B4ckY4rd!", AuthMethods.Login, 993, true))
                        //var ic = new ImapClient("imap.aim.com", "indi_kator@aim.com", "1nd!k4t0r", AuthMethods.Login, 993, true))
                    {

                        foreach (var mailbox in Mailboxes)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("Checking if there are new messages at imap.aim.com in Mailbox '{0}'...", mailbox);
                            Console.ForegroundColor = ConsoleColor.White;

                            // Select a mailbox. Case-insensitive
                            var mbox = ic.SelectMailbox(mailbox);

                            // AOL doesn't show if mail is unread or unseen :-(

                            var messageCount = mbox.NumMsg;
                            if (messageCount > 0)
                            {
                                // Messages in inbox
                                Console.WriteLine("MessageCount = {0}", ic.GetMessageCount());

                                var messages = ic.GetMessages(0, messageCount - 1, false, true);

                                foreach (var message in messages.OrderBy(x=>x.Date))
                                {
                                    Console.WriteLine(
                                        "Letting handlers have a go at message {0}, received at {1}, subject: {2}",
                                        message.MessageID, message.Date, message.Subject);

                                    foreach (var mailHandler in MailHandlers)
                                    {
                                        if (mailHandler.HandleMail(message))
                                            break;
                                    }

                                    Console.WriteLine("Deleting message {0}", message.MessageID);
                                    ic.DeleteMessage(message);
                                }
                            }
                            else
                            {
                                Console.WriteLine("There appeared to be no new messages");
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    var oldColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;

                    Console.WriteLine(e);

                    Console.ForegroundColor = oldColor;

                    _stateManager.SetState(new State
                    {
                        ColorSequence = new List<ColorDuration> { new ColorDuration { Color = Color.Error, Duration = new TimeSpan(24, 0, 0) } },
                        Duration = new TimeSpan(0, 0, 0, 25),
                        Id = new Guid("{0a55c9af-23c5-41fe-9ccf-be925acf6b9c}"),
                        Priority = Int32.MaxValue,
                        SoundEffect = String.Empty
                    });
                }

                // Force a garbage collect. This is supposed to run as a demon and we want it to have as small footprint as possible
                GC.Collect(3, GCCollectionMode.Forced);

                // TODO: Configurable interval!
                Thread.Sleep(30000);
            }
        }
    }
}
