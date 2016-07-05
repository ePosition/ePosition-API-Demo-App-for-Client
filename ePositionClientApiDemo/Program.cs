using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ePositionClientApiDemo
{
    class Program
    {
        static void Main(string[] args)
        {

            args = Environment.GetCommandLineArgs();


            if (args.Length <= 1)
            {
                Console.WriteLine("No argument(s)");
                Console.WriteLine("Syntax : ");
                Console.WriteLine("\tepo:id#domain");
                Console.WriteLine("Press Any Key...");
                Console.ReadKey();
                return;
            }

            // Check Registry of ePosition Protocol
            CheckRegistryEpositionProtocol(args[0]);

            string id="";
            string domain="";
            SplitIdDomain(args[1], ref id, ref domain);

            string ePositionServerName = "";
            string hostName = GetHostName(domain);

            if (hostName == null)
            {
                Console.WriteLine("Unable to find Server or DNS Error", domain);
                return;   // terminate program
            }

            ePositionServerName = GetHostName(domain) + domain;

            //Console.WriteLine(ePositionServerName);

            string apiUrlJson = "http://" + ePositionServerName + "/v1/eposition/?epid=" + id + "&answer=json";
            string apiUrlXml = "http://" + ePositionServerName + "/v1/eposition/?epid=" + id + "&answer=xml";

            using (var wb = new WebClient())
            {

                Console.WriteLine("------- JSON Result --------");
                var response = wb.DownloadString(apiUrlJson);
                JObject jsonResult = JObject.Parse(response);
                Console.WriteLine(jsonResult);
                File.WriteAllText(@"Result.json", jsonResult.ToString());


                Console.WriteLine("-------  XML Result --------");
                response = wb.DownloadString(apiUrlXml);
                XDocument doc = XDocument.Parse(response,LoadOptions.PreserveWhitespace);
                Console.WriteLine(doc);
                doc.Save("Result.xml");
                
            }

            Console.ReadKey();

        }



        static string GetHostName(string domain)
        {

            DnsMessage dnsMessage = DnsClient.Default.Resolve(DomainName.Parse(domain), RecordType.Any);
            if ((dnsMessage == null) || ((dnsMessage.ReturnCode != ReturnCode.NoError) && (dnsMessage.ReturnCode != ReturnCode.NxDomain)))
            {
                //throw new Exception("DNS request failed");
                return null;
            }
            else
            {
                foreach (DnsRecordBase dnsRecord in dnsMessage.AnswerRecords)
                {
                    UnknownRecord unknownRecord = dnsRecord as UnknownRecord;
                    if (unknownRecord != null)
                    {
                        string rrType = unknownRecord.RecordType.ToString();

                        if (rrType == "9922")    //ePosition url service RR Type
                        {
                            string rRecord = unknownRecord.ToString();
                            return ParsingResourceRecord(rRecord);
                        }

                    }
                }
            }
            return null;
        }

        static string ParsingResourceRecord(string rRecord)
        {
            string[] strArr = rRecord.Split(' ', '\t');

            string hexaName = strArr[strArr.Length - 1];       // RDATA
            int cntChar = Int32.Parse(strArr[strArr.Length - 2]);       //# of RDATA Byte

            return ConvertHexToString(hexaName);

        }


        static string ConvertHexToString(string HexValue)
        {
            string StrValue = "";

            while (HexValue.Length > 0)
            {
                StrValue += System.Convert.ToChar(System.Convert.ToUInt32(HexValue.Substring(0, 2), 16)).ToString();
                HexValue = HexValue.Substring(2, HexValue.Length - 2);

            }
            return StrValue;
        }


        //If you use InstallSheild, This method will be not used.
        static void CheckRegistryEpositionProtocol(string appPath)
        {
            RegistryKey key = Registry.ClassesRoot.OpenSubKey("epo");  //open ePositionUri protocol's subkey

            if (key == null)  //if the protocol is not registered yet...we register it
            {
                key = Registry.ClassesRoot.CreateSubKey("epo");
                key.SetValue(string.Empty, "URL: ePosition URI Protocol");
                key.SetValue("URL Protocol", string.Empty);

                key = key.CreateSubKey(@"shell\open\command");
                key.SetValue(string.Empty, appPath + " " + "%1");
                //%1 represents the argument - this tells windows to open this program with an argument / parameter
            }

            key.Close();

        }



        static void SplitIdDomain(string fullString, ref string id, ref string domain)
        {
            string[] strArr = fullString.Split('#', ':');

            id = strArr[1];
            domain = strArr[2];
        }

    }
}
