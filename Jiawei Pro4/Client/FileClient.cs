/////////////////////////////////////////////////////////////////////
// FileClient.cs - sends .dll files to Repository, Download logs or files from Repository //
//                                                                 //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * Test Harness will create instance of Client
 * FileClient instances connect between Client and Repository
 * User upload files, download files by calling functions in this class
 * 
 * Author: Jiawei Wang
 * 
 * Required Files:
 * - Client.cs, ITest.cs, Logger.cs, FileClient.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 20 Nov 2016
 * - first release
 */
using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace TestHarness
{
    public class FileClient
        //File Client is for the File Sender, it will be called by Clients and Test Harness. Because 
        //they should connect to repository to transfer files.
        //this class is based on FileStream Solution provided by Prof.Fawcett. And I add some functions.
    {
        public IStreamService channel;
        public string ToSendPath = "..\\..\\ToSend"; //the default value of ToSendPath
        public string SavePath = "..\\..\\SavedFiles"; //the default value of SavePath
        public int BlockSize = 1024;
        public byte[] block;

        public FileClient()
        {
            block = new byte[BlockSize];
        }
        public static IStreamService CreateServiceChannel(string url) //create the channel.
        {
            BasicHttpSecurityMode securityMode = BasicHttpSecurityMode.None;

            BasicHttpBinding binding = new BasicHttpBinding(securityMode);
            binding.TransferMode = TransferMode.Streamed;
            binding.MaxReceivedMessageSize = 500000000;
            EndpointAddress address = new EndpointAddress(url);

            ChannelFactory<IStreamService> factory
              = new ChannelFactory<IStreamService>(binding, address);
            return factory.CreateChannel();
        }

        public bool uploadFile(string filename) //this is for uploading files.
        {
            Console.Write("\n  sending file \"{0}\"", filename);
            string fqname = Path.Combine(ToSendPath, filename);
            try
            {
                
                using (var inputStream = new FileStream(fqname, FileMode.Open))
                {
                    FileTransferMessage msg = new FileTransferMessage();
                    msg.filename = filename;
                    msg.transferStream = inputStream;
                    channel.upLoadFile(msg);
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public void download(string filename) //this is for downloading files.
        {
            try
            {
                Stream strm = channel.downLoadFile(filename);
                string rfilename = Path.Combine(SavePath, filename);
                if (!Directory.Exists(SavePath))
                    Directory.CreateDirectory(SavePath);
                using (var outputStream = new FileStream(rfilename, FileMode.Create))
                {
                    while (true)
                    {
                        int bytesRead = strm.Read(block, 0, BlockSize);
                        if (bytesRead > 0)
                            outputStream.Write(block, 0, bytesRead);
                        else
                            break;
                    }
                }
                Console.Write("\n  Received file \"{0}\"", filename);
            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}\n", ex.Message);
            }
        }
        public string[] showfiles(string filetype) //This function is to return all files of a certain file type, e.g. .dll .txt and so on.
        {
            string[] files = channel.showfiles(filetype);

            return files;
        }

    }
}