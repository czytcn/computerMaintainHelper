using computerMaintainHelper.Core;
using Nancy;
using Nancy.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Management;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace computerMaintainHelper.Modules
{
    /// <summary>
    /// 需授权操作API
    /// </summary>
    public class ComputerExtModule : NancyModule
    {
        public ComputerExtModule()
        {
            // 启用验证，只验证了token
            this.RequiresAuthentication();
            // 重启服务
            Post("/restartservice", async (r, w) =>
            {
                // 取传入参数
                var request = this.Request.Form;
                // 如果是非form传参，就取query值
                if (request.Count == 0)
                {
                    request = this.Request.Query;
                }
                var servicename = request.servicename.HasValue ? request.servicename.Value : "";
                RestartServiceResult restartresult = new RestartServiceResult();
                await Task.Run(() =>
                {
                    restartresult = restartservice(servicename);

                });
                return JsonConvert.SerializeObject(restartresult);
                // 局部函数 重启服务
                RestartServiceResult restartservice(string svcname)
                {
                    RestartServiceResult result = new RestartServiceResult();

                    using (ServiceController svcController = new ServiceController(svcname))
                    {
                        result.ServiceName = svcname;
                        try
                        {

                            if ((svcController.Status.Equals(ServiceControllerStatus.Running)) || (svcController.Status.Equals(ServiceControllerStatus.StartPending)))
                            {
                                svcController.Stop();
                            }
                            svcController.WaitForStatus(ServiceControllerStatus.Stopped);
                            svcController.Start();
                            svcController.WaitForStatus(ServiceControllerStatus.Running);
                            result.RestartSucess = true;
                            result.ServiceStatus = svcController.Status.ToString();
                        }
                        catch (Exception e)
                        {
                            result.RestartSucess = false;
                            result.ServiceStatus = svcController.Status.ToString();
                            result.FailErr = e.Message;
                        }
                    }
                    return result;

                }

            });
            // 修改远程桌面端口
            //Post("/changeremoteport", async (r, w) => { });
            // 添加防火墙规则
            // 节点互ping
            // 重启网卡
            Post("/RestartNetAdapter",async(r,w)=> {
                // 取传入参数
                var request = this.Request.Form;
                // 如果是非form传参，就取query值
                if (request.Count == 0)
                {
                    request = this.Request.Query;
                }
                var adapterName = request.adapterName;
                dynamic o = new ExpandoObject() { };
                await Task.Run(() =>
                {
                    o = RestartNetAdapter(adapterName);

                });
                return JsonConvert.SerializeObject(o);
                dynamic RestartNetAdapter(string NetAdapterName)
                {
                    dynamic result = new ExpandoObject() { };
                    SelectQuery wmiQuery = new SelectQuery("SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionId != NULL");
                    ManagementObjectSearcher searchProcedure = new ManagementObjectSearcher(wmiQuery);
                    foreach (ManagementObject item in searchProcedure.Get())
                    {
                        if (((string)item["NetConnectionId"]) == NetAdapterName)
                        {
                            item.InvokeMethod("Disable", null);
                            //启用
                            item.InvokeMethod("Enable", null);
                            result.Sucess = true;
                            result.time = DateTime.Now;
                            break;
                        }
                        else
                        {
                            result.Sucess = false;
                            result.time = DateTime.Now;
                        }
                    }

                    return result;
                }

            });
            

        }
    }
}
