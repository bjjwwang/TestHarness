/////////////////////////////////////////////////////////////////////
// TestExec.cs - Demonstrate TestHarness, Client, and Repository   //
// ver 1.0                                                         //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * Test Exec create a thread that waits for the request from client and process the test
 * This Package is based on Prof. Fawcett's Project 2 Demo.
 * 
 * Required files:
 * ---------------
 * - TestExec.cs
 * - ITest.cs
 * - Client.cs, Repository.cs, TestHarness.cs, Communication.cs
 * - LoadAndTest, Logger, Messages
 * 
 * Maintanence History:
 * --------------------
 * ver 2.0 : 19 Nov 2016
 * ver 1.0 : 16 Oct 2016
 * - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using System.ServiceModel;

namespace TestHarness
{
    class TestExec
    {
        ReceiverMsg recvr; //receive test Request from the client
        Message rcvdMsg; 
        
        Thread rcvThrd; // the thread wait for request from clients


        public TestHarness.TestHarness_ testHarnessA { get; set; } //create instance of TestHarness ,Client, Repository
        public Client client { get; set; }
        public Repository repository { get; set; }
        TestExec()
        {
            
            testHarnessA = new TestHarness_(repository);
            client = new Client(testHarnessA as ITestHarness);  //initialize the test Harness
            repository = new Repository();
            testHarnessA.setClient(client);
            client.setRepository(repository); 
        }
        void sendTestRequest(Message testRequest)
        {
            client.sendTestRequest(testRequest);
        }

        void ThreadProc() //wait for the test Request and Process the request.
        {
            Console.Write("\n  Demonstrating TestHarness - Project #4 with Threading");
            Console.Write("\n =======================================================");

            while (true)
            {

                // get message out of receive queue - will block if queue is empty
                rcvdMsg = recvr.GetMessage();

                TestExec te = new TestExec();
                te.sendTestRequest(rcvdMsg);
                // te.sendTestRequest(msg);
                te.testHarnessA.processMessages();
                //te.client.makeQuery("test1");
                //te.testHarnessA.wait();

            }
        }
        void RevRequest()  // 4051 is the port of Test Harness
        {
            string localPort = "4051";
            string endpoint = "http://localhost:" + localPort + "/IMessage";
            try
            {
                recvr = new ReceiverMsg();
                recvr.CreateRecvChannel(endpoint); //create the channel for the message

                // create receive thread which calls rcvBlockingQ.deQ() (see ThreadProc above)
                rcvThrd = new Thread(new ThreadStart(this.ThreadProc));
                rcvThrd.IsBackground = true;
                rcvThrd.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        static void Main(string[] args)
        {
            new TestExec().RevRequest(); //Main Function. Start and wait for 



            Console.ReadKey();
        }
    }
}
