using computerMaintainHelper.Core;
using Microsoft.VisualBasic.Devices;
using Nancy;
using Nancy.Security;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace computerMaintainHelper.Modules
{
    /// <summary>
    /// 无需授权操作API
    /// </summary>
    public class ComputerBasicInfoModule : NancyModule
    {
        public ComputerBasicInfoModule()
        {

            Get("/processinfo", async (r, w) =>
            {
                 //this.RequiresAuthentication();
                var result = new List<ProcessInfoEx>();
                await Task.Run(() =>
                   {
                       var info = Core.MaintainModel.GetAllTcpConnections();

                       foreach (var el in info)
                       {
                           result.Add(new ProcessInfoEx { LocalAdress = el.LocalAddress.ToString(), LocalPort = el.LocalPort, RemoteAdress = el.RemoteAddress.ToString(), RemotePort = el.RemotePort, PID = el.PID, State = Core.MaintainModel.GetTCPState(el.State) });

                       }

                   });
                return JsonConvert.SerializeObject(result);
            });

            Post("/getserverdelay", async (r, w) =>
            {
                var result = new Dictionary<string, ServerDelayInfo>();
                await Task.Run(() =>
                {
                    var request = this.Request.Form;
                    // 如果是非form传参，就取query值
                    if (request.Count == 0)
                    {
                        request = this.Request.Query;
                    }
                    var serverlist = request.serverlist.HasValue ? request.serverlist.Value : "";
                    var pingtimes = request.pingtimes.HasValue ? request.pingtimes.Value : "10";
                    result = Core.MaintainModel.GetDelayFromServers((serverlist as string).Split(',').ToList(), int.TryParse(pingtimes, out int pingtime) ? pingtime : 10);
                });
                return JsonConvert.SerializeObject(result);
            });
            Get("/osinfo", async (r, w) =>
            {
                ComputerInfo info = new ComputerInfo { };
                await Task.Run(() =>
                {

                    info = (new Computer()).Info;
                });
                return JsonConvert.SerializeObject(info);
            });
            Get("/diskinfo", async (r, w) =>
            {
                DriveInfo[] info = DriveInfo.GetDrives();
                Dictionary<string, DiskInfo> result = new Dictionary<string, DiskInfo>();
                await Task.Run(() =>
                {
                    foreach (var item in info)
                    {
                        result.Add(item.Name, new DiskInfo { TotalSize = item.TotalSize / 1024 / 1024, TotalFreeSpace = item.TotalFreeSpace / 1024 / 1024, AvaliableFreeSpace = item.AvailableFreeSpace / 1024 / 1024, DriveFormat = item.DriveFormat, DriveType = Core.MaintainModel.GetDriveType(item.DriveType), IsReady = item.IsReady, VolumeLabel = item.IsReady ? item.VolumeLabel : "UNKNOWN", SizeUnit = "MB" });
                    }

                });
                return JsonConvert.SerializeObject(result);
            });

            Get("/listservice", async (r, w) =>
            {
                var info = ServiceController.GetServices();
                List<ServiceInfo> result = new List<ServiceInfo>();
                await Task.Run(() =>
                {
                    foreach (var item in info)
                    {

                        result.Add(new ServiceInfo { ServiceDisplayName = item.DisplayName, ServiceName = item.ServiceName, ServiceStatus = item.Status.ToString() });
                    }
                }
                );
                return JsonConvert.SerializeObject(result);
            });
            Get("/netinterfaces", async (r, w) =>
            {
                List<NetInterfaceInfo> result = new List<NetInterfaceInfo>();

                await Task.Run(()=> {
                    foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                    {

                        var info = new NetInterfaceInfo { };
                        info.InterfaceName = nic.Name;
                        info.PhysicalAddress = nic.GetPhysicalAddress().ToString();
                        info.InterfaceStatistics = nic.GetIPv4Statistics();
                        info.Speed = nic.Speed;
                        result.Add(info);
                       
                    }

                });

                return JsonConvert.SerializeObject(result);

            });


        }
    }
}
