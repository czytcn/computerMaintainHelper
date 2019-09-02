using Topshelf;

namespace computerMaintainHelper
{

    class Program
    {

        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<ServiceCore>(
                    s =>
                    {
                        s.ConstructUsing(name => new ServiceCore());
                        s.WhenStarted(tc => tc.Start());
                        s.WhenStopped(tc => tc.Stop());
                    }
                     );
                x.RunAsLocalSystem();
                x.SetDescription("客户端信息即时查询接口。");
                x.SetDisplayName("computerMaintainHelper");
                x.SetServiceName("computerMaintainHelper");
            });
        }
    }
}
