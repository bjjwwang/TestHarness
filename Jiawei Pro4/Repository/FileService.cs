/////////////////////////////////////////////////////////////////////
// FileService.cs - holds test code for TestHarness                 //
//                                                                 //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package provide the functions to maintain files in Repository, to Query the logs, to upload, download or delete files.
 * User call functions to download files, upload files, query logs
 * TestHarness call functions to download required files, upload testing logs.
 * 
 * Required Files:
 * - Client.cs, ITest.cs, Logger.cs, Repository.cs
 * 
 * Author: Jiawei Wang
 * 
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 20 Nov 2016
 * - first release
 */


using System;
using System.IO;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace TestHarness
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class StreamService : IStreamService
    {
        string filename;
        string savePath = "..\\..\\..\\Repository\\RepositoryStorage\\"; //the path that user upload files to 
        string ToSendPath = "..\\..\\..\\Repository\\RepositoryStorage\\";// the path that user download files from
        int BlockSize = 1024;
        byte[] block;

        StreamService()
        {
            block = new byte[BlockSize];
        }

        public void upLoadFile(FileTransferMessage msg) //uploadfiles
        {
            HRTimer.HiResTimer hrt = new HRTimer.HiResTimer();
            hrt.Start();
            filename = msg.filename;
            string rfilename = Path.Combine(savePath, filename);
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);
            using (var outputStream = new FileStream(rfilename, FileMode.Create))
            {
                while (true)
                {
                    int bytesRead = msg.transferStream.Read(block, 0, BlockSize);
                    if (bytesRead > 0)
                        outputStream.Write(block, 0, bytesRead);
                    else
                        break;
                }
            }
            hrt.Stop();
            Console.Write("\n  Received file \"{0}\"", filename);
        }

        public Stream downLoadFile(string filename) //download files
        {
            string sfilename = Path.Combine(ToSendPath, filename);
            FileStream outStream = null;
            if (File.Exists(sfilename))
            {
                outStream = new FileStream(sfilename, FileMode.Open);
                Console.Write("\n  Sending File \"{0}\"", filename);
            }
            else
                throw new Exception("open failed for \"" + filename + "\"");
            return outStream;
        }

        public string[] showfiles(string filetype) //Show the files to users, e.g. .dll files, .txt files.
        {
            string[] files = System.IO.Directory.GetFiles(savePath,filetype);
            return files;
        }
        public void deleteFile(string filename)//delete the file
        {
            string file = savePath + "\\" + filename;
            System.IO.File.Delete(file);
            Console.Write("\n  Deleting File \"{0}\"", filename);
        }
        public List<string> ClientQuery(string msg) {// do query according to keyword
            return new Repository().queryLogs(msg);
        }

        public static ServiceHost CreateServiceChannel(string url)
        {
            // Can't configure SecurityMode other than none with streaming.
            // This is the default for BasicHttpBinding.
            //   BasicHttpSecurityMode securityMode = BasicHttpSecurityMode.None;
            //   BasicHttpBinding binding = new BasicHttpBinding(securityMode);

            BasicHttpBinding binding = new BasicHttpBinding();
            binding.TransferMode = TransferMode.Streamed;
            binding.MaxReceivedMessageSize = 50000000;
            Uri baseAddress = new Uri(url);
            Type service = typeof(StreamService);
            ServiceHost host = new ServiceHost(service, baseAddress);
            host.AddServiceEndpoint(typeof(IStreamService), binding, baseAddress);
            return host;
        }
    }
}
