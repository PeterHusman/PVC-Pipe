using PVCPipeClient;
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
            Console.WriteLine("PVC is a work-in-progress rudimentary VCS. Please enter the path to the folder containing the repository.");
            string path = Console.ReadLine();
            PVCServerInterface interf = new PVCServerInterface(path);
            string[] parameters = new string[1];
            Dictionary<string, Action> cmds = new Dictionary<string, Action> { { "clone", ()=>interf.Clone(parameters[1]) }, { "pull", async()=>await interf.Pull() }, { "checkout", () => interf.Checkout(parameters[1]) }, { "commit", () => interf.Commit(parameters[1], parameters[2], parameters[3]) }, { "push", async () => await interf.Push() }, { "branch", () => { if (parameters.Length > 2) { interf.CreateBranch(parameters[1], int.Parse(parameters[2])); } else { interf.CreateBranch(parameters[1]); } } } };
            while(true)
            {
                Console.Clear();
                Console.WriteLine(path);
                Console.WriteLine("Commands:");
                foreach(string cmd in cmds.Keys.ToArray())
                {
                    Console.WriteLine("\t" + cmd);
                }
                string input = Console.ReadLine();
                parameters = input.Split(' ');
                cmds[parameters[0]]();
            }
        }
    }
}
