using Nancy.Hosting.Self;
using System;
using System.Configuration;
using CronNET;
using System.Threading.Tasks;
using WebSocketSharp;
using computerMaintainHelper.Core;

namespace computerMaintainHelper
{
    public class ServiceCore
    {
        private string Url = "http://localhost";
        internal static readonly string Port = ConfigurationManager.AppSettings["ServePort"];
        internal static readonly string ComputerRemark = ConfigurationManager.AppSettings["ComputerRemark"];
        internal static readonly string Tag = ConfigurationManager.AppSettings["Tag"];
        private readonly string RegisterCron = ConfigurationManager.AppSettings["RegisterCron"];
        private readonly string CentreServer = ConfigurationManager.AppSettings["CentreServer"];
        private readonly string ReConnectInterval = ConfigurationManager.AppSettings["ReConnectInterval"];
        private NancyHost _Nancy;
        public bool WsCloseSignal = false;
        public void Start()
        {
            try
            {
                // 初始化自托管服务
                HostConfiguration hostConf = new HostConfiguration();
                hostConf.RewriteLocalhost = true;
                int maxconn = int.Parse(ConfigurationManager.AppSettings["MaxConn"]);
                hostConf.MaximumConnectionCount = maxconn;
                hostConf.UrlReservations.CreateAutomatically = true;
                var url = new Uri($"{Url}:{Port}");
                _Nancy = new NancyHost(hostConf, url);
                _Nancy.Start();
                Console.WriteLine($"当前监听:{url}\r\n诊断地址:{url}/_Nancy/\r\n访问Token:{Bootstrapper.ServiceToken}");
                //启动websocket服务
                WsHandler();

            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }

        }
        /// <summary>
        /// 注册websocket对象及事件
        /// </summary>
        private void WsHandler()
        {
            if (!string.IsNullOrEmpty(CentreServer))
            {

                Task.Run(() =>
                {
                    try
                    {
                        using (var ws = new WebSocket(CentreServer))
                        {
                            // 任务是否已经添加
                            bool TaskIsAdded = false;
                            CronDaemon crondaemon = new CronDaemon();
                            ws.EmitOnPing = true;
                            ws.OnOpen += (sender, e) =>
                            {
                                Console.WriteLine("Websokcet Connected");
                                // 是否已经加添加过定时任务
                                if (!TaskIsAdded)
                                {
                                    crondaemon.AddJob(RegisterCron, SendServiceInfo);
                                    TaskIsAdded = true;
                                }

                                crondaemon.Start();
                            };
                            ws.OnClose += (sender, e) =>
                            {
                                Console.WriteLine($"错误:{e.Code}:{e.Reason}.Websokcet Closed");
                                if (TaskIsAdded)
                                {
                                    //停止定时任务
                                    crondaemon.Stop();
                                }

                                Reconnect(ws);
                                //重连
                                void Reconnect(WebSocket websocket)
                                {
                                    int.TryParse(ReConnectInterval, out int reconninterv);
                                    Task.Delay(reconninterv).Wait();
                                    websocket.ConnectAsync();
                                }



                            };
                            ws.OnMessage += (sender, e) =>
                            {
                                if (e.IsText)
                                {
                                    Console.WriteLine(e.Data);
                                }
                                if (e.IsBinary)
                                {
                                    Console.WriteLine(e.RawData);
                                }
                                if (e.IsPing)
                                {
                                    Console.WriteLine("收到ping数据");
                                }
                            };
                            ws.OnError += (sender, e) =>
                            {
                                Console.WriteLine(e.Message);
                                //ws.CloseAsync();
                            };
                            ws.ConnectAsync();
                            while (!WsCloseSignal)
                            {
                                Task.Delay(100).Wait();
                            }

                            // 向websocket服务器发送当前服务的相关信息
                            void SendServiceInfo()
                            {
                                Task.Run(() =>
                                {
                                    if (ws.IsAlive)
                                    {
                                        ws.SendAsync($"{MsgType.REG}{MaintainModel.GetServiceRegisterInfo()}", (b) =>
                                        {
                                            if (b)
                                            {
                                                Console.WriteLine("send data ok");
                                            }
                                        });
                                    }
                                    else
                                    {
                                        Console.WriteLine("websocket is not Connected");
                                    }

                                }
                                );
                            }
                        }

                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine(ex.Message);
                    }
                });
            }


        }



        public void Stop()
        {
            try
            {
                // 停止Nancy服务
                _Nancy.Stop();
                WsCloseSignal = true;

            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }

        }



    }
}
