/////////////////////////////////////////////////////////////////////
// ITest.cs - interfaces for communication between system parts    //
//                                                                 //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * ITest.cs provides interfaces:
 * - ITestHarness   used by TestExec and Client
 * - ICallback      used by child AppDomain to send messages to TestHarness
 * - IRequestInfo   used by TestHarness
 * - ITestInfo      used by TestHarness
 * - ILoadAndTest   used by TestHarness
 * - ITest          used by LoadAndTest
 * - IRepository    used by Client and TestHarness
 * - IClient        used by TestExec and TestHarness
 * - IMessage       used by TestExec, TestHarness, TestWPF
 * - IStreamServer  used by TestHarness, TestWPF, Repository
 *
 * Author: Jiawei Wang
 * 
 * Required files:
 * ---------------
 * - ITest.cs
 * 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
namespace TestHarness
{
    /////////////////////////////////////////////////////////////
    // used by child AppDomain to send messages to TestHarness

    public interface ICallback
    {
        void sendMessage(Message msg);
    }
    public interface ITestHarness
    {
        void setClient(IClient client);
        void sendTestRequest(Message testRequest);
    }
    /////////////////////////////////////////////////////////////
    // used by child AppDomain to invoke test driver's test()

    public interface ITest
    {
        bool test();
        string getLog();
    }
    /////////////////////////////////////////////////////////////
    // used by child AppDomain to communicate with Repository
    // via TestHarness Comm

    public interface IRepository
    {
        bool getFiles(string path, string fileList);  // fileList is comma separated list of files
        
        List<string> queryLogs(string queryText);


    }
    /////////////////////////////////////////////////////////////
    // used by child AppDomain to send results to client
    // via TestHarness Comm

    public interface IClient
    {
        void sendResults(Message result);
    }
    /////////////////////////////////////////////////////////////
    // used by TestHarness to communicate with child AppDomain

    public interface ILoadAndTest
    {
        ITestResults test(IRequestInfo requestInfo);
        void setCallback(ICallback cb);
        void loadPath(string path);
    }
    // the interface of test information
    public interface ITestInfo
    {
        string testName { get; set; } //test Name
        List<string> files { get; set; } //test files, including test driver and test code
    }
    public interface IRequestInfo
    {
        List<ITestInfo> requestInfo { get; set; } // an array of test compose a request
    }
    public interface ITestResult //interface for testresult
    {
        string testName { get; set; }  
        string testResult { get; set; }
        string testLog { get; set; }
    }
    public interface ITestResults
    {
        string testKey { get; set; }
        DateTime dateTime { get; set; }
        List<ITestResult> testResults { get; set; }
    }

    [ServiceContract]
    public interface IMessage //IMessage is the interface between Client and TestHarness
    {
        [OperationContract(IsOneWay = true)]
        void PostMessage(TestHarness.Message msg);

        // used only locally so not exposed as service method
        TestHarness.Message GetMessage();
    }



    [ServiceContract(Namespace = "http://testHarness")]
    public interface IStreamService //IStreamService is the interface for the Repository Server
    {
        [OperationContract(IsOneWay = true)]
        void upLoadFile(FileTransferMessage msg);//upload files
        [OperationContract]
        Stream downLoadFile(string filename);//download files
        [OperationContract]
        string[] showfiles(string filetype); //show files according to file type
        [OperationContract]
        void deleteFile(string filename); //delete files according to file name
        [OperationContract]
         List<string> ClientQuery(string msg);//query the logs according to the keyword.
    }
    [MessageContract]
    public class FileTransferMessage //this class is for uploading files in IStreamService
    {
        [MessageHeader(MustUnderstand = true)]
        public string filename { get; set; }

        [MessageBodyMember(Order = 1)]
        public Stream transferStream { get; set; }
    }


}
