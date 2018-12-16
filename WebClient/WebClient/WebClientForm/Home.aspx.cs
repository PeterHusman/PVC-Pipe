using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace WebClientForm
{
    public partial class Home : System.Web.UI.Page
    {
        bool loggedIn = false;
        string username;
        string password;
        string[] repoNames;
        protected void Page_Load(object sender, EventArgs e)
        {

            if(IsPostBack)
            {
                return;
            }

            repos.Text = "You are not signed in";
            if (Session["username"] != null)
            {
                loggedIn = true;
                welcomeLabel.Text = $"Hello, {Session["username"]}!";
                loginButton.Text = "Sign Out";
                username = (string)Session["username"];
                password = (string)Session["password"];
                repoNames = (string[])Session["repoNames"];
                repos.Items.Clear();
                for (int i = 0; i < repoNames.Length; i++)
                {
                    repos.Items.Add(repoNames[i]);
                }
                repos.Text = "Select a repository";
                if (repoNames.Length <= 0)
                {
                    repos.Text = "You have no repositories";
                }
                
            }

            //if(loggedIn)
            //{
            //    HttpClient client = new HttpClient();
            //    var resp1 = client.PostAsync(ConstantsContainer.ApiURL + "checkAuth", new StringContent(JsonConvert.SerializeObject(new Account(username, password)), Encoding.UTF8, "application/json"));
            //    var resp = await resp1;
            //    repoNames = JsonConvert.DeserializeObject<string[]>(resp.Content.ReadAsStringAsync().Result);
            //    repos.Items.Clear();
            //    for(int i = 0; i < repoNames.Length; i++)
            //    {
            //        repos.Items.Add(repoNames[i]);
            //    }
            //    if(repoNames.Length <= 0)
            //    {
            //        repos.Items.Add("You have no repositories");
            //    }
            //}
        }

        protected void loginButton_Click(object sender, EventArgs e)
        {
            if (loginButton.Text == "Sign Out")
            {
                Session["username"] = null;
                Session["password"] = null;
                Session["repoNames"] = null;
                Response.Redirect("Home.aspx");
                return;
            }
            Server.Transfer("Login.aspx");
        }

        protected void repos_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        protected void goToRepo_Click(object sender, EventArgs e)
        {
            if(repos.SelectedIndex < 0 || repos.SelectedItem.Text == "")
            {
                return;
            }
            StringBuilder finalUrl = new StringBuilder("RepoPage.aspx?repo=");
            string startingName = repos.SelectedValue;
            for (int i = 0; i < startingName.Length; i++)
            {
                finalUrl.Append($"%{(int)startingName[i]:X}");
            }
            Response.Redirect(finalUrl.ToString());
        }
    }
}