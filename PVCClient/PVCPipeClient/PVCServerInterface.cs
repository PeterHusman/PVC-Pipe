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

        class AllDiffs
        {
            public string[] Directories;
            public Dictionary<string, Diff[]> FilesAndDiffs;
        }

        class Diff
        {
            public int Position;
            public int NumberToRemove;
            public string ContentToAdd;
        }

        Diff[] GetDiffs(string left, string right)
        {
            List<Diff> diffs = new List<Diff>();
            //Try checking each character of right against left until there is a mismatch. Then maybe recursive call on shortened file?
            return diffs.ToArray();
        }

        string MergeDiffs(string left, Diff[] diffs)
        {
            for(int i = diffs.Length - 1; i >= 0; i--)
            {
                left = left.Remove(diffs[0].Position, diffs[0].NumberToRemove).Insert(diffs[0].Position, diffs[0].ContentToAdd);
            }
            return left;
        }

        public string Origin { get; set; }
        public string Path { get; set; }
        //public Func<string, string, string> GetDiffs { get; set; }
        //public Func<string, string, string> MergeDiffs { get; set; }
        //public Commit Head { get; set; }
        //public Commit[] Branches { get; set; }
        HttpClient client;

        public PVCServerInterface(/*Commit head,*/ string origin)
        {
            client = new HttpClient();
            //Head = head;
            Origin = origin;
        }

        public async void Pull()
        {
            //Check if uncommitted/unpushed changes.
            string branch = File.ReadAllText($@"{Path}\.pvc\refs\HEAD");
            Directory.Delete(Path + @"\.pvc", true);
            Clone();

            File.WriteAllText($@"{Path}\.pvc\refs\HEAD", branch);
        }

        public void Checkout(string branch)
        {
            File.WriteAllText($@"{Path}\.pvc\refs\HEAD", branch);
            GetUpdatedFiles(int.Parse(File.ReadAllText($@"{Path}\.pvc\refs\branches\{branch}")));
        }

        public void GetUpdatedFiles(int latestCommit)
        {
            
        }

        public async void Clone()
        {
            string path = Path + @"\.pvc";
            Directory.CreateDirectory(path);
            File.WriteAllText(path + @"\origin", Origin);
            Directory.CreateDirectory($@"{path}\refs");
            Directory.CreateDirectory($@"{path}\refs\branches");
            Dictionary<string, int> branches = (Dictionary<string, int>)JsonConvert.DeserializeObject(await client.GetStringAsync(Origin + "/branches"));
            foreach (string branch in branches.Keys)
            {
                File.WriteAllText($@"{path}\refs\branches\{branch}", branches[branch].ToString());
            }
            File.WriteAllText($@"{path}\refs\HEAD", branches[branches.Keys.ToArray()[0]].ToString());
            Directory.CreateDirectory($@"{path}\commits");
            GetAllCommits(branches.Keys.ToArray());
        }

        async /*Task<Dictionary<int, Commit>>*/void GetAllCommits(string[] branches)
        {
            // Dictionary<int, Commit> allCommits = new Dictionary<int, Commit>();
            for (int i = 0; i < branches.Length; i++)
            {
                Commit[] commits = JsonConvert.DeserializeObject<Commit[]>(await client.GetStringAsync($"{Origin}?branch={branches[i]}"));
                for (int j = 0; j < commits.Length; j++)
                {
                    string path2 = $@"{Path}\.pvc\commits\{commits[j].GetHashCode() % 100}";
                    Directory.CreateDirectory(path2);
                    File.WriteAllText($@"{path2}\{commits[j].GetHashCode() / 100}", JsonConvert.SerializeObject(commits[j]));
                    // allCommits.Add(commits[j].GetHashCode(),commits[j]);
                }
            }
            //return allCommits;
        }

        public void Commit(Commit commit)
        {
            string path = $@"{Path}\.pvc\commits\{commit.GetHashCode() % 100}";
            Directory.CreateDirectory(path);
            File.WriteAllText($@"{path}\{commit.GetHashCode() / 100}", JsonConvert.SerializeObject(commit));
        }

        public void Commit(string author, string committer, string diffs, string message, int parentId)
        {
            Commit(new Commit(diffs, message, author, committer, new int[] { parentId, 0 }));
        }

        public void Commit()
        {

        }

        public void DeleteBranch(string branch)
        {
            File.Delete($@"{Path}\.pvc\refs\branches\{branch}");
            //await client.DeleteAsync($"{Origin}/{branch}");
        }

        public async void Push()
        {
            //await client.PostAsync(Origin, new StringContent(JsonConvert.SerializeObject(commit), Encoding.UTF8, "application/json"));
        }
    }
}
