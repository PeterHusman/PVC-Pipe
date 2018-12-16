using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using PVCPipeLibrary;
using CredentialManagement;
using System.ComponentModel.DataAnnotations;
using static PVCPipeLibrary.PVCServerInterface;

namespace PVCCommandLine
{
    [Command("pvc")]
    [Subcommand("commit", typeof(CommitCommand))]
    [Subcommand("login", typeof(LogInCommand))]
    [Subcommand("logout", typeof(LogOutCommand))]
    [Subcommand("clone", typeof(CloneCommand))]
    [Subcommand("pull", typeof(PullCommand))]
    [Subcommand("push", typeof(PushCommand))]
    [Subcommand("branch", typeof(BranchCommand))]
    class PVC : PVCCommandBase
    {

        //public static IEnumerable<int> items(int param)
        //{
        //   if (param > 42)
        //    {
        //        throw new InvalidOperationException();
        //    }

        //    IEnumerable<int> itemsInternal()
        //    {
        //        yield return 42;
        //    }


        //    return itemsInternal();
        //}

        //static Task ThisThrows(int param)
        //{
        //    if (param > 42)
        //    {
        //        throw new InvalidOperationException("Don't call this");
        //    }

        //    async Task ThisThrowsInternal()
        //    {
        //        await Task.Delay(500);
        //        await Task.Delay(800);
        //    }

        //    return ThisThrowsInternal();
        //}

        //static async Task ThisThrowsAsync(int param)
        //{
        //    if (param > 42)
        //    {
        //        throw new InvalidOperationException("Don't call this");
        //    }
        //    await Task.Delay(500);
        //}

        static async Task Main(string[] args)
        {
            await CommandLineApplication.ExecuteAsync<PVC>(args);
            Console.ReadKey();
        }

        //https://github.com/natemcmaster/CommandLineUtils/blob/master/docs/samples/subcommands/inheritance/Program.cs
        //CredentialCache
        //NetworkCredential

        public Credential GetCredential()
        {
            Credential credential = new Credential { Target = "pvc-pipe" };
            if (!credential.Load())
            {
                Creds = null;
                return Creds;
            }
            Creds = credential;
            return Creds;
        }

        public void DeleteCredential()
        {
            new Credential { Target = "pvc-pipe" }.Delete();
            Creds = null;
        }

        public Credential SetCredential(string username, string password)
        {

            Creds = new Credential { Target = "pvc-pipe", Username = username, Password = password, PersistanceType = PersistanceType.LocalComputer };
            Creds.Save();
            return Creds;
        }

        [Option("--dir=<path>")]
        [DirectoryExists]
        public string PVCDir { get; set; }

        //public string Command { get; set; }

        public Credential Creds;

        public PVCServerInterface CreateInterface()
        {
            return new PVCServerInterface(string.IsNullOrEmpty(PVCDir) ? Directory.GetCurrentDirectory() : PVCDir);
        }

        protected override Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            app.ShowHelp();
            return Task.FromResult(0);
        }

        public override List<string> CreateArgs()
        {
            List<string> args = new List<string>();
            if (PVCDir != null)
            {
                args.Add("--dir=" + PVCDir);
            }
            return args;
        }
    }

    [Command(Description = "Create a branch")]
    class BranchCommand : PVCCommandBase
    {
        private PVC Parent { get; set; }

        [Required]
        [Option()]
        public string Branch { get; set; }

        [Option("--id")]
        public int CommitId { get; set; }

        public override List<string> CreateArgs()
        {
            var args = Parent.CreateArgs();
            args.Add("branch");
            if(Branch != null)
            {
                args.Add(Branch);
            }
            if(CommitId > 0)
            {
                args.Add("--id");
                args.Add(CommitId.ToString());
            }
            return args;
        }

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            var interf = Parent.CreateInterface();
            if (CommitId <= 0)
            {
                await interf.CreateBranch(Branch, CommitId);
            }
            else
            {
                await interf.CreateBranch(Branch);
            }
            return 0;
        }
    }



    [Command(Description = "Sign out")]
    class LogOutCommand : PVCCommandBase
    {
        private PVC Parent { get; set; }

        public override List<string> CreateArgs()
        {
            List<string> args = Parent.CreateArgs();
            args.Add("logout");
            return args;
        }

        protected override Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            Parent.DeleteCredential();
            return Task.FromResult(0);
        }
    }

    [Command(Description = "Pull the repository")]
    class PullCommand : PVCCommandBase
    {
        private PVC Parent { get; set; }

        public override List<string> CreateArgs()
        {
            var args = Parent.CreateArgs();
            args.Add("pull");
            return args;
        }

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            var interf = Parent.CreateInterface();
            Console.WriteLine((await interf.Pull()).ToString().Replace('_', ' '));
            return 0;
        }
    }

    [Command(Description = "Push the repository")]
    class PushCommand : PVCCommandBase
    {
        private PVC Parent { get; set; }

        [Option("-b")]
        public string Branch { get; set; }

        public override List<string> CreateArgs()
        {
            var args = Parent.CreateArgs();
            args.Add("push");
            if (Branch != null)
            {
                args.Add("-b");
                args.Add(Branch);
            }
            return args;
        }

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            var interf = Parent.CreateInterface();
            Parent.GetCredential();
            if(Parent.Creds == null)
            {
                Console.WriteLine("You must log in to push.");
                return 0;
            }
            Dictionary<string, PushResult> resp;
            interf.User = new Account(Parent.Creds.Username, Parent.Creds.Password);
            if (string.IsNullOrEmpty(Branch))
            {
                resp = await interf.Push();
            }
            else
            {
                resp = await interf.Push(Branch);
            }
            foreach (string s in resp.Keys)
            {
                Console.WriteLine(s + ": " + resp[s].ToString().Replace("_", " "));
            }
            return 0;
        }
    }

    [Command(Description = "Sign in. Only authenticates when a command which requires an account is run")]
    class LogInCommand : PVCCommandBase
    {
        private PVC Parent { get; set; }

        public override List<string> CreateArgs()
        {
            List<string> args = Parent.CreateArgs();
            args.Add("login");
            return args;
        }

        protected override Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            Console.Write("Username: ");
            string username = Console.ReadLine();
            Console.Write("Password: ");
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
            Console.WriteLine();
            Parent.SetCredential(username, password);
            return Task.FromResult(0);
        }
    }

    [Command(Description = "Clone from a repository")]
    class CloneCommand : PVCCommandBase
    {
        private PVC Parent { get; set; }

        [Url]
        [Required]
        [Option()]
        public string Origin { get; set; }

        public override List<string> CreateArgs()
        {
            List<string> args = Parent.CreateArgs();
            args.Add("clone");
            if (Origin != null)
            {
                args.Add(Origin);
            }
            return args;
        }

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            PVCServerInterface interf = new PVCServerInterface(string.IsNullOrEmpty(Parent.PVCDir) ? Directory.GetCurrentDirectory() : Parent.PVCDir);
            if (string.IsNullOrEmpty(Origin))
            {
                Console.WriteLine("The --origin field is required.");
                return 1;
            }
            await interf.Clone(Origin);
            return 0;
        }
    }

    [Command(Description = "Commit changes to a repository")]
    class CommitCommand : PVCCommandBase
    {

        [Option("-m")]
        public string Message { get; set; }

        [Option("-a")]
        public string Author { get; set; }

        private PVC Parent { get; set; }

        public override List<string> CreateArgs()
        {
            List<string> args = Parent.CreateArgs();
            args.Add("commit");
            if (Message != null)
            {
                args.Add("-m");
                args.Add(Message);
            }
            if (Author != null)
            {
                args.Add("-a");
                args.Add(Author);
            }
            return args;
        }

        protected override async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            PVCServerInterface interf = new PVCServerInterface(string.IsNullOrEmpty(Parent.PVCDir) ? Directory.GetCurrentDirectory() : Parent.PVCDir);
            var message = app.Option("-m <MESSAGE>", "The message", CommandOptionType.SingleValue);
            var author = app.Option("-a <AUTHOR>", "The author", CommandOptionType.SingleValue);
            Parent.GetCredential();
            if (string.IsNullOrEmpty(Message))
            {
                Console.WriteLine("A commit must have a message.");
                return 0;
            }
            if (string.IsNullOrEmpty(Author))
            {
                if (Parent.Creds == null)
                {
                    Console.WriteLine("A commit must have an author. This can be specified either by using the -a option or by logging in.");
                    return 0;
                }
                Author = Parent.Creds.Username;
            }
            await interf.Commit(Message, Author, Author, DateTime.Now);
            return 0;
        }
    }

    [HelpOption("--help")]
    abstract class PVCCommandBase
    {
        public abstract List<string> CreateArgs();

        protected virtual Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            var args = CreateArgs();

            Console.WriteLine("Result = pvc " + ArgumentEscaper.EscapeAndConcatenate(args));
            return Task.FromResult(0);
        }
    }
}
