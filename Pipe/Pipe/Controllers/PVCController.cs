using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Pipe.Models;

namespace Pipe.Controllers
{
    [Route("api/pipe")]
    public class PVCController : Controller
    {
        SqlConnection connection = new SqlConnection("server=GMRMLTV; database=PVC; user=sa; password=GreatMinds110");

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

        Commit[] commitHistoryFromDataBaseEntry(DataTable table)
        {
            Commit[] commits = new Commit[table.Rows.Count];
            for (int i = 0; i < commits.Length; i++)
            {
                commits[i] = new Commit((string)table.Rows[i]["TextDiffs"], (string)table.Rows[i]["Message"], (string)table.Rows[i]["Author"], (string)table.Rows[i]["Committer"], (int)table.Rows[i]["ParentID"]);
            }
            return commits;
        }

        //[HttpPost]
        //public void Post([FromBody]Commit[] commits, [FromBody]string branch)
        //{

        //}

        [HttpPost("{repo}/{branch}")]
        public void CreateBranch(string repo, string branch, int commitID)
        {
            int repoID = GetRepoID(repo);
            SqlCommand cmd = new SqlCommand("usp_CreateBranch", connection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("RepositoryID", repoID));
            cmd.Parameters.Add(new SqlParameter("BranchName", branch));
            cmd.Parameters.Add(new SqlParameter("CommitID", commitID));
            cmd.ExecuteNonQuery();
        }

        [HttpPost("{repo}")]
        public void CreateRepo(string repo)
        {
            connection.Open();
            SqlCommand cmd = new SqlCommand("usp_CreateRepoWOID", connection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("Name", repo));
            cmd.ExecuteNonQuery();
        }

        [HttpPut("{repo}/{branch}")]
        public void PushBranch(string repo, string branch, [FromBody]Commit[] commits)
        {
            int repoID = GetRepoID(repo);
            SqlCommand cmd = new SqlCommand("usp_AddCommit", connection);
            cmd.CommandType = CommandType.StoredProcedure;
            for (int i = 0; i < commits.Length; i++)
            {
                cmd.Parameters.Clear();
                cmd.Parameters.Add(new SqlParameter("CommitID", commits[i].GetHashCode()));
                cmd.Parameters.Add(new SqlParameter("Message", commits[i].Message));
                cmd.Parameters.Add(new SqlParameter("ParentID", commits[i].Parent));
                cmd.Parameters.Add(new SqlParameter("Author", commits[i].Author));
                cmd.Parameters.Add(new SqlParameter("Committer", commits[i].Committer));
                cmd.Parameters.Add(new SqlParameter("RepositoryID", repoID));
                cmd.Parameters.Add(new SqlParameter("TextDiffs", commits[i].Diffs));
                cmd.ExecuteNonQuery();
            }

            cmd.CommandText = "usp_UpdateBranch";
            cmd.Parameters.Clear();
            cmd.Parameters.Add(new SqlParameter("RepositoryID", repoID));
            cmd.Parameters.Add(new SqlParameter("BranchName", branch));
            cmd.Parameters.Add(new SqlParameter("CommitID", commits[commits.Length - 1].GetHashCode()));
            int rows = cmd.ExecuteNonQuery();
            if (rows <= 0)
            {
                cmd.CommandText = "usp_CreateBranch";
                cmd.ExecuteNonQuery();
            }
        }

        // DELETE api/pipe/repo/branch
        [HttpDelete("{repo}/{branch}")]
        public void DeleteBranch(string repo, string branch)
        {
            SqlCommand cmd = new SqlCommand("usp_DeleteBranch", connection);
            int repoID = GetRepoID(repo);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("RepositoryID", repoID));
            cmd.Parameters.Add(new SqlParameter("BranchName", branch));
            cmd.ExecuteNonQuery();
        }

        private int GetRepoID(string repo)
        {
            connection.Open();
            SqlCommand cmd = new SqlCommand("usp_GetRepoID", connection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("RepositoryName", repo));
            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
            DataTable data = new DataTable();
            adapter.Fill(data);
            cmd.Parameters.Clear();
            int retVal = (int)data.Rows[0][0];
            data.Clear();
            return retVal;
        }

        //VERY DANGEROUS!!!! USE AT YOUR OWN RISK!!!
        // DELETE api/pipe/repo
        [HttpDelete("{repo}")]
        public void DeleteRepo(string repo)
        {
            int repoID = GetRepoID(repo);
            SqlCommand cmd = new SqlCommand("usp_DeleteRepo", connection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("RepositoryID", repoID);
            cmd.ExecuteNonQuery();
        }
    }
}
