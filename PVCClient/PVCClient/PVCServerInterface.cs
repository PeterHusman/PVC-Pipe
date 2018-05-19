using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PVCClient
{
    public class PVCServerInterface
    {
        public string Uri { get; set; }
        public Commit Head { get; set; }
        public Commit[] Branches { get; set; }
        HttpClient client;

        public PVCServerInterface(Commit head)
        {
            client = new HttpClient();
            Head = head;
        }

        public async void Push()
        {
            await client.PostAsync(Uri,new StringContent(JsonConvert.SerializeObject()));
        }
    }
}
