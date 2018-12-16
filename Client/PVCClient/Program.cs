using Newtonsoft.Json;
using PVCPipeLibrary;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PVCClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //pvc ARG -arg
            //Directory.GetCurrentDirectory()
            Account account = null;
            bool exit = false;
            while (!exit)
            {
                bool home = false;
                Console.WriteLine("PVC is a work-in-progress rudimentary VCS. Please enter the number of the repository or enter the full file path.");
                string rootPath = $@"C:\Users\{Environment.UserName}\Documents\PVCPipe\";
                if (File.Exists($@"{rootPath}\PVCPipeAccount"))
                {
                    string startingText = File.ReadAllText($@"{rootPath}\PVCPipeAccount");
                    StringBuilder output = new StringBuilder();
                    for (int i = 0; i < startingText.Length; i++)
                    {
                        output.Append((char)(((int)startingText[i] * -1) + 137));
                    }
                    account = JsonConvert.DeserializeObject<Account>(output.ToString());
                }
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
                    ["commit"] = "commit M E S S A G E",
                    ["revert"] = "revert <NUMBER OF COMMITS BEFORE HEAD>",
                    ["push"] = "push <BRANCH>",
                    ["branch"] = "branch BRANCHNAME <COMMITID>",
                    ["help"] = "help <COMMAND>",
                    ["status"] = "status <BRANCH>",
                    ["log"] = "log <NUMBER>",
                    ["ignore"] = "ignore <SUBPATH1 SUBPATH2 ...>",
                    ["unignore"] = "unignore SUBPATH1 SUBPATH2 ...",
                    ["ignoreR"] = "ignoreR <R E G E X>",
                    ["redraw"] = "redraw",
                    ["home"] = "home",
                    ["login"] = "login",
                    ["register"] = "register",
                    ["resetpass"] = "resetpass USERNAME",
                    ["changepass"] = "changepass"
                };
                var cmds2 = new Dictionary<string, Func<Task>>();
                var cmds = new Dictionary<string, Func<Task>>
                {
                    ["clone"] = async () => await interf.Clone(parameters[1]),
                    ["pull"] = async () => Console.WriteLine((await interf.Pull()).ToString().Replace('_', ' ')),
                    ["checkout"] = async () => Console.WriteLine(await interf.Checkout(parameters[1])),
                    ["commit"] = async () =>
                    {
                        if (account == null)
                        {
                            Console.WriteLine("You must log in to commit.");
                            return;
                        }
                        await interf.Commit(input.Remove(0, parameters[0].Length + 1), account.Username, account.Username, DateTime.Now);
                    },
                    ["revert"] = async () =>
                    {
                        if (account == null)
                        {
                            Console.WriteLine("You must log in to commit.");
                            return;
                        }
                        int id = -1;
                        string message = "Undoes previous commit.";
                        if(parameters.Length > 1)
                        {
                            int numBeforeHead = int.Parse(parameters[1]);
                            id = (await interf.Log(numBeforeHead))[numBeforeHead - 1].Parent;
                            message = $"Undoes commit {id}.";
                        }
                        await interf.RevertCommit(message, account.Username, account.Username, DateTime.Now, id);
                        Console.WriteLine(await interf.Checkout(await interf.GetCurrentBranch()));
                    },
                    ["push"] = async () => { var result = await interf.Push(input.Remove(0, 4).Replace(" ", "")); foreach (string s in result.Keys) { Console.WriteLine(s+": " + result[s].ToString().Replace("_", " ")); } },
                    ["branch"] = async () =>
                    {
                        if (parameters.Length > 1)
                        {
                            if (parameters.Length > 2)
                            {
                                await interf.CreateBranch(parameters[1], int.Parse(parameters[2]));
                            }
                            else
                            {
                                await interf.CreateBranch(parameters[1]);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Current branch:\n - {await interf.GetCurrentBranch()}\nAll branches:");
                            foreach (string s in await interf.GetAllBranches())
                            {
                                Console.WriteLine(" - " + s);
                            }
                        }
                    },
                    ["help"] = () =>
                    {
                        {
                            if (parameters.Length > 1 && help.ContainsKey(parameters[1]))
                            {
                                Console.WriteLine(help[parameters[1]]);
                            }
                            else
                            {
                                foreach (string s in help.Keys.ToArray())
                                {
                                    Console.WriteLine($"{s}{repeatChar(' ', 20 - s.Length)}{help[s]}");
                                }
                            }
                            return Task.CompletedTask;
                        }
                    },
                    ["status"] = async () => Console.WriteLine((await interf.GetStatus(parameters.Length > 1 ? parameters[1] : File.ReadAllText($@"{path}\.pvc\refs\HEAD"))).ToString().Replace('_', ' ')),
                    ["log"] = async () =>
                    {
                        var output = (parameters.Length > 1 ? await interf.Log(int.Parse(parameters[1])) : await interf.Log());
                        for (int i = 0; i < output.Length; i++)
                        {
                            if (output[i] != null)
                            {
                                Console.WriteLine(output[i].DateAndTime.ToShortDateString() + ": " + output[i].Message);
                            }
                        }
                    },
                    ["ignore"] = async () =>
                    {
                        if (parameters.Length > 1)
                        {
                            await interf.IgnorePaths(input.Remove(0, 6).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                        }
                        else
                        {
                            Console.WriteLine("Ignored subpaths:");
                            foreach (string s in interf.IgnoredPaths)
                            {
                                Console.WriteLine(s);
                            }
                        }
                    },
                    ["unignore"] = async () => await interf.UnignorePaths(input.Remove(0, 6).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)),
                    ["ignoreR"] = async () => await interf.SetIgnoredRegex(input.Remove(0, 8)),
                    ["redraw"] = async () => await RedrawCommand(account, path, cmds2),
                    ["home"] = () => { home = true; return Task.CompletedTask; },
                    ["login"] = async () =>
                    {
                        account = EnterAccountInfo();
                        interf.User = account;
                        Console.ForegroundColor = ConsoleColor.Gray;
                        await ReDraw(path, cmds2, account.Username);
                    },
                    ["register"] = async () =>
                    {
                        account = EnterAccountInfo();
                        Console.WriteLine("Please enter a recovery email.");
                        interf.User = account;
                        Console.WriteLine(await interf.RegisterAccount(interf.Origin.Split(new string[] { "/api/pipe" }, StringSplitOptions.None)[0] + "/api/pipe/register", Console.ReadLine()));
                    },
                    ["resetpass"] = async () => Console.WriteLine((await interf.ResetPassword(interf.Origin.Split(new string[] { "/api/pipe" }, StringSplitOptions.None)[0] + "/api/pipe/passwordReset", parameters[1])) ? "Success" : "Failure"),
                    ["changepass"] = async () =>
                    {
                        Console.WriteLine("Input new password");
                        Console.WriteLine((await interf.ChangePassword(interf.Origin.Split(new string[] { "/api/pipe" }, StringSplitOptions.None)[0] + "/api/pipe/changePass", SecureEnterText())) ? "Success" : "Failure");
                    }
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
                Console.WriteLine($"File path: {path}\nOrigin: {(File.Exists($@"{path}\.pvc\origin") ? File.ReadAllText($@"{path}\.pvc\origin") : "none")}\nLogged in as: {(account == null ? "None" : account.Username)}");
                Console.WriteLine("Commands:");
                foreach (string cmd in cmds.Keys.ToArray())
                {
                    Console.WriteLine("\t" + cmd);
                }

                while (true)
                {
                    input = Console.ReadLine();
                    if(input == "")
                    {
                        if (account != null)
                        {
                            string startingText = JsonConvert.SerializeObject(account);
                            StringBuilder output = new StringBuilder();
                            for (int i = 0; i < startingText.Length; i++)
                            {
                                output.Append((char)(137 - ((int)startingText[i])));
                            }
                            File.WriteAllText($@"{rootPath}\PVCPipeAccount", output.ToString());
                        }
                        exit = true;
                        return;
                    }
                    parameters = input.Split(' ');
                    try
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Exception thro = null;
                        await Task.Run(async () =>
                        {
                            try
                            {
                                await cmds[parameters[0].ToLower()]();
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
                        if (home)
                        {
                            break;
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
                        if (help.ContainsKey(parameters[0].ToLower()))
                        {
                            Console.WriteLine($"Proper command usage: {help[parameters[0].ToLower()]}");
                        }
                        else
                        {
                            Console.WriteLine("Unsupported command");
                        }
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                }
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Clear();
            }



        }

        private static async Task RedrawCommand(Account account, string path, Dictionary<string, Func<Task>> cmds2)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            await ReDraw(path, cmds2, account == null ? "None" : account.Username);
        }

        public static Account EnterAccountInfo()
        {
            Console.WriteLine("Please enter your username."); string username = Console.ReadLine();
            Console.WriteLine("Please enter your password.");
            string password = SecureEnterText();
            return new Account(username, password);
        }

        private static string SecureEnterText()
        {
            string password = "";
            ConsoleKeyInfo key = new ConsoleKeyInfo('\x008', ConsoleKey.Backspace, false, false, false);
            do
            {
                if (Console.KeyAvailable)
                {
                    key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.Backspace:
                            if (password.Length > 0) { password = password.Remove(password.Length - 1); }
                            break;
                        case ConsoleKey.Enter: break;
                        default:
                            password += key.KeyChar; break;
                    }
                }
            }
            while (key.Key != ConsoleKey.Enter);
            return password;
        }

        public static Task ReDraw(string path, Dictionary<string, Func<Task>> cmds, string username)
        {
            Console.Clear();
            Console.WriteLine($"File path: {path}\nOrigin: {(File.Exists($@"{path}\.pvc\origin") ? File.ReadAllText($@"{path}\.pvc\origin") : "none")}\nLogged in as: {username}");
            Console.WriteLine("Commands:");
            foreach (string cmd in cmds.Keys.ToArray())
            {
                Console.WriteLine("\t" + cmd);
            }
            return Task.CompletedTask;
        }
    }
}
