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

namespace wechatscanWpf
{
    public class ServerMsg 
    {
        private string server = "";


        private syp.biz.SockJS.NET.Client.SockJS sockjs;
        private StompMessageSerializer serializer = new StompMessageSerializer();


        private long openTick;
        private List<ChromeReq> cmdList = new List<ChromeReq>();

        public ServerMsg(string server)
        {
            this.server = server + (server.EndsWith("/") ? "" : "/") +  "muke-ws";
        }

        public void Connect()
        {
            Utils.Log("****************** socket js connecting..........");
            //syp.biz.SockJS.NET.Client.SockJS.SetLogger(new ConsoleLogger());
            sockjs = new syp.biz.SockJS.NET.Client.SockJS(server);
            sockjs.AddEventListener("open", (sender, e) =>
            {
                Utils.Log("****************** socket js opened");
                openTick = DateTime.Now.Ticks;

                var connect = new StompMessage(StompFrame.CONNECT);
                connect["accept-version"] = "1.2";
                connect["host"] = "";
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
                Utils.Log($"******************sockjs closed");
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
            Utils.Log($"******************msg:" + msg);
            if(msg == null)
            {
                return;
            }
            if(msg.StartsWith("CONNECTED"))
            {
                openTick = 0;
                subscrible("/topic/wx_scan_req");
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
            Utils.Log($"******************msg:" + msg);
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
                string body = JsonConvert.SerializeObject(respond);
                var broad = new StompMessage(StompFrame.SEND, body);
                broad["content-type"] = "application/json";
                broad["destination"] = "/app/wx_scan_callback";
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
                string body = JsonConvert.SerializeObject(respond);
                var broad = new StompMessage(StompFrame.SEND, body);
                broad["content-type"] = "application/json";
                broad["destination"] = "/app/wx_scan_callback";
                string msg = serializer.Serialize(broad);
                sockjs.Send(msg);
            }
            catch (Exception) { }
        }

        public void RespondStatus(ChromeReq req, bool wxLogin)
        {
            try
            {
                WxScanStateRespond respond = new WxScanStateRespond();
                respond.id = req.id;
                respond.cmd = req.cmd;
                respond.wxLogin = wxLogin;
                var broad = new StompMessage(StompFrame.SEND, JsonConvert.SerializeObject(respond));
                broad["content-type"] = "application/json";
                broad["destination"] = "/app/wx_scan_callback";
                sockjs.Send(serializer.Serialize(broad));
            }
            catch (Exception ex) { }
        }


        public void RespondImg(ChromeReq req, long fileId)
        {
            try
            {
                WxScanStateRespond respond = new WxScanStateRespond();
                respond.id = req.id;
                respond.cmd = req.cmd;
                respond.screenImgId = fileId;
                var broad = new StompMessage(StompFrame.SEND, JsonConvert.SerializeObject(respond));
                broad["content-type"] = "application/json";
                broad["destination"] = "/app/wx_scan_callback";
                sockjs.Send(serializer.Serialize(broad));
            }
            catch (Exception ex) { }
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

    public class WxScanStateRespond
    {
        public long id;
        public string cmd;
        public bool wxLogin;
        public long screenImgId;
    }

    internal class ConsoleLogger : syp.biz.SockJS.NET.Common.Interfaces.ILogger
    {
       
        public void Debug(string message) => Console.WriteLine($"{DateTime.Now:s} [DBG] {message}");

      
        public void Info(string message) => Console.WriteLine($"{DateTime.Now:s} [INF] {message}");

     
        public void Error(string message) => Console.WriteLine($"{DateTime.Now:s} [ERR] {message}");
    }
}
