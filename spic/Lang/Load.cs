using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace spic.Lang
{
    internal class Load
    {
        internal static void Detect()
        {
            if (Prop.Lang.Default.UserLanguage != "cn")
            {
                Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
                {
                    Source = new Uri(@"/spic;component/Lang/cn.xaml", UriKind.Relative)
                    //Source = new Uri(@"/spic;component/Lang/en.xaml", UriKind.Relative)
                };
                return;
            }
        }
    }
}
