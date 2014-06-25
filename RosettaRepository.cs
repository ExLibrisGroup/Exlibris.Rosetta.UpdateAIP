using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using System.Net;
using System.IO;
using Exlibris.Rosetta.UpdateAIP.DeliveryAccessWebService;
using Exlibris.Rosetta.UpdateAIP.IEWebService;
using System.Security.Cryptography;

namespace Exlibris.Rosetta.UpdateAIP
{
    class RosettaRepository
    {
        const string _pdsHandleName = "&pds_handle=";

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public string Host { get; set; }
        public string Port { get; set; }
        public string PdsHandle { get; set; }

        /// <summary>
        /// Creates a Rosetta repository
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="portNumber"></param>
        public RosettaRepository(string hostName, string portNumber)
        {
            Host = hostName;
            Port = portNumber;
        }

        /// <summary>
        /// Gets IE PID by calling and parsing SRU on the filename
        /// </summary>
        /// <param name="fileName">Name of file</param>
        /// <returns>IE PID</returns>
        public string GetIePidFromFileName(string fileName)
        {
            // Call SRU
            string sru = GetWebResponse(String.Format("http://{0}:{1}/delivery/sru?operation=searchRetrieve&startRecord=1&query=FILE.generalFileCharacteristics.fileOriginalName={2}",
                Host, Port, Uri.EscapeDataString(fileName)));

            XPathNavigator nav;
            XPathDocument docNav;
            XPathNodeIterator NodeIter;
            String strExpression;
            XmlNamespaceManager nsmanager;

            // Open the XML
            docNav = new XPathDocument(new StringReader(sru));

            // Create a navigator to query with XPath.
            nav = docNav.CreateNavigator();
            nsmanager = new XmlNamespaceManager(new NameTable());
            nsmanager.AddNamespace("sru", "http://www.loc.gov/zing/srw/");
            nsmanager.AddNamespace("dc", "http://purl.org/dc/elements/1.1/");
            nsmanager.AddNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance");

            strExpression = "/sru:searchRetrieveResponse/sru:records/sru:record/sru:recordData/dc:record/dc:identifier[@xsi:type='PID']";

            // Select the node and place the results in an iterator.
            NodeIter = nav.Select(strExpression, nsmanager);

            // Confirm found
            if (NodeIter.Count == 0)
                throw new Exception(String.Format("No IE found with file name {0}", fileName));

            if (NodeIter.Count > 1)
                throw new Exception(String.Format("File not unique. {0} IEs found with files named {1}", NodeIter.Count, fileName));

            NodeIter.MoveNext();

            return NodeIter.Current.Value;
        }

        /// <summary>
        /// Returns Rep and File PIDs of a particular file name by calling GetIE on the provided IE PID and parsing the returned METS for the specified file name.
        /// </summary>
        /// <param name="iePid">IE PID in which to find the file</param>
        /// <param name="fileName">Name of the file</param>
        /// <param name="repPid">Returns the Rep PID which contains the specified file</param>
        /// <param name="filePid">Returns the File PID of the specified file</param>
        public void GetRepAndFilePid(string iePid, string fileName, out string repPid, out string filePid)
        {
            XPathNavigator nav;
            XPathDocument docNav;
            XPathNodeIterator NodeIter;
            String strExpression;
            XmlNamespaceManager nsmanager;

            // Open the XML.
            docNav = new XPathDocument(new StringReader(GetIEMets(iePid)));

            // Create a navigator to query with XPath.
            nav = docNav.CreateNavigator();
            nsmanager = new XmlNamespaceManager(new NameTable());
            nsmanager.AddNamespace("dnx", "http://www.exlibrisgroup.com/dps/dnx");
            nsmanager.AddNamespace("mets", "http://www.loc.gov/METS/");

            // File PID
            strExpression = String.Format(
                "/mets:mets/mets:amdSec/mets:techMD/mets:mdWrap/mets:xmlData/dnx:dnx[dnx:section/dnx:record/dnx:key[@id='fileOriginalName' and text() ='{0}']]/dnx:section/dnx:record[dnx:key[@id='internalIdentifierType' and text() ='PID']]/dnx:key[@id='internalIdentifierValue']",
                fileName);

            // Select the node and place the results in an iterator.
            NodeIter = nav.Select(strExpression, nsmanager);

            // Confirm found
            if (NodeIter.Count == 0)
                throw new Exception(String.Format("No file named {0} found in {1}", fileName, iePid));

            if (NodeIter.Count > 1)
                throw new Exception(String.Format("File not unique. {0} files named {1} found in {2}", NodeIter.Count, fileName, iePid));

            NodeIter.MoveNext();

            filePid = NodeIter.Current.Value;

            // Now the same for Rep PID
            strExpression = String.Format(
                "/mets:mets/mets:fileSec/mets:fileGrp[mets:file[@ID='{0}']]/@ID", filePid);

            // Select the node and place the results in an iterator.
            NodeIter = nav.Select(strExpression, nsmanager);

            // Confirm found
            if (NodeIter.Count == 0)
                throw new Exception(String.Format("No REP found for file named {0} in {1}", fileName, iePid));

            if (NodeIter.Count > 1)
                throw new Exception(String.Format("File not unique. {0} files named {1} found in {2}", NodeIter.Count, fileName, iePid));

            NodeIter.MoveNext();

            repPid = NodeIter.Current.Value;

        }

        /// <summary>
        /// Get the METS of an IE
        /// </summary>
        /// <param name="iePid">PID of the requested IE</param>
        /// <returns></returns>
        public string GetIEMets(string iePid)
        {
            IEWebServices ws = new IEWebServices();
            ws.Url = String.Format("http://{0}:{1}/dpsws/repository/IEWebServices",
                Host, Port);

            IEWebService.getIEResponse resp = ws.getIE(new IEWebService.getIE() 
                {   pdsHandle = PdsHandle, 
                    iePid = iePid, 
                    flags = 0  // Get Full IE with only last revision
                });

            return resp.getIE;

            #region Old
            /*
            // Old method- used delivery since GetIE using PDS was not available
             
            string dvs = ExtractDvsIdFromDelivery(GetWebResponse(string.Format("http://{0}:{1}/delivery/DeliveryManagerServlet?dps_pid={2}",
                host, port, iePid)));

            DeliveryAccessWS ws = new DeliveryAccessWS();
            ws.Url = String.Format("http://{0}:{1}/dpsws/delivery/DeliveryAccessWS",
                host, port);
            getFullIEByDVS req = new getFullIEByDVS();
            getFullIEByDVSResponse resp = ws.getFullIEByDVS(new getFullIEByDVS() { dvs = dvs });
            return resp.@return;
             */
            #endregion

        }

        /// <summary>
        /// Replaces a single file by adding a new revision to an existing representation
        /// </summary>
        /// <param name="iePid">PID of the IE</param>
        /// <param name="repPid">PID of the representation for which a new revision will be created</param>
        /// <param name="filePid">PID of the file being replaced</param>
        /// <param name="submissionReason">A text note which indicates why the revision is being created</param>
        /// <param name="filePath">Path of file on remote server</param>
        /// <param name="localPath">Local path of file, optional. If provided, fixity will be calculated and provided to Rosetta.</param>
        public void ReplaceFileInRep(string iePid, string repPid, string filePid, string submissionReason, string filePath, string localPath = null)
        {

            IEWebServices ws = new IEWebServices();
            ws.Url = String.Format("http://{0}:{1}/dpsws/repository/IEWebServices",
                Host, Port);

            fixity fixity = new fixity();

            // If local path exists, calculate fixity
            if (!String.IsNullOrEmpty(localPath))
            {
                fixity.algorithmType = "MD5";
                var md5 = MD5.Create();
                using (var stream = File.OpenRead(localPath))
                {
                    fixity.value = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLower();
                }
            }

            // Call service
            updateRepresentationResponse resp = ws.updateRepresentation(new updateRepresentation() 
                {   pdsHandle = PdsHandle, 
                    iePid = iePid, 
                    repPid = repPid, 
                    representationContent = new representationContent[] { 
                        new representationContent() {
                            oldFilePid = filePid,
                            newFile = filePath,
                            operation = operation.REPLACE,
                            operationSpecified = true, // c# oddity when serializing enums
                            fixity = fixity,
                            fileOriginalPath = Path.GetFullPath(localPath)
                        }
                    }, 
                    submissionReason = "updated by vendor" 
                });
        }

        /// <summary>
        /// Logs into the Rosetta instance using the credentials provided and persists the PDS handle.
        /// </summary>
        /// <param name="institution">Institution ID</param>
        /// <param name="username">User Name</param>
        /// <param name="password">Password</param>
        public void Login(string institution, string username, string password)
        {
            PdsHandle = GetPdsHandle(institution, username, password);
        }


        private string GetPdsHandle(string institution, string username, string password)
        {
            // Retrieve PDS Handle
            // PDS Port hard coded- whatever

            string pdsUrl = String.Format("http://{0}:8991/pds?func=login-url&institute={1}&bor_id={2}&bor_verification={3}",
                Host, institution, username, password);
            return ExtractPdsHandle(GetWebResponse(pdsUrl));
        }

        #region RosettaUtilities

        private string ExtractDvsIdFromDelivery(string deliveryResponse)
        {
            Regex r = new Regex(@"dps_dvs=([\d~]+)", RegexOptions.IgnoreCase);
            Match m = r.Match(deliveryResponse);

            string dvs = "";
            while (m.Success)
            {
                // hard coded- get value from second match
                dvs = m.Groups[1].Captures[0].Value;
                break;
            }

            return dvs;
        }

        /// <summary>
        /// Extracts the PDS handle out of a response from the PDS URL-based API
        /// </summary>
        /// <param name="responseBody">HTML response from the call to the PDS login method</param>
        /// <returns>String containing the PDS handle</returns>
        private static string ExtractPdsHandle(string responseBody)
        {
            int pos = responseBody.IndexOf(_pdsHandleName);
            if (pos < 0)
                throw new UnauthorizedAccessException("User not authenticated in PDS");
            responseBody = responseBody.Substring(pos + _pdsHandleName.Length);
            int digit;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; int.TryParse(responseBody[i].ToString(), out digit); i++)
            {
                sb.Append(digit);
            }
            string pdsHandle = sb.ToString();
            if (String.IsNullOrEmpty(pdsHandle))
                throw new UnauthorizedAccessException("User not authenticated in PDS");
            return pdsHandle;
        }

        #endregion


        #region Utilities

        private string GetWebResponse(string url)
        {
            WebRequest request = WebRequest.Create(url);
            request.Method = "GET";
            // Get the response.
            WebResponse response = request.GetResponse();
            // Get the stream containing content returned by the server.
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();
            // Clean up the streams.
            reader.Close();
            dataStream.Close();
            response.Close();

            return responseFromServer;
        }

        #endregion
    }

}
