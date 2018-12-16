using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Net.Http;
using System.Web.UI.WebControls;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;

namespace WebClientForm
{
    public class Folder
    {
        public string Path;
        public Folder[] Folders;
        public FileObj[] Files;
    }
    public class FileObj
    {
        public string Path;
        public string Contents;

        public FileObj(string path, string contents)
        {
            Path = path;
            Contents = contents;
        }
    }
    public class Commit
    {
        public string Diffs { get; set; }

        public int Parent { get; set; }

        public string Message { get; set; }

        public string Author { get; set; }

        public string Committer { get; set; }

        public DateTime DateAndTime { get; set; }

        public Commit(string diffs, string message, string author, string committer, int parent, DateTime dateTime)
        {
            Diffs = diffs;
            Message = message;
            Author = author;
            Committer = committer;
            Parent = parent;
            DateAndTime = dateTime;
        }
    }
    public class Account
    {
        public string Username;
        public string Password;
        public Account(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }

    public class Diff
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
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected async void loginButton_Click(object sender, EventArgs e)
        {
            string username = usrnameBox.Text;
            string password = passwordBox.Text;
            HttpClient client = new HttpClient();
            var resp1 = client.PostAsync(ConstantsContainer.ApiURL + "checkAuth", new StringContent(JsonConvert.SerializeObject(new Account(username, password)), Encoding.UTF8, "application/json"));
            var resp = await resp1;
            if (resp.IsSuccessStatusCode)
            {
                Session["username"] = username;
                Session["password"] = password;
                Session["repoNames"] = JsonConvert.DeserializeObject<string[]>(resp.Content.ReadAsStringAsync().Result);

                //Response.Cookies["username"].Value = username;
                //Response.Cookies["username"].Expires = DateTime.Now.AddHours(3);
                //Response.Cookies["password"].Value = password;
                //Response.Cookies["password"].Expires = DateTime.Now.AddHours(3);

                //Response.RedirectLocation = "Home.aspx";
                Server.Transfer("Home.aspx");
            }
            else
            {
                inval.Visible = true;
            }
        }
    }
}