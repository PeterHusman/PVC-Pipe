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
            if(data.Rows.Count == 0)
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
            for(int i = 0; i < commits.Length;i++)
            {
                commits[i] = new Commit((string)table.Rows[i]["TextDiffs"],(string)table.Rows[i]["Message"],(string)table.Rows[i]["Author"],(string)table.Rows[i]["Committer"],(int)table.Rows[i]["ParentID"]);
            }
            return commits;
        }



        // GET api/pipe/5
        [HttpGet("{id}/{id2}")]
        public string Get(int id, int id2)
        {
            return id + " " + id2;
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]Commit[] commits, [FromBody]string branch)
        {

        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
