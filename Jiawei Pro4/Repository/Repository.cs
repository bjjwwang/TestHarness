/////////////////////////////////////////////////////////////////////
// Repository.cs - holds test code for TestHarness                 //
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
 * - Client.cs, ITest.cs, Logger.cs, FileService.cs, Timer.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.ServiceModel;
using System.Threading;

namespace TestHarness
{
    public class Repository : IRepository
    {




        string repoStoragePath = "..\\..\\..\\Repository\\RepositoryStorage\\";

        public Repository()
        {
            Console.Write("\n  Creating instance of Repository");
        }
        //----< search for text in log files >---------------------------
        /*
         * This function should return a message.  I'll do that when I
         * get a chance.
         */
        public List<string> queryLogs(string queryText)
        {
            List<string> queryResults = new List<string>();
            string path = System.IO.Path.GetFullPath(repoStoragePath);
            string[] files = System.IO.Directory.GetFiles(repoStoragePath, "*.txt");
            foreach (string file in files)
            {
                string contents = File.ReadAllText(file);
                if (contents.Contains(queryText))
                {
                    string name = System.IO.Path.GetFileName(file);
                    queryResults.Add(name);
                }
            }
            return queryResults;
        }
        //----< send files with names on fileList >----------------------
        /*
         * This function is not currently being used.  It may, with a
         * Message interface, become part of Project #4.
         */
        public bool getFiles(string path, string fileList)
        {
            string[] files = fileList.Split(new char[] { ',' });
            //string repoStoragePath = "..\\..\\RepositoryStorage\\";

            foreach (string file in files)
            {
                string fqSrcFile = repoStoragePath + file;
                string fqDstFile = "";
                try
                {
                    fqDstFile = path + "\\" + file;
                    File.Copy(fqSrcFile, fqDstFile);
                }
                catch
                {
                    Console.Write("\n  could not copy \"" + fqSrcFile + "\" to \"" + fqDstFile);
                    return false;
                }
            }
            return true;
        }
        //----< intended for Project #4 >--------------------------------


        static public void ForUserUpload() //This Channel is for user to upload files.
        {
            ServiceHost host = StreamService.CreateServiceChannel("http://localhost:4061/StreamService");//4061 is the port of repository
            host.Open();
            Console.Write("\n  Press key to terminate service:\n");
            Console.ReadKey();
            Console.Write("\n");
            host.Close();
        }



#if (TEST_REPOSITORY)
        static void Main(string[] args)
        {
            /*
             * ToDo: add code to test 
             * - Test code in Repository class that sends files to TestHarness.
             * - Modify TestHarness code that now copies files from RepositoryStorage folder
             *   to call Repository.getFiles.
             * - Add code to respond to client queries on files and logs.
             * - Add RepositoryTest class that implements ITest so Repo
             *   functionality can be tested in TestHarness.
             */
            Console.Write("\n  Demonstrating Repository - Project #4 with Threading");
            Console.Write("\n =======================================================");

            Thread foruserupload = new Thread(ForUserUpload);  //create a thread for waiting for file request from Clients or Test harenss.
            foruserupload.Start();


                
        }
#endif
    }
}
