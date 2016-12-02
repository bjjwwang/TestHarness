/////////////////////////////////////////////////////////////////////
// Client.cs - sends TestRequests, displays results                //
//                                                                 //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * Test Harness will create instance of Client
 * FileClient instances connect between Client and Repository, Test Harness and Repository
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestHarness
{
    //this class is based on Prof.Fawcett's InstrSol-Project2
    public class Client:IClient
    {
        public SWTools.BlockingQueue<string> inQ_ { get; set; }
        private ITestHarness th_ = null;
        private IRepository repo_ = null;

        public Client(ITestHarness th)
        {
            Console.Write("\n  Creating instance of Client");
            th_ = th;
        }
        public Client() { }
        public void setRepository(IRepository repo)
        {
            repo_ = repo;
        }

        public void sendTestRequest(Message testRequest)
        {
            th_.sendTestRequest(testRequest);
        }
        public void sendResults(Message results)
        {
            Console.Write("\n  Client received results message:");
            Console.Write("\n  " + results.ToString());
            RLog.putLine();
        }

#if (TEST_CLIENT)
        static void Main(string[] args)
        {
            Message Test = TestMessages.getMsg();
            new Client().sendResults(Test);
        }
#endif
    }
}
