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
        static async Task Main(string[] args)
        {
            Console.WriteLine("PVC is a work-in-progress rudimentary VCS. Please enter the number of the repository or enter the full file path.");
            string rootPath = $@"C:\Users\{Environment.UserName}\Documents\PVCPipe\";
            string[] repos = Directory.GetDirectories(rootPath);
            for (int i = 0; i < repos.Length; i++)
            {
                Console.WriteLine($"{i} - {repos[i].Remove(0, rootPath.Length)}");
            }
            string path = Console.ReadLine();
            int selection;
            if (int.TryParse(path, out selection))
            {
                path = repos[selection];
            }
            PVCServerInterface interf = new PVCServerInterface(path);
            string[] parameters = new string[1];
            string input = "";
            Dictionary<string, string> help = new Dictionary<string, string> { { "clone", "clone ORIGIN" }, { "pull", "pull" } };
            var cmds = new Dictionary<string, Func<Task>>
            {
                ["clone"] = async () => await interf.Clone(parameters[1]),
                ["pull"] = async () => await interf.Pull(),
                ["checkout"] = async () => await interf.Checkout(parameters[1]),
                ["commit"] = async () => await interf.Commit(input.Remove(0, parameters[0].Length + parameters[1].Length + parameters[2].Length + 3), parameters[1], parameters[2]),
                ["push"] = async () => await interf.Push(),
                ["branch"] = async () => { if (parameters.Length > 2) { await interf.CreateBranch(parameters[1], int.Parse(parameters[2])); } else { await interf.CreateBranch(parameters[1]); } },
                ["help"] = async () => await Task.Run(() => Console.WriteLine(help[parameters[1]]))
            };
            while (true)
            {
                Console.Clear();
                Console.WriteLine(path);
                Console.WriteLine("Commands:");
                foreach (string cmd in cmds.Keys.ToArray())
                {
                    Console.WriteLine("\t" + cmd);
                }
                input = Console.ReadLine();
                parameters = input.Split(' ');
                try
                {
                    await Task.Run(() => cmds[parameters[0]]());
                }
                catch(Exception e)
                {
                    ;
                }
            }
        }
    }
}
