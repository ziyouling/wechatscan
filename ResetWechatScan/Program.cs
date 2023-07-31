using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ResetWechatScan
{
    class Program
    {
        static void Main(string[] args)
        {
            Program program = new Program();
            while(true)
            {
                Process[] processList = Process.GetProcessesByName("wechatscanWpf");
                if(processList.Length <= 0)
                {
                    program.reset();
                }
                Thread.Sleep(1000);
            }
        }

        public void reset()
        {
            Console.WriteLine(DateTime.Now.ToLongTimeString() + ": reset");
            kill("WeChat");
            kill("WeChatApp");
            kill("WechatBrowser");
            kill("WeChatPlayer");
            kill("wechatscanWpf");


            //启动微信，
            start("WeChat", @"C:\Program Files (x86)\Tencent\WeChat\WeChat.exe", "");
            //2,启动scanwpf
            start("wechatscanWpf", getAbsolutePath("wechatscanWpf.exe"), "");
        }


        public void start(string name, string ext, string url)
        {
            if (string.IsNullOrEmpty(ext))
            {
                return;
            }
            Console.WriteLine("start :" + url);
            Process process = new Process();
            Process p = Process.Start(ext, url);
            Thread.Sleep(2000);
            Process[] processList = Process.GetProcessesByName(name);
            if (processList == null || processList.Count() <= 0)
            {
                Console.WriteLine(name + " exe is not found! restart ");
                start(name, ext, url);
            }

        }

        public void kill(string name)
        {
            Process[] processList = Process.GetProcessesByName(name);
            foreach (Process process in processList)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception ex) { }
            }
            Console.WriteLine(" kill: " + name);
        }

        public string getAbsolutePath(string path)
        {
            if (path.IndexOf(":") >= 0 || path.IndexOf("/") >= 0 || path.StartsWith("\\")) return path;
            var dir = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().CodeBase.Substring(8));
            return dir + "\\" + path;
        }
    }
}
