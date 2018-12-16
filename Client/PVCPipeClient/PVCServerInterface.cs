using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PVCPipeLibrary
{
    public partial class PVCServerInterface
    {
        class Folder
        {
            public string Path;
            public Folder[] Folders;
            public FileObj[] Files;
        }
        class FileObj
        {
            public string Path;
            public string Contents;

            public FileObj(string path, string contents)
            {
                Path = path;
                Contents = contents;
            }
        }


        class Diff
        {
            public int Position;
            public int NumberToRemove;
            public string ContentToAdd;
            public Diff(int pos, int numToRem, string toAdd)
            {
                Position = pos;
                NumberToRemove = numToRem;
                ContentToAdd = toAdd;
            }
        }

        public Account User { get; set; }

        public Task<string> GetCurrentBranch()
        {
            var result = File.ReadAllText($@"{Path}\.pvc\refs\HEAD");
            return Task.FromResult(result);
        }

        public Task<string[]> GetAllBranches()
        {
            string[] temp = Directory.EnumerateFiles($@"{Path}\.pvc\refs\branches").ToArray();
            for (int i = 0; i < temp.Length; i++)
            {
                temp[i] = temp[i].Remove(0, $@"{Path}\.pvc\refs\branches\".Length);
            }
            return Task.FromResult(temp);
        }


        Task<Diff[]> GetDiffs(string left, string right)
        {
            List<Diff> diffs = new List<Diff>();
            //await Task.Run(() =>
            //{
                //Try checking each character of right against left until there is a mismatch. Then maybe recursive call on shortened file?
                //New strat: For now, trying out comparing lines. Check line against line
                string[] leftLines = left.Split('{');
                string[] rightLines = right.Split('{');
                for (int i = 1; i < leftLines.Length; i++)
                {
                    leftLines[i] = '{' + leftLines[i];
                }
                for (int i = 1; i < rightLines.Length; i++)
                {
                    rightLines[i] = '{' + rightLines[i];
                }
                int loopLength = leftLines.Length > rightLines.Length ? leftLines.Length : rightLines.Length;
                for (int i = 0; i < loopLength; i++)
                {
                    string strLeft = i < leftLines.Length ? leftLines[i] : null;
                    string strRight = i < rightLines.Length ? rightLines[i] : null;
                    if (strLeft != strRight)
                    {
                        if (strLeft == null)
                        {
                            diffs.Add(new Diff(cumulativeLength(rightLines, leftLines.Length) + cumulativeLength(rightLines, i, leftLines.Length), 0, strRight));
                        }
                        else if (strRight == null)
                        {
                            diffs.Add(new Diff(cumulativeLength(rightLines, rightLines.Length), leftLines[i].Length, ""));
                        }
                        else
                        {
                            diffs.Add(new Diff(cumulativeLength(rightLines, i), leftLines[i].Length, strRight));
                        }
                    }
                }
                int cumulativeLength(string[] array, int endingIndexPlusOne, int startingIndex = 0)
                {
                    int cumLen = 0;
                    for (int i = startingIndex; i < endingIndexPlusOne; i++)
                    {
                        cumLen += array[i].Length;
                    }
                    return cumLen;
                }
            //});
            return Task.FromResult(diffs.ToArray());
        }

        string MergeDiffs(string left, Diff[] diffs)
        {
            for (int i = 0; i < diffs.Length; i++)
            {
                left = left.Remove(diffs[i].Position, diffs[i].NumberToRemove).Insert(diffs[i].Position, diffs[i].ContentToAdd);
            }
            return left;
        }

        public string Origin
        {
            get
            {
                return File.ReadAllText($@"{Path}\.pvc\origin");
            }
        }

        public string Path { get; set; }

        public string[] IgnoredPaths
        {
            get
            {
                if (!File.Exists($@"{Path}\.pvcignore"))
                {
                    return new string[] { @"\.pvc" };
                }
                string[] igPaths = File.ReadAllLines($@"{Path}\.pvcignore");
                string[] igPaths2 = new string[igPaths.Length + 1];
                igPaths.CopyTo(igPaths2, 1);
                igPaths2[0] = @"\.pvc";
                return igPaths2;
            }
        }

        public string IgnoredRegex
        {
            get
            {
                return IgnoredPaths.Length > 1 ? (IgnoredPaths[1].StartsWith("R: ") ? IgnoredPaths[1].Remove(0, 3) : "") : "";
            }
        }
        public Task SetIgnoredRegex(string regexToIgnore)
        {
            if (IgnoredPaths.Length > 1 && IgnoredPaths[1].StartsWith("R: "))
            {
                string[] linesa = File.ReadAllLines($@"{Path}\.pvcignore");
                linesa[0] = "R: " + regexToIgnore;
                File.WriteAllLines($@"{Path}\.pvcignore", linesa);
                return Task.CompletedTask;
            }
            if (!File.Exists($@"{Path}\.pvcignore"))
            {
                File.WriteAllText($@"{Path}\.pvcignore", "R: " + regexToIgnore);
                return Task.CompletedTask;
            }
            string[] lines = File.ReadAllLines($@"{Path}\.pvcignore");
            string[] lines2 = new string[lines.Length + 1];
            lines.CopyTo(lines2, 1);
            lines2[0] = "R: " + regexToIgnore;
            File.WriteAllLines($@"{Path}\.pvcignore", lines2);
            return Task.CompletedTask;
        }
        public Task IgnorePaths(string[] pathsToIgnore)
        {
            File.AppendAllLines($@"{Path}\.pvcignore", pathsToIgnore);
            return Task.CompletedTask;
        }
        public Task UnignorePaths(string[] pathsToUnignore)
        {
            if (File.Exists($@"{Path}\.pvcignore"))
            {
                File.WriteAllLines($@"{Path}\.pvcignore", File.ReadAllLines($@"{Path}\.pvcignore").Except(pathsToUnignore));
            }
            return Task.CompletedTask;
        }
        //public Func<string, string, string> GetDiffs { get; set; }
        //public Func<string, string, string> MergeDiffs { get; set; }
        //public Commit Head { get; set; }
        //public Commit[] Branches { get; set; }
        HttpClient client;

        public HttpClient HttpClient
        {
            get
            {
                return client;
            }
        }

        public PVCServerInterface(/*Commit head,*/ string path)
        {
            client = new HttpClient();
            //Head = head;
            Path = path;
        }

        /// <summary>
        /// Registers the account specified in the User property of this client-server interface at the specified URL and with the specified recovery email.
        /// </summary>
        /// <param name="registerURL"></param>
        /// <param name="recoveryEmail"></param>
        /// <returns></returns>
        public async Task<RegisterResult> RegisterAccount(string registerURL, string recoveryEmail)
        {
            var registered = await client.PostAsync(registerURL, new StringContent(JsonConvert.SerializeObject((User, recoveryEmail)), Encoding.UTF8, "application/json"));
            return registered.IsSuccessStatusCode ? RegisterResult.Success : RegisterResult.Failure;
        }

        public async Task<RegisterResult> RegisterAccount(string registerURL, Account account, string recoveryEmail)
        {
            User = account;
            var registered = await client.PostAsync(registerURL, new StringContent(JsonConvert.SerializeObject((User, recoveryEmail)), Encoding.UTF8, "application/json"));
            return registered.IsSuccessStatusCode ? RegisterResult.Success : RegisterResult.Failure;
        }

        public async Task<bool> ResetPassword(string generalResetURL, string username)
        {
            var result = await client.PostAsync($"{generalResetURL}/{username}", new StringContent(username, Encoding.UTF8, "application/json"));
            return result.IsSuccessStatusCode;
        }

        public async Task<bool> ChangePassword(string passwordChangeURL, string newPassword)
        {
            var result = await client.PostAsync(passwordChangeURL, new StringContent(JsonConvert.SerializeObject((User, newPassword)), Encoding.UTF8, "application/json"));
            return result.IsSuccessStatusCode;
        }

        /// <summary>
        /// Creates and commits a commit which undoes the changes of the specified commit.
        /// </summary>
        /// <returns></returns>
        public async Task RevertCommit(string message, string author, string committer, DateTime dateTime, int idToUndo = -1)
        {
            int head = await GetHead();
            if(idToUndo == -1)
            {
                idToUndo = head;
            }
            Commit commitToUndo = GetCommit(idToUndo, $@"{Path}\.pvc");
            Diff[] diffs = JsonConvert.DeserializeObject<Diff[]>(commitToUndo.Diffs);
            Commit parent = GetCommit(commitToUndo.Parent, $@"{Path}\.pvc");
            string parentFiles = GetUpdatedFiles(commitToUndo.Parent);
            Diff[] undoDiffs = await GetDiffs(MergeDiffs(parentFiles, diffs), parentFiles);
            Commit commitToCommit = new Commit(JsonConvert.SerializeObject(undoDiffs), message, author, committer, head, dateTime);
            int specialHash = await Commit(commitToCommit);
            string branch = File.ReadAllText($@"{Path}\.pvc\refs\HEAD");
            File.WriteAllText($@"{Path}\.pvc\refs\branches\{branch}", specialHash.ToString());
        }

        public async Task<Commit[]> Log()
        {
            List<Commit> ret = new List<Commit>();
            int head = await GetHead();
            string path2 = Path + "\\.pvc";
            Commit temp = GetCommit(head, path2);
            while (true)
            {
                ret.Add(temp);
                if (temp.Parent == 0)
                {
                    break;
                }
                temp = GetCommit(temp.Parent, path2);
            }
            return ret.ToArray();
        }

        public async Task<Commit[]> Log(int number)
        {
            Commit[] ret = new Commit[number];
            int head = await GetHead();
            string path2 = Path + "\\.pvc";
            Commit temp = GetCommit(head, path2);
            for (int i = 0; i < number; i++)
            {
                ret[i] = temp;
                if (temp.Parent == 0)
                {
                    break;
                }
                temp = GetCommit(temp.Parent, path2);
            }
            return ret;

        }

        /// <summary>
        /// WIP
        /// </summary>
        /// <returns></returns>
        public async Task<PullResult> Pull()
        {
            if (await UncommittedChanges())
            {
                return PullResult.Uncommitted_Changes;
            }

            string branch = File.ReadAllText($@"{Path}\.pvc\refs\HEAD");
            string path2 = Path + @"\.pvc\tempRepoClone";
            await Clone(path2, Origin, true, "");
            bool allValid = true;
            foreach (string path in Directory.EnumerateDirectories($@"{Path}\.pvc\refs\branches"))
            {
                string branchCheck = path.Remove(0, Path.Length + 19);
                int branch1 = int.Parse(File.ReadAllText($@"{Path}\.pvc\refs\branches\{branchCheck}"));
                int branch2 = int.Parse(File.ReadAllText($@"{path2}\refs\branches\{branchCheck}"));
                if (CommonAncestor(branch1, branch2, Path + @"\.pvc", path2) != branch1)
                {
                    allValid = false;
                    break;
                }
            }
            if (allValid)
            {
                Directory.Move(path2, $@"{Path}\.pvcTemp");
                Directory.Delete($@"{Path}\.pvc", true);
                Directory.Move($@"{Path}\.pvcTemp", $@"{Path}\.pvc");
                await Checkout(branch);
                return PullResult.Success;
            }
            Directory.Delete(path2, true);
            return PullResult.Merge_Conflict;
        }

        public async Task<Status> GetStatus(string branch)
        {
            if (branch == await GetCurrentBranch() && await UncommittedChanges())
            {
                return Status.Uncommitted_Changes;
            }
            else
            {
                string path2 = Path + @"\.pvc\tempRepoClone";
                await Clone(path2, Origin, true, "");
                int branch1 = int.Parse(File.ReadAllText($@"{Path}\.pvc\refs\branches\{branch}"));
                if (!File.Exists($@"{path2}\refs\branches\{branch}"))
                {
                    return Status.Ahead_of_Origin;
                }
                int branch2 = int.Parse(File.ReadAllText($@"{path2}\refs\branches\{branch}"));
                int ca = CommonAncestor(branch1, branch2, Path + @"\.pvc", path2);
                Directory.Delete(path2, true);
                if (branch1 == branch2)
                {
                    return Status.Up_to_Date;
                }
                else if (ca == branch1)
                {
                    return Status.Behind_Origin;
                }
                else//(ca == branch2)
                {
                    return Status.Ahead_of_Origin;
                }
            }
            //else if (await CanPush(File.ReadAllText($@"{Path}\.pvc\refs\HEAD")))
            //{
            //    return Status.Up_to_Date;
            //}
            //return Status.Out_of_Date;
        }


        public async Task<bool> Checkout(string branch)
        {
            if (!File.Exists($@"{Path}\.pvc\refs\branches\{branch}") || await UncommittedChanges())
            {
                return false;
            }
            await Task.Run(async () =>
            {
                foreach (string path in Directory.EnumerateDirectories(Path))
                {
                    if (path != Path + @"\.pvc")
                    {
                        Directory.Delete(path, true);
                    }
                }
                foreach (string path in Directory.EnumerateFiles(Path))
                {
                    File.Delete(path);
                }
                File.WriteAllText($@"{Path}\.pvc\refs\HEAD", branch);
                await SetFiles(JsonConvert.DeserializeObject<Folder>(GetUpdatedFiles(int.Parse(File.ReadAllText($@"{Path}\.pvc\refs\branches\{branch}")))), Path);
            });
            return true;
        }

        async Task SetFiles(Folder folder, string rootPath)
        {
            Directory.CreateDirectory(rootPath + folder.Path);
            foreach (FileObj file in folder.Files)
            {
                File.WriteAllText(rootPath + file.Path, file.Contents);
            }
            foreach (Folder dir in folder.Folders)
            {
                await SetFiles(dir, rootPath);
            }
        }

        async Task<Folder> LoadFiles()
        {
            return await LoadFiles(Path, Path.Length, IgnoredPaths, IgnoredRegex);
        }

        async Task<Folder> LoadFiles(string path, int charsToRemoveFromPath, string[] excludedDirs, string regex, string rootPath = "")
        {
            if (rootPath == "")
            {
                rootPath = path;
            }
            string[] firstCharsRemoved(string[] start, int charsToRem)
            {
                for (int i = 0; i < start.Length; i++)
                {
                    start[i] = start[i].Remove(0, charsToRem);
                }
                return start;
            }
            string[] exceptRegex(string[] array, string regExpression)
            {
                if (regExpression == "")
                {
                    return array;
                }
                Regex regexp = new Regex(regExpression);
                var temp = array.ToList();
                for (int i = 0; i < array.Length; i++)
                {
                    if (regexp.IsMatch(array[i]))
                    {
                        temp.Remove(array[i]);
                    }
                }
                return temp.ToArray();
            }
            Folder folder = new Folder()
            {
                Path = path.Remove(0, charsToRemoveFromPath)
            };
            string[] filePaths = exceptRegex(firstCharsRemoved(Directory.GetFiles(path), charsToRemoveFromPath).Except(excludedDirs).ToArray(), regex);
            folder.Files = new FileObj[filePaths.Length];
            for (int i = 0; i < filePaths.Length; i++)
            {
                folder.Files[i] = new FileObj(filePaths[i], File.ReadAllText($"{rootPath}\\{filePaths[i]}"));
            }
            Regex regextest = new Regex(regex);
            var folderPaths = exceptRegex(firstCharsRemoved(Directory.GetDirectories(path), charsToRemoveFromPath).Except(excludedDirs).ToArray(), regex);
            folder.Folders = new Folder[folderPaths.Length];

            for (int i = 0; i < folderPaths.Length; i++)
            {
                folder.Folders[i] = await LoadFiles($"{rootPath}{folderPaths[i]}", charsToRemoveFromPath, excludedDirs, regex, rootPath);
            }

            return folder;
        }

        string GetUpdatedFiles(int latestCommit)
        {
            Commit lastCommit = JsonConvert.DeserializeObject<Commit>(File.ReadAllText($@"{Path}\.pvc\commits\{latestCommit % 100}\{latestCommit / 100}"));
            return MergeDiffs(lastCommit.Parent == 0 ? "" : GetUpdatedFiles(lastCommit.Parent), JsonConvert.DeserializeObject<Diff[]>(lastCommit.Diffs));
        }

        public async Task CreateBranch(string name)
        {
            await CreateBranch(name, int.Parse(File.ReadAllText($@"{Path}\.pvc\refs\branches\{File.ReadAllText($@"{Path}\.pvc\refs\HEAD")}")));
        }

        public async Task CreateBranch(string name, int commitID)
        {
            await Task.Run(() => File.WriteAllText($@"{Path}\.pvc\refs\branches\{name}", commitID.ToString()));
        }

        public async Task Clone(string orig)
        {
            await Clone(Path, orig, true, @"\.pvc");
            await Checkout(File.ReadAllText($@"{Path}\.pvc\refs\HEAD"));
        }

        async Task Clone(string path, string orig, bool loadCommits, string dataAdditional = @"\.pvc")
        {
            await Task.Run(async () =>
            {
                string path2 = path + dataAdditional;
                Directory.CreateDirectory(path2);
                File.WriteAllText(path2 + @"\origin", orig);
                Directory.CreateDirectory($@"{path2}\refs");
                Directory.CreateDirectory($@"{path2}\refs\branches");
                var received = await client.GetAsync(Origin + "/branches");
                Dictionary<string, int> branches = JsonConvert.DeserializeObject<Dictionary<string, int>>(received.Content.ReadAsStringAsync().Result);
                foreach (string branch in branches.Keys)
                {
                    File.WriteAllText($@"{path2}\refs\branches\{branch}", branches[branch].ToString());
                }
                File.WriteAllText($@"{path2}\refs\HEAD", branches.Keys.ToArray()[0]);
                Directory.CreateDirectory($@"{path2}\commits");

                if (loadCommits)
                {
                    await GetAllCommits(path, branches.Keys.ToArray(), dataAdditional);
                }
            });
        }

        async /*Task<Dictionary<int, Commit>>*/ Task GetAllCommits(string path, string[] branches, string extraString = @"\.pvc")
        {
            // Dictionary<int, Commit> allCommits = new Dictionary<int, Commit>();
            for (int i = 0; i < branches.Length; i++)
            {
                var received = await client.GetAsync($"{Origin}?branch={branches[i]}");
                Dictionary<int, Commit> commits = JsonConvert.DeserializeObject<Dictionary<int, Commit>>(received.Content.ReadAsStringAsync().Result);
                for (int j = 0; j < commits.Keys.Count; j++)
                {
                    string path2 = $@"{path}{extraString}\commits\{commits.Keys.ToArray()[j] % 100}";
                    Directory.CreateDirectory(path2);
                    File.WriteAllText($@"{path2}\{commits.Keys.ToArray()[j] / 100}", JsonConvert.SerializeObject(commits.Values.ToArray()[j]));
                    // allCommits.Add(commits[j].GetHashCode(),commits[j]);
                }
            }
            //return allCommits;
        }

        async Task<int> GetHead()
        {
            int result = 0;
            await Task.Run(() => result = int.Parse(File.ReadAllText($@"{Path}\.pvc\refs\branches\{File.ReadAllText($@"{Path}\.pvc\refs\HEAD")}")));
            return result;
        }

        async Task<int> Commit(Commit commit)
        {
            Guid guid = Guid.NewGuid();
            int hash = guid.GetHashCode();
            string path = $@"{Path}\.pvc\commits\{hash % 100}";
            await Task.Run(() =>
            {
                Directory.CreateDirectory(path);
                File.WriteAllText($@"{path}\{hash / 100}", JsonConvert.SerializeObject(commit));
            });
            return hash;
        }

        async Task Commit(string author, string committer, string diffs, string message, int parentId, DateTime dateTime)
        {
            await Commit(new Commit(diffs, message, author, committer, parentId, dateTime));
        }

        public async Task Commit(string message, string author, string committer, DateTime dateTime)
        {
            if (dateTime == null)
            {
                dateTime = DateTime.Now;
            }
            int head = await GetHead();
            Commit commitToCommit = new Commit(JsonConvert.SerializeObject(await GetDiffs(GetUpdatedFiles(head), JsonConvert.SerializeObject(await LoadFiles(Path, Path.Length, IgnoredPaths, IgnoredRegex)))), message, author, committer, head, dateTime);
            int specialHash = await Commit(commitToCommit);
            string branch = File.ReadAllText($@"{Path}\.pvc\refs\HEAD");
            File.WriteAllText($@"{Path}\.pvc\refs\branches\{branch}", specialHash.ToString());
        }

        public async Task DeleteBranch(string branch)
        {
            await Task.Run(() => File.Delete($@"{Path}\.pvc\refs\branches\{branch}"));
            //await client.DeleteAsync($"{Origin}/{branch}");
        }


        //TODO: Detect uncommitted changes or out-of-date repo
        async Task<bool> UncommittedChanges()
        {
            int head = await GetHead();
            return (await GetDiffs(GetUpdatedFiles(head), JsonConvert.SerializeObject(await LoadFiles(Path, Path.Length, IgnoredPaths, IgnoredRegex)))).Length != 0;
        }


        /// <summary>
        /// Describes whether or not a branch can be pushed without causing merge conflicts.
        /// </summary>
        /// <returns></returns>
        async Task<bool> CanPush(string branch)
        {
            string path2 = Path + @"\.pvc\tempRepoClone";
            await Clone(path2, Origin, true, "");
            bool canPush = false;
            int branch1 = int.Parse(File.ReadAllText($@"{Path}\.pvc\refs\branches\{branch}"));
            int branch2 = int.Parse(File.ReadAllText($@"{path2}\refs\branches\{branch}"));
            if (CommonAncestor(branch1, branch2, Path + @"\.pvc", path2) == branch2)
            {
                canPush = true;
            }
            Directory.Delete(path2, true);
            return canPush;
        }


        async Task<bool> CanPull(string branch)
        {
            string path2 = Path + @"\.pvc\tempRepoClone";
            await Clone(path2, Origin, true, "");
            bool canPull = false;
            int branch1 = int.Parse(File.ReadAllText($@"{Path}\.pvc\refs\branches\{branch}"));
            int branch2 = int.Parse(File.ReadAllText($@"{path2}\refs\branches\{branch}"));
            if (CommonAncestor(branch1, branch2, Path + @"\.pvc", path2) == branch1)
            {
                canPull = true;
            }
            Directory.Delete(path2, true);
            return canPull;
        }

        int CommonAncestor(int commit1, int commit2)
        {
            return CommonAncestor(commit1, commit2, Path + @"\.pvc", Path + @"\.pvc");
        }

        int CommonAncestor(int commit1, int commit2, string path1, string path2)
        {
            int commit2Start = commit2;
            Commit cmit = GetCommit(commit1, path1);
            Commit cmit2Strt = GetCommit(commit2, path2);
            while (true)
            {
                Commit cmit2 = cmit2Strt;
                commit2 = commit2Start;
                while (true)
                {
                    if (commit2 == commit1)
                    {
                        return commit2;
                    }
                    if (cmit2.Parent == 0)
                    {
                        break;
                    }
                    commit2 = cmit2.Parent;
                    cmit2 = GetCommit(cmit2.Parent, path2);
                }
                if (cmit.Parent == 0)
                {
                    break;
                }
                commit1 = cmit.Parent;
                cmit = GetCommit(cmit.Parent, path1);
            }
            return 0;
        }

        Commit GetCommit(int id, string pvcPath)
        {
            return JsonConvert.DeserializeObject<Commit>(File.ReadAllText($@"{pvcPath}\commits\{id % 100}\{id / 100}"));
        }
        /// <summary>
        /// Pushes committed changes.
        /// </summary>
        /// <param name="branchToPush">The branch to push. If empty, pushes all branches.</param>
        /// <returns></returns>
        public async Task<Dictionary<string, PushResult>> Push(string branchToPush = "")
        {
            var results = new Dictionary<string, PushResult>();
            //HttpStatusCode responseCode = HttpStatusCode.InternalServerError;
            if (await UncommittedChanges())
            {
                return new Dictionary<string, PushResult> { [await GetCurrentBranch()] = PushResult.Uncommited_Changes };
            }
            string branch = File.ReadAllText($@"{Path}\.pvc\refs\HEAD");
            string path2 = Path + @"\.pvc\tempRepoClone";
            await Clone(path2, Origin, true, "");
            bool allValid = true;
            if (branchToPush == "")
            {
                foreach (string path in Directory.EnumerateFiles($@"{Path}\.pvc\refs\branches"))
                {
                    if (!await checkValid(path.Remove(0, Path.Length + 19)))
                    {
                        break;
                    }
                }
            }
            else
            {
                await checkValid(branchToPush);
            }

            Task<bool> checkValid(string branchCheck)
            {
                int branch1 = int.Parse(File.ReadAllText($@"{Path}\.pvc\refs\branches\{branchCheck}"));
                if (File.Exists($@"{path2}\refs\branches\{branchCheck}"))
                {
                    int branch2 = int.Parse(File.ReadAllText($@"{path2}\refs\branches\{branchCheck}"));
                    if (CommonAncestor(branch1, branch2, Path + @"\.pvc", path2) != branch2)
                    {
                        allValid = false;
                        results.Add(branchCheck.Remove(0, 1), PushResult.Conflict);
                        return Task.FromResult(false);
                    }
                }
                return Task.FromResult(true);
            }

            if (allValid)
            {
                if (branchToPush == "")
                {
                    foreach (string path in Directory.EnumerateFiles($@"{Path}\.pvc\refs\branches"))
                    {
                        await PushBranch(path.Remove(0, Path.Length + 19));
                    }
                }
                else
                {
                    await PushBranch(branchToPush);
                }
                async Task PushBranch(string branchCheck)
                {
                    int branch1 = int.Parse(File.ReadAllText($@"{Path}\.pvc\refs\branches\{branchCheck}"));
                    int commonAncestor;
                    if (File.Exists($@"{path2}\refs\branches\{branchCheck}"))
                    {
                        int branch2 = int.Parse(File.ReadAllText($@"{path2}\refs\branches\{branchCheck}"));
                        commonAncestor = CommonAncestor(branch1, branch2, Path + @"\.pvc", path2);
                    }
                    else
                    {
                        commonAncestor = 0;
                    }
                    int commit = int.Parse(File.ReadAllText($@"{Path}\.pvc\refs\branches\{branchCheck}"));
                    Dictionary<int, Commit> commits = new Dictionary<int, Commit>();
                    while (commit != commonAncestor)
                    {
                        Commit commitInCommitForm = JsonConvert.DeserializeObject<Commit>(File.ReadAllText($@"{Path}\.pvc\commits\{commit % 100}\{commit / 100}"));
                        commits.Add(commit, commitInCommitForm);
                        commit = commitInCommitForm.Parent;
                    }
                    commits.Reverse();
                    var response = client.PutAsync($"{Origin}/{branchCheck}", new StringContent(JsonConvert.SerializeObject((User, commits)), Encoding.UTF8, "application/json"));
                    results.Add(branchCheck.Remove(0, 1), await FromResponseCode(response.Result.StatusCode));
                    await response;
                }
            }
            Task<PushResult> FromResponseCode(HttpStatusCode statusCode)
            {
                switch (statusCode)
                {
                    case HttpStatusCode.OK:
                        return Task.FromResult(PushResult.Success);
                    case HttpStatusCode.Unauthorized:
                        return Task.FromResult(PushResult.Authentication_Error);
                    case HttpStatusCode.BadRequest:
                        return Task.FromResult(PushResult.Bad_Request);
                    case HttpStatusCode.Accepted:
                        return Task.FromResult(PushResult.Up_to_Date);
                    default:
                        return Task.FromResult(PushResult.No_Response_From_Server);
                }
            }
            Directory.Delete(path2, true);
            return results;
            //await client.PostAsync(Origin, new StringContent(JsonConvert.SerializeObject(commit), Encoding.UTF8, "application/json"));
        }
    }
}
