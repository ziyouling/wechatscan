using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.System.UserProfile;

namespace wechatscanWpf
{
    class MainViewModel : DispatcherObject
    {
        private string server;

        private bool isStopped;

        private long imgIndex;

        private Dictionary<String, SchoolScancode> appIdsCodes = new Dictionary<String, SchoolScancode>();

        private Win32ServiceImpl service;
        private IntPtr wechatHwnd;

        private Thread syncThread;

        private ServerMsg msger;
        private Thread msgThread;

        //public void testOCR(IntPtr hwnd, int width, int height)
        //{
        //    Bitmap bitmap = Utils.GetBitmap(hwnd, 0, 0, width, height);
        //    string path = getAbsolutePath("wx.bmp");
        //    bitmap.Save(path);
        //    ExtractText(path, "zh-Hans-CN");
        //}

        public void Start(string server)
        {
            if(string.IsNullOrEmpty(server))
            {
                return;
            }
            this.server = server;
            isStopped = false;
            msger = new ServerMsg(server);
            syncThread = new Thread(new ThreadStart(syncLoginLoop));
            syncThread.SetApartmentState(ApartmentState.STA);
            syncThread.IsBackground = true;
            syncThread.Start();

            msgThread = new Thread(new ThreadStart(msgThreadLoop));
            msgThread.IsBackground = true;
            msgThread.Start();


        }

        public void Stop()
        {
            isStopped = true;
        }

        public bool IsWxLogin()
        {
            if(service == null)
            {
                service = new Win32ServiceImpl();
            }
            IntPtr hwnd = service.FindWindow("WeChatMainWndForPC", "微信");
            if(hwnd.ToInt32() == 0)
            {
                return false;
            }
            //hwnd = service.FindWindow("WeChatLoginWndForPC", "微信");
            //if(hwnd.ToInt32() != 0 && service.IsWindowVisible(hwnd))
            //{
            //    return false;
            //}
            return true;
        }

        private void prepareEnv()
        {
            //1,移动微信到指定位置；//
            log("获取微信窗口...");
            if(service == null)
            {
                service = new Win32ServiceImpl();
            }
            wechatHwnd = service.FindWindow("WeChatMainWndForPC", "微信");
            while (wechatHwnd.ToInt32() == 0)
            {
                Thread.Sleep(1000);
                wechatHwnd = service.FindWindow("WeChatMainWndForPC", "微信");
            }
            log("Got 微信窗口");
            service.SwitchToThisWindow(wechatHwnd,true);
            service.ChangePosition(wechatHwnd, 0, 0);
            service.ResizeWindow(wechatHwnd, 720, 800);
            Thread.Sleep(1000);
            //置顶文件传输助手，然后点击
            service.MouseClick(wechatHwnd, 150, 100);
        }

        private void msgThreadLoop()
        {
            msger.Connect();
            while (!isStopped)
            {
               try
                {

                    msger.CheckConnected();
                    ChromeReq cmd = msger.GetCmd();
                    if (cmd == null)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    bool wxLogin = IsWxLogin();
                    if (cmd.cmd == "status")
                    {
                        msger.RespondStatus(cmd, wxLogin);
                    }
                    else if (cmd.cmd == "img")
                    {
                        long fileId = uploadScreenImg();
                        msger.RespondImg(cmd, fileId);
                    }
                    else if (cmd.cmd.StartsWith("mouse"))
                    {
                        click(cmd.cmd);
                    }
                    else if (cmd.cmd == "reset")
                    {
                        exitAndStart();
                    }
                }
                catch(Exception ex)
                {

                }
            }
        }

        private void  syncLoginLoop()
        {
            long from = DateTime.Now.Ticks - 5 * 60 *  10000000L;
            log("开始同步登录状态...");
            bool lastLogout = true;
            //start("WeChat", @"C:\Program Files (x86)\Tencent\WeChat\WeChat.exe");
            while (!isStopped)
            {
                bool wxLogin = IsWxLogin();
                if (!wxLogin)
                {
                    lastLogout = true;
                    Thread.Sleep(100);
                    continue;
                }
                if(lastLogout)
                {
                    prepareEnv();
                }
                lastLogout = false;

                checkScancodeAndScan(from);
                from += 50000000;
            }
            log("退出同步...");
        }

        private void checkScancodeAndScan(long from)
        {
            DateTime date = new DateTime(from);
            string str = date.ToString("yyyy-MM-dd HH:mm:ss");
            string result = Get(server + "/chrome/login_list?from=0&fromstr=" + str);
            if (string.IsNullOrEmpty(result))
            {
                Thread.Sleep(5000);
                return;
            }
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            JsonResult jsonResult = serializer.Deserialize<JsonResult>(result);
            if (jsonResult.code != 0)
            {
                Thread.Sleep(5000);
                return;
            }
            List<SchoolScancode> codes = jsonResult.result;
            if (codes != null && codes.Count > 0)
            {
                foreach (SchoolScancode code in codes)
                {
                    loginScan(code);
                }
            }
            Thread.Sleep(5000);
        }

        private long uploadScreenImg()
        {
            //1,截图上传
            Rectangle rect = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            Bitmap memoryImage = new Bitmap(rect.Width, rect.Height);
            Graphics memoryGraphics = Graphics.FromImage(memoryImage);
            // 拷贝屏幕对应区域为 memoryGraphics 的 BitMap  
            memoryGraphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(rect.Width, rect.Height));
            string url = getAbsolutePath("screen_" + DateTime.Now.Ticks + ".jpg");
            memoryImage.Save(url, System.Drawing.Imaging.ImageFormat.Jpeg);
            string fileId  = upload(url);
            IEnumerable<string> items = System.IO.Directory.EnumerateFiles(getAbsolutePath(""), "screen_*.jpg");
            foreach (string item in items)
            {
                System.IO.File.Delete(item);
            }
            ////2,通知过期上传
            //string result = Get(server + "/chrome/wx_scan_logout?fileId=" + fileId);
            //log("notify wx scan logout :" + result);
            return long.Parse(fileId);
        }


        private void exitAndStart()
        {
            Process.GetCurrentProcess().Kill();
        }

       

        private void log(string msg)
        {
            this.Dispatcher.BeginInvoke((Action)delegate {
                Utils.Log(msg);
            }, DispatcherPriority.Normal);
        }

        private void loginScan(SchoolScancode code)
        {
           try
            {
                string appId = code.appId;
                if (appIdsCodes.ContainsKey(appId))
                {
                    SchoolScancode saved = appIdsCodes[appId];
                    DateTime date = DateTime.Parse(saved.time);
                    DateTime date2 = DateTime.Parse(code.time);
                    if (date2.Ticks <= date.Ticks)
                    {
                        return;
                    }
                }
                if (downloadAndScan(code))
                {
                    appIdsCodes[appId] = code;
                }
            }
            catch(Exception ex)
            {
                log("login scan exception: " + ex.Message);
            }
        }


        /**
         * <summary>返回是否扫码行为进行ok，不代表结果</summary>
         */
        public bool downloadAndScan(SchoolScancode code)
        {
            if(code.logined)
            {
                return true;
            }
            imgIndex = imgIndex % 10;
            string filepath = getAbsolutePath(imgIndex + ".png");
            imgIndex++;
        
            string error = downloadFile(server + code.scancode, filepath);
            if(!string.IsNullOrEmpty(error))
            {
                return false;
            }
            Clipboard.SetImage(new BitmapImage(new Uri(filepath)));
            //关闭升级框
            IntPtr updateHwnd = service.FindWindow("UpdateWnd", null);
            int sleepcount = 0;
            while (updateHwnd.ToInt32() != 0 && sleepcount < 10)
            {
                Thread.Sleep(1000);
                service.CloseWindow(updateHwnd);
                updateHwnd = service.FindWindow("UpdateWnd", null);
                sleepcount++;
            }
          

            //定位到对话框
            service.MouseClickRight(wechatHwnd, 400, 700);
            Thread.Sleep(500);
            //粘贴
            service.MouseClick(wechatHwnd, 400 + 10, 700 + 10);
            Thread.Sleep(500);
            //发送
            //service.MouseMove(wechatHwnd, 650, 750);
            service.MouseClick(wechatHwnd, 650, 750);
            Thread.Sleep(2000);
           
            //点击图片
            service.MouseClick(wechatHwnd, 560, 500);
            //图片被打开
            IntPtr imgHwnd = service.FindWindow("ImagePreviewWnd", null);
            sleepcount = 0;
            while (imgHwnd.ToInt32() == 0 && sleepcount < 10)
            {
                Thread.Sleep(1000);
                imgHwnd = service.FindWindow("ImagePreviewWnd", null);
                sleepcount++;
            }
            if (imgHwnd.ToInt32() <= 0)
            {
                log("图片窗口获取超时!!!");
                File.Delete(filepath);
                return false;
            }
            log("Got 图片窗口");

            //识别二维码
            Thread.Sleep(1000);
            service.MouseClickRight(imgHwnd, 300, 300);
            Thread.Sleep(1000);
            service.MouseClick(imgHwnd, 300-20, 300-20);
            Thread.Sleep(2000);
            service.MouseClickRight(imgHwnd, 300, 300);

            //点击“识别二维码”
            Thread.Sleep(1000);
            service.MouseClick(imgHwnd,300 + 50,300 + 85);

            sleepcount = 0;
            IntPtr webHwnd = service.FindWindow("CefWebViewWnd", "微信");
            while (webHwnd.ToInt32() == 0 && sleepcount < 10)
            {
                Thread.Sleep(1000);
                webHwnd = service.FindWindow("CefWebViewWnd", "微信");
                sleepcount++;
            }
            if (webHwnd.ToInt32() == 0)
            {
                log("微信登录确认窗口获取超时!!!");
   
                service.CloseWindow(imgHwnd);
                return false;
            }
            log("Got微信登录确认窗口");
            service.ChangePosition(webHwnd, 400, 0);
            service.ResizeWindow(webHwnd, 720, 1000);
            Thread.Sleep(1000);


            //10秒
            long now = DateTime.Now.Ticks;
            Scaner scaner = new Scaner(this.Dispatcher);
            Rect bounds = scaner.Scan(webHwnd, 0, 0, 720, 1000, code.miniName, 10000);
            if (bounds.IsEmpty)
            {
                log("没有找到目标小程序名称!!!");
                service.CloseWindow(imgHwnd);
                service.CloseWindow(webHwnd);
                return false;
            }
            service.MouseClick(webHwnd, (int)(bounds.X + 10), (int)(bounds.Y + 10));
            bounds = scaner.Scan(webHwnd, 0, 0, 720, 1000, "成功", 10000);
            if (bounds.IsEmpty)
            {
                log("扫码失败!!!");
                service.CloseWindow(imgHwnd);
                service.CloseWindow(webHwnd);
                return false;
            }
            Thread.Sleep(2000);
            service.CloseWindow(imgHwnd);
            service.CloseWindow(webHwnd);
            log("扫码成功!!!");
            return true;
        }

        public void click(string msg)
        {
            int length = "mouse:".Length;
            string location = msg.Substring(length);
            System.Windows.Point p = System.Windows.Point.Parse(location);
          
            if (service == null)
            {
                service = new Win32ServiceImpl();
            }
            if(p.X <= 0 && p.Y <= 0)
            {
                IntPtr hwnd = service.FindWindow("WeChatLoginWndForPC", "微信");
                if(hwnd.ToInt32() != 0 && service.IsWindowVisible(hwnd))
                {
                    service.MouseClick(hwnd, 150, 280);
                    log("click login window");
                }
                return;
            }
            log("click:" + location);
            service.MouseClickGlobal((int)p.X, (int)p.Y);
        }


        public void start(string name, string ext)
        {
            if (string.IsNullOrEmpty(ext))
            {
                return;
            }
            log("start :" + ext);
            Process process = new Process();
            Process p = Process.Start(ext);
            Thread.Sleep(2000);
            Process[] processList = Process.GetProcessesByName(name);
            if (processList == null || processList.Count() <= 0)
            {
                log(name + " exe is not found! restart ");
                start(name, ext);
            }

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
                //Utils.Log("it take " + (DateTime.Now.Ticks - tick) / 10000 + " ms to request " + url);
                return line;
            }
            catch (Exception ex)
            {
                string ksg = ex.Message;
                Utils.Log(url+ " exception: " + ksg);
            }
            //Utils.Log("it take " + (DateTime.Now.Ticks - tick) / 10000 + " ms to request " + url);
            return null;
        }

        private string upload(string file)
        {
            try
            {
                WebClient client = new WebClient();
                string url = server + "/file_upload";
                log("begin to upload:" + url + " file:" + file);
                Byte[] result = client.UploadFile(url, file);
                string str2 = Encoding.UTF8.GetString(result);
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                Respond status = serializer.Deserialize<Respond>(str2);
                if (status.code != 0 || status.result == null || string.IsNullOrEmpty(status.result.id))
                {
                    log("failed to upload：" + status.code + " " + status.errorMsg);
                    return null;
                }
                String id = status.result.id;
                log("end to upload, file id:" + id);
                return id;
            }
            catch (Exception ex) { Console.WriteLine("error:" + ex.Message); }
            return null;
        }

        private string downloadFile(string url, string dest)
        {
            string tmpPath = dest + ".tmp" + DateTime.Now.Ticks;
            string error = null;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "	application/octet-stream";
                request.Timeout = 10000;
                Stream myRequestStream = request.GetRequestStream();

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                HttpStatusCode code = response.StatusCode;
                Stream stream = response.GetResponseStream();

                if (code == HttpStatusCode.OK)
                {
                    FileStream file = File.Create(tmpPath);
                    byte[] bt = new byte[1024];
                    do
                    {
                        int length = stream.Read(bt, 0, 1024);
                        file.Write(bt, 0, length);
                        if (length <= 0)
                        {
                            break;
                        }
                    } while (true);
                    file.Close();
                }
                error = response.Headers.Get("error");
                if (!string.IsNullOrEmpty(error))
                {
                    error = HttpUtility.UrlDecode(error);
                }
            }
            catch (Exception e)
            {
                WebException we = e as WebException;
                if (we != null && we.Response != null)
                {
                    error = we.Response.Headers.Get("error");
                    if (!string.IsNullOrEmpty(error))
                    {
                        error = HttpUtility.UrlDecode(error);
                    }
                }
                if (string.IsNullOrEmpty(error))
                {
                    error = e.Message;
                }
            }
            if (string.IsNullOrEmpty(error))
            {
                if(File.Exists(dest))
                {
                    File.Delete(dest);
                }
                File.Move(tmpPath, dest);
            }
            else
            {
                File.Delete(tmpPath);
            }
            return error;
        }


        public string getAbsolutePath(string path)
        {
            if (path.IndexOf(":") >= 0 || path.IndexOf("/") >= 0 || path.StartsWith("\\")) return path;
            var dir = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().CodeBase.Substring(8));
            return dir + "\\" + path;
        }
    }

    class JsonResult
    {
        public int code;
        public string errorMsg;
        public List<SchoolScancode> result;
    }

    class SchoolScancode
    {
        public long id;
        public String appId;
        //public int schoolId;
        public bool logined;
        public string scancode;
        public string time;
        public string miniName;
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
}
