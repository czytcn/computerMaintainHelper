using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace computerMaintainHelper.Core
{
    public enum MsgType
    {
        REG =1,
        CMD
    }
    #region Inner Class
    /// <summary>
    /// 进程连接信息
    /// </summary>
    public class ProcessInfoEx
    {
        /// <summary>
        /// 本机地址
        /// </summary>
        public string LocalAdress { get; set; }
        /// <summary>
        /// 本机监听端口
        /// </summary>
        public int LocalPort { get; set; }
        /// <summary>
        /// 远端地址
        /// </summary>
        public string RemoteAdress { get; set; }
        /// <summary>
        /// 远端端口
        /// </summary>
        public int RemotePort { get; set; }
        /// <summary>
        /// 当前状态
        /// </summary>
        public string State { get; set; }
        /// <summary>
        /// 对应进程PID
        /// </summary>
        public uint PID { get; set; }


    }
    /// <summary>
    /// 服务器延迟
    /// </summary>
    public class ServerDelayInfo
    {
        public long[] AllPingDelay { get; set; }
        /// <summary>
        /// 最小延迟
        /// </summary>
        public long MinDelay { get; set; }
        /// <summary>
        /// 最大延迟
        /// </summary>
        public long MaxDelay { get; set; }
        /// <summary>
        /// 平均延迟
        /// </summary>
        public double AvgDelay { get; set; }
        /// <summary>
        /// 单位
        /// </summary>
        public string Unit { get; set; }
    }
    /// <summary>
    /// 磁盘信息
    /// </summary>
    public class DiskInfo
    {
        public long TotalSize { get; set; }
        public long TotalFreeSpace { get; set; }
        public long AvaliableFreeSpace { get; set; }
        public string DriveFormat { get; set; }
        public string DriveType { get; set; }
        public bool IsReady { get; set; }
        public string VolumeLabel { get; set; }
        public string SizeUnit { get; set; }
    }
    /// <summary>
    /// 服务信息
    /// </summary>
    public class ServiceInfo
    {
        /// <summary>
        /// 服务名称
        /// </summary>
        public string ServiceName { get; set; }
        /// <summary>
        /// 服务显示名称
        /// </summary>
        public string ServiceDisplayName { get; set; }
        /// <summary>
        /// 服务状态
        /// </summary>
        public string ServiceStatus { get; set; }
    }

    /// <summary>
    /// 服务重启结果类
    /// </summary>
    public class RestartServiceResult
    {
        /// <summary>
        /// 服务名称
        /// </summary>
        public string ServiceName { get; set; }
        /// <summary>
        /// 服务状态
        /// </summary>
        public string ServiceStatus { get; set; }
        /// <summary>
        /// 重启是否成功
        /// </summary>
        public bool RestartSucess  { get; set; }
        /// <summary>
        /// 失败错误描述
        /// </summary>
        public string FailErr { get; set; }
    }
    /// <summary>
    /// 服务器发送信息
    /// </summary>
    public class ServiceRegisterInfo
    {
        /// <summary>
        /// 高级功能访问Token
        /// </summary>
        public Guid Token { get; set; }
        /// <summary>
        /// CPU 物理ID
        /// </summary>
        public string CPUID { get; set; }
        /// <summary>
        /// 服务访问地址
        /// </summary>
        public List<string> ServiceAddress { get; set; }
        /// <summary>
        /// 最后活跃时间
        /// </summary>

        public DateTime LastActive { get; set; }
        /// <summary>
        /// 计算机备注
        /// </summary>
        public string ComputerRemark { get; set; }
        /// <summary>
        /// 附加属性
        /// </summary>
        public string Tag { get; set; }
    }
    #endregion
    public class MaintainModel
    {
        [DllImport("iphlpapi.dll", SetLastError = true)]
        static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwOutBufLen, bool sort, int ipVersion, TCP_TABLE_CLASS tblClass, uint reserved = 0);


        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCPROW_OWNER_PID
        {
            // DWORD is System.UInt32 in C#
            System.UInt32 state;
            System.UInt32 localAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            byte[] localPort;
            System.UInt32 remoteAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            byte[] remotePort;
            System.UInt32 owningPid;
            public uint PID
            {
                get
                {
                    return owningPid;
                }
            }

            public uint State
            {
                get { return state; }
            }

            public System.Net.IPAddress LocalAddress
            {
                get
                {
                    return new System.Net.IPAddress(localAddr);
                }
            }
            public ushort LocalPort
            {
                get
                {
                    return BitConverter.ToUInt16(
                    new byte[2] { localPort[1], localPort[0] }, 0);
                }
            }
            public System.Net.IPAddress RemoteAddress
            {
                get
                {
                    return new System.Net.IPAddress(remoteAddr);
                }
            }
            public ushort RemotePort
            {
                get
                {
                    return BitConverter.ToUInt16(
                    new byte[2] { remotePort[1], remotePort[0] }, 0);
                }
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_TCPTABLE_OWNER_PID
        {
            public uint dwNumEntries;
            MIB_TCPROW_OWNER_PID table;
        }
        enum TCP_TABLE_CLASS
        {
            TCP_TABLE_BASIC_LISTENER,
            TCP_TABLE_BASIC_CONNECTIONS,
            TCP_TABLE_BASIC_ALL,
            TCP_TABLE_OWNER_PID_LISTENER,
            TCP_TABLE_OWNER_PID_CONNECTIONS,
            TCP_TABLE_OWNER_PID_ALL,
            TCP_TABLE_OWNER_MODULE_LISTENER,
            TCP_TABLE_OWNER_MODULE_CONNECTIONS,
            TCP_TABLE_OWNER_MODULE_ALL
        }



        static DateTime getAllTcpConnections_cached_lastTime;
        static MIB_TCPROW_OWNER_PID[] getAllTcpConnections_cached;
        //public TcpRow[] GetAllTcpConnections()
        public static MIB_TCPROW_OWNER_PID[] GetAllTcpConnections()
        {
            if (getAllTcpConnections_cached == null || getAllTcpConnections_cached_lastTime + new TimeSpan(0, 0, 1) < DateTime.Now)
            {
                //  TcpRow is my own class to display returned rows in a nice manner.
                //    TcpRow[] tTable;
                MIB_TCPROW_OWNER_PID[] tTable;
                int AF_INET = 2;    // IP_v4
                int buffSize = 0;
                // how much memory do we need?
                uint ret = GetExtendedTcpTable(IntPtr.Zero, ref buffSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);
                IntPtr buffTable = Marshal.AllocHGlobal(buffSize);
                try
                {
                    ret = GetExtendedTcpTable(buffTable, ref buffSize, true, AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL);
                    if (ret != 0)
                    {
                        return new MIB_TCPROW_OWNER_PID[0];
                    }
                    // get the number of entries in the table
                    //MibTcpTable tab = (MibTcpTable)Marshal.PtrToStructure(buffTable, typeof(MibTcpTable));
                    MIB_TCPTABLE_OWNER_PID tab = (MIB_TCPTABLE_OWNER_PID)Marshal.PtrToStructure(buffTable, typeof(MIB_TCPTABLE_OWNER_PID));
                    //IntPtr rowPtr = (IntPtr)((long)buffTable + Marshal.SizeOf(tab.numberOfEntries) );
                    IntPtr rowPtr = (IntPtr)((long)buffTable + Marshal.SizeOf(tab.dwNumEntries));
                    // buffer we will be returning
                    //tTable = new TcpRow[tab.numberOfEntries];
                    tTable = new MIB_TCPROW_OWNER_PID[tab.dwNumEntries];
                    //for (int i = 0; i < tab.numberOfEntries; i++)        
                    for (int i = 0; i < tab.dwNumEntries; i++)
                    {
                        //MibTcpRow_Owner_Pid tcpRow = (MibTcpRow_Owner_Pid)Marshal.PtrToStructure(rowPtr, typeof(MibTcpRow_Owner_Pid));
                        MIB_TCPROW_OWNER_PID tcpRow = (MIB_TCPROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_PID));
                        //tTable[i] = new TcpRow(tcpRow);
                        tTable[i] = tcpRow;
                        rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(tcpRow));   // next entry
                    }
                }
                finally
                {
                    // Free the Memory
                    Marshal.FreeHGlobal(buffTable);
                }


                getAllTcpConnections_cached = tTable;
                getAllTcpConnections_cached_lastTime = DateTime.Now;
            }
            return getAllTcpConnections_cached;
        }
        /// <summary>
        /// 获取IP状态
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static string GetTCPState(UInt32 state)
        {
            switch (state)
            {
                case 1:
                    return "MIB_TCP_STATE_CLOSED";
                case 2:
                    return "MIB_TCP_STATE_LISTEN";
                case 3:
                    return "MIB_TCP_STATE_SYN_SENT";
                case 4:
                    return "MIB_TCP_STATE_SYN_RCVD";
                case 5:
                    return "MIB_TCP_STATE_ESTAB";
                case 6:
                    return "MIB_TCP_STATE_FIN_WAIT1";
                case 7:
                    return "MIB_TCP_STATE_FIN_WAIT2";
                case 8:
                    return "MIB_TCP_STATE_CLOSE_WAIT";
                case 9:
                    return "MIB_TCP_STATE_CLOSING";
                case 10:
                    return "MIB_TCP_STATE_LAST_ACK";
                case 11:
                    return "MIB_TCP_STATE_TIME_WAIT";
                case 12:
                    return "MIB_TCP_STATE_DELETE_TCB";
                default:
                    return "UNKNOWN";
            }

        }
        /// <summary>
        /// 获取磁盘类型
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static string GetDriveType(DriveType t)
        {

            switch (t)
            {
                case DriveType.Unknown:
                    return "未知的驱动器类型";
                case DriveType.NoRootDirectory:
                    return "驱动器没有根目录";
                case DriveType.Removable:
                    return "可移动存储设备";
                case DriveType.Fixed:
                    return "本地固定磁盘";
                case DriveType.Network:
                    return "网络驱动器";
                case DriveType.CDRom:
                    return "光盘设备";
                case DriveType.Ram:
                    return "RAM 磁盘";
                default:
                    return "未知的驱动器类型";
            }
        }
        /// <summary>
        /// 从指定列表服务器获取延迟
        /// </summary>
        /// <param name="urls"></param>
        /// <param name="TryTimes"></param>
        /// <returns></returns>
        public static Dictionary<string, ServerDelayInfo> GetDelayFromServers(List<string> urls, int TryTimes = 10)
        {
            var result = new Dictionary<string, ServerDelayInfo>();
            foreach (string item in urls)
            {

                var delay = Enumerable.Repeat<string>(item, TryTimes).AsParallel().WithDegreeOfParallelism(64).Select(h =>
                {

                    try
                    {
                        return new Ping().Send(h).RoundtripTime;
                    }
                    catch (Exception)
                    {
                        return 0;

                    }
                });
                result.Add(item, new ServerDelayInfo { AllPingDelay = delay.ToArray(), MinDelay = delay.Min(), MaxDelay = delay.Max(), AvgDelay = delay.Average(), Unit = "ms" });
            }
            return result;
        }
        /// <summary>
        /// 获取服务相关信息
        /// </summary>
        /// <returns></returns>
        public static string GetServiceRegisterInfo()
        {
            var result = string.Empty;
            var regInfo = new ServiceRegisterInfo();
            regInfo.ServiceAddress = GetNancyServceUrl(ServiceCore.Port, bool.Parse(ConfigurationManager.AppSettings["OnlyPushIPv4"]));
            regInfo.Token = Bootstrapper.ServiceToken;
            regInfo.LastActive = DateTime.Now;
            regInfo.ComputerRemark = ServiceCore.ComputerRemark;
            regInfo.Tag = ServiceCore.Tag;
            regInfo.CPUID = CPUID.GetCpuId();
            return JsonConvert.SerializeObject(regInfo);
            List<string> GetNancyServceUrl(string port, bool OnlyPushIPv4)
            {
                Regex regex = new Regex(@"^((2(5[0-5]|[0-4]\d))|[0-1]?\d{1,2})(\.((2(5[0-5]|[0-4]\d))|[0-1]?\d{1,2})){3}$");
                List<string> adress = new List<string>();
                string strHostName = Dns.GetHostName(); //得到本机的主机名
                IPHostEntry ipEntry = Dns.GetHostEntry(strHostName); //取得本机IP
                var Addr = ipEntry.AddressList;
                foreach (var item in Addr)
                {
                    var Ipadress = item.ToString();
                    if (OnlyPushIPv4)
                    {
                        if (regex.IsMatch(Ipadress))
                        {
                            adress.Add($"{Ipadress}:{port}");
                        }
                    }
                    else
                    {
                        adress.Add($"{Ipadress}:{port}");
                    }

                }
                return adress;
            }
        }
    }
}
