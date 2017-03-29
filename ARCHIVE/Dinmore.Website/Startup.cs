using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Dinmore.Website.Startup))]
namespace Dinmore.Website
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
