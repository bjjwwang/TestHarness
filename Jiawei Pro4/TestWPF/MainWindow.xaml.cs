/////////////////////////////////////////////////////////////////////
// MainWindow.cs - holds test code for TestHarness                 //
//                                                                 //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * TestWPF is the package to implement WPF. 
 * And Send test request to the test Harness Server
 * Get connected to Test Harness and Repository
 * User Guide Document is provided.
 * 
 * Required Files:
 * - TestExec.cs, Repository.cs, Message.cs, ITest.cs, Timer.cs, FileClient.cs
 * 
 * Author: Jiawei Wang
 * Maintenance History:
 * --------------------
 * ver 1.0 : 20 Nov 2016
 * - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

namespace TestWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        TestHarness.ReceiverMsg recvr;  //This is the receiver for the channel between Client and Test Harness
        TestHarness.SenderMsg sndr;   // This is the sender for the channel between Client and Test Harnessl
        TestHarness.Message rcvdMsg; //This is Message for receving the result 
        TestHarness.Message TestRequest; // This is the Message for sending Test Request
        HiResTimer TestTime;
        TestHarness.FileClient clnt = new TestHarness.FileClient(); // This is a instance to connect to repository
        

        Thread rcvThrd = null; // This is the Thread which waits for the result from Test Harness Server

        delegate void NewMessage(TestHarness.Message msg); //Delegate for receiving results.
        event NewMessage OnNewMessage;

        void ThreadProc()//This thread is for receiving results from Test Harness,
        {
            while (true)
            {
                // get result out of receive queue - will block if queue is empty
                rcvdMsg = recvr.GetMessage();

                // call window functions on UI thread
                this.Dispatcher.BeginInvoke(
                  System.Windows.Threading.DispatcherPriority.Normal,
                 OnNewMessage,
                rcvdMsg);
            }
        }

        //----< called by UI thread when dispatched from rcvThrd >-------
        void OnNewMessageHandler(TestHarness.Message msg)
        {
            Console.Write("\nReceiving Test Result from Test Harness.");
            ConsoleBox.Text = "";//If there is a result received
            if (msg.body.Contains("does Not Exist in Repository")) //if file does not exist, alert
            {
                System.Windows.Forms.MessageBox.Show(msg.body);
                Console.Write("\n  File Not Exist in Repository --------Project 4. Req 3");
                return;
            }
            foreach (string line in msg.ResultLogs)
            {
                ConsoleBox.Text = ConsoleBox.Text + line + "\n";//put the result to the Text Box
            }
            SendMsg.IsEnabled = true;
            TestTime.Stop();
            Console.WriteLine("The test time is "+TestTime.ElapsedMicroseconds + "--------Project 4. Req 12");
            Console.Write("\n  Test Harness Server send back Result to Client --------Project 4. Req 6 and 7");
        }
        public MainWindow() //Constructor
        {
            InitializeComponent();
            OnNewMessage += new NewMessage(OnNewMessageHandler);
            
            Console.Write("\n  Demonstrating Client {0} - Project #4 with Threading", Process.GetCurrentProcess().Id.ToString());
            Console.Write("\n =======================================================");

            TestRequest = new TestHarness.Message(); // This message is prepared for the new Test Request
            TestRequest.tr = new TestHarness.testRequest();
            TestRequest.to = "TH";
            TestRequest.from = "CL";
            

            string localPort = Process.GetCurrentProcess().Id.ToString(); // the process ID is the port. And this port will be inserted into the message
            TestRequest.port = localPort;
            string endpoint = "http://localhost:" + localPort + "/IMessage";
            try
            {
                recvr = new TestHarness.ReceiverMsg();
                recvr.CreateRecvChannel(endpoint); //create the channel and start the receive thread.

                // create receive thread which calls rcvBlockingQ.deQ() (see ThreadProc above)
                rcvThrd = new Thread(new ThreadStart(this.ThreadProc));
                rcvThrd.IsBackground = true;
                rcvThrd.Start();
                connect.IsEnabled = true;
                SendMsg.IsEnabled = false;
                Browse.IsEnabled = false;
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
        }

        private void connect_Click(object sender, RoutedEventArgs e) //click and connect to the TH and Repo
        {
            string remoteAddress = "http://localhost:";
            string remotePort = "4051";      // We set the port of Test Harness 4051, repository 4061
            string endpoint = remoteAddress  + remotePort + "/IMessage"; //connect to Test Harness

            sndr = new TestHarness.SenderMsg(endpoint);
            Console.Write("\n Successfully connect to Test Harness.");
            remotePort = "4061";
            endpoint = remoteAddress + remotePort + "/StreamService"; // connect to Repository

            int count = 0;
            while (true)
            {
                try
                {
                    clnt.channel = TestHarness.FileClient.CreateServiceChannel(endpoint); 
                    Console.Write("\n Successfully connect to Repository.");
                    connect.IsEnabled = false;
                    SendMsg.IsEnabled = true; // if two servers are connected, release two buttons
                    Browse.IsEnabled = true;
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



        private void Browse_Click(object sender, RoutedEventArgs e) //This button is for choosing files and uploading to repository
        {
            try
            {
                string[] files = OpenDialog(); // open dialog and choose files, support multi-choice.
                foreach (string file in files)
                {

                    if (!clnt.uploadFile(file))  // send the files, uploadFile() is the function defined in the interface
                        Console.Write("\n  could not send file");
                }
            }
            catch { }
        }
        private string[] OpenDialog()  //this function is for choosing files
        {
            OpenFileDialog ofd = new OpenFileDialog();
            
            ofd.Title = "Select Files";
            string[] files = new string[50];
            ofd.Multiselect = true;  // support multi- choice.
            if (ofd.ShowDialog()== System.Windows.Forms.DialogResult.OK)
            {
                
                files = ofd.SafeFileNames;
                string path = System.IO.Path.GetDirectoryName(ofd.FileName);
                clnt.ToSendPath = path;  // we get the filename and folder, and then set the ToSendPath, their combination is the full path.
                return files;
            }
            else
            {
                return files; //if no choose, return empty string[]
            }
        }

        private void dllButton_Checked(object sender, RoutedEventArgs e) //it is radio button, click this and show all .dll files
        {
            try
            {
                string[] files = clnt.channel.showfiles("*.dll"); //showfiles() is the function in the interface, return all the .dll filenames.
                RepolistBox.Items.Clear(); 
                foreach (string file in files)
                {
                    string f = System.IO.Path.GetFileName(file); //remove the path, only show filename
                    RepolistBox.Items.Add(f); //insert the files into the listbox, and user can select and do another operations
                }
            }
            catch { }
        }


        private void logButton_Checked(object sender, RoutedEventArgs e) //it is radio button, click this and show all .txt files
        {
            try
            {
                string[] files = clnt.channel.showfiles("*.txt");//showfiles() is the function in the interface, return all the .dll filenames.
                RepolistBox.Items.Clear();
                foreach (string file in files)
                {
                    string f = System.IO.Path.GetFileName(file); //remove the path, only show filename
                    RepolistBox.Items.Add(f);//insert the files into the listbox, and user can select and do another operations
                }
            }
            catch { }
        }

        private void DownloadRepo_Click(object sender, RoutedEventArgs e) //Download the selected file
        {
            try
            {
                string filename = (string)RepolistBox.SelectedItem;
                FolderBrowserDialog folderDlg = new FolderBrowserDialog(); //show a dialog for users to choose a folder
                folderDlg.ShowDialog();
                string path = folderDlg.SelectedPath;
                if (path == "") return;
                clnt.SavePath = path;  // set the SavePath of clnt, and download files to this folder.
                clnt.download(filename);
            }
            catch {

            }
        }

        private void DeleteRepo_Click(object sender, RoutedEventArgs e) //delete the selected item
        {
            string filename = (string)RepolistBox.SelectedItem;
            if (filename != null)
            {
                RepolistBox.Items.Remove(RepolistBox.SelectedItem); //remove the selected item from the list box, which means this file has been deleted.
                clnt.channel.deleteFile(filename); //deleteFile is the function in the interface, which deletes files in the server.
            }
        }

        private void allButton_Checked(object sender, RoutedEventArgs e)//it is radio button, click this and show all *.* files
        {
            try
            {
                string[] files = clnt.channel.showfiles("*.*");//showfiles() is the function in the interface, return all the .dll filenames.
                RepolistBox.Items.Clear();
                foreach (string file in files)
                {
                    string f = System.IO.Path.GetFileName(file);//remove the path, only show filename
                    RepolistBox.Items.Add(f);//insert the files into the listbox, and user can select and do another operations
                }
            }
            catch { }
        }



        private void Search_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                QueryResBox.Items.Clear(); // Clean the listbox for the new search.
                List<string> result = clnt.channel.ClientQuery(SearchText.Text); //ClientQuery() is the function in the interface, which returns the matched logs
                foreach (string item in result)
                {
                    QueryResBox.Items.Add(item); // add these log files into listbox
                }
                Console.Write("\n  Client query logs from Repository, and the KeyWord is "+ SearchText.Text+"--------Project 4. Req 9");
            }
            catch {

            }
        }

        private void Download_Click(object sender, RoutedEventArgs e) //download queried logs from repository
        {
            try
            {
                string filename = (string)QueryResBox.SelectedItem; 
                FolderBrowserDialog folderDlg = new FolderBrowserDialog();//show a dialog for users to choose a folder
                folderDlg.ShowDialog();

                string path = folderDlg.SelectedPath;
                if (path == "") return;
                clnt.SavePath = path;// set the SavePath of clnt, and download files to this folder.
                clnt.download(filename);
            }
            catch
            {

            }
        }

        private void DeleteLog_Click(object sender, RoutedEventArgs e) //delete the selected item
        {
            string filename = (string)QueryResBox.SelectedItem;
            if (filename != null)
            {
                RepolistBox.Items.Remove(QueryResBox.SelectedItem);//remove the selected item from the list box, which means this file has been deleted.
                clnt.channel.deleteFile(filename);
            }
            QueryResBox.Items.Remove(QueryResBox.SelectedItem); //deleteFile is the function in the interface, which deletes files in the server.
        }

        private void DownloadAll_Click(object sender, RoutedEventArgs e)//download all the matched logs from repository
        {
            try
            {
                FolderBrowserDialog folderDlg = new FolderBrowserDialog();
                folderDlg.ShowDialog();
                string path = folderDlg.SelectedPath; //user select the folder
                if (path == "") return;
                clnt.SavePath = path;
                foreach (object item in QueryResBox.Items)//traverse all the items in the listbox, and download all of them.
                {
                    string filename = (string)item;
                    clnt.download(filename);
                }
            }
            catch { }
        }

        private void Add_Code_Click(object sender, RoutedEventArgs e)
        {
            TestCodeList.Items.Add((object)tstCdBox.Text); //we type the test code file into the TextBox
        }

        private void Add_Test_Click(object sender, RoutedEventArgs e)
        {
            if (testNameBox.Text == "" || tstDrBox.Text=="")
            {
                System.Windows.Forms.MessageBox.Show("No Test Name or Test Driver!");
                return;
            }
            
            TestHarness.testElement te = new TestHarness.testElement(testNameBox.Text);
            te.addDriver(tstDrBox.Text);
            TestList.Items.Add((object)testNameBox.Text);
            foreach (object code in TestCodeList.Items)// we add several test code into the listbox, and add one test driver.
            {
                te.addCode((string)code);// all the information will be inserted into Test Request message.
            }
            
            
            TestRequest.tr.tests.Add(te);
        }

        private void SendMsg_Click(object sender, RoutedEventArgs e)
        {
            TestRequest.author = authorBox.Text;  // we set the author 
            TestRequest.body = TestRequest.tr.ToString(); // put all xml forms to the body, but this part will not be parsed in the repository
            Console.Write("\n  Sending Test Request to Test Harness through WCF --------Project 4. Req 10");
            sndr.PostMessage(TestRequest);  // post message
            SendMsg.IsEnabled = false; //invalid the sendMsg button until receiving the result.
            TestTime = new HiResTimer();
            TestTime.Start();
        }

        private void Clean_Code_Click(object sender, RoutedEventArgs e)
        {
            TestCodeList.Items.Clear(); // clean all the test code
        }

        private void Clean_Test_Click(object sender, RoutedEventArgs e) //clean all the test
        {
            TestRequest.tr = new TestHarness.testRequest();
            TestList.Items.Clear();
            TestCodeList.Items.Clear();
        }
    }
}
