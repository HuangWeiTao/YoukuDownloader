using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using YoukuDL.Extension;
using YoukuDL.Helper;

namespace YoukuDL
{
    public class DownloadItem : INotifyPropertyChanged
    {
        #region 内部字段

        private int _receivedSize;

        private int? _totalSize;

        private DownloadStatus _downloadStatus;

        private CancellationTokenSource _cancelSource;

        #endregion       

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 下载分段列表
        /// </summary>
        public List<DownloadPart> PartList { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 文件存储路径
        /// </summary>
        public string StorePath { get; set; }

        /// <summary>
        /// 文件所有部分的总大小
        /// </summary>
        public int? TotalSize
        {
            get
            {
                return this._totalSize;
            }
            private set
            {
                if (value != this._totalSize)
                {
                    this._totalSize = value;
                    NotifyPropertyChanged("TotalSize");
                }
            }
        }

        /// <summary>
        /// 已接收的总大小
        /// </summary>
        public int ReceivedSize
        {
            get
            {
                return _receivedSize;
            }
            private set
            {
                if (value != this._receivedSize)
                {
                    this._receivedSize = value;
                    NotifyPropertyChanged("ReceivedSize");
                }
            }
        }

        /// <summary>
        /// 当前状态
        /// </summary>
        public DownloadStatus Status
        {
            get
            {
                return this._downloadStatus;
            }
            set
            {
                if (value != this._downloadStatus)
                {
                    this._downloadStatus = value;
                    NotifyPropertyChanged("Status");
                }
            }
        }

        /// <summary>
        /// 下载进度
        /// </summary>
        public double Progress
        {
            get
            {
                if (TotalSize != null && TotalSize.Value != 0)
                {
                    return (ReceivedSize / (double)TotalSize);
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// 视频输出格式
        /// </summary>
        /// <returns></returns>
        public VideoFormat OutputFormat { get; set; }

        #region 接口方法

        public async Task Start()
        {
            Reset();            
            
            try
            {
                this.Status = DownloadStatus.Downloading;

                await Download();

                //下载完成，开始合并和转换格式
                this.Status = DownloadStatus.Converting;
                await ProcessVideo(PartList.Select(part => part.StorePath).ToList(), this.StorePath, this.OutputFormat);

                this.Status = DownloadStatus.Success;
            }
            catch (TaskCanceledException e)
            {
                this.Status = DownloadStatus.New;
                return;
            }
            catch(Exception e)
            {
                this.Status = DownloadStatus.Error;
            }           
        }

        public void Cancel()
        {
            this._cancelSource.Cancel();
        }

        #endregion

        #region 视频下载与转换

        private void Reset()
        {
            if (this._cancelSource != null)
            {
                this._cancelSource.Dispose();
            }

            if (this.PartList != null)
            {
                foreach (var part in PartList)
                {
                    part.ReceivedProgress -= Part_ReceivedProgress;
                }
            }

            this._cancelSource = new CancellationTokenSource();

            this.ReceivedSize = 0;
            this.TotalSize = null;
            this.Status = DownloadStatus.New;
        }

        /// <summary>
        /// 视频下载
        /// </summary>
        /// <returns></returns>
        private async Task Download()
        {
            #region 绑定下载进度事件

            List<Task> taskList = new List<Task>();

            if (this.PartList != null)
            {
                PartList.ForEach(part =>
                {
                    part.ReceivedProgress += Part_ReceivedProgress;
                    Task task = part.Start(this._cancelSource.Token);
                    taskList.Add(task);
                });
            }

            #endregion

            List<string> downloadPartList = this.PartList.Select(part => part.StorePath).ToList();

            await Task.WhenAll(taskList);            
        }

        /// <summary>
        /// 视频合并，合并后的视频格式与被合并的视频格式一致
        /// </summary>
        /// <param name="mergingFileList"></param>
        /// <returns>合并后的文件路径</returns>
        private async Task<string> MergeVideoParts(List<string> mergingFileList)
        {
            //生成合并信息供ffmpeg使用
            string mergedSource = FileHelper.GenerateRandomFilePath(".txt");
            string content = string.Join(Environment.NewLine, mergingFileList.Select(file => "file " + "'" + file + "'"));
            File.WriteAllText(mergedSource, content);

            string tempMergedFile = FileHelper.GenerateRandomFilePath(Path.GetExtension(mergingFileList.First()));

            //视频合并
            string cmd = string.Format("{0} -safe 0 -f concat -i {1} -c copy {2}", string.Empty, mergedSource, tempMergedFile);
            await ProcessHelper.Run(ConfigHelper.FFmpegTool, cmd, TimeSpan.FromMilliseconds(0));

            return tempMergedFile;
        }

        /// <summary>
        /// 视频转换
        /// </summary>
        /// <returns>返回转换后的文件路径</returns>
        private async Task<string> ConvertVideoFormat(string sourceFile, VideoFormat outputFormat)
        {
            string tempConvertedFile = FileHelper.GenerateRandomFilePath(outputFormat.GetDescription());
            string cmd = string.Format("-i {0} -codec copy {1}", sourceFile, tempConvertedFile);
            await ProcessHelper.Run(ConfigHelper.FFmpegTool, cmd, TimeSpan.FromMilliseconds(0));

            return tempConvertedFile; 
        }

        private async Task ProcessVideo(List<string> mergingFileList, string outputFile, VideoFormat outputFormat)
        {
            string mergedFile = await MergeVideoParts(mergingFileList);
            string convertedFile = await ConvertVideoFormat(mergedFile, outputFormat);


            //如果目标位置存在同名文件，先删除
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }

            File.Move(convertedFile, outputFile);
        }

        #endregion

        #region 事件方法
        private void Part_ReceivedProgress(int received, int? total)
        {
            //直接访问DownloadPart相关的参数即可，这里传入的参数可忽略。
            int allPartReceived = 0;
            int allPartSize = 0;

            //重新计算所有part的接收大小和总大小
            lock (this)
            {
                foreach (var part in PartList)
                {
                    allPartReceived += part.Received;
                }

                foreach (var part in PartList)
                {
                    //只要有一个part未知总的大小，那么整个DownloadItem总大小也是未知的
                    if (part.Size == null)
                    {
                        allPartSize = 0;
                        break;
                    }
                    else
                    {
                        allPartSize += (int)part.Size;
                    }
                }
            }

            this.ReceivedSize = allPartReceived;

            if (allPartSize != 0)
            {
                this.TotalSize = allPartSize;
            }

            //Progress应该依赖于ReceivedSize和TotalSize
            NotifyPropertyChanged("Progress");
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
