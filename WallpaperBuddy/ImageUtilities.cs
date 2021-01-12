using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;



namespace WallpaperBuddy
{
    public static class ImageUtilities
    {
        /// <summary>
        /// Retrieve the dimensions of an online image
        /// TO-DO: Optimize to download only part of the header that contains image information 
        /// ref: https://stackoverflow.com/questions/30054517/get-image-dimensions-directly-from-url-in-c-sharp
        /// https://stackoverflow.com/questions/111345/getting-image-dimensions-without-reading-the-entire-file
        /// https://www.codeproject.com/Articles/35978/Reading-Image-Headers-to-Get-Width-and-Height
        ///
        /// </summary>
        public static string GetWebImageSize(string url)
        {
            string destPath = Path.GetTempPath();
            WebClient Client = new WebClient();

            if (url=="" || url == null)
            {
                return null;
            }

            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                Client.DownloadFile(url, destPath + Path.DirectorySeparatorChar + "wallpaperbuddytmp");
                var img = Image.FromFile(destPath + Path.DirectorySeparatorChar + "wallpaperbuddytmp");
                return (img.Width + "x" + img.Height);
            } 
            catch(WebException webEx)
            {
                DateTime dt = DateTime.Now;
                Console.WriteLine("ERROR [" + dt.ToString("dd-MMM-yyyy HH:mm:ss: ") + "] - " + webEx.ToString());
                return null;                
            }
           
        }
    }

}
