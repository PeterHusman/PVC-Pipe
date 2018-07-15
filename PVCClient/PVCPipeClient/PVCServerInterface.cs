using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PVCPipeClient
{
    public class PVCServerInterface
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

        async Task<Diff[]> GetDiffs(string left, string right)
        {
            List<Diff> diffs = new List<Diff>();
            await Task.Run(() =>
            {
                //Try checking each character of right against left until there is a mismatch. Then maybe recursive call on shortened file?
                //New strat: For now, trying out comparing lines. Check line against line
                string[] leftLines = left.Split('\n');
                string[] rightLines = right.Split('\n');
                int biggerLength = leftLines.Length > rightLines.Length ? leftLines.Length : rightLines.Length;
                for (int i = 0; i < biggerLength; i++)
                {
                    string strLeft = i < leftLines.Length ? leftLines[i] : null;
                    string strRight = i < rightLines.Length ? rightLines[i] : null;
                    if (strLeft != strRight)
                    {
                        diffs.Add(new Diff(cumulativeLength(leftLines, i), leftLines[i].Length, i < rightLines.Length ? rightLines[i] : ""));
                    }
                }
                int cumulativeLength(string[] array, int numOfItemsToSum)
                {
                    int cumLen = 0;
                    for (int i = 0; i < numOfItemsToSum; i++)
                    {
                        cumLen += array[i].Length + 1;
                    }
                    return cumLen;
                }
            });
            return diffs.ToArray();
        }

        string MergeDiffs(string left, Diff[] diffs)
        {
            for (int i = diffs.Length - 1; i >= 0; i--)
            {
                left = left.Remove(diffs[0].Position, diffs[0].NumberToRemove).Insert(diffs[0].Position, diffs[0].ContentToAdd);
            }
            return left;
        }

        string origin
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

        public async Task IgnorePaths(string[] pathsToIgnore)
        {
            File.AppendAllLines($@"{Path}\.pvcignore", pathsToIgnore);
        }
        public async Task UnignorePaths(string[] pathsToUnignore)
        {
            if (File.Exists($@"{Path}\.pvcignore"))
            {
                File.WriteAllLines($@"{Path}\.pvcignore", File.ReadAllLines($@"{Path}\.pvcignore").Except(pathsToUnignore));
            }
        }
        //public Func<string, string, string> GetDiffs { get; set; }
        //public Func<string, string, string> MergeDiffs { get; set; }
        //public Commit Head { get; set; }
        //public Commit[] Branches { get; set; }
        HttpClient client;

        public PVCServerInterface(/*Commit head,*/ string path)
        {
            client = new HttpClient();
            //Head = head;
            Path = path;
        }

        public async Task<string[]> Log()
        {
            List<string> ret = new List<string>();
            int head = await GetHead();
            string path2 = Path + "\\.pvc";
            Commit temp = GetCommit(head, path2);
            while (true)
            {
                ret.Add(temp.Message);
                if (temp.Parent == 0)
                {
                    break;
                }
                temp = GetCommit(temp.Parent, path2);
            }
            return ret.ToArray();
        }

        public async Task<string[]> Log(int number)
        {
            string[] ret = new string[number];
            int head = await GetHead();
            string path2 = Path + "\\.pvc";
            Commit temp = GetCommit(head, path2);
            for (int i = 0; i < number; i++)
            {
                ret[i] = temp.Message;
                if (temp.Parent == 0)
                {
                    break;
                }
                temp = GetCommit(temp.Parent, path2);
            }
            return ret;

        }

        public enum PullResult
        {
            Success = 0,
            Uncommitted_Changes,
            Merge_Conflict
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
            await Clone(path2, origin, true, "");
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

        public enum Status
        {
            Uncommitted_Changes,
            Ahead_of_Origin,
            Behind_Origin,
            Up_to_Date
        }

        public async Task<Status> GetStatus(string branch)
        {
            if (await UncommittedChanges())
            {
                return Status.Uncommitted_Changes;
            }
            else
            {
                string path2 = Path + @"\.pvc\tempRepoClone";
                await Clone(path2, origin, true, "");
                int branch1 = int.Parse(File.ReadAllText($@"{Path}\.pvc\refs\branches\{branch}"));
                if(!File.Exists($@"{path2}\refs\branches\{branch}"))
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
                else if(ca == branch1)
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


        public async Task Checkout(string branch)
        {
            await Task.Run(async () =>
            {
                foreach (string path in Directory.EnumerateDirectories(Path))
                {
                    if (path != Path + @"\.pvc")
                    {
                        Directory.Delete(path);
                    }
                }
                foreach (string path in Directory.EnumerateFiles(Path))
                {
                    File.Delete(path);
                }
                File.WriteAllText($@"{Path}\.pvc\refs\HEAD", branch);
                await SetFiles(JsonConvert.DeserializeObject<Folder>(GetUpdatedFiles(int.Parse(File.ReadAllText($@"{Path}\.pvc\refs\branches\{branch}")))), Path);
            });
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
            return await LoadFiles(Path, Path.Length, IgnoredPaths);
        }

        async Task<Folder> LoadFiles(string path, int charsToRemoveFromPath, string[] excludedDirs, string rootPath = "")
        {
            if(rootPath == "")
            {
                rootPath = path;
            }
            string[] firstCharsRemoved(string[] start, int charsToRem)
            {
                for(int i = 0; i < start.Length; i++)
                {
                    start[i] = start[i].Remove(0, charsToRem);
                }
                return start;
            }
            Folder folder = new Folder()
            {
                Path = path.Remove(0, charsToRemoveFromPath)
            };
            string[] filePaths = firstCharsRemoved(Directory.GetFiles(path),charsToRemoveFromPath).Except(excludedDirs).ToArray();
            folder.Files = new FileObj[filePaths.Length];
            for (int i = 0; i < filePaths.Length; i++)
            {
                folder.Files[i] = new FileObj(filePaths[i], File.ReadAllText($"{rootPath}\\{filePaths[i]}"));
            }

            var folderPaths = firstCharsRemoved(Directory.GetDirectories(path), charsToRemoveFromPath).Except(excludedDirs).ToArray();
            folder.Folders = new Folder[folderPaths.Length];

            for (int i = 0; i < folderPaths.Length; i++)
            {
                folder.Folders[i] = await LoadFiles($"{rootPath}{folderPaths[i]}", charsToRemoveFromPath, excludedDirs,rootPath);
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
                var received = await client.GetAsync(origin + "/branches");
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
                var received = await client.GetAsync($"{origin}?branch={branches[i]}");
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

        async Task Commit(Commit commit)
        {
            string path = $@"{Path}\.pvc\commits\{commit.GetHashCode() % 100}";
            await Task.Run(() =>
            {
                Directory.CreateDirectory(path);
                File.WriteAllText($@"{path}\{commit.GetHashCode() / 100}", JsonConvert.SerializeObject(commit));
            });
        }

        async Task Commit(string author, string committer, string diffs, string message, int parentId)
        {
            await Commit(new Commit(diffs, message, author, committer, parentId));
        }

        public async Task Commit(string message, string author, string committer)
        {
            int head = await GetHead();
            Commit commitToCommit = new Commit(JsonConvert.SerializeObject(await GetDiffs(GetUpdatedFiles(head), JsonConvert.SerializeObject(await LoadFiles(Path, Path.Length, IgnoredPaths)))), message, author, committer, head);
            await Commit(commitToCommit);
            string branch = File.ReadAllText($@"{Path}\.pvc\refs\HEAD");
            File.WriteAllText($@"{Path}\.pvc\refs\branches\{branch}", commitToCommit.GetHashCode().ToString());
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
            return (await GetDiffs(GetUpdatedFiles(head), JsonConvert.SerializeObject(await LoadFiles(Path, Path.Length, IgnoredPaths)))).Length != 0;
        }


        /// <summary>
        /// Describes whether or not a branch can be pushed without causing merge conflicts.
        /// </summary>
        /// <returns></returns>
        async Task<bool> CanPush(string branch)
        {
            string path2 = Path + @"\.pvc\tempRepoClone";
            await Clone(path2, origin, true, "");
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
            await Clone(path2, origin, true, "");
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
            Commit cmit = GetCommit(commit1, path1);
            Commit cmit2Strt = GetCommit(commit2, path2);
            while (true)
            {
                Commit cmit2 = cmit2Strt;
                while (true)
                {
                    if (cmit2.GetHashCode() == cmit.GetHashCode())
                    {
                        return cmit2.GetHashCode();
                    }
                    if (cmit2.Parent == 0)
                    {
                        break;
                    }
                    cmit2 = GetCommit(cmit2.Parent, path2);
                }
                if (cmit.Parent == 0)
                {
                    break;
                }
                cmit = GetCommit(cmit.Parent, path1);
            }
            return 0;
        }

        Commit GetCommit(int id, string pvcPath)
        {
            return JsonConvert.DeserializeObject<Commit>(File.ReadAllText($@"{pvcPath}\commits\{id % 100}\{id / 100}"));
        }

        //TODO
        public async Task<bool> Push()//string branch = "")
        {

            if (await UncommittedChanges())
            {
                return false;
            }
            string branch = File.ReadAllText($@"{Path}\.pvc\refs\HEAD");
            string path2 = Path + @"\.pvc\tempRepoClone";
            await Clone(path2, origin, true, "");
            bool allValid = true;
            foreach (string path in Directory.EnumerateFiles($@"{Path}\.pvc\refs\branches"))
            {
                string branchCheck = path.Remove(0, Path.Length + 19);
                int branch1 = int.Parse(File.ReadAllText($@"{Path}\.pvc\refs\branches\{branchCheck}"));
                int branch2 = int.Parse(File.ReadAllText($@"{path2}\refs\branches\{branchCheck}"));
                if (CommonAncestor(branch1, branch2, Path + @"\.pvc", path2) == branch2)
                {
                    allValid = false;
                    break;
                }
            }

            if (allValid)
            {
                foreach (string path in Directory.EnumerateFiles($@"{Path}\.pvc\refs\branches"))
                {
                    string branchCheck = path.Remove(0, Path.Length + 19);
                    int branch1 = int.Parse(File.ReadAllText($@"{Path}\.pvc\refs\branches\{branchCheck}"));
                    int branch2 = int.Parse(File.ReadAllText($@"{path2}\refs\branches\{branchCheck}"));
                    int commonAncestor = CommonAncestor(branch1, branch2, Path + @"\.pvc", path2);
                    int commit = int.Parse(File.ReadAllText(path));
                    Dictionary<int, Commit> commits = new Dictionary<int, Commit>();
                    while (commit != commonAncestor)
                    {
                        Commit commitInCommitForm = JsonConvert.DeserializeObject<Commit>(File.ReadAllText($@"{Path}\.pvc\commits\{commit % 100}\{commit / 100}"));
                        commits.Add(commit, commitInCommitForm);
                        commit = commitInCommitForm.Parent;
                    }
                    commits.Reverse();
                    await client.PutAsync($"{origin}/{branchCheck}", new StringContent(JsonConvert.SerializeObject(commits), Encoding.UTF8, "application/json"));
                }
            }
            Directory.Delete(path2, true);
            return allValid;
            //await client.PostAsync(Origin, new StringContent(JsonConvert.SerializeObject(commit), Encoding.UTF8, "application/json"));
        }
    }
}
