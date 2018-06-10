using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVCClient
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("PVC is a work-in-progress rudimentary VCS. Commands:\nWIP clone\nWIP commit\nWIP log\nWIP push\nWIP pull\nWIP checkout\nPlease enter the path to the folder containing the repository.");
            string path = Console.ReadLine();
            string path2 = Console.ReadLine();
            Directory.Move(path, path2);
        }
    }
}
