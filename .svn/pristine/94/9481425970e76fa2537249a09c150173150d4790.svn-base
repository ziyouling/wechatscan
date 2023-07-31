using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace scancodeuploader
{
    class Program
    {

        static void Main(string[] args)
        {
            Program app = new Program();
            int hour = -1;
            string chromepath = null;
            if(args.Length > 0)
            {
                string tag = "--chrome=";
                string tag2 = "--hour=";
                foreach (string item in args)
                {
                    Console.WriteLine("arg:" + item);
                    if(item.StartsWith(tag))
                    {
                        chromepath = item.Substring(tag.Length);
                    }
                    else if(item.StartsWith(tag2))
                    {
                        hour = int.Parse(item.Substring(tag2.Length));
                    }
                }
            }
            app.readConfig(app.GetAbsolutePath("bg.js"));
           // app.readConfig("D:\\jiang.dev\\wx-muke-plugin\\bg.js");
            Log("got server:" + app.server);
            
            string dir = System.Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
            Log("scan:" + dir);
            app.Scan(chromepath, hour, app.server, dir);
        }

     

        private string server;
        private string miniName;
        private string appId;

        private MsgConnector connector;
        public void Scan(string chromepath, int hour, string server, string dir)
        {
            this.server = server;
            long time = 0;
            int lastDay = -1;
            bool chromeNeedRestart = true;

            connector = new MsgConnector(miniName);
            connector.Connect();

            chromeRestart(chromepath);
            while(true)
            {
                try
                {
                    DateTime datetime = DateTime.Now;
                    int day = datetime.DayOfYear;
                    if(lastDay >= 0 && day != lastDay)
                    {
                        chromeNeedRestart = true;
                    }
                    if(chromeNeedRestart && hour == datetime.Hour)
                    {
                        chromeRestart(chromepath);
                        chromeNeedRestart = false;
                    }
                    lastDay = day;
                    time = scanLatest(dir, time);

                    connector.CheckConnected();

                    ChromeReq cmd  = connector.GetCmd();
                    if(cmd != null)
                    {
                        if(cmd.cmd == "reset")
                        {
                            chromeRestart(chromepath);
                        }
                        else if(cmd.cmd == "ping")
                        {
                            connector.RespondEcho(cmd);
                        }
                        else if(cmd.cmd == "status")
                        {
                            connector.RespondStatus(cmd);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log("scan latest error:" + ex.Message);
                }
                Thread.Sleep(1000);
            }
        }

        private long scanLatest(string dir, long from)
        {
            IEnumerable<string> items = System.IO.Directory.EnumerateFiles(dir, "live_muke_*.png");
            long max = 0;
            string dest = null;
            foreach (string item in items)
            {
                long time = System.IO.File.GetCreationTime(item).Ticks;
                if (time > max)
                {
                    max = time;
                    dest = item;
                }
            }
            //得到最新的
            if (max <= from)
            {
                return from;
            }
            //上传 
            Log("file new file:" + dest);
            upload(dest);
            foreach (string item in items)
            {
                System.IO.File.Delete(item);
            }
            return max;
        }

        private void upload(string file)
        {
            try
            {
                WebClient client = new WebClient();
                string url = server + "/file_upload";
                Log("begin to upload:" + url + " file:" + file);
                Byte[] result = client.UploadFile(url, file);
                string str2 = Encoding.UTF8.GetString(result);
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                Respond status = serializer.Deserialize<Respond>(str2);
                if (status.code != 0 || status.result == null || string.IsNullOrEmpty(status.result.id))
                {
                    Log("failed to upload：" + status.code + " " + status.errorMsg);
                    return;
                }
                file = System.IO.Path.GetFileName(file);
                Log("end to upload, file id:" + status.result.id);
                string prefix = "live_muke_";
                int index = file.IndexOf('_', prefix.Length);
                string appId = file.Substring(prefix.Length, index - prefix.Length);
                str2 = Get(server + "/chrome/login_scancode?appId=" + appId + "&miniName=" + miniName + "&scancode=" + status.result.url);
                Log("get login_scancode status :" + str2);
            }
            catch (Exception ex) { Console.WriteLine("error:" + ex.Message); }
        }

        public void readConfig(string path)
        {
            if(!System.IO.File.Exists(path))
            {
                return ;
            }
            StreamReader reader = new StreamReader(path);
            string line = reader.ReadLine();
            while(line != null)
            {
                line = line.Trim();
                if(line.StartsWith("//"))
                {
                    line = reader.ReadLine();
                    continue;
                }
                if(line.Contains("server") && line.Contains("http"))
                {
                    this.server = readValue(line);
                }
                else if(line.Contains("miniName"))
                {
                    this.miniName = readValue(line);
                }else if(line.Contains("appId"))
                {
                    this.appId = readValue(line);
                }
                if(!string.IsNullOrEmpty(this.server) && !string.IsNullOrEmpty(miniName) && !string.IsNullOrEmpty(appId))
                {
                    break;
                }
                line = reader.ReadLine();
            }
        }

        private string readValue(string line)
        {
            int index = line.IndexOf("=");
            line = line.Substring(index+1);
            line = line.Trim();
            line = line.Trim(';', '\"');
            return line;
        }

        public static void Log(string msg)
        {
            DateTime current = DateTime.Now;
            Console.WriteLine(current.ToString("yyy-MM-dd hh:mm:ss>  ") + msg);
        }

        public string Get(string url, int timeout = 5000)
        {
            long tick = DateTime.Now.Ticks;
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "GET";
                request.Timeout = timeout;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string line = reader.ReadToEnd();
                Log("it take " + (DateTime.Now.Ticks - tick) / 10000 + " ms to request " + url);
                return line;
            }
            catch (Exception ex)
            {
                string ksg = ex.Message;
                Log(" exception: " + ksg);
            }
            Log("it take " + (DateTime.Now.Ticks - tick) / 10000 + " ms to request " + url);
            return null;
        }

        public string GetAbsolutePath(string path)
        {
            if (path.IndexOf(":") >= 0 || path.IndexOf("/") >= 0 || path.StartsWith("\\")) return path;
            var dir = Path.GetDirectoryName(Assembly.GetEntryAssembly().CodeBase.Substring(8));
            return dir + "\\" + path;
        }

        public void chromeRestart(string chromepath)
        {
            if(string.IsNullOrEmpty(chromepath))
            {
                return;
            }
            chromeStop();
            string loginState = Get(server + "/chrome/get_login_state?appId=" + appId );
            Console.WriteLine("get_login_state:" + loginState);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string token = "";
            if (!string.IsNullOrEmpty(loginState))
            {
                ChromeLoginRespond status = serializer.Deserialize<ChromeLoginRespond>(loginState);
                if (status.code == 0 && status.result != null && !string.IsNullOrEmpty(status.result.token))
                {
                    token = status.result.token;
                }
            }
            //logout
            if (!string.IsNullOrEmpty(token))
            {
                Console.WriteLine("loginout:" + token);
                chromeStart(chromepath, "https://live.weixin.qq.com/livemp/loginout?token=" + token + "&lang=zh_CN");
                //Thread.Sleep(5000);
                //chromeStop();
            }
            else
            {
                chromeStart(chromepath, "https://live.weixin.qq.com/livemp/login");
            }
            connector.RespondReset();
        }


        public void chromeStop()
        {
            Process[] processList = Process.GetProcessesByName("chrome");
            foreach(Process process in processList)
            {
                try
                {
                    process.Kill();
                }catch(Exception ex){ }
            }
            Console.WriteLine("kill chrome！！！！！！！！");
        }

        public void chromeStart(string ext,string url)
        {
            if(string.IsNullOrEmpty(ext))
            {
                return;
            }
            Console.WriteLine("start crhome:" + url);
            Process process = new Process();
            Process p = Process.Start(ext, url);
            Thread.Sleep(2000);
            Process[] processList = Process.GetProcessesByName("chrome");
            if(processList == null || processList.Count() <= 0)
            {
                Console.WriteLine("Chrome exe is not found! restart chrome");
                chromeStart(ext, url);
            }

        }
    }

    class Respond
    {
        public int code;
        public string errorMsg;
        public string errorField;
        public UrlFile result;
    }

    class UrlFile
    {
        public string id;
        public string url;
    }

    class ChromeLoginRespond
    {
        public int code;
        public string errorMsg;
        public string errorField;
        public LoginState result;
    }

    class LoginState
    {
        public int id;
        public string appId;
        public string token;
        public bool logined;
    }

}
