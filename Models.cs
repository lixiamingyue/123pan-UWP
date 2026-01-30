using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage;

namespace _123云盘UWP
{
    public class RootObject { public Data data { get; set; } }
    public class Data
    {
        public long lastFileId { get; set; } 
        public List<File> fileList { get; set; }
    }
    public class File
    {
        public string fileName { get; set; }
        public string updateAt { get; set; }
        public string category { get; set; }
        public string type { get; set; }
        public string fileId { get; set; }
        public string trashed { get; set; }
    }
    public class FileInfoModel
    {
        public string FileName { get; set; }
        public string IconPath { get; set; }
        public string FileDate { get; set; }
        public string FileId { get; set; }
        public string Type { get; set; }
    }
    public class Folder
    {
        public string Name { get; set; }
        public string FolderId { get; set; }
    }

    public class FileUploadModel : INotifyPropertyChanged
    {
        private double _progress;
        private string _status;
        private string _progressText;

        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string MD5 { get; set; }
        public StorageFile StorageFile { get; set; }

        public double Progress
        {
            get => _progress;
            set { _progress = value; OnPropertyChanged(); UpdateProgressText(); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public string ProgressText
        {
            get => _progressText;
            private set { _progressText = value; OnPropertyChanged(); }
        }

        private void UpdateProgressText()
        {
            double uploadedMB = (FileSize * Progress / 100.0) / (1024.0 * 1024.0);
            double totalMB = FileSize / (1024.0 * 1024.0);
            ProgressText = $"{uploadedMB:F2} MB / {totalMB:F2} MB";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    public class ApiResponse<T>
    {
        public int code { get; set; }
        public string message { get; set; }
        public T data { get; set; }
    }

    public class CreateFileResponseData
    {
        public long fileID { get; set; }
        public bool reuse { get; set; }
        public string preuploadID { get; set; }
        public long sliceSize { get; set; }
        public List<string> servers { get; set; }
    }

    public class UploadCompleteResponseData
    {
        public bool completed { get; set; }
        public long fileID { get; set; }
    }
}