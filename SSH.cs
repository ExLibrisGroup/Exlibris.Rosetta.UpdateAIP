using System;
using System.Collections;
using Tamir.SharpSsh;

namespace Exlibris.Rosetta.UpdateAIP
{
    /// <summary>
    /// Sample showing the use of the SSH file trasfer features of 
    /// SharpSSH such as SFTP and SCP
    /// </summary>
    public class SshFileTransfer
    {
        public void TransferFile(SshConnectionInfo input)
        {
            try
            {
                string proto = "scp";
                SshTransferProtocolBase sshCp;

                if (proto.Equals("scp"))
                    sshCp = new Scp(input.Host, input.User);
                else
                    sshCp = new Sftp(input.Host, input.User);

                if (input.Pass != null) sshCp.Password = input.Pass;
                //if (input.IdentityFile != null) sshCp.AddIdentityFile(input.IdentityFile);
                sshCp.OnTransferStart += new FileTransferEvent(sshCp_OnTransferStart);
                sshCp.OnTransferProgress += new FileTransferEvent(sshCp_OnTransferProgress);
                sshCp.OnTransferEnd += new FileTransferEvent(sshCp_OnTransferEnd);

                Console.Write("Connecting...");
                sshCp.Connect();
                Console.WriteLine("OK");

                if (input.Direction == TransferDirection.To)
                    sshCp.Put(input.LocalFilePath, input.RemoteFilePath);
                else
                    sshCp.Get(input.RemoteFilePath, input.LocalFilePath);


                Console.Write("Disconnecting...");
                sshCp.Close();
                Console.WriteLine("OK");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw e;
            }
        }


        static ConsoleProgressBar progressBar;

        private static void sshCp_OnTransferStart(string src, string dst, int transferredBytes, int totalBytes, string message)
        {
            Console.WriteLine();
            progressBar = new ConsoleProgressBar();
            progressBar.Update(transferredBytes, totalBytes, message);
        }

        private static void sshCp_OnTransferProgress(string src, string dst, int transferredBytes, int totalBytes, string message)
        {
            if (progressBar != null)
            {
                progressBar.Update(transferredBytes, totalBytes, message);
            }
        }

        private static void sshCp_OnTransferEnd(string src, string dst, int transferredBytes, int totalBytes, string message)
        {
            if (progressBar != null)
            {
                progressBar.Update(transferredBytes, totalBytes, message);
                progressBar = null;
            }
        }

    }

    public enum TransferDirection
    {
        To = 1,
        From = 2
    }
    
    public struct SshConnectionInfo
    {
        public string Host;
        public string User;
        public string Pass;
        public TransferDirection Direction;
        public string RemoteFilePath;
        public string LocalFilePath;
        /*
        public string IdentityFile
        {
            get
            {
                string idfile = String.Format(@"{0}\{1}@{2}.id", Environment.CurrentDirectory, User, Host);
                if (!System.IO.File.Exists(idfile))
                {
                    System.IO.FileStream fs = System.IO.File.Create(idfile);
                    fs.Close();
                }

                return idfile;
            }
        }
        */
    }
}
