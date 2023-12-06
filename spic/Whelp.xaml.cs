using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace spic
{
    /// <summary>
    /// Setting.xaml 的交互逻辑
    /// </summary>
    public partial class Whelp : Window
    {
        public Whelp()
        {
            InitializeComponent();
            tVer.Text = "V " + Global.MajorVer.ToString() + "." + Global.MinorVer.ToString() + "." + Global.RevisionVer.ToString();
        }

        private void bClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
