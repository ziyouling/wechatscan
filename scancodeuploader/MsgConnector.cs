using Newtonsoft.Json;
using scancodeuploader.stomp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.Sockets;
//using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace scancodeuploader
{
    public class MsgConnector 
    {
        private string server = "https://course.muketang.com/muke-ws";

        //private string server = "http://localhost/muke-ws";

        private syp.biz.SockJS.NET.Client.SockJS sockjs;
        private StompMessageSerializer serializer = new StompMessageSerializer();

        private string miniName;

        private long openTick;
        private List<ChromeReq> cmdList = new List<ChromeReq>();

        public MsgConnector(string miniName)
        {
            this.miniName = miniName;
        }

        public void Connect()
        {
            Console.WriteLine("****************** socket js connecting..........");
            //syp.biz.SockJS.NET.Client.SockJS.SetLogger(new ConsoleLogger());
            sockjs = new syp.biz.SockJS.NET.Client.SockJS(server);
            sockjs.AddEventListener("open", (sender, e) =>
            {
                Console.WriteLine("****************** socket js opened");
                openTick = DateTime.Now.Ticks;

                var connect = new StompMessage(StompFrame.CONNECT);
                connect["accept-version"] = "1.2";
                connect["host"] = "";
                // first number Zero mean client not able to send Heartbeat, 
                //Second number mean Server will sending heartbeat to client instead
                connect["heart-beat"] = "0,10000";
                sockjs.Send(serializer.Serialize(connect));
              
            });
            sockjs.AddEventListener("message", (sender, e) =>
            {
                if (e.Length > 0 && e[0] is syp.biz.SockJS.NET.Client.Event.TransportMessageEvent msg)
                {
                    var dataString = msg.Data.ToString();
                    onMsg(dataString);
                }
            });
            sockjs.AddEventListener("close", (sender, e) =>
            {
                Console.WriteLine($"******************sockjs closed");
                Thread.Sleep(1000);
                reconnect();
            });
        }

        public void CheckConnected()
        {
            if(openTick==0)
            {
                return;
            }
            long delta = (DateTime.Now.Ticks - openTick)/ 10000;
            //5秒还没收到connected，可能需要重连
            if(delta >= 5000)
            {
                reconnect();
            }
        }

        public ChromeReq GetCmd()
        {
            lock(this)
            {
                if(cmdList.Count() <= 0)
                {
                    return null;
                }
                ChromeReq cmd = cmdList[0];
                cmdList.RemoveAt(0);
                return cmd;
            }
        }

        private void onMsg(string msg)
        {
            Console.WriteLine($"******************msg:" + msg);
            if(msg == null)
            {
                return;
            }
            if(msg.StartsWith("CONNECTED"))
            {
                openTick = 0;
                subscrible("/topic/chrome_req");
            }else if(msg.StartsWith("MESSAGE"))
            {
                int index = msg.IndexOf('{');
                if(index <= 0)
                {
                    return;
                }
                msg = msg.Substring(index);
                onChromeReq(msg);
            }
            //sockJs.Send(JsonConvert.SerializeObject(new { foo = "bar" }));
            //sockJs.Send("test");
        }

        private void subscrible(string destination)
        {
            var sub = new StompMessage(StompFrame.SUBSCRIBE);
            sub["id"] = "sub-" + DateTime.Now.Ticks;
            sub["destination"] = destination;
            sockjs.Send(serializer.Serialize(sub));
        }

        private void onChromeReq(string msg)
        {
            ChromeReq req = JsonConvert.DeserializeObject<ChromeReq>(msg);
            if(req.appName != miniName)
            {
                return;
            }
            Console.WriteLine($"******************msg:" + msg);
            lock (this)
            {
                cmdList.Add(req);
            }
        }

        public void RespondEcho(ChromeReq req)
        {
            try
            {
                ChromeRespond respond = new ChromeRespond();
                respond.id = req.id;
                respond.cmd = req.cmd;
                respond.appName = req.appName;
                string body = JsonConvert.SerializeObject(respond);
                var broad = new StompMessage(StompFrame.SEND, body);
                broad["content-type"] = "application/json";
                broad["destination"] = "/app/chrome_callback";
                string msg = serializer.Serialize(broad);
                sockjs.Send(msg);
            }
            catch (Exception) { }
        }

        public void RespondReset()
        {
            try
            {
                ChromeRespond respond = new ChromeRespond();
                respond.cmd = "reset";
                respond.appName = this.miniName;
                string body = JsonConvert.SerializeObject(respond);
                var broad = new StompMessage(StompFrame.SEND, body);
                broad["content-type"] = "application/json";
                broad["destination"] = "/app/chrome_callback";
                string msg = serializer.Serialize(broad);
                sockjs.Send(msg);
            }
            catch (Exception) { }
        }

        public void RespondStatus(ChromeReq req)
        {
            try
            {
                ChromeRespond respond = new ChromeRespond();
                respond.id = req.id;
                respond.cmd = req.cmd;
                respond.appName = req.appName;
                respond.memoryUsed = getMemUsed();
                respond.cpuUsed = getCpuUsed();
                Process[] processes = Process.GetProcessesByName("chrome");
                respond.chromeExist = processes != null && processes.Length > 0;
                var broad = new StompMessage(StompFrame.SEND, JsonConvert.SerializeObject(respond));
                broad["content-type"] = "application/json";
                broad["destination"] = "/app/chrome_callback";
                sockjs.Send(serializer.Serialize(broad));
            }
            catch (Exception ex) { }
        }

        private double getMemUsed()
        {
            //获取总物理内存大小
            ManagementClass cimobject1 = new ManagementClass("Win32_PhysicalMemory");
            ManagementObjectCollection moc1 = cimobject1.GetInstances();
            double available = 0, capacity = 0;
            foreach (ManagementObject mo1 in moc1)
            {
                capacity += ((Math.Round(Int64.Parse(mo1.Properties["Capacity"].Value.ToString()) / 1024 / 1024 / 1024.0, 1)));
            }
            moc1.Dispose();
            cimobject1.Dispose();

            //获取内存可用大小
            ManagementClass cimobject2 = new ManagementClass("Win32_PerfFormattedData_PerfOS_Memory");
            ManagementObjectCollection moc2 = cimobject2.GetInstances();
            foreach (ManagementObject mo2 in moc2)
            {
                available += ((Math.Round(Int64.Parse(mo2.Properties["AvailableMBytes"].Value.ToString()) / 1024.0, 1)));

            }
            moc2.Dispose();
            cimobject2.Dispose();

            //Console.WriteLine("总内存=" + capacity.ToString() + "G");
            //Console.WriteLine("可使用=" + available.ToString() + "G");
            double used = (capacity - available) / capacity;
            Console.WriteLine("内存:已使用=" + ((capacity - available)).ToString() + "G," + (Math.Round(used * 100, 0)).ToString() + "%");
            return used;
        }

        private double getCpuUsed()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_PerfFormattedData_PerfOS_Processor");
            var cpuTimes = searcher.Get()
                .Cast<ManagementObject>()
                .Select(mo => new
                {
                    Name = mo["Name"],
                    Usage = mo["PercentProcessorTime"]
                }
                )
                .ToList();

            var query = cpuTimes.Where(x => x.Name.ToString() == "_Total").Select(x => x.Usage);
            var cpuUsage = query.SingleOrDefault();
            Console.WriteLine("CPU:" + cpuUsage + "%");
            return cpuUsage != null ? double.Parse(cpuUsage.ToString()) /100.0: -1;
        }

        private void reconnect()
        {
            Connect();
        }
    }

    public class ChromeReq
    {
        public long id;
        public string appName;
        public string cmd;
    }

    public class ChromeRespond
    {
        public long id;
        public string appName;
        public string cmd;
        public int errorCode;
        public string errorMsg;
        public double cpuUsed;
        public double memoryUsed;
        public bool chromeExist;
     //   public ChromeReq req;
    }

    internal class ConsoleLogger : syp.biz.SockJS.NET.Common.Interfaces.ILogger
    {
       
        public void Debug(string message) => Console.WriteLine($"{DateTime.Now:s} [DBG] {message}");

      
        public void Info(string message) => Console.WriteLine($"{DateTime.Now:s} [INF] {message}");

     
        public void Error(string message) => Console.WriteLine($"{DateTime.Now:s} [ERR] {message}");
    }
}
