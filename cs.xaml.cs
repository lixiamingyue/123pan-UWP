using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Gaming.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace _123云盘UWP
{
    public sealed partial class cs : Page
    {
        private CancellationTokenSource _cancellationTokenSource;
        private HttpClient _httpClient;  // 声明变量

        public cs()
        {
            this.InitializeComponent();
            _httpClient = new HttpClient();  // 关键修复：初始化HttpClient
            where();
        }

        private void where()
        {
            if (App.where == "all")
            {
                DownloadButton1();
            }
        }

        private async void DownloadButton1()
        {
            try
            {
                DownloadProgressBar.Visibility = Visibility.Visible;
                DownloadProgressBar.Value = 0;
                ProgressTextBlock.Text = "0%";
                StatusTextBlock.Text = "准备下载...";
                _cancellationTokenSource = new CancellationTokenSource();
                StorageFolder destinationFolder = App.SaveFolder;
                if (destinationFolder == null)
                {
                    StatusTextBlock.Text = "未选择保存文件夹";
                    return;
                }
                await DownloadFileAsync(App.url, destinationFolder, _cancellationTokenSource.Token);
                App.url = "";
                App.where = "";
                StatusTextBlock.Text = "下载完成!";
                ProgressTextBlock.Text = "100%";
                DownloadProgressBar.Value = 100;
            }
            catch (OperationCanceledException)
            {
                StatusTextBlock.Text = "下载已取消";
                ProgressTextBlock.Text = "已取消";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"下载失败: {ex.Message}";
                ProgressTextBlock.Text = "失败";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }
        private async Task DownloadFileAsync(string url, StorageFolder saveFolder, CancellationToken cancellationToken)
        {
            string fileName = await GetFileNameFromUrlOrHeader(url, cancellationToken);
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = $"download_{Guid.NewGuid():N}.tmp";
            }
            StorageFile saveFile = await saveFolder.CreateFileAsync(
                fileName,
                CreationCollisionOption.GenerateUniqueName);
            using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                var contentLength = response.Content.Headers.ContentLength;
                StatusTextBlock.Text = contentLength.HasValue
                    ? $"开始下载: {saveFile.Name} ({contentLength.Value / 1024 / 1024:F2} MB)"
                    : $"开始下载: {saveFile.Name} (大小未知)";
                using (var httpStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = await saveFile.OpenStreamForWriteAsync())
                {
                    byte[] buffer = new byte[8192];
                    long totalBytesRead = 0;
                    int bytesRead;

                    while ((bytesRead = await httpStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                        totalBytesRead += bytesRead;

                        if (contentLength.HasValue)
                        {
                            var progress = (double)totalBytesRead / contentLength.Value * 100;
                            UpdateProgress(progress, totalBytesRead, contentLength.Value, saveFile.Name);
                        }
                        else
                        {
                            UpdateProgressWithoutLength(totalBytesRead, saveFile.Name);
                        }
                    }
                }
            }
        }
        private async Task<string> GetFileNameFromUrlOrHeader(string url, CancellationToken cancellationToken)
        {
            try
            {
                string fileNameFromUrl = Path.GetFileName(new Uri(url).LocalPath);
                if (!string.IsNullOrEmpty(fileNameFromUrl))
                {
                    fileNameFromUrl = DecodeUrlEncodedString(fileNameFromUrl);
                    if (!string.IsNullOrEmpty(Path.GetExtension(fileNameFromUrl)))
                    {
                        return fileNameFromUrl;
                    }
                }
                using (var request = new HttpRequestMessage(HttpMethod.Head, url))
                using (var response = await _httpClient.SendAsync(request, cancellationToken))
                {
                    if (response.IsSuccessStatusCode &&
                        response.Content?.Headers.ContentDisposition != null)
                    {
                        string contentDisposition = response.Content.Headers.ContentDisposition.ToString();
                        string rfc6266FileName = ExtractRfc6266FileName(contentDisposition);
                        if (!string.IsNullOrEmpty(rfc6266FileName))
                        {
                            return DecodeUrlEncodedString(rfc6266FileName);
                        }
                        string regularFileName = ExtractRegularFileName(contentDisposition);
                        if (!string.IsNullOrEmpty(regularFileName))
                        {
                            return TryDecodeFileName(regularFileName);
                        }
                    }
                    if (response.Content?.Headers.ContentType != null)
                    {
                        string contentType = response.Content.Headers.ContentType.MediaType;
                        string extension = GetExtensionFromContentType(contentType);

                        if (!string.IsNullOrEmpty(extension))
                        {
                            if (!string.IsNullOrEmpty(fileNameFromUrl))
                            {
                                return $"{Path.GetFileNameWithoutExtension(fileNameFromUrl)}{extension}";
                            }
                            return $"download{extension}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析文件名时出错: {ex.Message}");
            }
            return $"download_{Guid.NewGuid():N}.tmp";
        }
        private string ExtractRfc6266FileName(string contentDisposition)
        {
            const string prefix = "filename*=UTF-8''";
            int index = contentDisposition.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                int start = index + prefix.Length;
                int end = contentDisposition.IndexOf(';', start);
                if (end < 0) end = contentDisposition.Length;

                return contentDisposition.Substring(start, end - start).Trim();
            }

            return null;
        }
        private string ExtractRegularFileName(string contentDisposition)
        {
            const string filenameToken = "filename=";
            int index = contentDisposition.IndexOf(filenameToken, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return null;

            int start = index + filenameToken.Length;
            if (start >= contentDisposition.Length) return null;
            if (contentDisposition[start] == '"' || contentDisposition[start] == '\'')
            {
                char quoteChar = contentDisposition[start];
                start++;

                int end = contentDisposition.IndexOf(quoteChar, start);
                if (end >= 0)
                {
                    return contentDisposition.Substring(start, end - start);
                }
            }
            int semiIndex = contentDisposition.IndexOf(';', start);
            if (semiIndex >= 0)
            {
                return contentDisposition.Substring(start, semiIndex - start).Trim();
            }

            return contentDisposition.Substring(start).Trim();
        }
        private string TryDecodeFileName(string encodedName)
        {
            try
            {
                string decoded = DecodeUrlEncodedString(encodedName);
                if (IsValidFileName(decoded))
                    return decoded;
            }
            catch { }
            try
            {
                byte[] isoBytes = System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(encodedName);
                string utf8String = System.Text.Encoding.UTF8.GetString(isoBytes);
                if (IsValidFileName(utf8String))
                    return utf8String;
            }
            catch { }
            foreach (var encodingName in new[] { "GB2312", "GB18030", "Big5" })
            {
                try
                {
                    byte[] encodedBytes = System.Text.Encoding.GetEncoding(encodingName).GetBytes(encodedName);
                    string utf8String = System.Text.Encoding.UTF8.GetString(encodedBytes);
                    if (IsValidFileName(utf8String))
                        return utf8String;
                }
                catch { }
            }
            return $"{encodedName}_{Guid.NewGuid():N}.tmp";
        }
        private bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;
            char[] invalidChars = Path.GetInvalidFileNameChars();
            return !fileName.Any(c => invalidChars.Contains(c));
        }
        private string DecodeUrlEncodedString(string encoded)
        {
            encoded = encoded.Replace("+", " ");

            var bytes = new List<byte>();
            for (int i = 0; i < encoded.Length; i++)
            {
                char c = encoded[i];
                if (c == '%' && i + 2 < encoded.Length)
                {
                    string hex = encoded.Substring(i + 1, 2);
                    if (byte.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out byte b))
                    {
                        bytes.Add(b);
                        i += 2;
                        continue;
                    }
                }
                bytes.AddRange(System.Text.Encoding.UTF8.GetBytes(c.ToString()));
            }
            return System.Text.Encoding.UTF8.GetString(bytes.ToArray());
        }
        private string GetExtensionFromContentType(string contentType)
        {
            var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "text/plain", ".txt" },
                { "application/pdf", ".pdf" },
                { "application/zip", ".zip" },
                { "application/x-rar-compressed", ".rar" },
                { "image/jpeg", ".jpg" },
                { "image/png", ".png" },
                { "image/gif", ".gif" },
                { "application/msword", ".doc" },
                { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", ".docx" },
                { "application/vnd.ms-excel", ".xls" },
                { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx" },
                { "video/mp4", ".mp4" },
                { "audio/mpeg", ".mp3" }
            };

            if (mappings.TryGetValue(contentType, out string extension))
            {
                return extension;
            }

            return string.Empty;
        }
        private void UpdateProgress(double progress, long bytesRead, long totalBytes, string fileName)
        {
            _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                DownloadProgressBar.Value = progress;
                ProgressTextBlock.Text = $"{progress:F1}%";
                StatusTextBlock.Text = $"下载中: {fileName} - {bytesRead / 1024 / 1024:F2} MB / {totalBytes / 1024 / 1024:F2} MB";
            });
        }
        private void UpdateProgressWithoutLength(long bytesRead, string fileName)
        {
            _ = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ProgressTextBlock.Text = "计算中...";
                StatusTextBlock.Text = $"下载中: {fileName} - {bytesRead / 1024 / 1024:F2} MB";
            });
        }
    }
}
