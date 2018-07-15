using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Pipe
{
    public interface INotifier
    {
        string Address { get; set; }
        void SendMail();
    }

    public class EmailNotifier : INotifier
    {
        public EmailNotifier(string address)
        {
            Address = address;
        }

        public string Address { get; set; }

        public void SendMail()
        {
            System.Diagnostics.Debug.WriteLine($"Mail sent to {Address}");
        }
    }


    public class SMSNotifier : INotifier
    {
        public SMSNotifier(string address)
        {
            Address = address;
        }

        public string Address { get; set; }

        public void SendMail()
        {
            System.Diagnostics.Debug.WriteLine($"SMS sent to {Address}");
        }
    }


    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

#if USESMS
            services.AddTransient<INotifier, SMSNotifier>((i) => new SMSNotifier("123-555-1212"));
#else
            services.AddTransient<INotifier, EmailNotifier>(((i) => new EmailNotifier("peterAdmin@pipe.com")));
#endif
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}
