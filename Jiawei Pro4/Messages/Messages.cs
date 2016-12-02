/////////////////////////////////////////////////////////////////////
// Messages.cs - defines communication messages                    //
// ver 1.0                                                         //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * Messages provides helper code for building and parsing XML messages.
 * Messages save all the strings of  request files, and test Harness will use the attribute ResultLogs for sending back to Client
 *
 * Required files:
 * ---------------
 * - Messages.cs
 * 
 * Author :Jiawei Wang
 * Maintanence History:
 * --------------------
 * ver 1.0 :19 Nov 2016
 * - first release
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace TestHarness
{
    [Serializable]
    public class Message
    {
        public string to { get; set; }
        public string from { get; set; }
        public string author { get; set; } = "";
        public DateTime time { get; set; } = DateTime.Now; 
        public string body { get; set; } = "";
        public string port { get; set; }// it is the port of Client
        public List<string> ResultLogs = new List<string>(); //it is the variables for the testing result
        public string localDir; // it is the variables to save the Key{Author}+{TIME}
        public testRequest tr; //This is for test request information


// all the following code is referred to Prof. Fawcett's Project 2 Demo
        public Message(string bodyStr = "")
        {
            body = bodyStr;
        }
  
        public override string ToString()
        {
            string temp = "to: " + to;
            temp += ", from: " + from;
            if (author != "")
                temp += ", author: " + author;
            temp += ", time: " + time;
            temp += ", body:\n" + body;
            return temp;
        }
        public Message copy(Message msg)
        {
            Message temp = new Message();
            temp.to = msg.to;
            temp.from = msg.from;
            temp.author = msg.author;
            temp.time = DateTime.Now;
            temp.body = msg.body;
            return temp;
        }
    }
    //Test Elment is the class to save test driver, test code, test Name and so on, 
    // this class will be a part of Message.
    [Serializable]
    public class testElement
    {
        public string testName { get; set; }
        public string testDriver { get; set; }
        public List<string> testCodes { get; set; } = new List<string>();
        public testElement(string name)
        {
            testName = name;
        }
        public void addDriver(string name)
        {
            testDriver = name;
        }
        public void addCode(string name)
        {
            testCodes.Add(name);
        }
        public override string ToString()
        {
            string temp = "<test name=\"" + testName + "\">";
            temp += "<testDriver>" + testDriver + "</testDriver>";
            foreach (string code in testCodes)
                temp += "<library>" + code + "</library>";
            temp += "</test>";
            return temp;
        }
    }
    //A couple of test Element consist of testRequest.
    [Serializable]
    public class testRequest
    {
        public string author { get; set; }
        public List<testElement> tests { get; set; } = new List<testElement>();
        public override string ToString()
        {
            string temp = "<testRequest>";
            foreach (testElement te in tests)
                temp += te.ToString();
            temp += "</testRequest>";
            temp = "\n" + temp.formatXml(4);
            return temp;
        }
    }

    public static class extMethods
    {
        public static void show(this Message msg)
        {
            Console.Write("\n  formatted message:");
            string[] lines = msg.ToString().Split(',');
            foreach (string line in lines)
                Console.Write("\n    {0}", line.Trim());
            Console.WriteLine();
        }
        public static string shift(this string str, int n = 2)
        {
            string insertString = new string(' ', n);
            string[] lines = str.Split('\n');
            for (int i = 0; i < lines.Count(); ++i)
            {
                lines[i] = insertString + lines[i];
            }
            string temp = "";
            foreach (string line in lines)
                temp += line + "\n";
            return temp;
        }
        public static string formatXml(this string xml, int n = 2)
        {
            XDocument doc = XDocument.Parse(xml);
            return doc.ToString().shift(n);
        }
    }

    public class TestMessages
    {
        static void Main()
        {
            //Test Functions in Message
            Message Test = getMsg();
            Console.Write(Test.ToString());
            Message Test2 = Test.copy(Test);
            Console.Write(Test2.ToString());
        }
        public static Message getMsg()
        {

            Message msg = new Message();
            testElement te1 = new testElement("test1");
            te1.addDriver("TestDriver.dll");
            te1.addCode("TestedCode.dll");
            testElement te2 = new testElement("test2");
            te2.addDriver("AnotherTestDriver.dll");
            te2.addCode("AnotherTestedCode.dll");
            testRequest tr = new testRequest();
            tr.author = "Jim Fawcett";
            tr.tests.Add(te1);
            tr.tests.Add(te2);
            msg.tr = tr;
            msg.body = tr.ToString();
            return msg;
        }
    }
}
