// WallpaperBuddy --- Copyright (C) 2014 Tommaso D'Argenio <dev at tommasodargenio dot com> All rights reserved
/**
 * ***************************************************
 * THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF 
 * ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY 
 * IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR 
 * PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.  
 * ***************************************************
 * ***************************************************
 * 
 * Created by Tommaso D'Argenio
 * Contact <dev at tommasodargenio dot com>
 * http://www.tommasodargenio.com
 * 
 * License: GNU General Public License v3.0 (GNU-GPLv3)
 * Code: https://github.com/tommasodargenio/wallpaperbuddy
 * 
 * * This is a console application which downloads the daily bing background image and save it to the file system
 * It can be scheduled in the Windows Task Scheduler and run daily or as once off
 * The application has a number of parameters and options:
 *
 * -saveTo folder:           specify where to save the image files
 * -XMin resX[,xX]resY       specify the minimum resolution at which the image should be picked
 * -XMax resX[,xX]resY       specify the maximum resolution at which the image should be picked
 * -A landscape|portrait     specify which image aspect to prefer landscape or portrait
 * -Y:                       if the saving folder do not exists, create it
 * -S:                       silent mode, do not output stats/results in console
 * -L:                       set last downloaded image as lock screen (1)
 * -W:                       set last downloaded image as desktop wallpaper (1)
 * -D #:                     keep the size of the saving folder to # files - deleting the oldest
 * -region code:             download images specifics to a region (i.e.: en-US, ja-JP, etc.), if blank uses your internet option language setting (2)
 * -R:                       rename the file using different styles
 * attributes:               d   the current date and time     c     the image caption
 *                           sA  a string with alphabetic seq  sN    string with numeric sequence
 * -renameString string:     the string to use as prefix for sequential renaming - requires -R sA or -R sN
 * -help:                    shows this screen

 * (1):                     This feature it's only available for Windows 8.x systems,
                                                         the image will be saved in the system's temp folder if the saveTo option is not specified
                                                         note that wallpaper image shuffle and lockscreen slide show will be disabled using this option
 * (2):                    For a list of valid region/culture please refer to http://msdn.microsoft.com/en-us/library/ee825488%28v=cs.20%29.aspx

 * * You must run the application with a user account having writing permissions on the destination folder
 * **/
using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using HtmlAgilityPack;
using System.Text.RegularExpressions; 
using System.Net;
using System.Globalization;
/*using Windows.System.UserProfile;
using Windows.Storage.Streams;
using Windows.Storage;*/
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Collections.ObjectModel;
//using Microsoft.GroupPolicy;
using System.Runtime.InteropServices;
using LocalPolicy;
using System.Net.NetworkInformation;
using System.ServiceModel.Security.Tokens;

namespace WallpaperBuddy
{
    class Program
    {
        public static string saveFolder { get; set; }
        public static bool silent { get; set; }
        public static int deleteMax { get; set; }
        public static string region { get; set; }
        public static string rename { get; set; }
        public static string renameString { get; set; }
        public static bool setLockscreen { get; set; }
        public static bool setWallpaper { get; set; }
        public static string getLocalFile { get; set; }
        public static string resolutionMin {get; set;}
        public static string resolutionMax { get; set; }

        public static string aspect { get; set; }
        public static string rssURL { get; set; }

        public static bool resolutionMaxAvailable { get; set; }
        public static bool resolutionMinAvailable { get; set; }

        // Constants used for setWallPaper
        public const int SPI_SETDESKWALLPAPER = 20;
        public const int SPIF_UPDATEINIFILE = 1;
        public const int SPIF_SENDCHANGE = 2;
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int SystemParametersInfo(
          int uAction, int uParam, string lpvParam, int fuWinIni);

        [STAThread]
        static void Main(string[] args)
        {
            // check if any argument
            if (args.Length == 0)
            {
                // no arguments passed, show help screen
                showHelp();
            }
            else
            {
                // process arguments
                InputArguments arguments = new InputArguments(args);
                if (arguments.Contains("help"))
                {
                    showHelp();
                }
                if (arguments.Contains("-S"))
                {
                    silent = true;
                }
                else
                {
                    silent = false;
                }

                if (arguments.Contains("-L"))
                {
                    setLockscreen = true;
                }
                else
                {
                    setLockscreen = false;
                }

                if (arguments.Contains("-W"))
                {
                    setWallpaper = true;
                }
                else
                {
                    setWallpaper = false;
                }

                if (arguments.Contains("-XMin"))
                {                    
                    resolutionMin = arguments["-XMin"];
                    resolutionMinAvailable = true;
                }
                else
                {
                    resolutionMin = "0x0";
                    resolutionMinAvailable = false;
                }
                if (arguments.Contains("-XMax"))
                {
                    resolutionMax = arguments["-XMax"];
                    resolutionMaxAvailable = true;
                }
                else
                {
                    resolutionMax = "0x0";
                    resolutionMaxAvailable = false;
                }

                if (arguments.Contains("-D"))
                {
                    deleteMax = Convert.ToInt32(arguments["-D"]);
                }
                else
                {
                    deleteMax = -1;
                }
                if (arguments.Contains("-A"))
                {
                    if (arguments["-A"] == "landscape")
                    {
                        aspect = "landscape";
                    }
                    else
                    {
                        aspect = "portrait";
                    }
                         
                } else
                {
                    aspect = "landscape";
                }
                if (arguments.Contains("-region"))
                {
                    region = arguments["-region"];
                }
                else
                {
                    region = "";
                }

                if (arguments.Contains("-R"))
                {
                    rename = arguments["-R"];
                } 
                else 
                {
                    rename = "";
                }

                if (arguments.Contains("-renameString"))
                {
                    renameString = arguments["-renameString"];
                }
                else
                {
                    renameString = "";
                }
                // check that if -R sA or -R sN option is selected there is a non empty renameString otherwise return error
                if (rename == "sA" || rename == "sN")
                {
                    if (renameString == "")
                    {
                        writeLog("ERROR: You need to specify a rename String with the option -R sA and -R sN");
                        Environment.Exit(100);
                    }
                }

                saveFolder = "";

                if (arguments.Contains("-saveTo"))
                {                    
                    // check if the saveFolder exists
                    bool exists = Directory.Exists(arguments["-saveTo"]);

                    if (!exists)
                    {
                        if (arguments.Contains("-Y"))
                        {
                            // create the folder
                            Directory.CreateDirectory(arguments["-saveTo"]);
                            writeLog("Saving folder do not exists, creating... " + arguments["-saveTo"]);
                        }
                        else
                        {
                            // Exit with error
                            writeLog("ERROR - The specified saving path (" + arguments["-saveTo"] + ") do not exists!");
                            Environment.Exit(101);
                        }                        
                    }

                    // set the saveFolder
                    saveFolder = arguments["-saveTo"];               
                }
                else if(!arguments.Contains("-L") && !arguments.Contains("-W") && !arguments.Contains("-G") && !arguments.Contains("-F"))
                {
                    writeLog("ERROR - You must specify a destination folder");
                    Environment.Exit(102);
                }

                getLocalFile = "";

                if (arguments.Contains("-G"))
                {
                    bool exists = File.Exists(arguments["-G"]);
                    bool isPNG = arguments["-G"].Contains(".png");

                    if (!isPNG)
                    {
                        // Exit with error
                        writeLog("ERROR - The specified file (" + arguments["-G"] + ") is not a PNG!");
                        Environment.Exit(102);
                    }

                    if (!exists)
                    {
                        // Exit with error
                        writeLog("ERROR - The specified file (" + arguments["-G"] + ") doesn't exist!");
                        Environment.Exit(102);
                    }

                    // set the file
                    getLocalFile = arguments["-G"];
                }

                // start the process
                if (getLocalFile=="")
                {
                    //processGrab();
                    if (arguments.Contains("-F")) {
                        rssURL = "";
                        if (arguments["-F"] != null)
                        {
                            rssURL = arguments["-F"];
                        }
                        processRSS();
                    }
                    
                }
                else
                {
                    writeLog("Setting Wallpaper");
                    bool exists = File.Exists(getLocalFile);
                    if (!exists)
                    {
                        writeLog("specified wallpaper does not exist in: " + getLocalFile);
                    } else
                    {
                        writeLog(" setting: " + getLocalFile);
                    }
                    setWallPaper(getLocalFile);
                }
                

            }
        }

        static void showHelp()
        {
            Console.WriteLine("WallpaperBuddy - Bing Image Background Grabber v1.1");
            Console.WriteLine("Download the daily background image from bing.com");
            Console.WriteLine("\nWallpaperBuddy -saveTo [-Y] [-S] [-D #] [-L] [-W] [-region: code] [-R attributes [-renameString]] [-help]\n");
            
            Console.WriteLine("-saveTo folder:           specify where to save the image files");
            Console.WriteLine("-XMin resX[,xX]resY       specify the minimum resolution at which the image should be picked");
            Console.WriteLine("-XMax resX[,xX]resY       specify the maximum resolution at which the image should be picked");
            Console.WriteLine("-A landscape | portrait   specify which image aspect to prefer landscape or portrait");
            Console.WriteLine("-F URL:                   get a random image from a Reddit RSS feed");
            Console.WriteLine("-G filename:              set the specified file as wallpaper instead of downloading from BING");
            Console.WriteLine("-Y:                       if the saving folder do not exists, create it");
            Console.WriteLine("-S:                       silent mode, do not output stats/results in console");
            Console.WriteLine("-L:                       set last downloaded image as lock screen (1)");
            Console.WriteLine("-W:                       set last downloaded image as desktop wallpaper (1)");
            Console.WriteLine("-D #:                     keep the size of the saving folder to # files - deleting the oldest");
            Console.WriteLine("-region code:             download images specifics to a region (i.e.: en-US, ja-JP, etc.), if blank uses your internet option language setting (2)");
            Console.WriteLine("-R:                       rename the file using different styles");
            Console.WriteLine("attributes:               d   the current date and time     c     the image caption");
            Console.WriteLine("                          sA  a string with alphabetic seq  sN    string with numeric sequence");
            Console.WriteLine("-renameString string:     the string to use as prefix for sequential renaming - requires -R sA or -R sN");
            Console.WriteLine("-help:                    shows this screen");
            Console.WriteLine("");
            Console.WriteLine(@"(1):                      This feature it's only available for Windows 8.x systems,
                          the image will be saved in the system's temp folder if the saveTo option is not specified
                          note that wallpaper image shuffle and lockscreen slide show will be disabled using this option");
            Console.WriteLine(@"(2):                      For a list of valid region/culture please refer to http://msdn.microsoft.com/en-us/library/ee825488%28v=cs.20%29.aspx");

        }

       
        private static bool IsValidCultureInfoName(string name)
        {
            return
                CultureInfo
                .GetCultures(CultureTypes.SpecificCultures)
                .Any(c => c.Name == name);
        }

        // Convert a number in column letter excel style (i.e. column 3 = letter C, column 27 = letter AA)
        static string ColumnIndexToColumnLetter(int colIndex)
        {
            int div = colIndex;
            string colLetter = String.Empty;
            int mod = 0;

            while (div > 0)
            {
                mod = (div - 1) % 26;
                colLetter = (char)(65 + mod) + colLetter;
                div = (int)((div - mod) / 26);
            }
            return colLetter;
        }

        // Output a log message to the stdout if silent option is off
        static void writeLog(string message)
        {
            if (!silent)
            {
                DateTime dt = DateTime.Now;
                Console.WriteLine(dt.ToString("dd-MMM-yyyy HH:mm:ss: ") + message);
            }
        }

        // Process the image caption if required
        static string processCaption(HtmlAgilityPack.HtmlNode document)
        {
            var anchorNodes = document.SelectNodes("//a[contains(@id,'sh_cp')]");
            var imgCaption = "";

            foreach (var anchor in anchorNodes)
            {
                imgCaption = anchor.Attributes["alt"].Value;
            }

            if (imgCaption != "")
            {
                writeLog("Caption found: " + imgCaption);
            }
            else
            {
                writeLog("WARNING - Caption not found, switching to standard file name");
                return "";
            }

            // transform caption - remove commas, dots, parenthesis, etc.
            string[] stringSeparators = new string[] { "(©" };
            var result = imgCaption.Split(stringSeparators, StringSplitOptions.None);
            imgCaption = result[0].TrimEnd().Replace(",", "_").Replace(" ", "_").Replace(".", "_");

            return imgCaption;
        }

        // Process the -D option to keep the destination folder within a max number of files
        static void processDeleteMaxOption()
        {
            if (File.Exists(saveFolder + Path.DirectorySeparatorChar + "Thumbs.db"))
            {
                writeLog("Thumbs db found and deleted");
                File.Delete(saveFolder + Path.DirectorySeparatorChar + "Thumbs.db");
            }
            int fCount = Directory.GetFiles(saveFolder, "*", SearchOption.TopDirectoryOnly).Length;
            writeLog("Files found: " + fCount);

            if (fCount > deleteMax)
            {
                // there are more files than required, delete the oldest until reached the desired amount of files -1
                foreach (var fi in new DirectoryInfo(saveFolder).GetFiles().OrderByDescending(x => x.LastWriteTime).Skip(deleteMax))
                {
                    writeLog("Too many files. Deleting " + fi.FullName);
                    fi.Delete();
                }
            }
            else
            {
                writeLog("Files to keep: " + deleteMax + " - no files deleted");
            }
        }

        // Process the option -R to rename the image file
        static string processRenameFile(string imgCaption, string fName)
        {
            var destFileName = "";

            if (rename == "c" && imgCaption != "")
            {
                // rename with image caption
                destFileName = imgCaption + Path.GetExtension(fName);
            }
            else if (rename == "d")
            {
                // rename with date and time - fixed format used - ddMMMyyyy i.e. 21Dec2014
                DateTime dt = DateTime.Now;
                destFileName = dt.ToString("ddMMMyyyy") + Path.GetExtension(fName);
            }
            else if (rename == "sA" || rename == "sN")
            {
                var fi = Directory.EnumerateFiles(saveFolder).Max(filename => filename);
                var lastfile = Path.GetFileNameWithoutExtension(fi.ToString());
                var sequence = lastfile.Split('_')[1]; // can be A,B,C or 1,2,3 or a two-three-etc digit/letter
                var lastenum = (int)Convert.ToChar(sequence);
                var fCount = -1;
                if (lastenum >= 48 && lastenum <= 57)
                {
                    // numeric sequence
                    lastenum++;
                    destFileName = renameString + "_" + Convert.ToString(lastenum);
                }
                else
                {
                    // alphabetic sequence                                
                    fCount = Directory.GetFiles(saveFolder, "*.*", SearchOption.TopDirectoryOnly).Length + 1;
                    if (fCount > 0)
                    {
                        destFileName = renameString + "_" + ColumnIndexToColumnLetter(fCount);
                    }
                    else
                    {
                        destFileName = renameString + "_Err";
                    }

                }
                destFileName += Path.GetExtension(fName);
            }
            else
            {
                destFileName = Path.GetFileName(fName);
            }

            return destFileName;
        }

        static bool checkInternetConnection(string URL)
        {
            bool exceptionFlag = true;

            // variables used to check internet connection
            HttpWebRequest request = default(HttpWebRequest);
            HttpWebResponse response = default(HttpWebResponse);

            try
            {
                request = (HttpWebRequest)WebRequest.Create(URL);
                // get only the headers  
                request.Method = WebRequestMethods.Http.Head;
                response = (HttpWebResponse)request.GetResponse();
                // status checking  
                exceptionFlag = response.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                exceptionFlag = false;

                writeLog("ERROR: There is a problem with your internet connection or bing.com is down!");
                writeLog("Excepetion details: " + ex.Message);

                // Exit with error
                Environment.Exit(103);
            }
            return exceptionFlag;
        }

        /* Change Lockscreen settings via GPO changes
         * library used: https://bitbucket.org/MartinEden/local-policy
         * ref: http://www.lshift.net/blog/2013/03/25/programmatically-updating-local-policy-in-windows/
         * author: Martin Eden
         */
        static void setLockScreenGPO(string filename)
        {
            var gpo = new ComputerGroupPolicyObject();
            const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Group Policy Objects\{1E2AC4AE-C9D5-4E5B-B2B9-F4C1FF9040F4}Machine\Software\Policies\Microsoft\Windows\Personalization";
            
            using(var machine = gpo.GetRootRegistryKey(GroupPolicySection.Machine))
            {
                using(var terminalServicesKey = machine.CreateSubKey(keyPath))
                {
                    terminalServicesKey.SetValue("LockScreenImage", filename, RegistryValueKind.String);
                    terminalServicesKey.SetValue("NoChangingLockScreen", 1, RegistryValueKind.DWord);
                }

            }
            gpo.Save();
        }

        static void setLockScreenRegistry(string filename)
        {

            RegistryKey myKey;
            
            myKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Group Policy Objects\{1E2AC4AE-C9D5-4E5B-B2B9-F4C1FF9040F4}Machine\Software\Policies\Microsoft\Windows\Personalization\LockScreenImage", true);
            if (myKey == null)
            {
                // Key does not exist, create it
                myKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Group Policy Objects\{1E2AC4AE-C9D5-4E5B-B2B9-F4C1FF9040F4}Machine\Software\Policies\Microsoft\Windows\Personalization\LockScreenImage");
                
            }

            if (myKey == null)
            {
                writeLog("ERROR - Something went wrong while setting the lock screen, make sure to run the program with a user having administrative rights");
                Environment.Exit(111);
            }

            myKey.SetValue("LockScreenImage", filename, RegistryValueKind.String);
            myKey.Close();

            // Disable the user's ability to change lock screen, this is the only way to make the Policy above works
            myKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Group Policy Objects\{1E2AC4AE-C9D5-4E5B-B2B9-F4C1FF9040F4}Machine\Software\Policies\Microsoft\Windows\Personalization\NoChangingLockScreen", true);
            if (myKey == null)
            {
                // Key does not exist, create it
                myKey = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Group Policy Objects\{1E2AC4AE-C9D5-4E5B-B2B9-F4C1FF9040F4}Machine\Software\Policies\Microsoft\Windows\Personalization\NoChangingLockScreen");

            }

            if (myKey == null)
            {
                writeLog("ERROR - Something went wrong while setting the lock screen, make sure to run the program with a user having administrative rights");
                Environment.Exit(111);
            }

            myKey.SetValue("NoChangingLockScreen", 1, RegistryValueKind.DWord);


            myKey.Close();

        }

        // Set the last downloaded image as lockscreen
        /*
        static async void setLockScreen(string filename)
        {
            StorageFolder storageFolder = await StorageFolder.GetFolderFromPathAsync(Path.GetFullPath(filename));
            StorageFile imageFile = await storageFolder.GetFileAsync(Path.GetFileName(filename));
          
            if (imageFile != null)
            {
                try
                {
                    // Application now has access to the picked file, setting image to lockscreen.  This will fail if the file is an invalid format. 
                    await LockScreen.SetImageFileAsync(imageFile);
                    writeLog("LockScreen.SetImageFileAsync called now with imageFile obj");

                    // Retrieve the lock screen image that was set 
                    IRandomAccessStream imageStream = LockScreen.GetImageStream();
                    if (imageStream == null)
                    {
                        writeLog("ERROR - Setting the lock screen image failed.  Make sure your copy of Windows is activated.");
                        Environment.Exit(108);
                    }
                }
                catch (Exception)
                {
                    writeLog("ERROR - Setting the lock screen image failed. Invalid image selected or error opening file");
                    Environment.Exit(109);
                }
            }
            else
            {
                writeLog("ERROR - Setting the lock screen image failed. Image file not found");
                Environment.Exit(110);
            }
        }
        */
        static void setWallPaper(string filename)
        {
            SystemParametersInfo(
              SPI_SETDESKWALLPAPER, 0, filename,
              SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }


        static int processRSS()
        {
            string URL = "https://www.reddit.com/r/EarthPorn/.rss";
            
            if (rssURL != "")
            {
                
                URL = rssURL;
            }
            

            writeLog("Start RSS download from " + URL);

            XmlReader reader = XmlReader.Create(URL);

            HtmlDocument doc = new HtmlDocument();

            string regexpResolution = @"(([\d ]{2,5})[x|*|X|×|,]([\d ]{2,5}))";
            Regex rgx = new Regex(regexpResolution);
            int counter = 0;

            int userResWMin = 0;
            int userResHMin = 0;
            int userResWMax = 0;
            int userResHMax = 0;

            Match userRes = rgx.Match(resolutionMin);
            if (userRes.Success)
            {
                userResWMin = int.Parse(userRes.Groups[2].Value);
                userResHMin = int.Parse(userRes.Groups[3].Value);
            }

            userRes = rgx.Match(resolutionMax);
            if (userRes.Success)
            {
                userResWMax = int.Parse(userRes.Groups[2].Value);
                userResHMax = int.Parse(userRes.Groups[3].Value);
            }


            List<String> ImagesCandidates = new List<string>();
            String urlFound = "";

            while (reader.Read())
            {                
                if (reader.IsStartElement())
                {                    
                    switch (reader.Name.ToString())
                    {
                        case "content":
                            counter++;
                            string entry  = reader.ReadString();

                            doc.LoadHtml(entry);

                            var hrefList = doc.DocumentNode.SelectNodes("//a")
                                            .Select(p => p.GetAttributeValue("href", "not found"))
                                            .ToList();

                            urlFound = hrefList[2];                            
                            //writeLog(hrefList[2]);                            
                            break;
                        case "title":
                            String title = reader.ReadString();
                            Match match = rgx.Match(title);
                            if (match.Success)
                            {
                                if (urlFound != "")
                                {
                                    int imageResW = int.Parse(match.Groups[2].Value);
                                    int imageResH = int.Parse(match.Groups[3].Value);

                                    if (!resolutionMaxAvailable)
                                    {
                                        userResHMax = imageResH;
                                        userResWMax = imageResW;
                                    }
                                    if (!resolutionMinAvailable)
                                    {
                                        userResHMin = 0;
                                        userResWMin = 0;
                                    }
                                    
                                    if (imageResW<=userResWMax && imageResH<=userResHMax && imageResW>=userResWMin && imageResH>=userResHMin)
                                    {
                                        if (aspect == "landscape")
                                        {
                                            if (imageResW > imageResH)
                                            {
                                                ImagesCandidates.Add(urlFound);
                                            }
                                        } else if (aspect == "portrait")
                                        {
                                            if(imageResH > imageResW)
                                            {
                                                ImagesCandidates.Add(urlFound);
                                            }
                                        } else
                                        {
                                            ImagesCandidates.Add(urlFound);
                                        }
                                        
                                       // writeLog("Resolution: " + match.Groups[1].Value);
                                    } 
                                    /*else
                                    {
                                        writeLog("Resolution too high: " + match.Groups[1].Value + " should be between "+  userResWMin.ToString() + "x" + userResHMin.ToString() + " and " + userResWMax.ToString() + "x" + userResHMax.ToString());
                                    }*/
                                } 
                                /*else
                                {
                                    writeLog("Not a valid image, empty");
                                }*/
                                urlFound = "";
                                
                            }
                            /*else
                            {
                                writeLog("Can't find resolution in title, Not a valid image");
                            }*/
                            break;

                    }
                }
            }

            if (ImagesCandidates.Count()>0)
            {
                writeLog("Total candidates found: " + ImagesCandidates.Count().ToString());
                var random = new Random();
                int idx = random.Next(ImagesCandidates.Count);
                writeLog("We picked: " + ImagesCandidates[idx]);
                string destFileName = System.IO.Path.GetFileName(ImagesCandidates[idx]);
                
                WebClient Client = new WebClient();
                try
                {
                    var destPath = "";

                    if (setWallpaper && saveFolder == "")
                    {
                        destPath = Path.GetTempPath();
                    }
                    else if (saveFolder != "")
                    {
                        destPath = saveFolder;
                    }
                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    Client.DownloadFile(ImagesCandidates[idx], destPath + Path.DirectorySeparatorChar + destFileName);

                    writeLog("Image saved at: " + destPath + Path.DirectorySeparatorChar + destFileName);

                    
                    if (setWallpaper)
                    {
                        writeLog("Setting Wallpaper: " + destPath + destFileName);
                        setWallPaper(destPath + Path.DirectorySeparatorChar + destFileName);
                    }
                    
                }
                catch (WebException webEx)
                {
                    writeLog("ERROR - " + webEx.ToString());
                    Environment.Exit(107);
                }


            } else
            {
                writeLog("No valid images where found in the feed");
            }

            if (deleteMax > 0)
            {
                processDeleteMaxOption();
            }

            //get the page
            return 0;
        }

        // Main processing actions
        static int processGrab()
        {
            // flag for exceptions
            bool exceptionFlag = true;

          
            // set the base URL
            string bingURL = "http://www.bing.com";
            string bingURLwithRegion = bingURL + "/?mkt=";

            writeLog("Start Processing...");

            // check if bing.com is up - might be down or there may be internet connection issues
            exceptionFlag = checkInternetConnection(bingURL);

            // check if a region is specified and adjust the bingURL accordingly
            // valid region are http://msdn.microsoft.com/en-us/library/ee825488%28v=cs.20%29.aspx
            if (region != "")
            {
                if (IsValidCultureInfoName(region))
                {
                    bingURLwithRegion += region;
                }
                else
                {
                    // The provided region - culture is not valid - exit with error
                    writeLog("ERROR: The region provided is not valid!");                  
                    Environment.Exit(104);                    
                }
            }

            // set the regexp            
            //string regexpFile = @"(g_img={url:')(?<bib>(\/?[\w\-\.]+[^#?\s]+)(\?([^#]*))?(#(.*))?)(',id:'bgDiv')";
            string regexpFile = @"(g_img={url: "")(?<bib>(\\?\/?[\w\-\.]+[^#?\s]+)(\?([^#]*))?(#(.*))?)("",id:'bgDiv')";
            Regex rgx = new Regex(regexpFile);            

            //get the page
            var web = new HtmlWeb();
            var document = new HtmlDocument();

            writeLog("Downloading Bing Home Page");

            if (region != "")
            {
                document = web.Load(bingURLwithRegion);                     
            }
            else
            {
                document = web.Load(bingURL);                     
            }
            
            var page = document.DocumentNode;

            var counter = 1;
            var imgfound = false;
            var imgCaption = "";

            // If rename option is selected and it's caption, search for it before the file
            if (rename == "c")
            {
                imgCaption = processCaption(page);
            }
            

            // get only the script nodes --- using XPATH notation
            var scriptNodes = page.SelectNodes("//script/text()");

            writeLog("Nodes found: " + scriptNodes.Count);
            
            // process all script nodes
            if (scriptNodes != null)
            {
                foreach (var script in scriptNodes)
                {
                    writeLog("Processing node " + counter);
            
                    // use RegExp to search for the pattern
                    foreach (Match match in rgx.Matches(script.InnerHtml)) {
                        writeLog("Image found at position " + match.Index + " on node " + counter);
                        writeLog("Original file name: " + Path.GetFileName(match.Result("${bib}")));
                        

                        // if argument -D # passed, check the number of files in the dest folder if more than # delete the oldest
                        if(deleteMax > 0) 
                        {
                            processDeleteMaxOption();
                        }

                        var matchedUrl = match.Result("${bib}");
                        matchedUrl = matchedUrl.Replace(@"\", "");

                        var destFileName = processRenameFile(imgCaption, matchedUrl); 

                        // download the file and store in the destination folder
                        WebClient Client = new WebClient();
                        try
                        {
                            writeLog("Trying to download image at: " + bingURL + matchedUrl);

                            var destPath = "";

                            if (setLockscreen && saveFolder=="")
                            {
                                destPath = Path.GetTempPath();
                            }
                            else if (setWallpaper && saveFolder == "")
                            {
                                destPath = Path.GetTempPath();
                            }
                            else if(saveFolder!="")
                            {
                                destPath = saveFolder;
                            }

                            Client.DownloadFile(bingURL + matchedUrl, destPath + Path.DirectorySeparatorChar + destFileName);

                            writeLog("Image saved at: " + destPath + Path.DirectorySeparatorChar + destFileName);

                            if (setLockscreen)
                            {
                                writeLog("Setting Lock screen...");
                                //setLockScreenGPO(destPath + destFileName);
                               // setLockScreen(destPath + destFileName);
                            }

                            if (setWallpaper)
                            {
                                writeLog("Setting Wallpaper: " + destPath + destFileName);
                                setWallPaper(destPath + destFileName);
                            }

                            imgfound = true;
                        }
                        catch (WebException webEx)
                        {
                            writeLog("ERROR - " + webEx.ToString());
                            Environment.Exit(107);
                        }
                    }

                    counter++;
                }
            }
            else
            {
                writeLog("ERROR - Whoops there are no scripts node to parse");                
                Environment.Exit(105);
                return 0;
            }

            if (!imgfound)
            {
                writeLog("ERROR - Whoops No images were found");
                Environment.Exit(106);
                return 0;
            }
            return 1;
        }
    }
}
