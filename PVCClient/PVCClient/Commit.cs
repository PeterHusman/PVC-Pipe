using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PVCClient
{
    public class Commit
    {
        public byte[][] Blobs { get; set; }

        public Commit[] Parents { get; set; }

        public string Message { get; set; }

        public string Author { get; set; }

        public string Committer { get; set; }

        public Commit(byte[][] blobs, string message, string author, string committer, Commit[] parents)
        {
            Blobs = blobs;
            Message = message;
            Author = author;
            Committer = committer;
            Parents = parents;
        }
    }
}
