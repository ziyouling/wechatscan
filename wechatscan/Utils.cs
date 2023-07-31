using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wechatscan
{
    public static class Utils
    {
        public static void Log(string msg)
        {
            DateTime current = DateTime.Now;
            Console.WriteLine(current.ToString("yyy-MM-dd hh:mm:ss>  " ) + msg);
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
    }
}
