using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVCPipeClient
{
    class Commit
    {
        public string Diffs { get; set; }

        public int Parent { get; set; }

        public string Message { get; set; }

        public string Author { get; set; }

        public string Committer { get; set; }

        public Commit(string diffs, string message, string author, string committer, int parent)
        {
            Diffs = diffs;
            Message = message;
            Author = author;
            Committer = committer;
            Parent = parent;
        }
    }
}
