/////////////////////////////////////////////////////////////////////
// LoadAndTest.cs - loads and executes tests using reflection      //
// ver 1.1                                                         //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * LoadAndTest package operates in child AppDomain.  It loads and
 * executes test code defined by a TestRequest message.
 *
 * Required files:
 * ---------------
 * - LoadAndTest.cs
 * - ITest.cs
 * - Logger, Messages
 * 
 * Maintanence History:
 * --------------------
 * Author: Jiawei Wang
 * This package is based on Prof. Fawcett's Project 2 Demo.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;
using System.Threading;

namespace TestHarness
{
    public class LoadAndTest : MarshalByRefObject, ILoadAndTest
    {
       
        private ICallback cb_ = null;
        private string loadPath_ = "";
        object sync_ = new object();

        ///////////////////////////////////////////////////////
        // Data Structures used to store test information
        //
        [Serializable]
        private class TestResult : ITestResult //impletation of ITestResult
        {
            public string testName { get; set; }
            public string testResult { get; set; }
            public string testLog { get; set; }
        }
        [Serializable]
        private class TestResults : ITestResults //implemetation of ITestResults, that is a couple of ITestResult
        {
            public string testKey { get; set; } 
            public DateTime dateTime { get; set; }
            public List<ITestResult> testResults { get; set; } = new List<ITestResult>();
        }
        TestResults testResults_ = new TestResults();

        //----< initialize loggers >-------------------------------------

        public LoadAndTest()
        {
            // need to attach and start because
            // DLog in child AppDomain doesn't share the same static logger
            // as DLog in the parent AppDomain
        }
        public void loadPath(string path)  
        {
            loadPath_ = path;
            Console.Write("\n  loadpath = {0}", loadPath_);
        }
        //----< load libraries into child AppDomain and test >-----------
        /*
         * ToDo:
         * - refactor this function to make it more testable.
         */
        TestResult testResult = new TestResult();
        TestResults testResults = new TestResults();
        public ITestResults test(IRequestInfo testRequest)  //load file and test like Project 2
        {
            foreach (ITestInfo test in testRequest.requestInfo) //load test one by one
            {
                testResult.testName = test.testName;
                try{
                    Console.Write("\n  TID" + Thread.CurrentThread.ManagedThreadId + ": -- \"" + test.testName + "\" --");
                    ITest tdr = null;
                    string testDriverName = "";
                    string fileName = "";
                    foreach (string file in test.files) //Assemble each test file
                    {
                        fileName = file;
                        Assembly assem = null;
                        try{
                            if (loadPath_.Count() > 0){
                                for (int i = 0; i < 5; ++i){
                                    try{
                                        assem = Assembly.LoadFrom(loadPath_ + "/" + file);
                                        break;}
                                    catch { Thread.Sleep(100); }
                                }
                            }
                            else assem = Assembly.Load(file);}
                        catch{
                            CatchException("file not loaded", file); //catch exception, file not load
                            continue;}
                        Console.Write("\n    TID" + Thread.CurrentThread.ManagedThreadId + ": loaded \"" + file + "\"");
                        Type[] types = assem.GetExportedTypes();
                        foreach (Type t in types)
                        {
                            if (t.IsClass && typeof(ITest).IsAssignableFrom(t)){ // does this type derive from ITest ?
                                try {
                                    testDriverName = file;
                                    tdr = (ITest)Activator.CreateInstance(t);    // create instance of test driver
                                    Console.Write("\n    TID" + Thread.CurrentThread.ManagedThreadId + ": " + testDriverName + " implements ITest interface - Project 4 Req 5");
                                }catch{continue;}
                            }
                        }
                    }
                    Analyzetest(tdr, testResult,testDriverName);
                }
                catch (Exception ex){CatchException("exception thrown", ex.Message);}// catch exception thrown 
                testResults_.testResults.Add(testResult);
            }
            testResults_.dateTime = DateTime.Now; // add Date info and Key into TestResult.
            testResults_.testKey = System.IO.Path.GetFileName(loadPath_);
            return testResults_;
        }
        public void CatchException(string Ex,string ExMessage) //Catch Exception, two kind of Exception
        {
            if (Ex == "file not loaded")
            {
                testResult.testResult = "failed";
                testResult.testLog = "file not loaded";
                Console.Write("\n    TID" + Thread.CurrentThread.ManagedThreadId + ": can't load\"" + ExMessage + "\"");
            }
            if (Ex == "exception thrown")
            {
                testResult.testResult = "failed";
                testResult.testLog = "exception thrown";
                Console.Write("\n  TID" + Thread.CurrentThread.ManagedThreadId + ": " + ExMessage);
            }
        }
       
        public void Analyzetest(ITest tdr,ITestResult testResult, string testDriverName) 
            //Analyzetest() is the function to analyze and write down the test result.
        {
            Console.Write("\n    TID" + Thread.CurrentThread.ManagedThreadId + ": testing " + testDriverName);
            bool testReturn;
            try { testReturn = tdr.test(); }
            catch { testReturn = false; }
            if (tdr != null && testReturn == true)
            {
                testResult.testResult = "passed";
                testResult.testLog = tdr.getLog();
                Console.Write("\n    TID" + Thread.CurrentThread.ManagedThreadId + ": test passed");
                if (cb_ != null) cb_.sendMessage(new Message(testDriverName + " passed"));
            }
            else
            {
                testResult.testResult = "failed";
                if (tdr != null) testResult.testLog = tdr.getLog();
                else testResult.testLog = "file not loaded";
                Console.Write("\n    TID" + Thread.CurrentThread.ManagedThreadId + ": test failed");
                if (cb_ != null) cb_.sendMessage(new Message(testDriverName + ": failed"));
            }

        }

        public void setCallback(ICallback cb)
        {
            cb_ = cb;
        }


        static void Main(string[] args)
        {
            /*
             * ToDo: add code to test
             * - Use Callback to write to log instead of using log here.  That would be
             *   an improvement since the static logger in StaticLogger<LogType> is not
             *   shared between the child and parent AppDomains so logs get out of synch.
             * - Used TestHarness for testing, but the plan is to create a test class
             *   that derives from ITest and run tests either from this project or in 
             *   TestHarness. 
             */
            new LoadAndTest().CatchException("file not loaded", "a.dll"); // test infomation 
            new LoadAndTest().CatchException("exception thrown", "Unknown"); 


        }
    }
}
