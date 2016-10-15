using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YoukuDL
{
    public class DownloadItem:INotifyPropertyChanged
    {
        #region 内部字段

        private int _receivedSize;

        private int? _totalSize;

        private DownloadStatus _downloadStatus;

        private CancellationTokenSource _cancelSource;

        #endregion       

        public event PropertyChangedEventHandler PropertyChanged;

        public List<DownloadPart> PartList { get; set; }

        public string Title { get; set; }

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
            set
            {
                if(value != this._totalSize)
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
            set
            {
                if (value != this._receivedSize)
                {
                    this._receivedSize = value;
                    NotifyPropertyChanged("ReceivedSize");
                }
            }
        }

        public DownloadStatus Status
        {
            get
            {
                return this._downloadStatus;
            }
            set
            {
                if(value!=this._downloadStatus)
                {
                    this._downloadStatus = value;
                    NotifyPropertyChanged("Status");
                }
            }
        }

        public string Progress
        {
            get
            {
                if (TotalSize != null && TotalSize.Value != 0)
                {
                    return (ReceivedSize / (double)TotalSize).ToString("P2", CultureInfo.InvariantCulture);
                }
                else
                {
                    return "0.00%";
                }
            }
        }

        public async Task Download()
        {
            Reset();
              
            List<Task> taskList = new List<Task>();

            if (this.PartList != null)
            {
                PartList.ForEach(part => {
                    part.ReceivedProgress += Part_ReceivedProgress;
                    Task task = part.Start(this._cancelSource.Token);
                    taskList.Add(task);
                });
            }

            this.Status = DownloadStatus.Downloading;

            try
            {
                Task downloadTask = Task.WhenAll(taskList);

                await downloadTask;
            }
            catch(TaskCanceledException e)
            {
                this.Status = DownloadStatus.New;
                return;
            }

            //下载完成，开始合并和转换格式
            this.Status = DownloadStatus.Converting;
            MergeVideoParts(PartList.Select(part => part.StorePath).ToList(), this.StorePath);

            this.Status = DownloadStatus.Success;       
        }


        private void Reset()
        {
            if (this._cancelSource != null)
            {
                this._cancelSource.Dispose();
            }
            this._cancelSource = new CancellationTokenSource();
        }
        public void Cancel()
        {
            this._cancelSource.Cancel();
        }

        private void MergeVideoParts(List<string> mergingFileList, string mergedFile)
        {
            string mergedSource = FileHelper.GenerateRandomFilePath(".txt");
            string content = string.Join(Environment.NewLine, mergingFileList.Select(file => "file " + "'" + file + "'"));
            File.WriteAllText(mergedSource, content);

            string tempMergedFile = FileHelper.GenerateRandomFilePath();

            if (mergingFileList.Count != 1)
            {
                //日志记录该命令
                string cmd = string.Format("{0} -safe 0 -f concat -i {1} -c copy {2}", string.Empty, mergedSource, tempMergedFile);
                Process p = new Process();
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.FileName = ConfigHelper.FFmpegTool;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.Arguments = cmd;
                p.Start();
                p.WaitForExit();
                string output = p.StandardError.ReadToEnd();
            }
            else
            {
                //只有一个部分的视频时，不用合并操作
            }

            string tempfile = FileHelper.GenerateRandomFilePath(".mp4");

            string cmd2 = string.Format("-i {0} -codec copy {1}", tempMergedFile, tempfile);
            Process p2 = new Process();
            p2.StartInfo.UseShellExecute = false;
            p2.StartInfo.RedirectStandardOutput = true;
            p2.StartInfo.RedirectStandardError = true;
            p2.StartInfo.FileName = ConfigHelper.FFmpegTool;
            p2.StartInfo.CreateNoWindow = true;
            p2.StartInfo.Arguments = cmd2;
            p2.Start();
            p2.WaitForExit();
            string output2 = p2.StandardError.ReadToEnd();

            

            //如果目标位置存在同名文件，先删除
            if(File.Exists(mergedFile))
            {
                File.Delete(mergedFile);
            }
            File.Move(tempfile, mergedFile);
        }

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

        private void NotifyPropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
