using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using UpdateControls.Fields;
using System.Net;
using System.Windows;

namespace FacetedWorlds.MyCon.ImageUtilities
{
    public class ImageCacheCell
    {
        private static Regex NonAlphaNumeric = new Regex("[^a-zA-Z0-9]");
        private static object SingleLock = new object();
        public const string DefaultSmallImageUrl = "/Images/appbar.feature.search.rest.png";
        public const string DefaultLargeImageUrl = "/Images/appbar.feature.search.rest.png";

        private readonly string _sourceImageUrl;

        private bool _beganLoading = false;
        private Independent<string> _smallImageUrl = new Independent<string>();
        private Independent<string> _largeImageUrl = new Independent<string>();

        public ImageCacheCell(string sourceImageUrl)
        {
            _sourceImageUrl = sourceImageUrl;
        }

        public string SmallImageUrl
        {
            get
            {
                lock (this)
                {
                    BeginLoading();
                    return _smallImageUrl.Value ?? DefaultSmallImageUrl;
                }
            }
        }

        public string LargeImageUrl
        {
            get
            {
                lock (this)
                {
                    BeginLoading();
                    return _largeImageUrl.Value ?? DefaultLargeImageUrl;
                }
            }
        }

        private void BeginLoading()
        {
            if (_beganLoading)
                return;

            _beganLoading = true;
            try
            {
                string localFileNameSmall = CreateLocalFileName("small");
                string localFileNameLarge = CreateLocalFileName("large");
                using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    bool fileExistsSmall = isoStore.FileExists(localFileNameSmall);
                    bool fileExistsLarge = isoStore.FileExists(localFileNameLarge);
                    if (fileExistsSmall)
                        _smallImageUrl.Value = String.Format("storage:{0}", localFileNameSmall);
                    if (fileExistsLarge)
                        _largeImageUrl.Value = String.Format("storage:{0}", localFileNameLarge);
                    if (!fileExistsSmall || !fileExistsLarge)
                    {
                        HttpWebRequest request = HttpWebRequest.CreateHttp(_sourceImageUrl);
                        request.BeginGetResponse(result => EndLoading(request, result, localFileNameSmall, localFileNameLarge), null);
                    }
                }
            }
            catch (Exception ex)
            {
                // Failed to initiate HTTP request for image. Just continue to show the default.
            }
        }

        private void EndLoading(HttpWebRequest request, IAsyncResult result, string localFileNameSmall, string localFileNameLarge)
        {
            try
            {
                WebResponse response = request.EndGetResponse(result);
                byte[] buffer = new byte[response.ContentLength];
                using (Stream responseStream = response.GetResponseStream())
                {
                    responseStream.Read(buffer, 0, (int)response.ContentLength);
                }
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    ScaleBitmap(localFileNameSmall, localFileNameLarge, buffer));
            }
            catch (Exception ex)
            {
                // Failed to read the HTTP response. Just continue to show the default.
            }
        }

        private void ScaleBitmap(string localFileNameSmall, string localFileNameLarge, byte[] buffer)
        {
            try
            {
                using (MemoryStream bitmapStream = new MemoryStream(buffer))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.SetSource(bitmapStream);
                    using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        SaveScaledBitmap(bitmap, localFileNameSmall, isoStore, 80.0, 80.0);
                        SaveScaledBitmap(bitmap, localFileNameLarge, isoStore, 115.0, 115.0);
                    }
                }

                _smallImageUrl.Value = String.Format("storage:{0}", localFileNameSmall);
                _largeImageUrl.Value = String.Format("storage:{0}", localFileNameLarge);
            }
            catch (Exception ex)
            {
                // Failed to save the scaled image. Just continue to show the default.
            }
        }

        private static void SaveScaledBitmap(BitmapImage bitmap, string localFileName, IsolatedStorageFile isoStore, double maxWidth, double maxHeight)
        {
            double scaleWidth = maxWidth / (double)bitmap.PixelWidth;
            double scaleHeight = maxHeight / (double)bitmap.PixelHeight;
            double scale = Math.Min(scaleWidth, scaleHeight);
            int width = (int)Math.Round(scale * (double)bitmap.PixelWidth);
            int height = (int)Math.Round(scale * (double)bitmap.PixelHeight);

            using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream(localFileName, FileMode.Create, isoStore))
            {
                WriteableBitmap writable = new WriteableBitmap(bitmap);
                writable.SaveJpeg(isoStream, width, height, 0, 80);
                isoStream.Flush();
            }
        }

        private string CreateLocalFileName(string suffix)
        {
            return String.Format("{0}_{1}", NonAlphaNumeric.Replace(_sourceImageUrl, String.Empty), suffix);
        }
    }
}
