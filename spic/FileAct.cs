using MetadataExtractor;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace spic
{
    internal class FileAct
    {

        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int StrCmpLogicalW(string x, string y);
        internal static System.Collections.Generic.List<string> PicIndex;

        internal enum SortFilesBy
        {
            Name,
            FileSize,
            Creationtime,
            Extension,
            Lastaccesstime,
            Lastwritetime,
            Random
        }

        internal static List<string> SupportExtList;
        internal static void InitFileList()
        {
            SupportExtList = new List<string>();
            SupportExtList.Add(".jpg");
            SupportExtList.Add(".jpeg");
            SupportExtList.Add(".jpe");
            SupportExtList.Add(".png");
            SupportExtList.Add(".bmp");
            SupportExtList.Add(".jfif");
            SupportExtList.Add(".ico");
            SupportExtList.Add(".webp");
            SupportExtList.Add(".wbmp");
            SupportExtList.Add(".tga");
            SupportExtList.Add(".gif");
        }

        /// <summary>
        /// Sort and return list of supported files
        /// </summary>
        internal static bool? CheckIfDirectoryOrFile(string path)
        {
            try
            {
                var getAttributes = File.GetAttributes(path);
                return getAttributes.HasFlag(FileAttributes.Directory);
            }
            catch (Exception)
            {
                return null;
            }
        }


        internal static bool IsFileSupport(string file)
        {
            if(file == null)
            {
                return false;
            }
            string file_ext = System.IO.Path.GetExtension(file).ToLower();
            switch (file_ext)
            {
                case ".jpg":
                case ".jpeg":
                case ".jpe":
                case ".png":
                case ".bmp":
                case ".jfif":
                case ".ico":
                case ".webp":
                case ".wbmp":
                case ".tga":
                case ".gif":
                    return true;
                default:
                    return false;
            }
        }


        /// <summary>
        /// Sort and return list of supported files
        /// </summary>
        public static List<string> GetFileList(string file)
        {
            SortFilesBy sortFilesBy = SortFilesBy.Name;
            SearchOption searchOption = SearchOption.TopDirectoryOnly;

            var items = System.IO.Directory.EnumerateFiles(System.IO.Path.GetDirectoryName(file), "*.*", searchOption).AsParallel();

            switch (sortFilesBy)
            {
                default:
                case SortFilesBy.Name: // Alphanumeric sort
                    var list = items.ToList();
                    list.Sort((x, y) => StrCmpLogicalW(x, y));
                    return list;
            }
        }

        internal static async Task<bool> AddAsync(string file)
        {
            if (Global.ImgInfoList[file].LoadingStatus == Global.LOAD_STATUS_OK || Global.ImgInfoList[file].LoadingStatus == Global.LOAD_STATUS_LOADING)// || preloadImg.LoadingStatus == Global.LOAD_STATUS_LOADING)
            {
                return true;
            }

            Global.ImgInfoList[file].LoadingStatus = Global.LOAD_STATUS_LOADING;
            BitmapSource bitmapSource = null;
            await Task.Run(async () =>
            {
                FileInfo fileInfo = new FileInfo(file);
                bitmapSource = await ReturnBitmapSourceAsync(fileInfo).ConfigureAwait(false);
                Global.ImgInfoList[file].BitmapSource = bitmapSource;
                Global.ImgInfoList[file].LoadingStatus = Global.LOAD_STATUS_OK;
                Global.ImgInfoList[file].FileInfo = fileInfo;
            }).ConfigureAwait(false);

            return true;
        }




        internal static void Remove(string file)
        {
            if(!Global.ImgInfoList.ContainsKey(file))
            {
                return;
            }
            Global.ImgInfoList[file].LoadingStatus = Global.LOAD_STATUS_INIT;
            Global.ImgInfoList[file].BitmapSource = null;            
        }

        internal static void RemoveAll()
        {
            foreach(var img in Global.ImgInfoList)
            {
                img.Value.LoadingStatus = Global.LOAD_STATUS_INIT;
                img.Value.BitmapSource = null;                
            }
        }


        private static Dictionary<string, string> GenExifTemplate()
        {
            var rt = new Dictionary<string, string>();
            rt.Add("", "");

            return rt;
        }


        /*
        /// <summary>下面两个函数为使用ExifLib 库获取exif信息的函数，
        /// 因为MetadataExtractor获取的信息更为详尽，已经换为使用MetadataExtractor
        /// ExifLib 官网 https://github.com/esskar/ExifLib.Net
        /// </summary>
        /// <param name="file">照片绝对路径</param>
        /// <returns></returns>
        internal static string GetExifByFile(string file)
        {
            string sExif = "";
            using (var reader = new ExifReader(file))
            {
                // Get the image thumbnail (if present)
                var thumbnailBytes = reader.GetJpegThumbnailBytes();

                // To read a single field, use code like this:
                //
                //DateTime datePictureTaken;
                //if (reader.GetTagValue(ExifTags.DateTimeDigitized, out datePictureTaken))
                //{
                //    MessageBox.Show(this, string.Format("The picture was taken on {0}", datePictureTaken), "Image information", MessageBoxButtons.OK);
                //}
                

                // Parse through all available fields and generate key-value labels
                var props = Enum.GetValues(typeof(ExifTags)).Cast<ushort>().Select(tagID =>
                {
                    object val;
                    if (reader.GetTagValue(tagID, out val))
                    {
                        // Special case - some doubles are encoded as TIFF rationals. These
                        // items can be retrieved as 2 element arrays of {numerator, denominator}
                        if (val is double)
                        {
                            int[] rational;
                            if (reader.GetTagValue(tagID, out rational))
                                val = string.Format("{0} ({1}/{2})", val, rational[0], rational[1]);
                        }

                        return string.Format("{0}: {1}", Enum.GetName(typeof(ExifTags), tagID), RenderTag(val));
                    }

                    return null;

                }).Where(x => x != null).ToArray();

                sExif = string.Join("\r\n", props);
            }

            return sExif;
        }

        private static string RenderTag(object tagValue)
        {
            // Arrays don't render well without assistance.
            var array = tagValue as Array;
            if (array != null)
                return string.Join(", ", array.Cast<object>().Select(x => x.ToString()).ToArray());

            return tagValue.ToString();
        }
        */


        public static Dictionary<string, string> TagList = new Dictionary<string, string>();
        private class TagInfo
        {
            public string DirectoryName;
            public string TagName;
            public string TagValue;
            public TagInfo(string dName, string tName, string tValue)
            {
                DirectoryName = dName;
                TagName = tName;
                TagValue = tValue;
            }
        }


        /// <summary>通过MetadataExtractor获取照片参数
        /// MetadataExtractor官网 https://github.com/drewnoakes/metadata-extractor-dotnet
        /// 主要显示内容和说明 [directory.Name] tag.Name   参数名称   说明文档
        /// [File] File Name   文件名
        /// [File] File Size  文件大小
        /// [Exif IFD0] Make     相机厂商
        /// [Exif IFD0] Model    设备型号
        /// [Exif IFD0] Date/Time  拍摄时间
        /// [File Type] Detected MIME Type   文件类型
        /// [Exif SubIFD] Exif Version   exif版本号
        /// [Exif SubIFD] F-Number  光圈
        /// [Exif SubIFD] Exposure Time 曝光时间
        /// [Exif SubIFD] Exposure Bias Value  曝光补偿
        /// [Exif SubIFD] ISO Speed Ratings   ISO感光度
        /// [Exif SubIFD] Focal Length   焦距
        /// [Exif SubIFD] Metering Mode  测光模式   https://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/meteringmode.html
        /// [Exif SubIFD] Flash  闪光灯   https://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/flash.html
        /// [Exif SubIFD] White Balance Mode  白平衡  https://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/whitebalance.html
        /// [Exif SubIFD] Brightness Value  亮度
        /// [Exif SubIFD] Exposure Program  曝光程序   https://www.awaresystems.be/imaging/tiff/tifftags/privateifd/exif/exposureprogram.html
        /// [Exif SubIFD] Lens Make  镜头制造商
        /// [Exif SubIFD] Lens Specification  镜头规格
        /// [Exif SubIFD] Lens Model  镜头型号
        /// [Exif SubIFD] Digital Zoom Ratio  数学变焦比例           
        /// 
        /// </summary>
        /// <param name="file">照片绝对路径</param>
        /// <returns></returns>
        internal static string GetExifByMe(string file) //Dictionary<string, string> GetExifByMe(string file)
        {
            Global.ImgInfo tmpInfo = Global.ImgInfoList[file];

            TagList.Clear();
            TagList.Add($"{ Application.Current.Resources["SInfoPath"]}", System.IO.Path.GetDirectoryName(file));
            TagList.Add($"{ Application.Current.Resources["SInfoName"]}", System.IO.Path.GetFileName(file));
            TagList.Add($"{ Application.Current.Resources["SInfoType"]}", "");

            string file_size = "";
            if (tmpInfo.FileInfo.Length / 1024.0 > 1024)
            {
                file_size = System.Math.Round(tmpInfo.FileInfo.Length / 1024.0 / 1024.0, 1).ToString("F1") + " MB";
            }
            else
            {
                file_size = System.Math.Round(tmpInfo.FileInfo.Length / 1024.0) + " KB";
            }

            TagList.Add($"{ Application.Current.Resources["SInfoByte"]}", file_size);
            TagList.Add($"{ Application.Current.Resources["SInfoSize"]}", Global.PicSourceW.ToString() + " x " + Global.PicSourceH.ToString());
            TagList.Add($"{ Application.Current.Resources["SInfoTime"]}", "");
            TagList.Add($"{ Application.Current.Resources["SInfoModifyTime"]}", tmpInfo.FileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
            TagList.Add($"{ Application.Current.Resources["SInfoExif"]}", "");
            TagList.Add($"{ Application.Current.Resources["SInfoMake"]}", "");
            TagList.Add($"{ Application.Current.Resources["SInfoModel"]}", "");
            TagList.Add($"{ Application.Current.Resources["SInfoLensMake"]}", "");
            TagList.Add($"{ Application.Current.Resources["SInfoLensSpec"]}", "");
            TagList.Add($"{ Application.Current.Resources["SInfoLensModel"]}", "");
            TagList.Add($"{ Application.Current.Resources["SInfoZoom"]}", "");
            TagList.Add($"{ Application.Current.Resources["SInfoFNumber"]}", "");
            TagList.Add($"{ Application.Current.Resources["SInfoExposure"]}", "");
            TagList.Add($"{ Application.Current.Resources["SInfoExposureBias"]}", "");
            TagList.Add($"{ Application.Current.Resources["SInfoISO"]}", "");
            TagList.Add($"{ Application.Current.Resources["SInfoFocal"]}", "");
            TagList.Add($"{ Application.Current.Resources["SInfoMeter"]}", "");
            TagList.Add($"{ Application.Current.Resources["SInfoFlash"]}", "");
            TagList.Add($"{ Application.Current.Resources["SInfoBalance"]}", "");
            TagList.Add($"{ Application.Current.Resources["SInfoBrightness"]}", "");
            TagList.Add($"{ Application.Current.Resources["SInfoExpProgram"]}", "");

            var sExif = "";
            var directories = ImageMetadataReader.ReadMetadata(file);

            foreach (var directory in directories)
            {
                foreach (var tag in directory.Tags)
                {
                    string tagName = EngToChs(tag.Name);
                    if (TagList.ContainsKey(tagName))
                    {
                        TagList[tagName] = tag.Description;
                    }
                    else
                    {
                        if(!TagList.ContainsKey("[" + directory.Name + "] " + tag.Name))
                        {
                            TagList.Add("[" + directory.Name + "] " + tag.Name, tag.Description);
                        }                                               
                    }
                }
            }


            for(int i = 0; i < TagList.Count; i++)
            {
                // 2017:12:25 16:53:22 => 2017-12-25 16:53:22
                if (TagList.ElementAt(i).Key == $"{ Application.Current.Resources["SInfoTime"]}")
                {
                    string date = "";
                    if(TagList.ElementAt(i).Value != "")
                    {
                        date = TagList.ElementAt(i).Value.Substring(0, 11).Replace(":", "-") + TagList.ElementAt(i).Value.Substring(11);                        
                    }
                    sExif += TagList.ElementAt(i).Key + " : " + date + "\n";
                }
                else
                {
                    sExif += TagList.ElementAt(i).Key + " : " + TagList.ElementAt(i).Value + "\n";
                }

                if(i == 6)
                {
                    sExif += $"{ Application.Current.Resources["SInfoHr"]}" + "\n";
                }

                if(i == 23)
                {
                    sExif += $"{ Application.Current.Resources["SInfoMore"]}" + "\n";
                }
                
            }


            return sExif;
        }

        /// <summary>筛选参数并将其名称转换为中文
        /// </summary>
        /// <param name="str">参数名称</param>
        /// <returns>参数中文名</returns>
        private static string EngToChs(string str)
        {
            var rt = str;
            switch (str)
            {
                case "Detected MIME Type":
                    rt = $"{ Application.Current.Resources["SInfoType"]}";
                    break;
                case "Date/Time":
                    rt = $"{ Application.Current.Resources["SInfoTime"]}";
                    break;
                case "Exif Version":
                    rt = $"{ Application.Current.Resources["SInfoExif"]}";
                    break;
                case "Make":
                    rt = $"{ Application.Current.Resources["SInfoMake"]}";
                    break;
                case "Model":
                    rt = $"{ Application.Current.Resources["SInfoModel"]}";
                    break;
                case "Lens Make":
                    rt = $"{ Application.Current.Resources["SInfoLensMake"]}";
                    break;
                case "Lens Specification":
                    rt = $"{ Application.Current.Resources["SInfoLensSpec"]}";
                    break;
                case "Lens Model":
                    rt = $"{ Application.Current.Resources["SInfoLensModel"]}";
                    break;
                case "Digital Zoom Ratio":
                    rt = $"{ Application.Current.Resources["SInfoZoom"]}";
                    break;
                case "F-Number":
                    rt = $"{ Application.Current.Resources["SInfoFNumber"]}";//Aperture Value也表示光圈
                    break;
                case "Exposure Time":
                    rt = $"{ Application.Current.Resources["SInfoExposure"]}";
                    break;
                case "Exposure Bias Value":
                    rt = $"{ Application.Current.Resources["SInfoExposureBias"]}";
                    break;
                case "ISO Speed Ratings":
                    rt = $"{ Application.Current.Resources["SInfoISO"]}";
                    break;
                case "Focal Length":
                    rt = $"{ Application.Current.Resources["SInfoFocal"]}";
                    break;
                case "Metering Mode":
                    rt = $"{ Application.Current.Resources["SInfoMeter"]}";
                    break;
                case "Flash":
                    rt = $"{ Application.Current.Resources["SInfoFlash"]}";
                    break;
                case "White Balance Mode":
                    rt = $"{ Application.Current.Resources["SInfoBalance"]}";
                    break;
                case "Brightness Value":
                    rt = $"{ Application.Current.Resources["SInfoBrightness"]}";
                    break;
                case "Exposure Program":
                    rt = $"{ Application.Current.Resources["SInfoExpProgram"]}";
                    break;



                    /*
                case "File Modified Date":
                    rt = "修改时间";
                    break;
                case "X Resolution":
                    rt = "水平分辨率";
                    break;
                case "Y Resolution":
                    rt = "垂直分辨率";
                    break;
                case "Color Space":
                    rt = "色彩空间";
                    break;
                case "Shutter Speed Value":
                    rt = "快门速度";
                    break;
                case "Flash Mode":
                    rt = "闪光灯";
                    break;
                case "Exposure Mode":
                    rt = "曝光模式";
                    break;
                case "Continuous Drive Mode":
                    rt = "驱动模式";
                    break;
                case "Focus Mode":
                    rt = "对焦模式";
                    break;
                    */
            }
            return rt;
        }


        internal static async Task<BitmapSource> ReturnBitmapSourceAsync(FileInfo fileInfo)
        {
            if (fileInfo == null) { return null; }
            if (fileInfo.Length <= 0) { return null; }

            string ext = fileInfo.Extension;
            switch (fileInfo.Extension)
            {
                case ".jpg":
                case ".jpeg":
                case ".jpe":
                case ".png":
                case ".bmp":
                case ".gif":
                case ".jfif":
                case ".ico":
                case ".webp":
                case ".wbmp":
                default:
                    return await GetWriteableBitmapAsync(fileInfo).ConfigureAwait(false);

                    /*
                case ".tga":
                    return await Task.FromResult(GetDefaultBitmapSource(fileInfo, true)).ConfigureAwait(false);

                case ".svg":
                    // TODO convert to drawingimage instead.. maybe
                    // TODO svgz only works in getDefaultBitmapSource, need to figure out how to fix white bg instead of transparent 
                    return await GetTransparentBitmapSourceAsync(fileInfo, MagickFormat.Svg).ConfigureAwait(false);

                case ".b64":
                    //return await Base64.Base64StringToBitmap(fileInfo).ConfigureAwait(false);

                default:
                    return await Task.FromResult(GetDefaultBitmapSource(fileInfo)).ConfigureAwait(false);
                    */
            }
        }



        private static async Task<BitmapSource> GetWriteableBitmapAsync(FileInfo fileInfo)
        {
            FileStream filestream = null; // https://devblogs.microsoft.com/dotnet/file-io-improvements-in-dotnet-6/
            byte[] data;

            try
            {
                filestream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true);
                data = new byte[filestream.Length];
                await filestream.ReadAsync(data, 0, (int)filestream.Length).ConfigureAwait(false);
                //await filestream.DisposeAsync().ConfigureAwait(false);

                var sKBitmap = SKBitmap.Decode(data);
                if (sKBitmap is null) { return null; }

                var skPic = sKBitmap.ToWriteableBitmap();
                skPic.Freeze();
                sKBitmap.Dispose();
                return skPic;
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine($"{nameof(GetWriteableBitmapAsync)} {fileInfo.Name} exception, \n {e.Message}");
#endif
                return null;
            }
        }



        internal static string GetWritingPath()
        {
            string tmpDir;
            try
            {
                var userConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
                tmpDir = userConfig.FilePath;
            }
            catch (ConfigurationException e)
            {
                tmpDir = e.Filename;
            }

            return Path.GetDirectoryName(tmpDir) ?? string.Empty;
        }
    }
}
