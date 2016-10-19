using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using YoukuDL.Extension;
using YoukuDL.Helper;

namespace YoukuDL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        public MainWindow()
        {
            InitializeComponent();

            Init();
        }           

        private void Init()
        {        
            #region 创建下载目录

            if (!Directory.Exists(ConfigHelper.DownloadDir))
            {
                Directory.CreateDirectory(ConfigHelper.DownloadDir);
            }

            if (!Directory.Exists(ConfigHelper.TempDir))
            {
                Directory.CreateDirectory(ConfigHelper.TempDir);
            }

            #endregion

            #region 初始化下载列表

            DownloadManager.Bind(this.dg_downlist);

            #endregion            
        }

        #region UI数据收集方法

        /// <summary>
        /// 视频的保存格式
        /// </summary>
        /// <returns></returns>
        private VideoFormat GetVideoOutputFormat()
        {
            return VideoFormat.MP4;
        }

        #endregion

        #region 事件处理方法

        private void dg_item_delete(object sender, RoutedEventArgs e)
        {
            var item = GetSelectedItem(sender, e);
            DownloadManager.RemoveItem(item);
        }

        private async void dg_item_redownload(object sender, RoutedEventArgs e)
        {
            var item = GetSelectedItem(sender, e);
            await item.Start();
        }

        private void dg_item_position(object sender, RoutedEventArgs e)
        {
            var item = GetSelectedItem(sender, e);            
            FileHelper.LocatingFile(item.StorePath);
        }

        private DownloadItem GetSelectedItem(object sender, RoutedEventArgs e)
        {
            //Get the clicked MenuItem
            var menuItem = (MenuItem)sender;

            //Get the ContextMenu to which the menuItem belongs
            var contextMenu = (ContextMenu)menuItem.Parent;

            //Find the placementTarget
            var target = (DataGrid)contextMenu.PlacementTarget;

            //Get the underlying item, that you cast to your object that is bound
            //to the DataGrid (and has subject and state as property)
            var item = (DownloadItem)target.SelectedCells[0].Item;

            return item;
        }

        private void tbx_url_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string text = Clipboard.GetText().Trim();
            Uri result = null;

            if (Uri.TryCreate(text, UriKind.Absolute, out result))
            {
                this.tbx_url.Text = result.ToString();
            }
        }

        private async void btn_download_Click(object sender, RoutedEventArgs e)
        {
            string url = tbx_url.Text.Trim();

            if(DownloadManager.CheckIfInQueue(url))
            {
                MessageBox.Show("已经在下载列表中！");
            }
            else
            {
                DownloadItem videoItem = await ParseVideoInfo(url, VideoFormat.MP4);
                await DownloadManager.AddItem(videoItem);
            }            
            
        }

        #endregion

        #region 辅助方法

        private string BuildVideoMetaDataCmd(string videoUrl)
        {
            return string.Format(" --get-url --skip-download --get-title   {0}", videoUrl);
        }

        private async Task<DownloadItem> ParseVideoInfo(string parseUrl, VideoFormat outputFormat)
        {
            string cmd = BuildVideoMetaDataCmd(parseUrl);

            var msg = await ProcessHelper.Run(ConfigHelper.VideoResolveTool, cmd, TimeSpan.FromMilliseconds(10000));
            List<string> infoList = msg.Item1.Split('\n').ToList();

            //获取视频下载地址
            Uri uriResult;
            List<string> videoUrlList = infoList.Where(info => Uri.TryCreate(info, UriKind.Absolute, out uriResult)).ToList();
            List<DownloadPart> videoPartList = new List<DownloadPart>();
            foreach (var url in videoUrlList)
            {
                var part = new DownloadPart();
                part.Url = url;
                part.StorePath = FileHelper.GenerateRandomFilePath(VideoFormat.FLV.GetDescription());

                videoPartList.Add(part);
            }

            //获取视频名称
            string title = infoList.Except(videoUrlList).First();

            DownloadItem item = new DownloadItem();
            item.Title = title;
            item.PartList = videoPartList;
            item.Status = DownloadStatus.New;
            item.StorePath = Path.Combine(ConfigHelper.DownloadDir, title + outputFormat.GetDescription());
            item.OutputFormat = outputFormat;
            item.Url = parseUrl;

            return item;
        }

        #endregion        
    }
}
