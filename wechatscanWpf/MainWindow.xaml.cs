using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace wechatscanWpf
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        MainViewModel viewModel;
        public MainWindow()
        {
            InitializeComponent();
            this.Left = 720;
            this.Top = 100;
            Utils.Init(tb);
            this.Loaded += onLoaded;
        }

        private void onLoaded(object sender, RoutedEventArgs e)
        {
            if (viewModel != null)
            {
                viewModel.Stop();
            }
            viewModel = new MainViewModel();
            viewModel.Start("https://course.muketang.com");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(viewModel != null)
            {
                viewModel.Stop();
            }
            viewModel = new MainViewModel();
            viewModel.Start(serverInput.Text.Trim());

            //match("水利部老年大学", "长江老年大学");
        }

        //private bool match(string src, string destFull)
        //{
        //    if (src == destFull || src.Contains(destFull))
        //    {
        //        return true;
        //    }
        //    //dest可能不全，占一定比例，就认为ok.
        //    int k = 0;
        //    int length = destFull.Length;
        //    int count = 0;
        //    for (int i = 0; i < length; i++)
        //    {
        //        string item = destFull.Substring(i, 1);

        //        for (int j = k; j < src.Length; j++)
        //        {
        //            string jitem = src.Substring(j, 1);
        //            if (jitem.Equals(item))
        //            {
        //                k = j;
        //                count++;
        //                break;
        //            }
        //        }
        //    }
        //    return (destFull.Length - count) <= 2;
        //}
    }
}
