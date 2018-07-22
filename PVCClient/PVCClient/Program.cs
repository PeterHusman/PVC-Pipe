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
            Dictionary<string, string> help = new Dictionary<string, string>
            {
                ["clone"] = "clone ORIGIN",
                ["pull"] = "pull",
                ["checkout"] = "checkout BRANCH",
                ["commit"] = "commit AUTHOR COMMITTER M E S S A G E",
                ["push"] = "push <BRANCH>",
                ["branch"] = "branch <BRANCHNAME> <COMMITID>",
                ["help"] = "help <COMMAND>",
                ["status"] = "status <BRANCH>",
                ["log"] = "log <NUMBER>",
                ["ignore"] = "ignore <SUBPATH1 SUBPATH2 ...>",
                ["unignore"] = "unignore SUBPATH1 SUBPATH2 ...",
                ["ignoreR"] = "ignoreR <R E G E X>"
            };
            var cmds2 = new Dictionary<string, Func<Task>>();
            var cmds = new Dictionary<string, Func<Task>>
            {
                ["clone"] = async () => { await interf.Clone(parameters[1]);/* await ReDraw(path, cmds2);*/ },
                ["pull"] = async () => Console.WriteLine((await interf.Pull()).ToString().Replace('_', ' ')),
                ["checkout"] = async () => await interf.Checkout(parameters[1]),
                ["commit"] = async () => await interf.Commit(input.Remove(0, parameters[0].Length + parameters[1].Length + parameters[2].Length + 3), parameters[1], parameters[2]),
                ["push"] = async () => await interf.Push(input.Remove(0,4).Replace(" ", "")),
                ["branch"] = async () => { if (parameters.Length > 1) { if (parameters.Length > 2) { await interf.CreateBranch(parameters[1], int.Parse(parameters[2])); } else { await interf.CreateBranch(parameters[1]); } } else { Console.WriteLine($"Current branch:\n - {await interf.GetCurrentBranch()}\nAll branches:" ); foreach (string s in await interf.GetAllBranches()) { Console.WriteLine(" - " + s); } } },
                ["help"] = async () => await Task.Run(() => { if (parameters.Length > 1 && help.ContainsKey(parameters[1])) { Console.WriteLine(help[parameters[1]]); } else { foreach (string s in help.Keys.ToArray()) { Console.WriteLine($"{s}{repeatChar(' ', 20 - s.Length)}{help[s]}"); } } }),
                ["status"] = async () => Console.WriteLine((await interf.GetStatus(parameters.Length > 1 ? parameters[1] : File.ReadAllText($@"{path}\.pvc\refs\HEAD"))).ToString().Replace('_', ' ')),
                ["log"] = async () => { string[] output = (parameters.Length > 1 ? await interf.Log(int.Parse(parameters[1])) : await interf.Log()); for (int i = 0; i < output.Length; i++) { if (output[i] != null) { Console.WriteLine(output[i]); } } },
                ["ignore"] = async () => { if (parameters.Length > 1) { await interf.IgnorePaths(input.Remove(0, 6).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)); } else { Console.WriteLine("Ignored subpaths:"); foreach (string s in interf.IgnoredPaths) { Console.WriteLine(s); } } },
                ["unignore"] = async () => await interf.UnignorePaths(input.Remove(0, 6).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)),
                ["ignoreR"] = async () => await interf.SetIgnoredRegex(input.Remove(0,8))
            };
            string repeatChar(char chr, int num)
            {
                StringBuilder s = new StringBuilder();
                for (int i = 0; i < num; i++)
                {
                    s.Append(chr);
                }
                return s.ToString();
            }
            cmds2 = cmds;
            Console.Clear();
            Console.WriteLine($"File path: {path}\nOrigin: {(File.Exists($@"{path}\.pvc\origin") ? File.ReadAllText($@"{path}\.pvc\origin") : "none")}");
            Console.WriteLine("Commands:");
            foreach (string cmd in cmds.Keys.ToArray())
            {
                Console.WriteLine("\t" + cmd);
            }


            while (true)
            {
                input = Console.ReadLine();
                parameters = input.Split(' ');
                try
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Exception thro = null;
                    await Task.Run(async () =>
                    {
                        try
                        {
                            await cmds[parameters[0]]();
                        }
                        catch (Exception e)
                        {
                            thro = e;
                        }
                    });
                    if (thro != null)
                    {
                        Fail(thro);
                    }
                    //await Task.Run(() => cmds[parameters[0]]);
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("Finished");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                catch (Exception e)
                {
                    Fail(e);
                }
                void Fail(Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: {e.Message}\nLocation: {e.StackTrace/*}");*/.Remove(e.StackTrace.IndexOf("--- End of"))}");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    if (help.ContainsKey(parameters[0]))
                    {
                        Console.WriteLine($"Proper command usage: {help[parameters[0]]}");
                    }
                    else
                    {
                        Console.WriteLine("Unsupported command");
                    }
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }



        }
        public static async Task ReDraw(string path, Dictionary<string, Func<Task>> cmds)
        {
            Console.Clear();
            Console.WriteLine($"File path: {path}\nOrigin: {(File.Exists($@"{path}\.pvc\origin") ? File.ReadAllText($@"{path}\.pvc\origin") : "none")}");
            Console.WriteLine("Commands:");
            foreach (string cmd in cmds.Keys.ToArray())
            {
                Console.WriteLine("\t" + cmd);
            }
        }
    }
}
