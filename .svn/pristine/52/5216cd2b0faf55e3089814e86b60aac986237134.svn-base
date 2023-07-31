using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.System.UserProfile;

namespace wechatscan
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            //1,移动微信到指定位置；//
            Utils.Log("获取微信窗口...");
            Win32ServiceImpl service = new Win32ServiceImpl();
            IntPtr hwnd =   service.FindWindow("WeChatMainWndForPC", "微信");
            while (hwnd.ToInt32() == 0)
            {
                Thread.Sleep(1000);
                hwnd = service.FindWindow("WeChatMainWndForPC", "微信");
            }
            Utils.Log("Got 微信窗口");
            service.ChangePosition(hwnd, 0, 0);
            service.ResizeWindow(hwnd, 720, 1000);
            Thread.Sleep(500);
            //置顶文件传输助手，然后点击
            service.MouseClick(hwnd, 150, 100);

            //Clipboard.SetImage(Image.FromFile(@"C:\Users\Lenovo\Pictures\123.png"));
            ////
            //service.MouseClickRight(hwnd, 400, 900);
            //Thread.Sleep(500);
            //service.MouseClick(hwnd, 400+10, 900+10);
            //Thread.Sleep(500);
            //service.MouseClick(hwnd, 650, 950);
            //Thread.Sleep(1000);
            //service.MouseClick(hwnd, 560, 600);
            //IntPtr imgHwnd = service.FindWindow("ImagePreviewWnd", null);
            //while (imgHwnd.ToInt32() == 0)
            //{
            //    Thread.Sleep(1000);
            //    imgHwnd = service.FindWindow("ImagePreviewWnd", null);
            //}
            //Utils.Log("Got 图片窗口");
            //service.MouseClickRight(imgHwnd, 200, 200);
            //Thread.Sleep(500);
            //service.MouseClick(imgHwnd, 200 +50, 200 + 85);
            //IntPtr webHwnd = service.FindWindow("CefWebViewWnd", "微信");
            //while (webHwnd.ToInt32() == 0)
            //{
            //    Thread.Sleep(1000);
            //    webHwnd = service.FindWindow("CefWebViewWnd", "微信");
            //}
            //Utils.Log("Got 扫描窗口");
            //Thread.Sleep(5000);
            //service.MouseClick(webHwnd, 100, 310);
            Console.ReadLine();
        }

        private  void testOCR(IntPtr hwnd,int width, int height)
        {
            Bitmap bitmap = Utils.GetBitmap(hwnd, 0, 0, width, height);
            string path = getAbsolutePath("wx.bmp");
            bitmap.Save(path);
            ExtractText(path, "zh-Hans-CN");
        }

        public async void ExtractText(string image, string languageCode)
        {
           if (!GlobalizationPreferences.Languages.Contains(languageCode))
           {
                foreach (string item in GlobalizationPreferences.Languages)
                {
                    Console.WriteLine("valid languageCode:" + item);
                }
                return ;
            }
            StringBuilder text = new StringBuilder();

            using (var fileStream = File.OpenRead(image))
            {
                var bmpDecoder = await BitmapDecoder.CreateAsync(BitmapDecoder.BmpDecoderId, fileStream.AsRandomAccessStream());
                var softwareBmp = await bmpDecoder.GetSoftwareBitmapAsync();

                var ocrEngine = OcrEngine.TryCreateFromLanguage(new Language(languageCode));
                var ocrResult = await ocrEngine.RecognizeAsync(softwareBmp);

                foreach (var line in ocrResult.Lines)
                {
                    text.AppendLine(line.Text);
                }
            }
        }

        public string getAbsolutePath(string path)
        {
            if (path.IndexOf(":") >= 0 || path.IndexOf("/") >= 0 || path.StartsWith("\\")) return path;
            var dir = Path.GetDirectoryName(Assembly.GetEntryAssembly().CodeBase.Substring(8));
            return dir + "\\" + path;
        }
    }
}
