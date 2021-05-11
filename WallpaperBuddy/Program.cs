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
 * -SI:                      specify to perform a strong image validation (i.e. check if url has a real image encoding - slow method)
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
using McMaster.Extensions.CommandLineUtils;
using System.IO;
using HtmlAgilityPack;
using System.Text.RegularExpressions; 
using System.Net;
using System.Globalization;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Web;
using System.Drawing;

namespace WallpaperBuddy
{
    internal enum ExitCode : int
    {
        SUCCESS = 0,
        EXCEPTION_ERROR = 100,
        MISSING_REQUIRED_PARAMETER = 101,
        CANT_INSTANCIATE_CLASS = 102,
        CALLBACK_NOT_FOUND_OR_INVALID = 103,
        WRONG_PARAMETER = 104,
        UNKNOWN_ERROR = 200
    }

    static class appIdentity
    {
        public const string appFullName = "Wallpaper Buddy";
        public const string appRuntimeName = "wallpaperbuddy";
        public const string appDescription = "Download random wallpapers for desktop and lockscreen";
        public const string appUsage = "Usage: WallpaperBuddy [options] [-help]";
        public const string version = "1.0.0-beta.4";

        public const string FullVersionToString = appFullName + " v" + version + "\n" + appDescription + "\n\n" + appUsage + "\n\n";
        public const string ShortVersionToString = appFullName + " v" + version;        
    }


    [VersionOption(appIdentity.FullVersionToString)]
    public class Program
    {
        #region Private Properties
        private string _saveFolder;
        private string _backupFolder;
        private string _backupFilename;
        private bool _silent;
        private int _deleteMax;
        private string _region;
        private string _rename;
        private string _renameString;
        private bool _setLockscreen;
        private bool _setWallpaper;
        private string _getLocalFile;
        private string _resolutionMin;
        private string _resolutionMax;
        private bool _createFolders;
        private bool _strongImageValidation;
        private string _aspect;
        private string _rssURL;
        private bool _resolutionMaxAvailable;
        private bool _resolutionMinAvailable;
        private int _userResWMin;
        private int _userResHMin;
        private int _userResWMax;
        private int _userResHMax;
        private string _channelName;
        private string _method;
        private string _rssType;
        private string _setStaticWallpaper;
        #endregion

        #region Public Properties

        #endregion

        #region Public Getters / Setters 
        public bool resolutionMaxAvailable { get; set; }
        public bool resolutionMinAvailable { get; set; }
        public int userResWMin { get; set; }
        public int userResHMin { get; set; }
        public int userResWMax { get; set; }
        public int userResHMax { get; set; }



        [Option("-F", CommandOptionType.SingleValue, Description = "[source]:\t\tspecify the source from where to download the image\n" +
                               "\t\t\t[B]ing download from Bing Daily Wallpaper\n" +
                               "\t\t\t[R]eddit download from a subreddit, use -C ChannelName to specify the subreddit\n" +
                               "\t\t\t[D]eviantArt download from a topic on DeviantArt.com, use -C ChannelName to specify the topic\n")]
        public string rssType { get { return _rssType; } set { setRSS(new string[] { "B", "R", "D" }, value); } }

        [Option("-C", CommandOptionType.SingleValue, Description = "channelName:\t\tspecify from which subreddit or deviantart topic to downloade the image from")]
        public string channelName { get { return _channelName; } set { setChannelName(value); } }

        [Option("-Y", CommandOptionType.NoValue, Description = "\t\t\tif the saving folder do not exists, create it")]
        public bool createFolders { get { return _createFolders; } set { _createFolders = value; } }

        [Option("-G", CommandOptionType.SingleValue, Description = "filename:\t\tset the specified file as wallpaper instead of downloading from a source")]
        public string setStaticWallpaper { get { return _setStaticWallpaper; } set { setFileAsWallpaper(value); } }

        [Option("-M", CommandOptionType.SingleValue, Description = "[method]:\t\tspecify the method to use for selecting the image to download\n" +
                                                                   "\t\t\t[R]andom, download a random image from the channel if more than one present - default\n" +
                                                                   "\t\t\t[L]ast, download the most recent image from the channel")]
        public string method { get { return _method; } set { setMethod(new string[] { "R", "L" }, value); } }

        [Option("-saveTo", CommandOptionType.SingleValue, Description = "folder:\t\tspecify where to save the image files")]
        public string saveFolder { get { return _saveFolder; } set { setSaveFolder(value); } }

        [Option("-backupTo", CommandOptionType.SingleValue, Description = "folder:\t\tspecify a backup location where to save the image files")]
        public string backupFolder { get { return _backupFolder; } set { setBackupFolder(value); } }

        [Option("-backupFilename", CommandOptionType.SingleValue, Description = "filename:\t\tspecify the filename to use for the image when saved in the backup folder,\n\t\t\tif not specified it will be the same as the image saved in the saveTo Folder")]
        public string backupFilename { get { return _backupFilename; } set { _backupFilename = value; } }

        [Option("-XMin", CommandOptionType.SingleValue, Description = "resX[,xX]resY\tspecify the minimum resolution at which the image should be picked")]
        public string resolutionMin { get { return _resolutionMin; } set { setXMin(value); } }

        [Option("-XMax", CommandOptionType.SingleValue, Description = "resX[,xX]resY\tspecify the maximum resolution at which the image should be picked")]
        public string resolutionMax { get { return _resolutionMax; } set { setXMax(value); } }

        [Option("-SI", CommandOptionType.NoValue, Description = "\t\t\tperform a strong image validation (i.e. check if url has a real image encoding - slow method")]
        public bool strongImageValidation { get { return _strongImageValidation; } set { _strongImageValidation = true; } }

        [Option("-A", CommandOptionType.SingleValue, Description = "landscape | portrait\tspecify which image aspect to prefer landscape or portrait")]
        public string aspect { get { return _aspect; } set { setAspect(new string[] { "landscape", "portrait" }, value); } }

        [Option("-S", CommandOptionType.NoValue, Description = "\t\t\tsilent mode, do not output stats/results in console")]
        public bool silent { get { return _silent; } set { _silent = value; } }

        [Option("-W", CommandOptionType.NoValue, Description = "\t\t\tset last downloaded image as desktop wallpaper (1)")]
        public bool setWallpaper { get { return _setWallpaper; } set { _setWallpaper = value; } }

        [Option("-L", CommandOptionType.NoValue, Description = "\t\t\tset last downloaded image as lockscreen (3)")]
        public bool setLockscreen { get { return _setLockscreen; } set { _setLockscreen = value; } }


        [Option("-D", CommandOptionType.SingleValue, Description = "#:\t\t\tkeep the size of the saving folder to # files - deleting the oldest")]
        public int deleteMax { get { return _deleteMax; } set { _deleteMax = Convert.ToInt32(value); } }

        [Option("-region", CommandOptionType.SingleValue, Description = "code:\t\t[Bing only] download images specifics to a region (i.e.: en-US, ja-JP, etc.), if blank uses your internet option language setting (2)")]
        public string region { get { return _region; } set { _region = value; } }
        // addParameter("-L", "-L:                       set last downloaded image as lock screen (1)", "");
        [Option("-R", CommandOptionType.SingleValue, Description = "\t\t\trename the file using different styles\n" +
                                                                    "attributes:\t\td   the current date and time     c     the image caption\n" +
                                                                    "\t\t\tsA  a string with alphabetic seq  sN    string with numeric sequence\n" +
                                                                    "\t\t\tsO  string only - this will overwrite any existing file with the same name")]
        public string rename { get { return _rename; } set { setRenameStyle(new string [] {"d","c","sA","sN","sO" }, value); } }

        [Option("-renameString", CommandOptionType.SingleValue, Description = "string:\t\tthe string to use as prefix for sequential renaming - requires -R sA or -R sN")]
        public string renameString { get { return _renameString; } set { _renameString = value; } }

        //addParameter("-help", "-help:                    shows this screen", "showHelp");


        #region Private Internal Properties 
        private static string urlFound = "";
        private static List<string> imagesCaptions = new List<string>();
        private static List<string> imagesCandidates = new List<string>();

        // Parameters
        private static List<string> parameters = new List<string>();
        private static List<string> parametersHelp = new List<string>();
        private static List<string> parametersSet = new List<string>();
        #endregion
        #region Constants
        // Constants used for setWallPaper
        public const int SPI_SETDESKWALLPAPER = 20;
        public const int SPIF_UPDATEINIFILE = 1;
        public const int SPIF_SENDCHANGE = 2;

        // Internal application constants
        public const string BING_BASE_URL = "https://www.bing.com/HPImageArchive.aspx?format=xml&idx=0&n=1";
        public const string BING_REGION_BASE_PARAM = "&mkt=";
        public const string REDDIT_BASE_URL = "https://www.reddit.com/r/%channel%/.rss";
        public const string DEVIANTART_BASE_URL = "https://backend.deviantart.com/rss.xml?q=%channel%";
        #endregion

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int SystemParametersInfo(
          int uAction, int uParam, string lpvParam, int fuWinIni);
        #endregion
        [STAThread]
        #region Properties Setters
        public void initDefaults()
        {
            if (deleteMax == 0) { deleteMax = -1; }
            if (aspect == null) { aspect = "landscape"; }
            if (method == null) { method = "R"; }
            // Xmin
            if (resolutionMin == null) { resolutionMin = "0x0"; }
            // Xmax
            if (resolutionMax == null) { resolutionMax = "0x0"; }
        }

        public void setRenameStyle(string[] validOptions, string parameterValue)
        {
            if (!checkValidOptions(validOptions, parameterValue))
            {
                writeLog("ERROR: You specified a non valid renaming style  (" + parameterValue + "), valid values are: " + string.Join(",", validOptions));
                Environment.Exit((int)ExitCode.WRONG_PARAMETER);
            }            
            _rename = parameterValue;
        }
        public void setAspect(string[] validOptions, string parameterValue) 
        {
            if (!checkValidOptions(validOptions, parameterValue))
            {
                writeLog("ERROR: You specified a non valid aspect ratio  (" + parameterValue + "), valid values are: " + string.Join(",", validOptions));
                Environment.Exit((int)ExitCode.WRONG_PARAMETER);
            }
            _aspect = parameterValue;
        }

        public void setXMin(string parameterValue)
        {
            _resolutionMin = parameterValue; 
            _resolutionMinAvailable = true;
            int[] userRes = processResolution(_resolutionMin);

            _userResWMin = userRes[0];
            _userResHMin = userRes[1];
        }
        public void setXMax(string parameterValue)
        {
            _resolutionMax = parameterValue; 
            _resolutionMaxAvailable = true;
            int[] userRes = processResolution(_resolutionMax);
            _userResWMax = userRes[0];
            _userResHMax = userRes[1];
        }
        public void setBackupFolder(string parameterValue) 
        {             
            // check if the backupFolder exists
            bool exists = Directory.Exists(parameterValue);

            if (!exists)
            {
                if (_createFolders)
                {
                    // create the folder
                    Directory.CreateDirectory(parameterValue);
                    writeLog("Backup folder do not exists, creating... " + parameterValue);
                }
                else
                {
                    // Exit with error
                    writeLog("ERROR - The specified backup path (" + parameterValue + ") do not exists!");
                    Environment.Exit(101);
                }
            }

            // set the backupFolder
            _backupFolder = parameterValue;
        }
        public void setSaveFolder(string parameterValue)
        {
            // check if the saveFolder exists
            bool exists = Directory.Exists(parameterValue);

            if (!exists)
            {
                if (_createFolders)
                {
                    // create the folder
                    Directory.CreateDirectory(parameterValue);
                    writeLog("Saving folder do not exists, creating... " + parameterValue);
                }
                else
                {
                    // Exit with error
                    writeLog("ERROR - The specified saving path (" + parameterValue + ") do not exists!");
                    Environment.Exit(101);
                }
            }

            // set the saveFolder
            _saveFolder = parameterValue;
        }
        

        public void setMethod(string[] validOptions, string parameterValue)
        {
            if (!checkValidOptions(validOptions, parameterValue))
            {
                writeLog("ERROR: You specified a non valid method  (" + parameterValue + "), valid values are: " + string.Join(",", validOptions));
                Environment.Exit((int)ExitCode.WRONG_PARAMETER);
            }

            if (parameterValue == "R" || parameterValue == "L" || parameterValue == "Random" || parameterValue == "Last")
            {
                _method = parameterValue;
            }
            else
            {
                _method = "R";
            }
        }

        public void setFileAsWallpaper(string parameterValue)
        {            
            bool exists = File.Exists(parameterValue);
            bool isImage = new[] { ".png", ".gif", ".jpg", ".tiff", ".bmp", ".jpeg", ".dib", ".jfif", ".jpe", ".tif", ".wdp" }.Any(c => parameterValue.Contains(c));


            if (!isImage)
            {
                // Exit with error
                writeLog("ERROR - The specified file (" + parameterValue + ") is not an image!");
                Environment.Exit(102);
            }

            if (!exists)
            {
                // Exit with error
                writeLog("ERROR - The specified file (" + parameterValue + ") doesn't exist!");
                Environment.Exit(102);
            }

            // set the file
            _setStaticWallpaper = parameterValue;
            writeLog(" setting: " + _setStaticWallpaper + " as wallpaper");
            setWallPaper(_setStaticWallpaper);
            Environment.Exit(0);
        }
        public void setChannelName(string parameterValue) {
            bool isChannel = false;

            if (parameterValue.Length > 0)
            {
                var channels = parameterValue;
                if (channels != "")
                {
                    _channelName = channels;
                    _rssURL = _rssURL.Replace("%channel%", channels);
                    isChannel = true;
                }

            }

            if (!isChannel) 
            {
                // Exit with error
                writeLog("ERROR - You must specify a channel (option -C channelname) when using Reddit or DeviantArt as source and it cannot be blank");
                Environment.Exit(102);
            }
        }
        private void setRSS(string[] validOptions, string parameterValue)
        {
            if (!checkValidOptions(validOptions, parameterValue))
            {
                writeLog("ERROR: You specified a non valid RSS type  (" + parameterValue + "), valid values are: " + string.Join(",", validOptions));
                Environment.Exit((int)ExitCode.WRONG_PARAMETER);
            }

            string rssTypeRequested = parameterValue.ToLower();

            if (parameterValue.Length > 0)
            {
                switch (rssTypeRequested)
                {
                    case "bing":
                    case "b":
                        _rssURL = BING_BASE_URL;
                        _rssType = "BING";
                        // check if a region is specified and adjust the bingURL accordingly
                        // valid region are http://msdn.microsoft.com/en-us/library/ee825488%28v=cs.20%29.aspx
                        if (region != "" && region != null)
                        {
                            if (IsValidCultureInfoName(region))
                            {
                                _rssURL += BING_REGION_BASE_PARAM + region;
                            }
                            else
                            {
                                // The provided region - culture is not valid - exit with error
                                writeLog("ERROR: The region provided is not valid!");
                                Environment.Exit(104);
                            }
                        }
                        else
                        {
                            _rssURL += BING_REGION_BASE_PARAM + "en-WW";
                        }
                        break;
                    case "reddit":
                    case "r":
                        _rssURL = REDDIT_BASE_URL;
                        _rssType = "REDDIT";
                        break;
                    case "deviantart":
                    case "d":
                        _rssURL = DEVIANTART_BASE_URL;
                        _rssType = "DEVIANTART";
                        break;
                    default:
                        _rssType = "";
                        _rssURL = "";
                        break;
                }
            }
            else
            {
                // Exit with error
                writeLog("ERROR - You must specify one of the following types: [B] for Bing, [R] for Reddit, [D] for DeviantArt");
                Environment.Exit((int)ExitCode.WRONG_PARAMETER);
            }

        }
        #endregion

        #region Private Internal methods
        private bool checkValidOptions(string[] validOptions, string parameterValue)
        {
            bool isValid = false;
            foreach (string option in validOptions)
            {
                if (option == parameterValue)
                {
                    isValid = true;
                    break;
                }
            }

            return isValid;
        }
        #endregion
        #region Public Methods
        #endregion
        #region Main        
        
        public static void Main(string[] args)
            => CommandLineApplication.Execute<Program>(args);

        
        
        private void OnExecute(CommandLineApplication app)
        {
            if (app.GetOptions().All(o=>!o.HasValue()))
            {
                app.ShowHelp();
                Environment.Exit((int)ExitCode.MISSING_REQUIRED_PARAMETER);
            }
            writeLog(appIdentity.FullVersionToString, true);
            writeLog("----Start Processing----", true);
            initDefaults();
            processRSS();

            if (_rssURL == null)
            {
                // Exit with error
                writeLog("ERROR - Source is missing, there is nothing else to do");
                Environment.Exit((int)ExitCode.MISSING_REQUIRED_PARAMETER);
            }

        }
        #endregion
        public bool isChannelAvailable(string channel)
        {
            if (channel == null || channel == "")
            {
                writeLog("ERROR - You must specify a channel (option -C channelname) when using Reddit or DeviantArt as source");
                Environment.Exit((int)ExitCode.WRONG_PARAMETER);
                return false;
            } else
            {
                return true;
            }
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
        public void writeLog(string message, bool suppressTime = false)
        {
            if (!silent)
            {
                DateTime dt = DateTime.Now;
                if (!suppressTime)
                {
                    Console.WriteLine(dt.ToString("dd-MMM-yyyy HH:mm:ss: ") + message);
                } else
                {
                    Console.WriteLine(message);
                }
                
            }
        }

        // Process the image caption if required
        public string processCaption(HtmlAgilityPack.HtmlNode document)
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

        // Clean the caption string from commas, dots, parenthesis, etc.
        public string cleanCaption(string caption)
        {
            var newCaption = "";
            string[] stringSeparators = new string[] { "(©" };
            var result = caption.Split(stringSeparators, StringSplitOptions.None);
            newCaption = result[0].TrimEnd().Replace(",", "_").Replace(" ", "_").Replace(".", "_");

            return newCaption;

        }

        // Process the -D option to keep the destination folder within a max number of files
        public void processDeleteMaxOption()
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
                    try
                    {
                        writeLog("Too many files. Deleting " + fi.FullName);
                        fi.Delete();
                    }
                    catch (IOException e)
                    {
                        writeLog("Can't delete " + fi.FullName + " the file is currently used or locked");
                    }
                }
            }
            else
            {
                writeLog("Files to keep: " + deleteMax + " - no files deleted");
            }
        }

        private string MakeValidFileName(string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }

        // Process the option -R to rename the image file
        private string processRenameFile(string imgCaption, string fName)
        {
            var destFileName = "";

            if (rename == "c" && imgCaption != "")
            {
                // rename with image caption
                destFileName = MakeValidFileName(imgCaption + Path.GetExtension(fName));
            }
            else if (rename == "d")
            {
                // rename with date and time - fixed format used - ddMMMyyyy i.e. 21Dec2014
                DateTime dt = DateTime.Now;
                destFileName = dt.ToString("ddMMMyyyy") + Path.GetExtension(fName);
            }
            else if(rename == "sO")
            {
                // check if Rename String is empty
                if (renameString == "" || renameString == null)
                {
                    writeLog("ERROR: You need to specify a renameString when using the -R " + rename + " option");
                    Environment.Exit((int)ExitCode.MISSING_REQUIRED_PARAMETER);
                } 
                // rename with string, only preserve the extension
                destFileName = renameString + Path.GetExtension(fName); 
            }
            else if (rename == "sA" || rename == "sN")
            {
                // check if Rename String is empty
                if (renameString == "" || renameString == null)
                {
                    writeLog("ERROR: You need to specify a renameString when using the -R "+ rename +" option");
                    Environment.Exit((int)ExitCode.MISSING_REQUIRED_PARAMETER);
                }

                var sequence = "";                
                var fi = Directory.EnumerateFiles(saveFolder).Max(filename => filename);
                if (fi == null)
                {
                    // directory is empty
                    sequence = "0";                    
                }
                else
                {
                    var lastfile = Path.GetFileNameWithoutExtension(fi.ToString());                    
                    if (lastfile.Contains("_"))
                    {
                        sequence = lastfile.Split('_')[1]; // can be A,B,C or 1,2,3 or a two-three-etc digit/letter                    
                    }
                    else
                    {
                        sequence = "0";
                    }                 
                    
                }
                //lastenum = (int)Convert.ToChar(sequence);
                var isNumeric = int.TryParse(sequence, out int lastenum);
                var fCount = -1;                
                if (isNumeric && rename == "sN")
                {
                    // numeric sequence
                    lastenum++;
                    destFileName = renameString + "_" + Convert.ToString(lastenum);                    
                }
                else if(!isNumeric || rename == "sA")
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

        public bool checkInternetConnection(string URL)
        {
            if (URL == "" || URL == null)
            {
                writeLog("ERROR - Important parameters missing");
                Environment.Exit((int)ExitCode.MISSING_REQUIRED_PARAMETER);
            }
            bool exceptionFlag = true;

            // variables used to check internet connection
            HttpWebRequest request = default(HttpWebRequest);
            HttpWebResponse response = default(HttpWebResponse);

            Uri domainInfo = new Uri(URL);
            string host = domainInfo.Host;

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

                writeLog("ERROR: There is a problem with your internet connection or " + host + " is down!");
                writeLog("Excepetion details: " + ex.Message);

                // Exit with error
                Environment.Exit(103);
            }
            return exceptionFlag;
        }

        /*
            Set the LockScreen image to the filename, this is done via a registry key change leveraging the Personalization CSP feature
            ref: https://docs.microsoft.com/en-us/windows/client-management/mdm/personalization-csp

            no hard-requirements for adding the keys to the registry directly, however this can be done via GPO policy editor but will require the SetEduPolicies in ShareCSP be enabled
            via Intune. 

            Enabling the PersonalizationCSP will prevent the user to manually change the lockscreen, however deleting the keys will restore the user's ability to edit the lockscreen settings
         */
        public void setLockScreenRegistry(string filename)
        {

            RegistryKey personalizationCSP;


            personalizationCSP = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP", true);

            // Key does not exist, create it            
            if (personalizationCSP == null)
            {
                try
                {
                    personalizationCSP = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP");
                }
                catch (Exception e)
                {
                    writeLog("ERROR - Something went wrong while setting the lock screen, make sure to run the program with a user having administrative rights");
                    writeLog("Details: " + e.Message);
                    Environment.Exit(111);
                }
                
            }
            
            // set the LockScreenImagePath with the image file path
            personalizationCSP.SetValue("LockScreenImagePath", filename, RegistryValueKind.String);

            personalizationCSP.SetValue("LockScreenImageStatus", 1, RegistryValueKind.DWord);

            writeLog("Lockscreen set to: " + filename);

            personalizationCSP.Close();

        }
        public void setWallPaper(string filename)
        {
            SystemParametersInfo(
              SPI_SETDESKWALLPAPER, 0, filename,
              SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }

        public bool weakImageValidation(string url)
        {            
            string imageExtension = @"(?:([^:/?#]+):)?(?://([^/?#]*))?([^?#]*\.(jpg|gif|png|bmp|tiff|jpeg))(?:\?([^#]*))?(?:#(.*))?";
            Regex rgx_Ext = new Regex(imageExtension);
            Match checkExt = rgx_Ext.Match(url.ToLower());
            if (checkExt.Success)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool validateImage(string url)
        {
            bool weakImageValid = weakImageValidation(url);
            if (!strongImageValidation)
            {
                return weakImageValid;
            } else if (weakImageValid)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "HEAD";
                try
                {
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        if (response.StatusCode == HttpStatusCode.OK && response.ContentType.Contains("image"))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                catch
                {
                    return false;
                }
            } else
            {
                return false;
            }
        }

        public bool extractImage(string URL, string title)
        {
            // check if URL is valid
            if (URL == "" || URL == null)
            {
                return false;
            }
            if (!Uri.IsWellFormedUriString(URL, UriKind.Absolute))
            {
                return false;
            }
            if(!weakImageValidation(URL))
            {
                return false;
            }

            int imageResW = 0;
            int imageResH = 0;
            if (!strongImageValidation)
            {
                // tries to get the image resolution from the title
                int[] imageRes = processResolution(title);

                imageResW = imageRes[0];
                imageResH = imageRes[1];
            } else
            {
                // tries to get the image resolution from the remote file directly    
                var imageSize = new Size(0, 0);
                // attempt #1 - get image size via image file headers
                var imageSizeAlt = ImageUtilities.GetWebImageSize_Fast(new Uri(URL));
                imageSize = imageSizeAlt.GetAwaiter().GetResult();
                
                if (imageSize.IsEmpty)
                {
                    // attempt #2 - get image size downloading the whole file, this will be much slower
                    imageSize = ImageUtilities.GetWebImageSize_Slow(URL);
                }

                imageResW = imageSize.Width;
                imageResH = imageSize.Height;
            }

            if (!resolutionMaxAvailable)
            {
                userResHMax = imageResH;
                userResWMax = imageResW;
            }
            
            if (imageResW > 0 && imageResH > 0)
            {
                if (urlFound != "")
                {
                    if (imageResW <= userResWMax && imageResH <= userResHMax && imageResW >= userResWMin && imageResH >= userResHMin)
                    {
                        if (aspect == "landscape")
                        {
                            if (imageResW > imageResH)
                            {            
                                imagesCandidates.Add(urlFound);
                                return true;
                            }
                        }
                        else if (aspect == "portrait")
                        {
                            if (imageResH > imageResW)
                            {                             
                                imagesCandidates.Add(urlFound);
                                return true;
                            }
                        }
                        else
                        {                            
                            imagesCandidates.Add(urlFound);
                            return true;
                        }
                    }
                }
            }
            else if (urlFound != "")
            {
                // check if the url contains an image by looking at the extension                                
                if (validateImage(urlFound))
                {
                    imagesCandidates.Add(urlFound);
                    return true;
                }

            }
            return false;
        }

        public void processBingXML(XmlReader reader)
        {
            string caption = "";
            HtmlDocument doc = new HtmlDocument();
            switch (reader.Name.ToString())
            {
                case "url":
                    urlFound = "https://www.bing.com/" + reader.ReadString();
                    break;
                case "copyright":
                    caption = cleanCaption(reader.ReadString());
                    break;
            }

            // Check image size matches settings
            if (urlFound != "" && !imagesCandidates.Contains(urlFound))
            {
                extractImage(urlFound, caption);
            }

            if (caption != "" && !imagesCaptions.Contains(caption))
            {
                imagesCaptions.Add(caption);
            }
            
        }

        public void processDeviantXML(XmlReader reader)
        {

        }
        public void processRedditXML(XmlReader reader)
        {
            HtmlDocument doc = new HtmlDocument();

            switch (reader.Name.ToString())
            {
                case "content":
                    string entry = reader.ReadString();

                    doc.LoadHtml(entry);

                    var hrefList = doc.DocumentNode.SelectNodes("//a")
                                    .Select(p => p.GetAttributeValue("href", "not found"))
                                    .ToList();
                    if (hrefList.Count()>=2)
                    {
                        urlFound = hrefList[2];
                    } 
                    
                    break;
                case "title":
                    String title = reader.ReadString();
                    if (extractImage(urlFound, title))
                    {
                        imagesCaptions.Add(title);
                    }
                    break;
            }

        }

        /* Breaks down the min and max resolution constraints defined in the resolution paramenter. This could be the user defined resolution from the command line or coming from the image title
           Return an array of integer representing Width, Height*/
        public int[] processResolution(string resolution)
        {
            string regexpResolution = @"(([\d ]{2,5})[x|*|X|×|,]([\d ]{2,5}))";
            int[] processedRes = new int[2];
            Regex rgx = new Regex(regexpResolution);
            int userResW = 0;
            int userResH = 0;

            Match userRes = rgx.Match(resolution);
            if (userRes.Success)
            {
                userResW = int.Parse(userRes.Groups[2].Value);
                userResH = int.Parse(userRes.Groups[3].Value);
            }
            processedRes[0] = userResW;
            processedRes[1] = userResH;
            return processedRes;
        }

        public string extractFileNameFromURL(string URL)
        {
            Uri urlFoundUri = new Uri(URL);
            string fileName = "";
            switch (rssType)
            {
                case "BING":
                    fileName =  HttpUtility.ParseQueryString(urlFoundUri.Query).Get("id");
                    break;
                case "REDDIT":
                    fileName = urlFoundUri.Segments[1];
                    break;
                case "DEVIANTART":
                    fileName = "";
                    break;
            }
            return fileName;
        }

        private int processRSS()
        {
            string URL = "";

            // flag for exceptions
            bool exceptionFlag;

            if (_rssURL != null)
            {
                
                URL = _rssURL;
            } 
            else
            {
                // Exit with error
                writeLog("ERROR - Source has not been specified, there is nothing else to do");
                Environment.Exit((int)ExitCode.MISSING_REQUIRED_PARAMETER);
            }

            if (rssType == "REDDIT" || rssType == "DEVIANTART")
            {
                if (channelName == "" || channelName == null)
                {
                    // Exit with error
                    writeLog("ERROR - You must specify a channel (option -C channelname) when using Reddit or DeviantArt as source and it cannot be blank");
                    Environment.Exit((int)ExitCode.MISSING_REQUIRED_PARAMETER);

                }
            }

            if ( rename == "sA" || rename == "sO" || rename == "sN")
            {
                if (renameString == "" || renameString == null)
                {
                    writeLog("ERROR: You need to specify a renameString when using the -R " + rename + " option");
                    Environment.Exit((int)ExitCode.MISSING_REQUIRED_PARAMETER);
                }
            }

            // check if source is up - might be down or there may be internet connection issues
            exceptionFlag = checkInternetConnection(URL);

            writeLog("Start RSS download from " + URL);

            XmlReader reader = XmlReader.Create(URL);

            while (reader.Read())
            {                
                if (reader.IsStartElement())
                {
                    switch(rssType)
                    {
                        case "BING":
                            processBingXML(reader);
                            break;
                        case "REDDIT":
                            processRedditXML(reader);
                            break;
                        case "DEVIANTART":
                            processDeviantXML(reader); 
                            break;
                    }                    
                }
            }

            if (imagesCandidates.Count()>0)
            {
                writeLog("Total candidates found: " + imagesCandidates.Count().ToString());

                // if argument -D # passed, check the number of files in the dest folder if more than # delete the oldest
                if (deleteMax > 0)
                {
                    processDeleteMaxOption();
                }

                var random = new Random();
                int idx = 0;
                if (method == "R" || method == "Random")
                {
                    idx = random.Next(imagesCandidates.Count);
                    writeLog("We picked this random image: " + imagesCandidates[idx]);
                }  else
                {
                    writeLog("We picked the most recent uploaded image: " + imagesCandidates[idx]);
                }
                
                

                string fName = extractFileNameFromURL(imagesCandidates[idx]);


                string destFileName = processRenameFile(imagesCaptions[idx], fName);
                string destBackupFileName = "";
                WebClient Client = new WebClient();
                try
                {
                    var destPath = "";
                    if (setLockscreen && saveFolder == null)
                    {
                        destPath = Path.GetTempPath();
                    } 
                    else if (setWallpaper && saveFolder == null)
                    {
                        destPath = Path.GetTempPath();
                    }
                    else if (saveFolder != null)
                    {
                        destPath = saveFolder;
                    }
                    else
                    {
                        destPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    }

                    ServicePointManager.Expect100Continue = true;
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    Client.DownloadFile(imagesCandidates[idx], destPath + Path.DirectorySeparatorChar + destFileName);
                    writeLog("Image saved at: " + destPath + Path.DirectorySeparatorChar + destFileName);
                    // copy the file to the backup folder if defined
                    if (backupFolder!=null)
                    {
                        if (backupFilename!=null)
                        {
                            string ext = Path.GetExtension(destFileName);
                            destBackupFileName = backupFilename + ext;
                        } else
                        {
                            destBackupFileName = destFileName;
                        }
                        System.IO.File.Copy(destPath + Path.DirectorySeparatorChar + destFileName, backupFolder + Path.DirectorySeparatorChar + destBackupFileName, true);
                        writeLog("Backup saved at: " + backupFolder + Path.DirectorySeparatorChar + destBackupFileName);
                    }
                    
                    

                    
                    if (setWallpaper)
                    {
                        writeLog("Setting Wallpaper: " + destPath + Path.DirectorySeparatorChar + destFileName);
                        setWallPaper(destPath + Path.DirectorySeparatorChar + destFileName);
                    }
                    
                    if (setLockscreen)
                    {
                        writeLog("Setting Lock screen...");
                        setLockScreenRegistry(destPath + Path.DirectorySeparatorChar + destFileName);
                    }

                }
                catch (WebException webEx)
                {
                    writeLog("ERROR - " + webEx.ToString());
                    Environment.Exit((int)ExitCode.EXCEPTION_ERROR);
                }


            } else
            {
                writeLog("No valid images where found in the feed or invalid feed provided");
            }

            //get the page
            return 0;
        }     
    }
}
