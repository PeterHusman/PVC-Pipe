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
        public string Origin { get; set; }
        public string Path { get; set; }
        public Commit Head { get; set; }
        public Commit[] Branches { get; set; }
        HttpClient client;

        public PVCServerInterface(Commit head, string origin)
        {
            client = new HttpClient();
            Head = head;
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
            getAllCommits(branches.Keys.ToArray());
        }

        async void getAllCommits(string[] branches)
        {
            for (int i = 0; i < branches.Length; i++)
            {
                Commit[] commits = JsonConvert.DeserializeObject<Commit[]>(await client.GetStringAsync($"{Origin}?branch={branches[i]}"));
                for (int j = 0; j < commits.Length; j++)
                {
                    string path2 = $@"{Path}\.pvc\commits\{commits[j].GetHashCode() % 100}";
                    Directory.CreateDirectory(path2);
                    File.WriteAllText($@"{path2}\{commits[j].GetHashCode()/100}",JsonConvert.SerializeObject(commits[j]));
                }
            }
        }

        public void DeleteBranch(string branch)
        {
            File.Delete($@"{Path}\.pvc\refs\branches\{branch}");
        }

        public async void Push()
        {
            //await client.PostAsync(Origin, new StringContent(JsonConvert.SerializeObject(commit), Encoding.UTF8, "application/json"));
        }
    }
}
