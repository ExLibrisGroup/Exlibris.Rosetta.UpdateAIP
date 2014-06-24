using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using System.Net;
using System.IO;
using Exlibris.Rosetta.UpdateAIP.Properties;

namespace Exlibris.Rosetta.UpdateAIP
{
    class UpdateAIP
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        RosettaSettings _rs;
        RosettaRepository _ros;
        string _inst;

        public UpdateAIP(RosettaSettings rs)
        {
            _rs = rs;
            _inst = String.IsNullOrEmpty(_rs.Institution) ? "INS00" : rs.Institution;
            _ros = new RosettaRepository(_rs.Host, _rs.Port);
            _ros.Login(_inst, _rs.ApplicationUser, _rs.ApplicationPassword);
            log.Info(String.Format("Logged in user {0} and received PDS Handle {1}.", _rs.ApplicationUser, _ros.PdsHandle));
        }

        public void DeleteFile(string filePath, string iepid, string flpid)
        {
            throw new NotImplementedException();
        }

        public void AddFile(string filePath, string repPid)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Replaces an existing file in a Rosetta IP Representation with the provided file
        /// </summary>
        /// <param name="filePath">Local path to the file to replace</param>
        /// <param name="iepid">IE PID of the existing file. If not provided, an SRU search will be performed to find the relevant IE</param>
        /// <param name="flpid">File PID of the existing file. If not provided, the filename will be searched in the relevant IE</param>
        public void UpdateFile(string filePath, string iepid, string reppid, string flpid)
        {

            string fileName = Path.GetFileName(filePath);
            string remotePath;

            // If we only have a filename, get IE PID
            if (iepid == "" && flpid == "")
            {
                iepid = _ros.GetIePidFromFileName(fileName);
                log.Info(String.Format("Retrieved IE PID {0} for file name {1}", iepid, fileName));
            }
            
            // If we have an IE PID but no file PID, get file PID
            if (iepid != "" && flpid == "")
            {
                _ros.GetRepAndFilePid(iepid, fileName, out reppid, out flpid);
                log.Info(String.Format("Retrieved REP PID {0}, FL PID {1} for file name {2}", reppid, flpid, fileName));
            }

            // If we're missing something, bail out
            if (iepid == "" || reppid == "" || flpid == "")
                throw new Exception("IE, REP, and File PID could not be resolved.");

            // Upload file via scp
            remotePath = PutFile(filePath);

            // Call UpdateRep with file
            _ros.ReplaceFileInRep(iepid, reppid, flpid, "New file added by UpdateAIP app", remotePath, filePath);

            // All good....
            log.Info(String.Format("File {0} replaced in {1}, {2}", fileName, iepid, reppid));
            
        }

        /// <summary>
        /// Puts file via SCP to server
        /// </summary>
        /// <param name="filename">Path to local file</param>
        private string PutFile(string filename)
        {
            SshConnectionInfo info = new SshConnectionInfo();

            if (Path.GetPathRoot(filename) == "")
                filename = Environment.CurrentDirectory + @"\" + filename;
            
            if (!File.Exists(filename))
                throw new Exception(String.Format("File {0} does not exist.", filename));

            // massage remote file path- add trailing / if required
            string remoteFilePath = _rs.RemoteDirectory.Replace("\\", "/");
            remoteFilePath = remoteFilePath + (remoteFilePath.Substring(remoteFilePath.Length) == "/" ? "" : "/") + Path.GetFileName(filename);

            info.Direction = TransferDirection.To;
            info.LocalFilePath = filename;
            info.RemoteFilePath = remoteFilePath;
            info.User = _rs.ServerUser;
            info.Pass = _rs.ServerPassword;
            info.Host = _rs.Host;

            log.Info(String.Format("Uploading {0} to {1} on {2}", Path.GetFileName(filename), remoteFilePath, info.Host));

            SshFileTransfer ft = new SshFileTransfer();
            ft.TransferFile(info);

            return remoteFilePath;
            
        }
    }

    struct RosettaSettings
    {
        public string Host { get; set; }
        public string Port { get; set; }
        public string ServerUser { get; set; }
        public string ServerPassword { get; set; }
        public string ApplicationUser{ get; set; }
        public string ApplicationPassword { get; set; }
        public string Institution { get; set; }
        public string RemoteDirectory { get; set; }
    }
}
