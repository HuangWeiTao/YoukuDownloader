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

        private async void btn_download_Click(object sender, RoutedEventArgs e)
        {
            //TestMerge();
            //testDown();

            //TestAddNewItem();

            //return ;

            string url = tbx_url.Text.Trim();

            string cmd = BuildVideoMetaDataCmd(url);

            DownloadItem videoItem = await ParseVideoInfo(cmd);


            await DownloadManager.AddItem(videoItem);
            

            MessageBox.Show("OK");
        }

        private async Task<DownloadItem> ParseVideoInfo(string cmd)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = ConfigHelper.VideoResolveTool;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.Arguments = cmd;
            p.Start();
            p.WaitForExit(5000);
            string output = p.StandardOutput.ReadToEnd();
            List<string> infoList = output.Split('\n').ToList();

            //获取视频下载地址
            Uri uriResult;
            List<string> videoUrlList = infoList.Where(info => Uri.TryCreate(info, UriKind.Absolute, out uriResult)).ToList();
            List<DownloadPart> videoPartList = new List<DownloadPart>();
            foreach(var url in videoUrlList)
            {
                var part = new DownloadPart();
                part.Url = url;
                part.StorePath = FileHelper.GenerateRandomFilePath();

                videoPartList.Add(part);
            }


            //获取视频名称
            string title = infoList.Except(videoUrlList).First();


            DownloadItem item = new DownloadItem();
            item.Title = title;
            item.PartList = videoPartList;
            item.Status = DownloadStatus.New;
            item.StorePath = System.IO.Path.Combine(ConfigHelper.DownloadDir, title + ".mp4");


            await Task.Delay(0);

            return item;
        } 

        private string BuildVideoMetaDataCmd(string videoUrl)
        {
            return string.Format(" --get-url --skip-download --get-title   {0}", videoUrl);
        }
        private void TestMerge()
        {
            List<string> videoParts = new List<string>();
            videoParts.Add("D:\\youku-download\\temp\\b691ddfd-9716-4d1a-951c-7d0eaf5a5514.flv");
            videoParts.Add("D:\\youku-download\\temp\\c8f619f1-d81a-436e-a214-3717ea0dc262.flv");
            videoParts.Add("D:\\youku-download\\temp\\f97d7f00-91df-4917-bbae-54342442bbce.flv");

            //string mergedFile =System.IO.Path.Combine(downloadDir, "xest.flv");

            //MergeVideoParts(videoParts, mergedFile);
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

        #region 下载列表事件处理方法

        private void dg_item_delete(object sender, RoutedEventArgs e)
        {
            var item = GetSelectedItem(sender, e);
            DownloadManager.RemoveItem(item);
        }

        private async void dg_item_redownload(object sender, RoutedEventArgs e)
        {
            var item = GetSelectedItem(sender, e);
            await item.Download();
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

        #endregion
    }
}
