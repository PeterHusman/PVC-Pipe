using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebClientForm
{
    public partial class RepoPage : System.Web.UI.Page
    {
        string repository;
        HttpClient client;
        string[] branches;
        protected async void Page_Load(object sender, EventArgs e)
        {
            client = new HttpClient();
            if (string.IsNullOrEmpty(Request.QueryString["repo"]))
            {
                await Task.Run(() => Server.Transfer("Home.aspx?"));
                return;
            }
            repository = Request.QueryString["repo"];
            var response = await client.GetAsync(ConstantsContainer.ApiURL + $"{repository}/branches");
            branches = JsonConvert.DeserializeObject<Dictionary<string, int>>(response.Content.ReadAsStringAsync().Result).Keys.ToArray();
            if (branches.Length <= 0)
            {
                await Task.Run(() => Server.Transfer("Home.aspx?"));
                return;
            }
            branchList.Items.Clear();
            for (int i = 0; i < branches.Length; i++)
            {
                //branchList.Items.Add(new ListItem(branches[i], branches[i], true));
                branchList.Items.Add(branches[i]);
            }
            repoLabel.Text = "Repository: " + repository;
        }

        protected async void returnButton_Click(object sender, EventArgs e)
        {
            await Task.Run(() => Server.Transfer("Home.aspx?"));
        }

        protected async void branchList_Click(object sender, BulletedListEventArgs e)
        {
            BulletedList list = (BulletedList)sender;
            string branchName = "";
            branchName = list.Items[e.Index].Text;

            commitLabel.Text = $"Commits of {branchName}:";
            var response = await client.GetAsync(ConstantsContainer.ApiURL + $"{repository}?branch={branchName}");
            Dictionary<int, Commit> commits = JsonConvert.DeserializeObject<Dictionary<int, Commit>>(response.Content.ReadAsStringAsync().Result);
            int starterID = 0;
            HashSet<int> parents = new HashSet<int>();
            foreach (int id in commits.Keys)
            {
                parents.Add(commits[id].Parent);
            }
            foreach (int id in commits.Keys)
            {
                if (parents.Contains(id))
                {
                    continue;
                }
                starterID = id;
                if (id == 0)
                {
                    return;
                }
                break;
            }
            Commit startingCommit = commits[starterID];
            int number = 1;
            commitList.Items.Clear();
            while (true)
            {
                commitList.Items.Add(new ListItem($"{startingCommit.Author}: {startingCommit.Message}", starterID.ToString(), true));
                starterID = startingCommit.Parent;
                if (starterID == 0)
                {
                    break;
                }
                startingCommit = commits[starterID];
                number++;
            }
        }

        protected void commitList_Click(object sender, BulletedListEventArgs e)
        {
            BulletedList list = (BulletedList)sender;
            string branchName = "";
            branchName = list.Items[e.Index].Value;
        }
    }
}