using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace spic
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            System.Runtime.ProfileOptimization.SetProfileRoot(FileAct.GetWritingPath());
            System.Runtime.ProfileOptimization.StartProfile("ProfileOptimization");
            // 接收参数数组
            Global.args = e.Args;
        }
    }
}
