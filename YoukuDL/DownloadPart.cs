using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YoukuDL
{
    public class DownloadPart
    {
        public delegate void ReceiveProgress(int received, int? total);

        public event ReceiveProgress ReceivedProgress;

        /// <summary>
        /// 下载地址
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 下载位置
        /// </summary>
        public string StorePath { get; set; }

        /// <summary>
        /// 已接收大小
        /// </summary>
        public int Received { get; protected set; }

        /// <summary>
        /// 总大小
        /// </summary>
        public int? Size { get; protected set; }

        public async Task Start(CancellationToken cancelToken)
        {
            using (ProgressMessageHandler handler = new ProgressMessageHandler())
            {

                handler.HttpReceiveProgress += Handler_HttpReceiveProgress;

                using (HttpClient client = HttpClientFactory.Create(handler))
                {
                    client.Timeout = TimeSpan.FromMinutes(30);

                    HttpResponseMessage response = await client.GetAsync(this.Url, HttpCompletionOption.ResponseHeadersRead, cancelToken);

                    Stream data = await response.Content.ReadAsStreamAsync();

                    using (FileStream file = new FileStream(this.StorePath, FileMode.Create))
                    {
                        await data.CopyToAsync(file,2048,cancelToken);
                    }
                }
            }            
        }

        private void Handler_HttpReceiveProgress(object sender, HttpProgressEventArgs e)
        {
            if(Size == null)
            {
                Size = (int)e.TotalBytes;
            }

            Received = (int)e.BytesTransferred;

            //触发事件
            ReceivedProgress?.Invoke(this.Received, this.Size);
        }
    }    
}
