using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using log4net;
using Exlibris.Rosetta.UpdateAIP.Properties;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace Exlibris.Rosetta.UpdateAIP
{
    class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {

            Action action = Action.Replace;
            string iepid="";
            string flpid="";
            string reppid="";
            string filename="";
            
            // Parse arguments
            if (args.Length > 0)
            {
                for (int n = 0; n < args.Length; n++)
                {
                    if (args[n].Substring(0, 1) == "-")
                    {
                        // action
                        switch (args[n].ToLower().Substring(1))
                        {
                            case "replace":
                            case "r":
                                action = Action.Replace;
                                break;
                            case "delete":
                            case "d":
                                action = Action.Delete;
                                break;
                            case "add":
                            case "a":
                                action = Action.Add;
                                break;
                            case "user":
                            case "u":
                                n++;
                                Settings.Default.user = args[n];
                                continue;
                            case "pass":
                            case "p":
                                n++;
                                Settings.Default.pass = Util.EncryptString(Util.ToSecureString(args[n]));
                                continue;
                            case "host":
                            case "h":
                                n++;
                                Settings.Default.host = args[n];
                                continue;
                            case "appuser":
                            case "au":
                                n++;
                                Settings.Default.appUser = args[n];
                                continue;
                            case "apppass":
                            case "ap":
                                n++;
                                Settings.Default.appPassword = Util.EncryptString(Util.ToSecureString(args[n]));
                                continue;
                            case "port":
                                n++;
                                Settings.Default.port = args[n];
                                continue;
                            case "remotedir":
                                n++;
                                Settings.Default.remotedir = args[n];
                                continue;
                            case "inst":
                                n++;
                                Settings.Default.institution = args[n];
                                continue;
                            default:
                                DisplayError("Invalid action.");
                                break;
                        }
                        continue;
                    }

                    if (args[n].Substring(0, 2) == "IE") { iepid = args[n]; continue; }

                    if (args[n].Substring(0, 2) == "FL") { flpid = args[n]; continue; }

                    if (args[n].Substring(0, 3) == "REP") { reppid = args[n]; continue; }

                    filename = args[n];
                }

                Settings.Default.Save();
                log.Info("Settings saved.");
            }

            // Validate we have enough data to continue
            // For replace, we need a file at a minimum
            if (action == Action.Replace && filename == "")
                DisplayUsage();

            // For add, we need the rep and the file
            if (action == Action.Add && (reppid == "" || filename ==""))
                DisplayUsage();

            // For delete, we need the filename or the file PID
            if (action == Action.Delete && (filename == "" && flpid == ""))
                DisplayUsage();

            RosettaSettings rs = new RosettaSettings()
            {
                ApplicationUser = Settings.Default.appUser,
                ApplicationPassword = Util.ToInsecureString(Util.DecryptString(Settings.Default.appPassword)),
                ServerUser = Settings.Default.user,
                ServerPassword = Util.ToInsecureString(Util.DecryptString(Settings.Default.pass)),
                Host = Settings.Default.host,
                Institution = Settings.Default.institution,
                RemoteDirectory = Settings.Default.remotedir,
                Port = Settings.Default.port
            };

            UpdateAIP updateAip = new UpdateAIP(rs);

            try
            {
                if (action == Action.Replace)
                    updateAip.UpdateFile(filename, iepid, reppid, flpid);
                else if (action == Action.Delete)
                    updateAip.DeleteFile(filename, iepid, flpid);
                else if (action == Action.Add)
                    updateAip.AddFile(filename, reppid);
            }
            catch (Exception ex)
            {
                DisplayError(ex.Message);
            }

        }

        static void DisplayError(string errorMessage)
        {
            log.Error(errorMessage);
            Environment.Exit(-1);
        }

        static void DisplayUsage()
        {
            Console.WriteLine("");
            Console.WriteLine("Update AIP can be used to replace or delete a file in an existing AIP stored in Rosetta. It uses Rosetta APIs to create a new revision of the representation which contains the file.");
            Console.WriteLine("UpdateAIP [-replace|-delete] [IEPID] [FLPID] [config options] file");
            Console.WriteLine("");
            Console.WriteLine("Parameters:");
            Console.WriteLine("\taction:     replace (default) or delete");
            Console.WriteLine("\tPID:        PID of IE (IEXXXX) and/or file (FLXXXX) (optional)");
            Console.WriteLine("\tfile:       Path to the local file to replace. Needs to be the same name as the file to be replaced/deleted.");
            Console.WriteLine("");
            Console.WriteLine("Configuration Parameters (saved between sessions):");
            Console.WriteLine("\tuser|u:     Server username for scp");
            Console.WriteLine("\tpass|p:     Server password for scp");
            Console.WriteLine("\thost|h:     Server host");
            Console.WriteLine("\tport:       Server application port (default 1801)");
            Console.WriteLine("\tappuser|au: Application username for web services");
            Console.WriteLine("\tapppass|ap: Application password for web services");
            Console.WriteLine("\tremotedir:  Directory on remote server to place file (default /tmp/updateaip)");
            Console.WriteLine("\tinst:       Application institution for web services (default INS00)");
            Environment.Exit(0);
        }

        enum Action
        {
            Replace=1,
            Delete=2,
            Add=3
        }
    }
}
