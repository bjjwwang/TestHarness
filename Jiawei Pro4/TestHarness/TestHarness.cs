/////////////////////////////////////////////////////////////////////
// TestHarness.cs - TestHarness Engine: creates child domains      //
// ver 1.1                                                         //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * TestHarness package provides integration testing services.  It:
 * - receives structured test requests
 * - retrieves cited files from a repository
 * - executes tests on all code that implements an ITest interface,
 *   e.g., test drivers.
 * - reports pass or fail status for each test in a test request
 * - stores test logs in the repository
 * - send the test results to the clients
 * It contains classes:
 * - TestHarness that runs all tests in child AppDomains
 * - Callback to support sending messages from a child AppDomain to
 *   the TestHarness primary AppDomain.
 * - Test and RequestInfo to support transferring test information
 *   from TestHarness to child AppDomain
 *  - File Service to provide functions to download required files and 
 *    upload logs.
 *   
 * Author: Jiawei Wang
 * 
 * Required Files:
 * ---------------
 * - TestHarness.cs, BlockingQueue.cs, FileService.cs
 * - ITest.cs
 * - LoadAndTest, Logger, Messages, Communications
 *
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Security.Policy;    // defines evidence needed for AppDomain construction
using System.Runtime.Remoting;   // provides remote communication between AppDomains
using System.Xml;
using System.Xml.Linq;
using System.ServiceModel;
using System.Threading;
namespace TestHarness
{
    ///////////////////////////////////////////////////////////////////
    // Callback class is used to receive messages from child AppDomain
    //
    public class Callback : MarshalByRefObject, ICallback
    {
        public void sendMessage(Message message)
        {
            Console.Write("\n  received msg from childDomain: \"" + message.body + "\"");
        }
    }
    ///////////////////////////////////////////////////////////////////
    // Test and RequestInfo are used to pass test request information
    // to child AppDomain
    //
    [Serializable]
    class Test : ITestInfo
    {
        public string testName { get; set; }  
        public List<string> files { get; set; } = new List<string>();
    }
    [Serializable]
    class RequestInfo : IRequestInfo
    {
        public string tempDirName { get; set; }
        public List<ITestInfo> requestInfo { get; set; } = new List<ITestInfo>();
    }
    ///////////////////////////////////////////////////////////////////
    // class TestHarness

    public class TestHarness_ : ITestHarness
    {
        string ClientPort; //ClientPort is the port of client, we send the result back to Client through this port.
        SenderMsg sndr; //Sender for test results.
        FileClient clnt = null;


        public SWTools.BlockingQueue<Message> inQ_ { get; set; } = new SWTools.BlockingQueue<Message>();//queue for test harness to arrange multi-tests.
        private ICallback cb_;
        private IRepository repo_; 
        private IClient client_;
        private string localDir_; //it is the folder for running the test
        private string repoPath_ = "../../../TestExecutive/Temporary/"; //we temperarily download files from repository to this folder
        private string filePath_;

        object sync_ = new object(); 
        List<Thread> threads_ = new List<Thread>(); //list of threads.
        Dictionary<int, string> TLS = new Dictionary<int, string>();

        //private string loaderPath_;

        public TestHarness_(IRepository repo)
        {
            Console.Write("\n  creating instance of TestHarness");
            repo_ = repo;
            repoPath_ = System.IO.Path.GetFullPath(repoPath_);
            cb_ = new Callback();
        }
        //----< called by TestExecutive >--------------------------------

        public void setClient(IClient client)
        {
            client_ = client;
        }
        //----< called by clients >--------------------------------------

        public void sendTestRequest(Message testRequest)
        {
            Console.Write("\n  Enqueue Test Request, And process concurrently if thread is available --------Project 4. Req 4");
            inQ_.enQ(testRequest);
        }
        //----< not used for Project #4 >--------------------------------


        //----< make path name from author and time >--------------------

        string makeKey(string author)  //make Author_Time key for the tests.
        {
            DateTime now = DateTime.Now;
            string nowDateStr = now.Date.ToString("d");
            string[] dateParts = nowDateStr.Split('/');
            string key = "";
            foreach (string part in dateParts)
                key += part.Trim() + '_';
            string nowTimeStr = now.TimeOfDay.ToString();
            string[] timeParts = nowTimeStr.Split(':');
            for (int i = 0; i < timeParts.Count() - 1; ++i)
                key += timeParts[i].Trim() + '_';
            key += timeParts[timeParts.Count() - 1];
            key = author + "_" + key;
            return key;
        }
        //----< retrieve test information from testRequest >-------------
        List<ITestInfo> extractTests(Message testRequest) {  //we parsed the message and extract each test
            ClientPort = testRequest.port;
            Console.Write("\n  parsing test request");
            List<ITestInfo> tests = new List<ITestInfo>();
            foreach (testElement te in testRequest.tr.tests) //analyze each test element in the message
            {
                Test test = new Test();
                string testDriverName = te.testDriver; // add required files into the Test variable.
                test.testName = te.testName;
                test.files.Add(testDriverName);
                foreach (string tc in te.testCodes)
                {
                    test.files.Add(tc);  
                }
                tests.Add(test); 
            }
            return tests;// return the test variable which save all the required files.
        }


        //----< retrieve test code from testRequest >--------------------

        List<string> extractCode(List<ITestInfo> testInfos)
        {
            Console.Write("\n  retrieving code files from testInfo data structure");
            List<string> codes = new List<string>();
            foreach (ITestInfo testInfo in testInfos)
                codes.AddRange(testInfo.files);
            return codes;
        }
        //----< create local directory and load from Repository >--------



        public void ConnectFilesToRepo() //This Channel is for user to upload files.
        {
            string remoteAddress = "http://localhost:";
            string remotePort = "4061"; //4061 is the port of repository
            string endpoint = remoteAddress + remotePort + "/StreamService";
            clnt = new FileClient();
            int count = 0;
            while (true)
            {
                try
                {
                    clnt.channel = FileClient.CreateServiceChannel(endpoint); //connect to repository for requiring files.
                    clnt.SavePath = repoPath_;
                    clnt.ToSendPath = repoPath_;
                    break;
                }
                catch
                {
                    Console.Write("\n  connection to service failed {0} times - trying again", ++count);
                    Thread.Sleep(500);
                    continue;
                }
            }
        }


        RequestInfo processRequestAndLoadFiles(Message testRequest)
        {
            RequestInfo rqi = new RequestInfo();
            rqi.requestInfo = extractTests(testRequest); //get the request info, which save the files of test request
            List<string> files = extractCode(rqi.requestInfo);
            localDir_ = makeKey(testRequest.author);            // name of temporary dir to hold test files
            filePath_ = System.IO.Path.GetFullPath(localDir_);  // LoadAndTest will use this path
            rqi.tempDirName = localDir_;
            Console.Write("\n  creating local test directory \"" + localDir_ + "\"");
            System.IO.Directory.CreateDirectory(localDir_); //create the running folder
            connect_to_Client(ClientPort); // connect to the client
            ConnectFilesToRepo();  //connect to repository
            foreach (string i in files)
            {
                Console.Write("\n  Test Harness Server download Required Files from Repository --------Project 4. Req 6");
                if (!clnt.download(i))  //download all the required files from repository, it not exist, return to client
                {
                    Message fileNotExist = new Message();
                    fileNotExist.body = i+" does Not Exist in Repository";
                    sndr.PostMessage(fileNotExist);
                    Console.Write("\n  Sending Notinication to Client through WCF --------Project 4. Req 10");
                }
            }
            lock (sync_)
            {
                filePath_ = System.IO.Path.GetFullPath(localDir_);  // LoadAndTest will use this path
                TLS[Thread.CurrentThread.ManagedThreadId] = filePath_;
            }

            Console.Write("\n  loading code from Repository");
            foreach (string file in files)  
            {
                string name = System.IO.Path.GetFileName(file);
                string src = System.IO.Path.Combine(repoPath_, file);
                if (System.IO.File.Exists(src))
                {
                    string dst = System.IO.Path.Combine(localDir_, name);
                    System.IO.File.Copy(src, dst, true); //copy the files from temporary folder to running folder
                    Console.Write("\n    TID" + Thread.CurrentThread.ManagedThreadId + ": retrieved file \"" + name + "\""); //print the file and Thread id
                }
                else
                {
                    Console.Write("\n    TID" + Thread.CurrentThread.ManagedThreadId + ": could not retrieve file \"" + name + "\"");
                }
            }
            Console.WriteLine();
            return rqi;
        }
        void connect_to_Client(string remotePort)
        {
            string remoteAddress = "http://localhost:";  //connect back to client, in order to sent the result back.
            string endpoint = remoteAddress + remotePort + "/IMessage"; //remotePort is the port of client, which is the process ID of the client.
            sndr = new TestHarness.SenderMsg(endpoint);
        }


        //----< save results and logs in Repository >--------------------

        bool saveResultsAndLogs(ITestResults testResults)
        {
            // sndr 
            
            string logName = testResults.testKey + ".txt"; //set the file name
            Console.Write("\n  The Key is "+logName+" --------Project 4. Req 8");
            Message toClient = new Message();
            toClient.from = "TH";
            toClient.to = "CL";
            System.IO.StreamWriter sr = null;
            try
            {
                sr = new System.IO.StreamWriter(System.IO.Path.Combine(repoPath_, logName));
                sr.WriteLine(logName); toClient.ResultLogs.Add(testResults.testKey);
                foreach (ITestResult test in testResults.testResults) //write to the txt files and massage simultaneously.
                {
                    sr.WriteLine("-----------------------------"); toClient.ResultLogs.Add("-----------------------------");
                    sr.WriteLine(test.testName); toClient.ResultLogs.Add(test.testName);
                    sr.WriteLine(test.testResult); toClient.ResultLogs.Add(test.testResult);
                    sr.WriteLine(test.testLog); toClient.ResultLogs.Add(test.testLog);
                }
                sr.WriteLine("-----------------------------"); toClient.ResultLogs.Add("-----------------------------");
            }
            catch
            {
                sr.Close();
                return false;
            }
            sr.Close();
            clnt.uploadFile(logName); //send the test log to repository
            sndr.PostMessage(toClient); // send the test result back to client.
            Console.Write("\n  Test Harness Server send back result to Client and Send log file to Repository --------Project 4. Req 7");
            Console.Write("\n  Sending Test Result to Client through WCF --------Project 4. Req 10");
            return true;
        }
        //----< run tests >----------------------------------------------
        /*
         * In Project #4 this function becomes the thread proc for
         * each child AppDomain thread.
         */
        ITestResults runTests(Message testRequest)
        {
            AppDomain ad = null;
            ILoadAndTest ldandtst = null;
            RequestInfo rqi = null;
            ITestResults tr = null;

            try
            {
                //lock (sync_)
                {
                    rqi = processRequestAndLoadFiles(testRequest);

                    ad = createChildAppDomain();
                    ldandtst = installLoader(ad);
                }
                if (ldandtst != null)
                {
                    tr = ldandtst.test(rqi);
                    tr.testKey = rqi.tempDirName;
                }
                // unloading ChildDomain, and so unloading the library

                saveResultsAndLogs(tr); //write the test log and send the test result

                lock (sync_)
                {
                    Console.Write("\n  TID" + Thread.CurrentThread.ManagedThreadId + ": unloading: \"" + ad.FriendlyName + "\"\n");
                    AppDomain.Unload(ad);
                    try
                    {
                        System.IO.Directory.Delete(rqi.tempDirName, true);
                        Console.Write("\n  TID" + Thread.CurrentThread.ManagedThreadId + ": removed directory " + rqi.tempDirName);
                    }
                    catch (Exception ex)
                    {
                        Console.Write("\n  TID" + Thread.CurrentThread.ManagedThreadId + ": could not remove directory " + rqi.tempDirName);
                        Console.Write("\n  TID" + Thread.CurrentThread.ManagedThreadId + ": " + ex.Message);
                    }
                }
                return tr;
            }
            catch (Exception ex)
            {
                Console.Write("\n\n---- {0}\n\n", ex.Message);
                return tr;
            }
        }
        //----< make TestResults Message >-------------------------------
        Message makeTestResultsMessage(ITestResults tr) //write the test result in XML format, which is not used in project 4, but I leave the code here.
        {
            Message trMsg = new Message();
            trMsg.author = "TestHarness";
            trMsg.to = "CL";
            trMsg.from = "TH";
            XDocument doc = new XDocument();
            XElement root = new XElement("testResultsMsg");
            doc.Add(root);
            XElement testKey = new XElement("testKey");
            testKey.Value = tr.testKey;
            root.Add(testKey);
            XElement timeStamp = new XElement("timeStamp");
            timeStamp.Value = tr.dateTime.ToString();
            root.Add(timeStamp);
            XElement testResults = new XElement("testResults");
            root.Add(testResults);
            foreach (ITestResult test in tr.testResults)
            {
                XElement testResult = new XElement("testResult");
                testResults.Add(testResult);
                XElement testName = new XElement("testName");
                testName.Value = test.testName;
                testResult.Add(testName);
                XElement result = new XElement("result");
                result.Value = test.testResult;
                testResult.Add(result);
                XElement log = new XElement("log");
                log.Value = test.testLog;
                testResult.Add(log);
            }
            trMsg.body = doc.ToString();
            return trMsg;
        }
        //----< wait for all threads to finish >-------------------------
        public void wait()
        {
            foreach (Thread t in threads_)
                t.Join();
        }
        //----< main activity of TestHarness >---------------------------

        public void processMessages() //process the message, start a test in a thread, the maximum of thread is 8.
        {
            AppDomain main = AppDomain.CurrentDomain;
            Console.Write("\n  Starting in AppDomain " + main.FriendlyName + "\n");

            ThreadStart doTests = () =>
            {
                Message testRequest = inQ_.deQ();
                if (testRequest.body == "quit")
                {
                    inQ_.enQ(testRequest); //enqueue a test request.
                    return;
                }
                ITestResults testResults = runTests(testRequest);
                lock (sync_)
                {
                    client_.sendResults(makeTestResultsMessage(testResults));
                }
            };

            int numThreads = 8;
            Console.Write("\n  Creating AppDomain thread, We Support Multi-Thread Test Request. --------Project 4. Req 2");
            for (int i = 0; i < numThreads; ++i) //start thread
            {
                
                Thread t = new Thread(doTests);
                threads_.Add(t);
                t.Start();
            }
        }
        //----< was used for debugging >---------------------------------

        void showAssemblies(AppDomain ad)
        {
            Assembly[] arrayOfAssems = ad.GetAssemblies();
            foreach (Assembly assem in arrayOfAssems)
                Console.Write("\n  " + assem.ToString());
        }
        //----< create child AppDomain >---------------------------------

        public AppDomain createChildAppDomain()
        {
            try
            {
                Console.Write("\n  creating child AppDomain  --------Project 4. Req 4");

                AppDomainSetup domaininfo = new AppDomainSetup();
                domaininfo.ApplicationBase
                  = "file:///" + System.Environment.CurrentDirectory;  // defines search path for LoadAndTest library

                //Create evidence for the new AppDomain from evidence of current

                Evidence adevidence = AppDomain.CurrentDomain.Evidence;

                // Create Child AppDomain

                AppDomain ad
                  = AppDomain.CreateDomain("ChildDomain", adevidence, domaininfo);

                Console.Write("\n  created AppDomain \"" + ad.FriendlyName + "\"");
                return ad;
            }
            catch (Exception except)
            {
                Console.Write("\n  " + except.Message + "\n\n");
            }
            return null;
        }
        //----< Load and Test is responsible for testing >---------------

        ILoadAndTest installLoader(AppDomain ad)
        {
            ad.Load("LoadAndTest");
            //showAssemblies(ad);
            //Console.WriteLine();

            // create proxy for LoadAndTest object in child AppDomain

            ObjectHandle oh
              = ad.CreateInstance("LoadAndTest", "TestHarness.LoadAndTest");
            object ob = oh.Unwrap();    // unwrap creates proxy to ChildDomain
                                        // Console.Write("\n  {0}", ob);

            // set reference to LoadAndTest object in child

            ILoadAndTest landt = (ILoadAndTest)ob;

            // create Callback object in parent domain and pass reference
            // to LoadAndTest object in child

            landt.setCallback(cb_);
            lock (sync_)
            {
                filePath_ = TLS[Thread.CurrentThread.ManagedThreadId];
                landt.loadPath(filePath_);  // send file path to LoadAndTest
            }
            return landt;
        }
#if (TEST_TESTHARNESS)
        static void Main(string[] args)
        {
            Repository repository = new Repository(); ;
            Message Test = TestMessages.getMsg();
            new TestHarness_(repository). sendTestRequest(Test);
            new TestHarness_(repository).processMessages(); //it which we will create 8 threads
        }
#endif
    }
}
