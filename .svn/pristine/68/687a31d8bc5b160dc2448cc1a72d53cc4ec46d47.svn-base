using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace wechatscanWpf
{
    public static class Utils 
    {
        private static TextBox logText;
        public static void Init(TextBox tb)
        {
            logText = tb;
        }

        public static void Log(string msg)
        {
            DateTime current = DateTime.Now;
            string dd = current.ToString("yyy-MM-dd HH:mm:ss>  ") + msg;
            Console.WriteLine(dd);

            StreamWriter writer = new StreamWriter("log.txt",true);
            writer.WriteLine(msg);
            writer.Close();
            if (!Thread.CurrentThread.IsBackground && logText != null)
            {
                logText.Text += dd + "\r\n";
                if(logText.Text.Length > 1000)
                {
                    logText.Text = dd + "\r\n";
                }
            }
        }


        public static string Get(string url, int timeout = 5000)
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



        public static string GetAbsolutePath(string path)
        {
            if (path.IndexOf(":") >= 0 || path.IndexOf("/") >= 0 || path.StartsWith("\\")) return path;
            var dir = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().CodeBase.Substring(8));
            return dir + "\\" + path;
        }

        public static Bitmap GetBitmap(IntPtr hwnd, int x, int y, int width, int height)
        {
            Graphics src = Graphics.FromHwnd(hwnd);
            Bitmap bitmap = new Bitmap(width, height);
            Graphics dest = Graphics.FromImage(bitmap);

            IntPtr srcHdc = src.GetHdc();
            IntPtr destHdc = dest.GetHdc();

            Win32NativeUtil.BitBlt(destHdc, 0, 0, width, height, srcHdc, x, y, Win32NativeUtil.TernaryRasterOperations.SRCCOPY);

            src.ReleaseHdc(srcHdc);
            dest.ReleaseHdc(destHdc);

            return bitmap;
        }

        public static Bitmap GetBitmap2(IntPtr hwnd, int x, int y, int width, int height)
        {
            Win32NativeUtil.Win32RECT rect;
            Win32NativeUtil.GetWindowRect(hwnd, out rect);
            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            
            Graphics gfxScreenshot = Graphics.FromImage(bitmap);
            gfxScreenshot.CopyFromScreen(rect.Left, rect.Top,0, 0, new Size(width, height),CopyPixelOperation.SourceCopy);
            return bitmap;
        }
    }
}
