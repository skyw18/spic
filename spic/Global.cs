using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace spic
{
    internal static class Global
    {
        internal readonly static int MajorVer = 0;
        internal readonly static int MinorVer = 1;
        internal readonly static int RevisionVer = 6;

        internal static string DebuInfo = "";
        internal static string ProName = "";

        //图片浏览相关参数
        internal readonly static int PRELOAD_INFRONT = 6;
        internal readonly static int PRELOAD_BEHIND = 4;
        internal static bool Reverse = false;
        internal static string CurFile = "";
        internal static int CurImgId = 0;

        //全局状态变量，当前是否显示图片信息窗口
        internal static bool ShowPicInfo = false;

        internal struct POINT
        {
            internal int X;
            internal int Y;
            internal POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        internal static bool ShowLeftArrow = false;
        internal static bool ShowRightArrow = false;


        internal static string[] args;
        internal static double PicSourceW;
        internal static double PicSourceH;
        internal static double CurPicW;
        internal static double CurPicH;

        internal static List<string> ImgList = new List<string>();

        internal class ImgInfo
        {
            internal BitmapSource BitmapSource;
            internal int LoadingStatus; //0 init  1  loading 2 loaded
            internal FileInfo FileInfo;
            internal string Format = "";

            internal ImgInfo(BitmapSource bitmap, int loading, FileInfo fileInfo)
            {
                BitmapSource = bitmap;
                LoadingStatus = loading;
                FileInfo = fileInfo;
            }
        }

        internal static ConcurrentDictionary<string, ImgInfo> ImgInfoList = new ConcurrentDictionary<string, ImgInfo>();
        internal readonly static int LOAD_STATUS_INIT = 0;
        internal readonly static int LOAD_STATUS_LOADING = 1;
        internal readonly static int LOAD_STATUS_OK = 2;
    }
}
