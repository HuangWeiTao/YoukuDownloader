using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace YoukuDL
{
    public class DownloadManager
    {
        private static ObservableCollection<DownloadItem> itemList = new ObservableCollection<DownloadItem>();

        public static async Task AddItem(DownloadItem item)
        {
            itemList.Add(item);
            await item.Start();
        }

        public static void RemoveItem(DownloadItem item)
        {
            item.Cancel();
            //itemList.Remove(item);
        }

        public static void RemoveAll()
        {
            itemList.Clear();
        }        

        public static void Bind(DataGrid control)
        {
            control.ItemsSource = itemList;
        }
    }
}
