/////////////////////////////////////////////////////////////////////
// TestEntry.cs - sends TestRequests, displays results                //
//                                                                 //
// Jim Fawcett, CSE681 - Software Modeling and Analysis, Fall 2016 //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * The Entry of the whole solution.
 * Create Repository, TestHarness
 * Create two Clients concurrently
 * 
 * Author: Jiawei Wang
 * 
 * Required Files:
 * - Repository.exe, TestExecutive.exe, TestWPF.exe
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
using System.Diagnostics;
namespace TestHarnessEntry
{
    class Program
    {
        static void Main(string[] args)
        {
            Process.Start("Repository.exe");
            Process.Start("TestExecutive.exe");
            Process.Start("TestWPF.exe"); // we start two clients
            Process.Start("TestWPF.exe");
            

        }
    }
}
