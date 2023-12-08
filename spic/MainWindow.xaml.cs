using Microsoft.Win32;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace spic
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly float MILLI_SEC_STEP = 100;
        private readonly float MILLI_SEC_BEFORE_BAR_HIDE = 1000;
        private readonly float MILLI_SEC_BEFORE_TIP_HIDE = 1000;
        private readonly float MILLI_SEC_BEFORE_ARROW_HIDE = 1000;
        //dont less than 500, the keyboard input has a delay
        private readonly float MILLI_SEC_BEFORE_SWITCH_IMG_FAST = 1000;

        private float MilliSecArrowPress = -1f;
        private bool SwitchImgFast = false;

        private float MilliSecCountBar = 0f;
        private float MilliSecCountTip = 0f;
        private float MilliSecCountArrow = 0f;

        private readonly int DRAG_WIN = 1;
        private readonly int DRAG_IMAGE = 2;
        private int DragMode = 0;//0为没拖动  1 为拖动窗口  2 为拖动图像


        private readonly static int MinWidth = 320;
        private readonly static int MinHeight = 240;

        private bool MenuCreated = false;

        private bool mouseDown;
        private Point mouseXY;

        private readonly double MaxZoom = 500;//最小/最大放大倍数
        private readonly double MinZoom = 10;
        private readonly int ZOOM_STEP = 5; //放大缩小的步进  默认为5 表示每次5%

        private static int ZoomLevel = 100; //当前放大倍数 原尺寸为100 表示100%


        public MainWindow()
        {
            InitializeComponent();

            FileAct.InitFileList();
            Loaded += loadLang;
            //最大化时不遮挡任务栏 https://social.msdn.microsoft.com/Forums/sqlserver/en-US/76445474-7af6-41e7-8458-3e9738f2b683/maximized-wpf-app-window-show-underneath-taskbar?forum=wpf
            //几种其他办法 http://www.abhisheksur.com/2010/09/taskbar-with-window-maximized-and.html
            // https://stackoverflow.com/questions/46451382/wpf-window-is-under-top-left-placed-taskbar-in-maximized-state
            //
            //Loaded += (_, _) => { MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight; };
            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;

            WindowStyle = WindowStyle.None;
            WindowStartupLocation = WindowStartupLocation.Manual;
            TipBar.Visibility = Visibility.Hidden;
            TitleBar.Visibility = Visibility.Visible;
            ToolBar.Visibility = Visibility.Hidden;
            LeftBar.Visibility = Visibility.Hidden;
            RightBar.Visibility = Visibility.Hidden;


            ContentRendered += delegate
            {
                //MouseLeftButtonDown += async (sender, e) => await MouseLeftButtonDownAsync(sender, e).ConfigureAwait(false);
                PreviewKeyDown += (sender, e) => MainWindow_PreviewKeysDown(sender, e);
                KeyUp += (sender, e) => MainWindow_KeysUp(sender, e);
                MouseLeave += (sender, e) => MainWindow_MouseLeave(sender, e);
                MouseMove += (sender, e) => MainWindow_MouseMove(sender, e);
                MouseWheel += (sender, e) => MainWindow_MouseWheel(sender, e);
                MouseLeftButtonDown += (sender, e) => MainWindow_MouseLeftButtonDown(sender, e);
                //MouseLeftButtonUp += (sender, e) => MainWindow_MouseLeftButtonUp(sender, e);

                //object sender, MouseButtonEventArgs e
                Drop += (sender, e) => MainWindow_Drop(sender, e);
                //ContextMenu.Opened += (_, _) => RefreshRecentItemsMenu();
                SizeChanged += (sender, e) => MainWindow_Resize(sender, e);
            };

            mainContentControl.Width = this.Width;
            mainContentControl.Height = this.Height;

            MilliSecCountBar = -1;
            MilliSecCountTip = -1;
            MilliSecCountArrow = -1;

            Thread countThread = new Thread(new ThreadStart(CountThreadFunc));
            countThread.IsBackground = true;
            countThread.Start();

            if (Global.args.Length != 0)
            {
                OpenImg(Global.args[0]);
            }
        }



        //鼠标按下，判断是拖动窗口，调整窗口大小还是拖动图像
        internal void MainWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //判断是鼠标拖动图像，还是调整窗口大小，移动窗口
            if (e.GetPosition(TitleBar).Y <= 30) //如果鼠标拖动标题栏
            {
                DragMode = DRAG_WIN;
            }
            else if (e.GetPosition(ToolBar).Y >= 0) //如果鼠标拖动工具栏
            {
                DragMode = DRAG_WIN;
            }
            //如果鼠标拖动图像本身
            else if (e.GetPosition(MainImage).X >= 0 && e.GetPosition(MainImage).Y >= 0 && e.GetPosition(MainImage).X <= MainImage.Width && e.GetPosition(MainImage).Y <= MainImage.Height)
            {
                //if (MainImage.Width > this.Width || MainImage.Height > this.Height)//如果图像大于窗口大小
                //{

                //}
                DragMode = DRAG_IMAGE;
            }
            else //否则按移动窗口处理
            {
                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                {
                    DragMode = DRAG_WIN;
                }
            }

            if (DragMode == DRAG_WIN)
            {
                DragMove();
            }
            else if (DragMode == DRAG_IMAGE)
            {
            }
        }


        private void bOpenBig_Click(object sender, RoutedEventArgs e)
        {
            if (Global.CurFile == "")
            {
                OpenWithDialog();
            }
        }

        internal void MainWindow_Drop(object sender, DragEventArgs e)
        {
            Array aryFiles = ((System.Array)e.Data.GetData(DataFormats.FileDrop));
            OpenImg(aryFiles.GetValue(0).ToString());
        }



        //鼠标拖动图标，以及根据鼠标位置判断是否显示工具栏标题栏左右箭头
        internal void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            //如果没有打开任何文件，则不处理
            if (Global.CurFile == "")
            {
                return;
            }

            //Global.POINT p = new Global.POINT();
            //GetCursorPos(out p);

            if (e.GetPosition(TitleBar).Y <= 100 && e.GetPosition(TitleBar).Y > 0)
            {
                ToolBar.Visibility = Visibility.Visible;
                TitleBar.Visibility = Visibility.Visible;
                MilliSecCountBar = -1;
            }
            else
            {
                MilliSecCountBar = MILLI_SEC_BEFORE_BAR_HIDE;
            }

            if (e.GetPosition(ToolBar).Y >= -70 && e.GetPosition(ToolBar).Y <= 30)
            {
                ToolBar.Visibility = Visibility.Visible;
                TitleBar.Visibility = Visibility.Visible;
                MilliSecCountBar = -1;
            }
            else
            {
                MilliSecCountBar = MILLI_SEC_BEFORE_BAR_HIDE;
            }


            if (e.GetPosition(LeftBar).X <= 70 && e.GetPosition(LeftBar).X > 0)
            {
                if (Global.ShowLeftArrow)
                {
                    LeftBar.Visibility = Visibility.Visible;
                }

                if (Global.ShowRightArrow)
                {
                    RightBar.Visibility = Visibility.Visible;
                }

                MilliSecCountArrow = -1;
            }
            else
            {
                MilliSecCountArrow = MILLI_SEC_BEFORE_ARROW_HIDE;
            }


            if (e.GetPosition(RightBar).X >= -30 && e.GetPosition(RightBar).X <= 40)
            {
                if (Global.ShowLeftArrow)
                {
                    LeftBar.Visibility = Visibility.Visible;
                }

                if (Global.ShowRightArrow)
                {
                    RightBar.Visibility = Visibility.Visible;
                }
                MilliSecCountArrow = -1;
            }
            else
            {
                MilliSecCountArrow = MILLI_SEC_BEFORE_ARROW_HIDE;
            }
        }



        private void ContentControl_MouseMove(object sender, MouseEventArgs e)
        {
            var img = sender as Canvas;
            if (img == null)
            {
                return;
            }
            if (mouseDown)
            {
                Domousemove(img, e);
            }
        }

        private void MainWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var img = sender as ContentControl;
            if (img == null)
            {
                return;
            }
            var point = e.GetPosition(img);

            var delta = e.Delta * 0.001;
            DowheelZoom(point, delta);
        }


        private void Domousemove(Canvas img, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }
            var group = MainImage.FindResource("TfGroup") as TransformGroup;
            var transform = group.Children[1] as TranslateTransform;
            var position = e.GetPosition(img);
            transform.X -= mouseXY.X - position.X;
            transform.Y -= mouseXY.Y - position.Y;
            mouseXY = position;
        }

        private void DowheelZoom(Point point, double delta)
        {
            var group = MainImage.FindResource("TfGroup") as TransformGroup;
            var pointToContent = group.Inverse.Transform(point);
            var transform = group.Children[0] as ScaleTransform;
            if (ZoomLevel + (delta * 100) < MinZoom) return;
            if (ZoomLevel + (delta * 100) > MaxZoom) return;
            ZoomLevel += (int)(delta * 100);
            transform.ScaleX += delta;
            transform.ScaleY += delta;

            var transformImg = group.Children[1] as TranslateTransform;
            transformImg.X = -1 * ((pointToContent.X * transform.ScaleX) - point.X);
            transformImg.Y = -1 * ((pointToContent.Y * transform.ScaleY) - point.Y);
            tTip.Text = ZoomLevel.ToString() + "%";
            TipBar.Visibility = Visibility.Visible;
            MilliSecCountTip = MILLI_SEC_BEFORE_TIP_HIDE;
        }


        internal void ZoomTo(bool increment, bool Anim)
        {
            var point = new Point(this.Width / 2, this.Height / 2);
            var group = MainImage.FindResource("TfGroup") as TransformGroup;
            var pointToContent = group.Inverse.Transform(point);
            var transform = group.Children[0] as ScaleTransform;

            if (increment)
            {

                if (ZoomLevel + ZOOM_STEP > MaxZoom) return;
                transform.ScaleX += ZOOM_STEP / 100f;
                transform.ScaleY += ZOOM_STEP / 100f;
                ZoomLevel += ZOOM_STEP;
            }
            else
            {
                if (ZoomLevel - ZOOM_STEP < MinZoom) return;
                transform.ScaleX -= ZOOM_STEP / 100f;
                transform.ScaleY -= ZOOM_STEP / 100f;
                ZoomLevel -= ZOOM_STEP;
            }

            var transformImg = group.Children[1] as TranslateTransform;
            transformImg.X = -1 * ((pointToContent.X * transform.ScaleX) - point.X);
            transformImg.Y = -1 * ((pointToContent.Y * transform.ScaleY) - point.Y);

            tTip.Text = ZoomLevel.ToString() + "%";
            TipBar.Visibility = Visibility.Visible;
            MilliSecCountTip = MILLI_SEC_BEFORE_TIP_HIDE;

            return;
        }

        private void ContentControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var img = sender as Canvas;
            if (img == null)
            {
                return;
            }
            img.CaptureMouse();
            mouseDown = true;
            mouseXY = e.GetPosition(img);
        }



        private void ContentControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var img = sender as Canvas;
            if (img == null)
            {
                return;
            }
            img.ReleaseMouseCapture();
            mouseDown = false;
        }





        internal void CountThreadFunc()
        {
            while (true)
            {
                if (MilliSecCountBar == 0)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        ToolBar.Visibility = Visibility.Hidden;
                        TitleBar.Visibility = Visibility.Hidden;
                    }));
                    MilliSecCountBar -= MILLI_SEC_STEP;
                }
                else if (MilliSecCountBar > 0)
                {
                    MilliSecCountBar -= MILLI_SEC_STEP;
                }

                if (MilliSecCountTip == 0)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        TipBar.Visibility = Visibility.Hidden;
                    }));
                    MilliSecCountTip -= MILLI_SEC_STEP;
                }
                else if (MilliSecCountTip > 0)
                {
                    MilliSecCountTip -= MILLI_SEC_STEP;
                }

                if (MilliSecCountArrow == 0)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        LeftBar.Visibility = Visibility.Hidden;
                        RightBar.Visibility = Visibility.Hidden;
                    }));
                    MilliSecCountArrow -= MILLI_SEC_STEP;
                }
                else if (MilliSecCountArrow > 0)
                {
                    MilliSecCountArrow -= MILLI_SEC_STEP;
                }

                if (MilliSecArrowPress != -1)
                {
                    MilliSecArrowPress += MILLI_SEC_STEP;
                }

                Thread.Sleep(Convert.ToInt32(MILLI_SEC_STEP));    //Global.MILLI_SEC_STEP 毫秒执行一次
            }
        }



        internal void MainWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            if (Global.CurFile == "")
            {
                return;
            }
            MilliSecCountBar = MILLI_SEC_BEFORE_BAR_HIDE;
        }

        internal void MainWindow_PreviewKeysDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Add:
                case Key.OemPlus:
                    Zoom(true);
                    break;
                case Key.Subtract:
                case Key.OemMinus:
                    Zoom(false);
                    break;
                case Key.Left:
                    if (MilliSecArrowPress == -1)
                    {
                        MilliSecArrowPress = 0f;
                    }
                    ShowPrevImg();
                    break;
                case Key.Right:
                    if (MilliSecArrowPress == -1)
                    {
                        MilliSecArrowPress = 0f;
                    }
                    ShowNextImg();
                    break;
                default: break;
            }
        }


        internal void MainWindow_KeysUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    //SwitchImgFast = false;
                    if (MilliSecArrowPress >= MILLI_SEC_BEFORE_SWITCH_IMG_FAST)
                    {
                        FileAct.RemoveAll();
                    }
                    MilliSecArrowPress = -1f;
                    ShowPrevImg(true);
                    break;
                case Key.Right:
                    //SwitchImgFast = false;
                    if (MilliSecArrowPress >= MILLI_SEC_BEFORE_SWITCH_IMG_FAST)
                    {
                        FileAct.RemoveAll();
                    }
                    MilliSecArrowPress = -1f;
                    ShowNextImg(true);

                    break;
                default: break;
            }
        }

        internal void MainWindow_Resize(object sender, EventArgs e)
        {
            mainContentControl.Height = this.Height;
            mainContentControl.Width = this.Width;
            //ResizePosImage();
        }

        private void bZoomOut_Click(object sender, RoutedEventArgs e)
        {
            Zoom(false);
        }

        private void bZoomIn_Click(object sender, RoutedEventArgs e)
        {
            Zoom(true);
        }

        internal void Zoom(bool increment)
        {
            if (increment)
            {
                ZoomTo(increment, true);
            }
            else
            {
                ZoomTo(increment, true);
            }

        }




        private void loadLang(object sender, RoutedEventArgs e)
        {
            Lang.Load.Detect();

        }

        private void bOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenWithDialog();
        }

        internal static bool IsDialogOpen { get; set; }
        private void OpenWithDialog()
        {
            IsDialogOpen = true;

            var dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == true)
            {
                OpenImg(dlg.FileName);
            }
        }



        private void CopyImg()
        {
            BitmapSource pic;
            if (MainImage.Source != null)
            {
                if (MainImage.Effect != null)
                {
                    pic = GetRenderedBitmapFrame();
                }
                else
                {
                    pic = (BitmapSource)MainImage.Source;
                }

                if (pic == null)
                {
                    //ShowTooltipMessage(Application.Current.Resources["UnknownError"]);
                    return;
                }

                Clipboard.SetImage(pic);
            }
        }



        internal BitmapFrame GetRenderedBitmapFrame()
        {
            try
            {
                var sauce = MainImage.Source as BitmapSource;

                if (sauce == null)
                {
                    return null;
                }

                var effect = MainImage.Effect;

                var rectangle = new System.Windows.Shapes.Rectangle
                {
                    Fill = new ImageBrush(sauce),
                    Effect = effect
                };

                var sz = new Size(sauce.PixelWidth, sauce.PixelHeight);
                rectangle.Measure(sz);
                rectangle.Arrange(new Rect(sz));

                var rtb = new RenderTargetBitmap(sauce.PixelWidth, sauce.PixelHeight, sauce.DpiX, sauce.DpiY, PixelFormats.Default);
                rtb.Render(rectangle);

                BitmapFrame bitmapFrame = BitmapFrame.Create(rtb);
                bitmapFrame.Freeze();

                return bitmapFrame;
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(GetRenderedBitmapFrame)} exception, \n {e.Message}");
#endif
                return null;
            }
        }

        internal void CreateMenu()
        {
            ContextMenu = (ContextMenu)Application.Current.Resources["ContextMenu"];
            var opencm = (MenuItem)ContextMenu.Items[0];
            //opencm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + O";
            opencm.Click += (o, e) =>
            {
                OpenWithDialog();
            };

            var savecm = (MenuItem)ContextMenu.Items[1];
            //savecm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + S";
            savecm.Click += (o, e) =>
            {

            };

            var delcm = (MenuItem)ContextMenu.Items[3];
            //delcm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + D";
            delcm.Click += (o, e) =>
            {

            };

            var copycm = (MenuItem)ContextMenu.Items[4];
            //copycm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + C";
            copycm.Click += (o, e) =>
            {
                CopyImg();
            };



            var picinfocm = (MenuItem)ContextMenu.Items[6];
            //picinfocm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + I";
            picinfocm.Click += (o, e) =>
            {
                ShowPicinfo();
            };


            var settingcm = (MenuItem)ContextMenu.Items[7];
            //settingcm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + E";
            settingcm.Click += (o, e) =>
            {
                ShowSetting();
            };



            var aboutcm = (MenuItem)ContextMenu.Items[9];
            //aboutcm.InputGestureText = $"{Application.Current.Resources["Ctrl"]} + H";
            aboutcm.Click += (o, e) =>
            {
                ShowHelp();
            };
        }

        private async void WaitPicReady(string file)
        {
            if (Global.ImgList.Count == 0 || !Global.ImgInfoList.ContainsKey(file))
            {
                return;
            }
            while (Global.ImgInfoList[file].LoadingStatus == Global.LOAD_STATUS_LOADING)
            {
                await Task.Delay(20).ConfigureAwait(false);
            }
            SetPic(file);
        }


        private void SetPic(string file)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                var ImgInfo = Global.ImgInfoList[file];
                if (ImgInfo.BitmapSource == null)
                {
                    return;
                }
                string file_ext = System.IO.Path.GetExtension(file).ToLower();



                if (file_ext != "" && file_ext == ".gif")
                {
                    XamlAnimatedGif.AnimationBehavior.SetSourceUri(MainImage, new Uri(file));
                }
                else
                {
                    MainImage.Source = ImgInfo.BitmapSource;
                }

                MainImage.Margin = new Thickness(0, 0, 0, 0);
                //MainImage.Source = image.ToBitmapSource();
                Global.PicSourceW = ImgInfo.BitmapSource.Width;
                Global.PicSourceH = ImgInfo.BitmapSource.Height;

                FitImage(true);
                if (Global.ShowPicInfo)
                {
                    wPicinfo.RefreshPicinfo(file);
                }
            }));
        }

        internal async Task PreLoadImg()
        {
            if (Global.ImgList.Count == 0)
            {
                return;
            }



            int loadInfront = Global.PRELOAD_INFRONT;
            int loadBehind = Global.PRELOAD_BEHIND;

            int endPoint;

            int tmpid;

            if(Global.ImgList.Count < Global.PRELOAD_INFRONT + Global.PRELOAD_BEHIND + 2)
            {
                for (int i = 0; i < Global.ImgList.Count; i--)
                {
                    _ = await FileAct.AddAsync(Global.ImgList[i]).ConfigureAwait(false);
                }
            }
            else
            {
                if (Global.Reverse)
                {
                    endPoint = Global.CurImgId - 1 - loadInfront;

                    // Add first elements behind
                    for (int i = Global.CurImgId - 1; i > endPoint; i--)
                    {
                        //负数取余会变成负数，所以加上一个数 为什么要取余呢？ 因为要考虑小于0 或者大于count之后循环读取图片
                        tmpid = i % Global.ImgList.Count;
                        if (CheckNumValid(tmpid))
                        {
                            _ = await FileAct.AddAsync(Global.ImgList[tmpid]).ConfigureAwait(false);
                        }
                    }

                    // Add second elements
                    for (int i = Global.CurImgId + 1; i < (Global.CurImgId + 1) + loadBehind; i++)
                    {
                        tmpid = (i + Global.ImgList.Count) % Global.ImgList.Count;
                        if (CheckNumValid(tmpid))
                        {
                            _ = await FileAct.AddAsync(Global.ImgList[tmpid]).ConfigureAwait(false);
                        }
                    }

                    //Clean up infront
                    for (int i = (Global.CurImgId + 1) + loadBehind; i < (Global.CurImgId + 1) + loadInfront; i++)
                    {
                        tmpid = (i + Global.ImgList.Count) % Global.ImgList.Count;
                        if (CheckNumValid(tmpid))
                        {
                            FileAct.Remove(Global.ImgList[tmpid]);
                        }
                    }
                }
                else
                {
                    endPoint = (Global.CurImgId - 1) - loadBehind;
                    // Add first elements
                    for (int i = Global.CurImgId + 1; i < (Global.CurImgId + 1) + loadInfront; i++)
                    {
                        tmpid = i % Global.ImgList.Count;
                        if (CheckNumValid(tmpid))
                        {
                            _ = await FileAct.AddAsync(Global.ImgList[tmpid]).ConfigureAwait(false);
                        }
                    }
                    // Add second elements behind
                    for (int i = Global.CurImgId - 1; i > endPoint; i--)
                    {
                        tmpid = (i + Global.ImgList.Count) % Global.ImgList.Count;
                        if (CheckNumValid(tmpid))
                        {
                            _ = await FileAct.AddAsync(Global.ImgList[tmpid]).ConfigureAwait(false);
                        }
                    }

                    //Clean up behind
                    for (int i = Global.CurImgId - loadInfront; i <= endPoint; i++)
                    {
                        tmpid = (i + Global.ImgList.Count) % Global.ImgList.Count;
                        if (CheckNumValid(tmpid))
                        {
                            FileAct.Remove(Global.ImgList[tmpid]);
                        }
                    }
                }
            }
            
        }

        private bool CheckNumValid(int i)
        {
            if(i < 0)
            {
                return false;
            }
            
            if(i >= Global.ImgList.Count)
            {
                return false;
            }
            return true;
        }

        internal void OpenImg(string file)
        {
            if (!FileAct.IsFileSupport(file))
            {
                return;
            }

            if (!MenuCreated)
            {
                CreateMenu();
                MenuCreated = true;
            }

            OpenImgAsync(file);
        }


        internal async Task OpenImgAsync(string file)
        {
            if (RefreshImgList(file) == 0)
            {
                return;
            }

            Global.CurImgId = Global.ImgList.IndexOf(file);

            int tmpImgId = Global.CurImgId + 1;
            tTip.Text = tmpImgId.ToString() + " / " + Global.ImgList.Count.ToString();
            TipBar.Visibility = Visibility.Visible;
            MilliSecCountTip = MILLI_SEC_BEFORE_TIP_HIDE;

            tTitle.Text = System.IO.Path.GetFileName(file);
            MilliSecCountBar = MILLI_SEC_BEFORE_BAR_HIDE;
            pOpen.Visibility = Visibility.Hidden;
            Global.ShowLeftArrow = true;
            Global.ShowRightArrow = true;            

            if (Global.ImgList.Count == 1)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    Global.ShowLeftArrow = false;
                    LeftBar.Visibility = Visibility.Hidden;
                    Global.ShowRightArrow = false;
                    RightBar.Visibility = Visibility.Hidden;
                }));
            }


            //if (Global.CurImgId == 0)
            //{
            //    Dispatcher.Invoke(new Action(() =>
            //    {
            //        Global.ShowLeftArrow = false;
            //        LeftBar.Visibility = Visibility.Hidden;
            //    }));
            //}

            //if (Global.CurImgId == Global.ImgInfoList.Count - 1)
            //{
            //    Dispatcher.Invoke(new Action(() =>
            //    {
            //        Global.ShowRightArrow = false;
            //        RightBar.Visibility = Visibility.Hidden;
            //    }));
            //}


            
            if (MilliSecArrowPress >= MILLI_SEC_BEFORE_SWITCH_IMG_FAST)
            {
                return;
            }
            
            PreLoadImg();


            if (Global.ImgInfoList[file].LoadingStatus == Global.LOAD_STATUS_OK)
            {
                SetPic(file);
            }
            else if (Global.ImgInfoList[file].LoadingStatus == Global.LOAD_STATUS_INIT)
            {
                bool added = await FileAct.AddAsync(file).ConfigureAwait(false);
                if (added)
                {
                    SetPic(file);
                }
            }
            else if(Global.ImgInfoList[file].LoadingStatus == Global.LOAD_STATUS_LOADING)
            {
                WaitPicReady(file);
            }


            return;
        }


        /// <summary>
        /// 刷新当前目录下的文件列表
        /// </summary>
        internal int RefreshImgList(string file)
        {
            Global.CurFile = file;

            bool need_clear = true;
            // if directory not change
            if(Global.ImgList.Count > 0)
            {
                if(System.IO.Path.GetDirectoryName(Global.ImgList[0]) == System.IO.Path.GetDirectoryName(file))
                {
                    need_clear = false;
                }
            }

            if (need_clear)
            {
                Global.ImgInfoList.Clear();                
            }

            //refresh img list
            Global.ImgList.Clear();
            List<string> tmpFiles = FileAct.GetFileList(file);            
            foreach (string tmpfile in tmpFiles)
            {
                if (FileAct.IsFileSupport(tmpfile))
                {
                    Global.ImgList.Add(tmpfile);
                    if(!Global.ImgInfoList.ContainsKey(tmpfile))
                    {
                        var preloadImg = new Global.ImgInfo(null, Global.LOAD_STATUS_INIT, null);
                        Global.ImgInfoList.TryAdd(tmpfile, preloadImg);
                    }
                }
            }

            if(Global.ImgList.Count == 0)
            {
                return 0;
            }

            return 1;
        }

        //使Image能够在软件内完整显示
        internal void FitImage(bool FitPic)
        {
            double ZoomValue = 0;
            if(FitPic)
            {
                //if(Global.PicSourceW <= SystemParameters.WorkArea.Size.Width && Global.PicSourceH <= SystemParameters.WorkArea.Size.Height)
                if (Global.PicSourceW <= SystemParameters.WorkArea.Size.Width && Global.PicSourceH <= SystemParameters.WorkArea.Size.Height)
                {
                    
                }
            }

            if (Global.PicSourceW <= this.Width && Global.PicSourceH <= this.Height)
            {
                ZoomValue = 100;  // 100% and change windows size
            }
            else 
            {
            }

            if (ZoomValue == 0)
            {
                if (this.Width / Global.PicSourceW > this.Height / Global.PicSourceH)
                {
                    ZoomValue = this.Height / Global.PicSourceH * 100;
                }
                else
                {
                    ZoomValue = this.Width / Global.PicSourceW * 100;
                }
            }


            if(ZoomValue > 100)
            {
                ZoomValue = 100;
            }

            MainImage.Width = Global.CurPicW = Global.PicSourceW * ZoomValue / 100;
            MainImage.Height = Global.CurPicH = Global.PicSourceH * ZoomValue / 100;
            var group = MainImage.FindResource("TfGroup") as TransformGroup;
            var transform = group.Children[0] as ScaleTransform;
            transform.ScaleX = 1;
            transform.ScaleY = 1;
            var transformImg = group.Children[1] as TranslateTransform;
            transformImg.X = (this.Width - MainImage.Width) / 2;
            transformImg.Y = (this.Height - MainImage.Height) / 2;
            

            ZoomLevel = (int)ZoomValue;
            //MainImage.Width = 1000f;
            //MainImage.Height = 1000f;

            return;
        }

        private void bCloseWin_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void bMaxWin_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                //声明图片路径
                Uri uri = new Uri("/Img/max.png", UriKind.Relative);
                ////定义图片源
                //BitmapImage bitmap = new BitmapImage(uri);
                //为Source属性赋值
                iMaxWin.Source = new BitmapImage(uri);
                thResizeR.Visibility = Visibility.Visible;
                thResizeL.Visibility = Visibility.Visible;
            }
            else if(this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
                //声明图片路径
                Uri uri = new Uri("/Img/normal.png", UriKind.Relative);
                ////定义图片源
                //BitmapImage bitmap = new BitmapImage(uri);
                //为Source属性赋值
                iMaxWin.Source = new BitmapImage(uri);
                thResizeR.Visibility = Visibility.Hidden;
                thResizeL.Visibility = Visibility.Hidden;
            }
        }

        private void bMinWin_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive));
        }



        private void thResizeR_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            double yadjust = this.Height + e.VerticalChange;
            double xadjust = this.Width + e.HorizontalChange;
            if ((xadjust >= 0) && (yadjust >= 0))
            {
                this.Width = xadjust;
                this.Height = yadjust;
            }
        }

        private void thResizeL_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            //this.Width = e.HorizontalChange;
            //this.Height = e.VerticalChange;
            //Move the Thumb to the mouse position during the drag operation
            double yadjust = this.Height + e.VerticalChange;            
            double xadjust = this.Width - e.HorizontalChange;
            if (xadjust >= MinWidth)
            {
                this.Width = xadjust;
                this.Left += e.HorizontalChange;
            }

            if(yadjust >= MinHeight)
            {
                this.Height = yadjust;
            }
        }



        private void bNextImg_Click(object sender, RoutedEventArgs e)
        {
            ShowNextImg();
        }

        internal void ShowNextImg(bool keyup = false)
        {
            Global.Reverse = false;
            if(keyup)
            {
                OpenImgAsync(Global.ImgList[Global.CurImgId]);
            }
            else
            {
                if (Global.CurImgId + 1 < Global.ImgList.Count)
                {                    
                    OpenImgAsync(Global.ImgList[Global.CurImgId + 1]);
                    //curimgid has changed after OpenImgAsync
                }
                else if(Global.CurImgId + 1 == Global.ImgList.Count)
                {
                    OpenImgAsync(Global.ImgList[0]);
                }
            }
        }

        private void bPrevImg_Click(object sender, RoutedEventArgs e)
        {
            ShowPrevImg();
        }

        internal void ShowPrevImg(bool keyup = false)
        {
            Global.Reverse = true;
            if(keyup)
            {
                OpenImgAsync(Global.ImgList[Global.CurImgId]);
            }
            else
            {
                if (Global.CurImgId - 1 >= 0)
                {
                    //此处报错
                    OpenImgAsync(Global.ImgList[Global.CurImgId - 1]);
                    //curimgid has changed after OpenImgAsync
                }
                else if(Global.CurImgId - 1 < 0)
                {
                    OpenImgAsync(Global.ImgList[Global.ImgList.Count - 1]);
                }
            }           
            
        }

        private void bSetting_Click(object sender, RoutedEventArgs e)
        {
            ShowSetting();
        }

        private void ShowSetting()
        {
            Setting wSetting = new Setting();
            wSetting.Owner = this;
            wSetting.ShowDialog();
        }

        private void bHelp_Click(object sender, RoutedEventArgs e)
        {
            ShowHelp();
        }


        
        private void ShowHelp()
        {
            Whelp wHelp = new Whelp();
            wHelp.Owner = this;
            wHelp.ShowDialog();
        }

        private void bPicinfo_Click(object sender, RoutedEventArgs e)
        {
            ShowPicinfo();
        }


        Picinfo wPicinfo;
        private void ShowPicinfo()
        {
            wPicinfo = new Picinfo(this);
            wPicinfo.Owner = this;
            wPicinfo.Show();
        }


        private void bFit_Click(object sender, RoutedEventArgs e)
        {
            FitImage(true);
        }
    }
}
