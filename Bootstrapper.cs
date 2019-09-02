using Nancy;
using Nancy.Authentication.Basic;
using Nancy.Bootstrapper;
using Nancy.Configuration;
using Nancy.Diagnostics;
using Nancy.TinyIoc;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace computerMaintainHelper
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        public static Guid ServiceToken = Guid.NewGuid();
        public static byte[] IconToBytes(Icon icon)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                icon.Save(ms);
                return ms.ToArray();
            }
        }
        protected override byte[] FavIcon => IconToBytes(Properties.Resources.FavIcon);
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            pipelines.EnableBasicAuthentication(new BasicAuthenticationConfiguration(
                container.Resolve<IUserValidator>(),
                "advancefunc"));

        }
        public override void Configure(INancyEnvironment environment)
        {
            environment.Diagnostics(true, ServiceToken.ToString());
            base.Configure(environment);
        }
    }
}
