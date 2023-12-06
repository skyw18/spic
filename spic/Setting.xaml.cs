using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class Setting : Window
    {
        public BindingList<ExtInfo> ExtList = new BindingList<ExtInfo>();
        public List<ExtInfo> OldExtList = new List<ExtInfo>();
        public Setting()
        {
            InitializeComponent();

            foreach(string ext in FileAct.SupportExtList)
            {
                if(RegAct.CheckExtReg(ext))
                {
                    ExtList.Add(new ExtInfo() { Name = ext, Associated = true });
                    OldExtList.Add(new ExtInfo() { Name = ext, Associated = true });
                }
                else
                {
                    ExtList.Add(new ExtInfo() { Name = ext, Associated = false });
                    OldExtList.Add(new ExtInfo() { Name = ext, Associated = false });
                }
            }
            
            lvFileExt.ItemsSource = ExtList;
        }


        public class ExtInfo : INotifyPropertyChanged
        {
            public string Name { get; set; }
            private bool _Associated;
            public bool Associated { 
                get
                {
                    return _Associated;
                } 
                set
                {
                    _Associated = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("Checked"));
                }
            }
            public string Checked
            {
                get
                {
                    if (Associated)
                    {
                        return "√";
                    }
                    else
                    {
                        return " ";
                    }
                }
            }

            public override string ToString()
            {
                return Checked +  "   " + Name;
            }

            #region // INotifyPropertyChanged成员
            public event PropertyChangedEventHandler PropertyChanged;
            public void OnPropertyChanged(PropertyChangedEventArgs e)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, e);
                }
            }
            #endregion
        }

        private bool IfOldAssociated(string ext)
        {
            if (OldExtList.Find(x => x.Name == ext).Associated)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void bOk_Click(object sender, RoutedEventArgs e)
        {
            bool changed = false;
            foreach(ExtInfo ext in ExtList)
            {
                if(OldExtList.Find(x => x.Name == ext.Name).Associated == ext.Associated)
                {
                    //not change
                }
                else
                {
                    changed = true;
                    if (ext.Associated)
                    {
                        //如果用户勾选了关联文件，则需要操作注册表进行文件关联
                        if(!RegAct.CheckAppReg()) //如果application下面没有注册则先添加Application
                        {
                            RegAct.AddApplicationToReg();
                        }                        
                        RegAct.AssociationWith(ext.Name);                        
                    }
                    else
                    {
                        RegAct.DelAssociationWith(ext.Name);
                    }
                }
            }

            if(changed)
            {
                RegAct.SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
            }            

            this.Close();
        }

        private void bCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void lvFileExt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(lvFileExt.SelectedIndex != -1)
            {
                if(ExtList[lvFileExt.SelectedIndex].Associated)
                {
                    ExtList[lvFileExt.SelectedIndex].Associated = false;
                }
                else
                {
                    ExtList[lvFileExt.SelectedIndex].Associated = true;
                }
            }
        }
    }
}
