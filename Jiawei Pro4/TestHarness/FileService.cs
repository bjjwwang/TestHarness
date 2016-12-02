/////////////////////////////////////////////////////////////////////
// FileService.cs - Download required files, upload logs for Test Harness //
//                                                                 //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * Test Harness will create instance of Client
 * FileService instances connect between Test Harness and Repository
 * Test Harness can download files from Repository and upload .txt logs to Repository
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
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace TestHarness
{
    public class FileClient
    {
        public IStreamService channel;
        public string ToSendPath = "..\\..\\ToSend";
        public string SavePath = "..\\..\\SavedFiles";
        public int BlockSize = 1024;
        public byte[] block;

        public FileClient()
        {
            block = new byte[BlockSize];
        }
        public static IStreamService CreateServiceChannel(string url)
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

        public bool uploadFile(string filename)
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

        public bool download(string filename)
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
                return true;
            }
            catch (Exception ex)
            {
                Console.Write("\n  Cannot Find this file{0}\n", ex.Message);
                return false;
            }
        }

    }
}