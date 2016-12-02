using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/////////////////////////////////////////////////////////////////////
// Communication.svc.cs - Peer-To-Peer WCF Communicator            //
// ver 2.1                                                         //
// Jim Fawcett, CSE681 - Software Modeling & Analysis, Summer 2011 //
/////////////////////////////////////////////////////////////////////
/*
 * Maintenance History:
 * ====================
 * ver 2.3 : 19 Nov 19
 * - This Package is based on peer-to-peer WCF, and I change the data structure, 
 * - Message is the data structure in WCF, and Message will be serialized.
 * ver 2.2 : 01 Nov 11
 * - Removed unintended local declaration of ServiceHost in Receiver's 
 *   CreateReceiveChannel function
 * ver 2.1 : 10 Oct 11
 * - removed [OperationContract] from GetMessage() so only local client
 *   can dequeue messages
 * - added send thread to keep clients from blocking on slow sends
 * - added retries when creating Communication channel proxy
 * - added comments to clarify what code is doing
 * ver 2.0 : 06 Nov 08
 * - added close functions that close the service and receive channel
 * ver 1.0 : 14 Jul 07
 * - first release
 * 
 * Author: Jiawei Wang
 * 
 * 
 */

using System.ServiceModel;
using System.Threading;
using SWTools;

namespace TestHarness
{
    /////////////////////////////////////////////////////////////
    // Receiver hosts Communication service used by other Peers
    //This class is based on the WCF peer-to-peer, Client send testRequest and Test Harness send testresult through this class
    public class ReceiverMsg : IMessage
    {
        static BlockingQueue<TestHarness.Message> rcvBlockingQ = null;//queue for receiving messages
        ServiceHost service = null;

        public ReceiverMsg()
        {
            if (rcvBlockingQ == null)
                rcvBlockingQ = new BlockingQueue<TestHarness.Message>();
        }

        public void Close()
        {
            service.Close();
        }

        //  Create ServiceHost for Communication service

        public void CreateRecvChannel(string address)
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            Uri baseAddress = new Uri(address);
            service = new ServiceHost(typeof(ReceiverMsg), baseAddress);
            service.AddServiceEndpoint(typeof(IMessage), binding, baseAddress);
            service.Open();
        }

        // Implement service method to receive messages from other Peers

        public void PostMessage(TestHarness.Message msg)
        {
            rcvBlockingQ.enQ(msg);
        }

        // Implement service method to extract messages from other Peers.
        // This will often block on empty queue, so user should provide
        // read thread.

        public TestHarness.Message GetMessage()
        {
            return rcvBlockingQ.deQ();
        }
    }
    ///////////////////////////////////////////////////
    // client of another Peer's Communication service

    public class SenderMsg
    {
        IMessage channel;
        string lastError = "";
        BlockingQueue<TestHarness.Message> sndBlockingQ = null;
        Thread sndThrd = null;
        int tryCount = 0, MaxCount = 10;

        // Processing for sndThrd to pull msgs out of sndBlockingQ
        // and post them to another Peer's Communication service

        void ThreadProc()
        {
            while (true)
            {
                TestHarness.Message msg = sndBlockingQ.deQ();
                channel.PostMessage(msg);
                // if (msg == "quit")
                //    break;
            }
        }

        // Create Communication channel proxy, sndBlockingQ, and
        // start sndThrd to send messages that client enqueues

        public SenderMsg(string url)
        {
            sndBlockingQ = new BlockingQueue<TestHarness.Message>();
            while (true)
            {
                try
                {
                    CreateSendChannel(url);
                    tryCount = 0;
                    break;
                }
                catch (Exception ex)
                {
                    if (++tryCount < MaxCount)
                        Thread.Sleep(100);
                    else
                    {
                        lastError = ex.Message;
                        break;
                    }
                }
            }
            sndThrd = new Thread(ThreadProc);
            sndThrd.IsBackground = true;
            sndThrd.Start();
        }

        // Create proxy to another Peer's Communicator

        public void CreateSendChannel(string address)
        {
            EndpointAddress baseAddress = new EndpointAddress(address);
            BasicHttpBinding binding = new BasicHttpBinding();
            ChannelFactory<IMessage> factory
              = new ChannelFactory<IMessage>(binding, address);
            channel = factory.CreateChannel();
        }

        // Sender posts message to another Peer's queue using
        // Communication service hosted by receipient via sndThrd

        public void PostMessage(TestHarness.Message msg)
        {
            sndBlockingQ.enQ(msg);
        }

        public string GetLastError()
        {
            string temp = lastError;
            lastError = "";
            return temp;
        }

        public void Close()
        {
            ChannelFactory<IMessage> temp = (ChannelFactory<IMessage>)channel;
            temp.Close();
        }
        

         static void Main(string[] args)
        {
            

            Console.Write("\n  Comm Service Running:");
            Console.Write("\n  Press key to quit");
            Console.ReadKey();
            Console.Write("\n\n");
        }
    }
}
