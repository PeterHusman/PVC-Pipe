using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PVCPipeClient
{
    public class PVCServerInterface
    {
        public string Uri { get; set; }
        public Commit Head { get; set; }
        public Commit[] Branches { get; set; }
        HttpClient client;

        public PVCServerInterface(Commit head, string uri)
        {
            client = new HttpClient();
            Head = head;
            Uri = uri;
        }

        //public async void Pull

        public async void Push(Commit commit)
        {
            await client.PostAsync(Uri,new StringContent(JsonConvert.SerializeObject(commit), Encoding.UTF8, "application/json"));
        }
    }
}
