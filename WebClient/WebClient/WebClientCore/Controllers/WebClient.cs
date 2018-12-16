using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace WebClientCore.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class WebClient : ControllerBase
    {

        //private IMyThing imt;

        public WebClient()//IMyThing myThing)
        {
            //imt = myThing;
        }


        // GET api/values
        /// <summary>
        /// Default getter
        /// </summary>
        /// <remarks>
        /// Serves as a home page.
        /// </remarks>
        /// <returns>some trash</returns>
        [HttpGet]
        public ActionResult<string> Get()
        {
            return "Welcome to PVC-Pipe WebClient Core!";
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {

        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {

        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {

        }
    }
}
