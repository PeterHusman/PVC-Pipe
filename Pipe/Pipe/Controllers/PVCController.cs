using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Pipe.Models;

namespace Pipe.Controllers
{
    public static class Constants
    {
        public static int lastAccessDay;
    }


    [EnableCors("openCORS")]
    [Route("api/pipe")]
    public class PVCController : Controller
    {
        IConfiguration configuration;
        SqlConnection connection;
        NetworkCredential emailCredentials;
        string passwordResetBody;
        string passwordResetSubject;
        string siteAddress;
        Dictionary<int, (string, DateTime)> resetLinks;


        public PVCController(IConfiguration configuration)//, INotifier notifier)
        {
            this.configuration = configuration;
            connection = new SqlConnection(configuration.GetConnectionString("Dev"));
            var emailAuth = configuration.GetSection("Email");
            emailCredentials = new NetworkCredential(emailAuth["Username"], emailAuth["Password"]);
            passwordResetBody = emailAuth["Body"];
            passwordResetSubject = emailAuth["Subject"];
            siteAddress = configuration.GetSection("SiteData")["Address"];
            resetLinks = new Dictionary<int, (string, DateTime)>();
            //notifier.SendMail();
        }

        public class Account
        {
            public string Username;
            public string Password;
        }
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

        private byte[] GetFullPasswordHash(int userID, string password, bool openConnection)
        {
            if (openConnection)
            { connection.Open(); }
            SqlCommand cmd = SetUpUSP("usp_GetSaltUserID", new string[] { "UserID" }, new object[] { userID });
            var prospectiveSalt = SqlReturnObj(cmd);
            if (prospectiveSalt == null)
            {
                Response.StatusCode = 401;
                return new byte[0];
            }
            string salt = (string)prospectiveSalt;
            password = salt + password;
            SHA256Managed sha256 = new SHA256Managed();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        private SqlCommand SetUpUSP(string usp, string[] parameterNames, object[] parameterValues)
        {
            SqlCommand cmd = new SqlCommand(usp, connection);
            cmd.CommandType = CommandType.StoredProcedure;
            for (int i = 0; i < parameterNames.Length; i++)
            {
                cmd.Parameters.AddWithValue(parameterNames[i], parameterValues[i]);
            }
            return cmd;
        }
        private SqlCommand SetUpUSP(string usp, params (string, object)[] parameters)
        {
            SqlCommand cmd = new SqlCommand(usp, connection);
            cmd.CommandType = CommandType.StoredProcedure;
            for (int i = 0; i < parameters.Length; i++)
            {
                cmd.Parameters.AddWithValue(parameters[i].Item1, parameters[i].Item2);
            }
            return cmd;
        }

        private int GetUserID(string username, bool openConnection)
        {
            if (openConnection)
            { connection.Open(); }
            SqlCommand cmd = new SqlCommand("usp_GetUserID", connection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("Username", username));
            object userID = SqlReturnObj(cmd);
            if (userID != null)
            {
                return (int)SqlReturnObj(cmd);
            }
            return -1;
        }


        private static object SqlReturnObj(SqlCommand cmd)
        {
            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            DataTable data = new DataTable();
            adapter.Fill(data);
            if (data.Rows.Count <= 0)
            {
                return null;
            }
            object retVal = data.Rows[0][0];
            data.Clear();
            return retVal;
        }

        [HttpGet("{repo}/branches")]
        public string GetBranches(string repo)
        {
            if (repo == null)
            {
                return "";//new string[] { };
            }
            connection.Open();
            SqlCommand cmd = new SqlCommand("usp_GetRepoID", connection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("RepositoryName", repo));
            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            DataTable data = new DataTable();
            adapter.Fill(data);
            if (data.Rows.Count <= 0)
            {
                Response.StatusCode = 400;
                return "";
            }
            cmd.CommandText = "usp_GetBranchesOfRepo";
            cmd.Parameters.Clear();
            cmd.Parameters.Add(new SqlParameter("RepositoryID", (int)data.Rows[0][0]));
            data.Clear();
            adapter.Fill(data);
            if (data.Rows.Count == 0)
            {
                return "";
            }
            Dictionary<string, int> output = new Dictionary<string, int>(data.Rows.Count);
            for (int i = 0; i < data.Rows.Count; i++)
            {
                output.Add(data.Rows[i]["BranchName"].ToString(), (int)data.Rows[i]["CommitID"]);
            }
            return JsonConvert.SerializeObject(output);
        }

        string MergeDiffs(string left, Diff[] diffs)
        {
            for (int i = 0; i < diffs.Length; i++)
            {
                left = left.Remove(diffs[i].Position, diffs[i].NumberToRemove).Insert(diffs[i].Position, diffs[i].ContentToAdd);
            }
            return left;
        }

        [HttpGet("{repo}/{commitId}")]
        public string BuildZip(string repo, int commitId)
        {
            if (Constants.lastAccessDay != DateTime.UtcNow.DayOfYear)
            {
                foreach (string file in Directory.EnumerateFiles("wwwroot/Downloads"))
                {
                    System.IO.File.Delete(file);
                }
                Constants.lastAccessDay = DateTime.UtcNow.DayOfYear;
            }

            if (repo == null)
            {
                return "";//new string[] { };
            }
            connection.Open();
            SqlCommand cmd = new SqlCommand("usp_GetRepoID", connection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("RepositoryName", repo));
            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            DataTable data = new DataTable();
            adapter.Fill(data);
            cmd.CommandText = "usp_GetRepoHistoryFromRecentCommit";
            cmd.Parameters.Clear();
            if (data.Rows.Count <= 0)
            {
                Response.StatusCode = 400;
                return "";
            }
            cmd.Parameters.Add(new SqlParameter("RepositoryID", (int)data.Rows[0][0]));
            cmd.Parameters.Add(new SqlParameter("CommitID", commitId));
            data.Clear();
            adapter.Fill(data);
            if (data.Rows.Count == 0)
            {
                return "";
            }
            Dictionary<int, Commit> commits = commitHistoryFromDataBaseEntry(data);
            string GetUpdatedFiles(int id)
            {
                Commit cmit = commits[id];
                return MergeDiffs(cmit.Parent == 0 ? "" : GetUpdatedFiles(cmit.Parent), JsonConvert.DeserializeObject<Diff[]>(cmit.Diffs));
            }
            if (Directory.Exists($"TempFiles\\{repo}{commitId}"))
            {
                Directory.Delete($"TempFiles\\{repo}{commitId}", true);
            }
            if (System.IO.File.Exists($"wwwroot\\Downloads\\{repo}{commitId}.zip"))
            {
                System.IO.File.Delete($"wwwroot\\Downloads\\{repo}{commitId}.zip");
            }
            Directory.CreateDirectory($"TempFiles\\{repo}{commitId}");
            SetFiles(JsonConvert.DeserializeObject<Folder>(GetUpdatedFiles(commitId)), $"TempFiles\\{repo}{commitId}");
            ZipFile.CreateFromDirectory($"TempFiles\\{repo}{commitId}", $"wwwroot\\Downloads\\{repo}{commitId}.zip");
            Directory.Delete($"TempFiles\\{repo}{commitId}", true);
            return $"http://localhost:56854/downloads/{repo}{commitId}.zip";
        }


        void SetFiles(Folder folder, string rootPath)
        {
            Directory.CreateDirectory(rootPath + folder.Path);
            foreach (FileObj file in folder.Files)
            {
                System.IO.File.WriteAllText(rootPath + file.Path, file.Contents);
            }
            foreach (Folder dir in folder.Folders)
            {
                SetFiles(dir, rootPath);
            }
        }

        //Returns history of a branch
        // GET api/pipe/[repo]?branch=master&...
        [HttpGet("{repo}")]
        public string Get(string repo, string branch = "master")
        {
            if (repo == null)
            {
                return "";//new string[] { };
            }
            connection.Open();
            SqlCommand cmd = new SqlCommand("usp_GetRepoID", connection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("RepositoryName", repo));
            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            DataTable data = new DataTable();
            adapter.Fill(data);
            cmd.CommandText = "usp_GetBranchCommitID";
            cmd.Parameters.Clear();
            if (data.Rows.Count <= 0)
            {
                Response.StatusCode = 400;
                return "";
            }
            cmd.Parameters.Add(new SqlParameter("RepositoryID", (int)data.Rows[0][0]));
            cmd.Parameters.Add(new SqlParameter("BranchName", branch));
            data.Clear();
            adapter.Fill(data);
            if (data.Rows.Count == 0)
            {
                return "";
            }
            cmd.CommandText = "usp_GetRepoHistoryFromRecentCommit";
            cmd.Parameters[1] = new SqlParameter("CommitID", (int)data.Rows[0][1]);
            data.Clear();
            adapter.Fill(data);
            if (data.Rows.Count == 0)
            {
                return "";//new string[] { };
            }
            return JsonConvert.SerializeObject(commitHistoryFromDataBaseEntry(data));//new string[] { data.Rows[0]["Message"].ToString() };//new string[] { "value1", "value2", repo };
        }


        Dictionary<int, Commit> commitHistoryFromDataBaseEntry(DataTable table)
        {
            Dictionary<int, Commit> commits = new Dictionary<int, Commit>();
            for (int i = 0; i < table.Rows.Count; i++)
            {
                commits.Add((int)table.Rows[i]["CommitID"], new Commit((string)table.Rows[i]["TextDiffs"], (string)table.Rows[i]["Message"], (string)table.Rows[i]["Author"], (string)table.Rows[i]["Committer"], (int)table.Rows[i]["ParentID"], (DateTime)table.Rows[i]["Date"]));
            }
            return commits;
        }

        //[HttpPost]
        //public void Post([FromBody]Commit[] commits, [FromBody]string branch)
        //{

        //}

        [HttpPost("{repo}/{branch}")]
        public void CreateBranch(string repo, string branch, int commitID, [FromBody]Account accnt)
        {
            if (accnt == null)
            {
                Response.StatusCode = 401;
                return;
            }
            int repoID = GetRepoID(repo);
            if (repoID < 0)
            {
                Response.StatusCode = 400;
                return;
            }
            SqlCommand cmd = new SqlCommand("usp_AddBranch", connection);
            int userID = GetUserID(accnt.Username, false);
            if (userID < 0)
            {
                Response.StatusCode = 401;
                return;
            }
            byte[] hash = GetFullPasswordHash(userID, accnt.Password, false);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("RepositoryID", repoID));
            cmd.Parameters.Add(new SqlParameter("BranchName", branch));
            cmd.Parameters.Add(new SqlParameter("CommitID", commitID));
            cmd.Parameters.AddWithValue("UserID", userID);
            cmd.Parameters.AddWithValue("PasswordHash", hash);
            RunVoidAuthenticatable(cmd);
        }

        private bool RunVoidAuthenticatable(SqlCommand cmd)
        {
            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            DataTable table = new DataTable();
            adapter.Fill(table);
            if (table.Rows.Count <= 0)
            {
                Response.StatusCode = 401;
                return false;
            }

            return true;
        }

        [HttpPost("checkAuth")]
        public string[] CheckAccountPerms([FromBody]Account accnt)
        {
            if (accnt == null)
            {
                Response.StatusCode = 401;
                return null;
            }
            connection.Open();
            int userID = GetUserID(accnt.Username, false);
            if (userID < 0)
            {
                Response.StatusCode = 401;
                return null;
            }
            byte[] hash = GetFullPasswordHash(userID, accnt.Password, false);
            bool auth = RunVoidAuthenticatable(SetUpUSP("usp_CheckAuth", ("UserID", userID), ("PasswordHash", hash)));
            if (!auth)
            {
                Response.StatusCode = 401;
                return null;
            }
            SqlDataAdapter adapter = new SqlDataAdapter(SetUpUSP("usp_GetAuthRepos", ("UserID", userID)));
            DataTable data = new DataTable();
            adapter.Fill(data);
            string[] repoNames = new string[data.Rows.Count];
            for (int i = 0; i < repoNames.Length; i++)
            {
                repoNames[i] = (string)SqlReturnObj(SetUpUSP("usp_GetRepoName", ("RepositoryID", (int)data.Rows[i].ItemArray[0])));
            }
            return repoNames;
        }


        [HttpPost("{repo}")]
        public void CreateRepo(string repo, [FromBody]Account accnt)
        {
            if (accnt == null)
            {
                Response.StatusCode = 401;
                return;
            }
            connection.Open();
            SqlCommand cmd = new SqlCommand("usp_AddRepoWOID", connection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("Name", repo));
            int userID = GetUserID(accnt.Username, false);
            if (userID < 0)
            {
                Response.StatusCode = 401;
                return;
            }
            cmd.Parameters.Add(new SqlParameter("UserID", userID));
            bool res = RunVoidAuthenticatable(cmd);
            if (!res)
            {
                Response.StatusCode = 400;
                return;
            }
            cmd.CommandText = "usp_AddCommit";
            cmd.Parameters.Clear();


            byte[] hash = GetFullPasswordHash(userID, accnt.Password, false);
            int repoID = GetRepoID(repo, false);
            if (repoID < 0)
            {
                Response.StatusCode = 400;
                return;
            }
            int commitID = new Random().Next(1, int.MaxValue);
            cmd.Parameters.Add(new SqlParameter("CommitID", commitID));
            cmd.Parameters.Add(new SqlParameter("Message", "Initial commit for repository " + repo));
            cmd.Parameters.Add(new SqlParameter("ParentID", 0));
            cmd.Parameters.Add(new SqlParameter("Author", "Auto-generated"));
            cmd.Parameters.Add(new SqlParameter("Committer", "Auto-generated"));
            cmd.Parameters.Add(new SqlParameter("RepositoryID", repoID));
            cmd.Parameters.Add(new SqlParameter("Date", DateTime.Now));
            cmd.Parameters.Add(new SqlParameter("TextDiffs", JsonConvert.SerializeObject(new Diff[] { new Diff(0, 0, JsonConvert.SerializeObject(new Folder { Path = "", Folders = new Folder[0], Files = new FileObj[0] })) })));
            cmd.Parameters.AddWithValue("UserID", userID);
            cmd.Parameters.AddWithValue("PasswordHash", hash);
            RunVoidAuthenticatable(cmd);
            cmd.Parameters.Clear();
            cmd.CommandText = "usp_AddBranch";
            cmd.Parameters.Add(new SqlParameter("RepositoryID", repoID));
            cmd.Parameters.Add(new SqlParameter("BranchName", "master"));
            cmd.Parameters.Add(new SqlParameter("CommitID", commitID));
            cmd.Parameters.AddWithValue("UserID", userID);
            cmd.Parameters.AddWithValue("PasswordHash", hash);
            RunVoidAuthenticatable(cmd);

        }
        [HttpPost("register")]
        public void RegisterUser([FromBody](Account, string) account)
        {
            if (account.Item1 == null)
            {
                Response.StatusCode = 400;
                return;
            }
            connection.Open();
            string salt = "";
            Random random = new Random();
            for (int i = 0; i < 50; i++)
            {
                salt = salt + (char)(random.Next(32, 126));
            }
            string password = salt + account.Item1.Password;
            SHA256Managed sha256 = new SHA256Managed();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            SqlCommand cmd = SetUpUSP("usp_RegisterUser", new string[] { "Username", "PasswordHash", "Salt", "ResetAddress" }, new object[] { account.Item1.Username, hash, salt, account.Item2 });
            cmd.ExecuteNonQuery();
        }

        [HttpPut("{repo}/{branch}")]
        public void PushBranch(string repo, string branch, [FromBody](Account, Dictionary<int, Commit>) commitDictionary)
        {
            if(commitDictionary.Item2.Count <= 0)
            {
                Response.StatusCode = 202;
                //Response.StatusCode = 400;
                return;
            }
            int repoID = GetRepoID(repo);
            if (repoID < 0)
            {
                Response.StatusCode = 400;
                return;
            }
            Account accnt = commitDictionary.Item1;
            if (accnt == null)
            {
                Response.StatusCode = 401;
                return;
            }
            int userID = GetUserID(accnt.Username, false);
            if (userID < 0)
            {
                Response.StatusCode = 401;
                return;
            }
            byte[] hash = GetFullPasswordHash(userID, accnt.Password, false);
            SqlCommand cmd = new SqlCommand("usp_AddCommit", connection);
            cmd.CommandType = CommandType.StoredProcedure;
            DataTable table = new DataTable();
            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            adapter = new SqlDataAdapter(cmd);
            Commit[] commits = commitDictionary.Item2.Values.ToArray();
            for (int i = 0; i < commits.Length; i++)
            {
                cmd.Parameters.Clear();
                cmd.Parameters.Add(new SqlParameter("CommitID", commitDictionary.Item2.Keys.ToArray()[i]));
                cmd.CommandText = "usp_GetCommitByID";
                table.Clear();
                adapter.Fill(table);
                if (table.Rows.Count <= 0)
                {
                    cmd.CommandText = "usp_AddCommit";
                    cmd.Parameters.Add(new SqlParameter("Message", commits[i].Message));
                    cmd.Parameters.Add(new SqlParameter("ParentID", commits[i].Parent));
                    cmd.Parameters.Add(new SqlParameter("Author", commits[i].Author));
                    cmd.Parameters.Add(new SqlParameter("Committer", commits[i].Committer));
                    cmd.Parameters.Add(new SqlParameter("RepositoryID", repoID));
                    cmd.Parameters.Add(new SqlParameter("TextDiffs", commits[i].Diffs));
                    cmd.Parameters.Add(new SqlParameter("UserID", userID));
                    cmd.Parameters.Add(new SqlParameter("PasswordHash", hash));
                    cmd.Parameters.Add(new SqlParameter("Date", commits[i].DateAndTime));
                    RunVoidAuthenticatable(cmd);
                }
            }

            cmd.CommandText = "usp_UpdateBranch";
            cmd.Parameters.Clear();
            cmd.Parameters.Add(new SqlParameter("RepositoryID", repoID));
            cmd.Parameters.Add(new SqlParameter("BranchName", branch));
            int[] keys = commitDictionary.Item2.Keys.ToArray();
            cmd.Parameters.Add(new SqlParameter("CommitID", keys[0]));
            cmd.Parameters.Add(new SqlParameter("UserID", userID));
            cmd.Parameters.Add(new SqlParameter("PasswordHash", hash));
            table.Clear();
            adapter.Fill(table);
            if (table.Rows.Count <= 0)
            {
                Response.StatusCode = 401;
            }
            else if ((int)table.Rows[0][table.Rows[0].ItemArray.Length - 1] == 1)
            {
                cmd.CommandText = "usp_AddBranch";
                RunVoidAuthenticatable(cmd);
            }
        }

        // DELETE api/pipe/repo/branch
        [HttpDelete("{repo}/{branch}")]
        public void DeleteBranch(string repo, string branch, [FromBody]Account accnt)
        {
            if (accnt == null)
            {
                Response.StatusCode = 401;
                return;
            }
            SqlCommand cmd = new SqlCommand("usp_DeleteBranch", connection);
            int repoID = GetRepoID(repo);
            if (repoID < 0)
            {
                Response.StatusCode = 400;
                return;
            }
            int userID = GetUserID(accnt.Username, false);
            if (userID < 0)
            {
                Response.StatusCode = 401;
                return;
            }
            byte[] hash = GetFullPasswordHash(userID, accnt.Password, false);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("RepositoryID", repoID));
            cmd.Parameters.Add(new SqlParameter("BranchName", branch));
            cmd.Parameters.AddWithValue("UserID", userID);
            cmd.Parameters.AddWithValue("PasswordHash", hash);
            RunVoidAuthenticatable(cmd);
        }

        private int GetRepoID(string repo, bool openConnection = true)
        {
            if (openConnection)
            { connection.Open(); }
            SqlCommand cmd = new SqlCommand("usp_GetRepoID", connection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("RepositoryName", repo));
            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            DataTable data = new DataTable();
            adapter.Fill(data);
            cmd.Parameters.Clear();
            if (data.Rows.Count <= 0)
            {
                return -1;
            }
            int retVal = (int)data.Rows[0][0];
            data.Clear();
            return retVal;
        }

        //VERY DANGEROUS!!!! USE AT YOUR OWN RISK!!!
        // DELETE api/pipe/repo
        [HttpDelete("{repo}")]
        public void DeleteRepo(string repo, [FromBody]Account accnt)
        {
            if (accnt == null)
            {
                Response.StatusCode = 401;
                return;
            }
            int repoID = GetRepoID(repo);
            if (repoID < 0)
            {
                Response.StatusCode = 400;
                return;
            }
            int userID = GetUserID(accnt.Username, false);
            if (userID < 0)
            {
                Response.StatusCode = 401;
                return;
            }
            byte[] hash = GetFullPasswordHash(userID, accnt.Password, false);
            SqlCommand cmd = new SqlCommand("usp_DeleteRepo", connection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("RepositoryID", repoID);
            cmd.Parameters.AddWithValue("UserID", userID);
            cmd.Parameters.AddWithValue("PasswordHash", hash);
            RunVoidAuthenticatable(cmd);
        }

        [HttpDelete("perms/{repo}")]
        public void RemoveEdit(string repo, [FromBody](Account, Account) accounts)
        {
            int repoID = GetRepoID(repo);
            if (repoID < 0)
            {
                Response.StatusCode = 400;
                return;
            }
            int userID = GetUserID(accounts.Item1.Username, false);
            if (userID < 0)
            {
                Response.StatusCode = 401;
                return;
            }
            int targetID = GetUserID(accounts.Item2.Username, false);
            if (targetID < 0)
            {
                Response.StatusCode = 401;
                return;
            }
            SqlCommand cmd = SetUpUSP("usp_RevokeEdit", new (string, object)[] { ("UserID", userID), ("TargetID", targetID), ("RepositoryID", repoID), ("PasswordHash", GetFullPasswordHash(userID, accounts.Item1.Password, false)) });
            RunVoidAuthenticatable(cmd);
        }

        [HttpPost("perms/{repo}")]
        public void AllowEdit(string repo, [FromBody](Account, Account) accounts)
        {
            int repoID = GetRepoID(repo);
            if (repoID < 0)
            {
                Response.StatusCode = 400;
                return;
            }
            int userID = GetUserID(accounts.Item1.Username, false);
            if (userID < 0)
            {
                Response.StatusCode = 401;
                return;
            }
            int targetID = GetUserID(accounts.Item2.Username, false);
            if (targetID < 0)
            {
                Response.StatusCode = 401;
                return;
            }
            SqlCommand cmd = SetUpUSP("usp_AllowEdit", new (string, object)[] { ("UserID", userID), ("TargetID", targetID), ("RepositoryID", repoID), ("PasswordHash", GetFullPasswordHash(userID, accounts.Item1.Password, false)) });
            RunVoidAuthenticatable(cmd);
        }

        [HttpPost("changePass")]
        public void ChangePassword([FromBody](Account, string) account)
        {
            if (account.Item1 == null)
            {
                Response.StatusCode = 400;
                return;
            }
            int userID = GetUserID(account.Item1.Username, true);
            if (userID < 0)
            {
                Response.StatusCode = 401;
                return;
            }
            string salt = "";
            Random random = new Random();
            for (int i = 0; i < 50; i++)
            {
                salt = salt + (char)(random.Next(32, 126));
            }
            string password = salt + account.Item2;
            SHA256Managed sha256 = new SHA256Managed();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            RunVoidAuthenticatable(SetUpUSP("usp_ChangePassword", ("UserID", userID), ("PasswordHash", GetFullPasswordHash(userID, account.Item1.Password, false)), ("NewPasswordHash", hash), ("NewSalt", salt)));
        }

        [HttpPost("passwordReset/{username}")]
        public void ResetPassword(string username)
        {
            int userID = GetUserID(username, true);
            if (userID < 0)
            {
                Response.StatusCode = 401;
                return;
            }
            var resetAddress = SqlReturnObj(SetUpUSP("usp_GetResetAddress", ("UserID", userID)));
            if (resetAddress == null)
            {
                Response.StatusCode = 401;
                return;
            }
            var saltP = SqlReturnObj(SetUpUSP("usp_GetSaltUserID", ("UserID", userID)));
            if (saltP == null)
            {
                Response.StatusCode = 401;
                return;
            }


            // new SmtpClient()
            // new NetworkCredentials
            // new MailMessage().To.Add()
            SmtpClient client = new SmtpClient()
            {
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Host = "smtp.gmail.com",
                Credentials = emailCredentials
            };
            Random random = new Random();
            StringBuilder sb = new StringBuilder(siteAddress);
            sb.Append("/api/pipe/passwordResetFinal?userID=");
            sb.Append(userID);
            sb.Append("&code=");
            string salt = (string)saltP;
            for (int i = 0; i < salt.Length; i++)
            {
                sb.Append($"%{((int)salt[i]):X}");
            }
            try
            {
                MailMessage message = new MailMessage(emailCredentials.UserName, (string)resetAddress)
                {
                    Subject = passwordResetSubject,
                    Body = passwordResetBody.Replace("%LINK", sb.ToString())
                };
                client.Send(message);
            }
            catch (Exception)
            {
                Response.StatusCode = 400;
                return;
            }
        }

        [HttpGet("passwordResetFinal")]
        public string FinishPasswordReset(int userID, string code)
        {
            Random random = new Random();
            string newPassword = "";
            for (int i = 0; i < 15; i++)
            {
                newPassword += (char)random.Next(32, 126);
            }
            string salt = "";
            for (int i = 0; i < 50; i++)
            {
                salt = salt + (char)(random.Next(32, 126));
            }
            string password = salt + newPassword;
            SHA256Managed sha256 = new SHA256Managed();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            var saltToCheck = SqlReturnObj(SetUpUSP("usp_GetSaltUserID", ("UserID", userID)));
            if (saltToCheck == null)
            {
                Response.StatusCode = 401;
                return "Authentication Error: Incorrect userID";
            }
            var passwordHash = SqlReturnObj(SetUpUSP("usp_GetPasswordHash", ("UserID", userID)));
            if (passwordHash == null)
            {
                Response.StatusCode = 401;
                return "Authentication Error: Incorrect userID";
            }
            if ((string)saltToCheck != code)
            {
                Response.StatusCode = 401;
                return "Authentication Error: Incorrect code or userID";
            }
            RunVoidAuthenticatable(SetUpUSP("usp_ChangePassword", ("UserID", userID), ("PasswordHash", (byte[])passwordHash), ("NewPasswordHash", hash), ("NewSalt", salt)));
            return $"Please change your password. Your password has been reset to {newPassword}";
        }
    }
}
