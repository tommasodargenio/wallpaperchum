# Wallpaper Chum
[![CI](https://github.com/tommasodargenio/wallpaperbuddy/actions/workflows/NetFxCI.yml/badge.svg)](https://github.com/tommasodargenio/wallpaperbuddy/actions/workflows/NetFxCI.yml)
[![Build](https://github.com/tommasodargenio/wallpaperbuddy/actions/workflows/releasePipeline.yml/badge.svg?branch=1.0.0-beta.10)](https://github.com/tommasodargenio/wallpaperbuddy/actions/workflows/releasePipeline.yml)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

Also available on [![Download wallpaperchum](https://sourceforge.net/sflogo.php?type=11&group_id=3388809)](https://sourceforge.net/p/wallpaperchum/)

## In a nutshell
Wallpaper Chum it's a small Windows utility to download random images from a number of sources. It allows you to save the images, set them as desktop's wallpaper/lockscreen and much more

## What it does
The application downloads images from [BING](https://www.bing.com), [Reddit](https://www.reddit.com), and [DeviantArt](https://www.deviantart.com). The image is stored in a folder of your choice, so it can be used as a wallpaper or lockscreen. _please [read](#-Disclaimers-and-Copyrights-notice) disclaimer for each respective source_

You can set a task in the Windows Scheduler to run the app and download the image daily, this can be either stored in a folder or in the Temp folder. The image can be renamed using a number of methods.

You can choose to download images from different regions of BING as they all have different and wonderful images, just use the region/culture option. (For a list of valid region/culture please refer use this [link](http://msdn.microsoft.com/en-us/library/ee825488%28v=cs.20%29.aspx))

Or you can download the images from the many subreddits that have photos, wallpapers, such as: [Images](https://www.reddit.com/r/Images/), [EarthPorn](https://www.reddit.com/r/EarthPorn/), [Paintings](https://www.reddit.com/r/Paintings/), [Wallpapers](https://www.reddit.com/r/wallpapers/), and many more.

Or you can download an image from any artist, tag or topics on [DeviantArt](https://deviantart.com).

WallpaperChum does not need elevated privileges (i.e. Run As Administrator) to download and set the wallpaper or lockscreen. However the user running the application needs to have permissions to write on the destination folders, would this be an existing folder and not the Windows Temp.

## Work in Progress
- Implement configuration file (to avoid using command line parameters every time)
- Implement GUI Frontend to easily manage/change parameters
- Port to .NET Core (this app is very old, can really do with some cleaning up and modernization)

## Bugs and Feature Requests

Would you have any issue with the application, or if you want to request a new feature to be implemented, please submit a new issue on this github repository. 

Support is provided on best-effort basis, please upload screenshots of the logs and any relevant information to help expedite troubleshooting and resolution.

Feature requests will be assessed based on the project's roadmap and mission statement and feedback provided if we decide to implement it or else.

## Contribution

This project was initially created in 2014 with .NET framework, it had a very small scope, the code wasn't implemented exactly using best practices, it was never intended to be public. As such all contribution should take this into consideration.

Now that I've opened the source code and published the application publicly, I'm in the process of porting the whole codebase into .NET Core, so I can modernize and clean the code and finally use best practices.

If you still want to contribute I suggest you work on the current development branch which still uses .NET Framework, I'll port all code into .NET Core eventually.

Please refer to the [contributing](docs/CONTRIBUTING.md) guidelines for further information.

## Usage
This is a fully portable application and does not require installation, download the zip archive from the [release](https://github.com/tommasodargenio/wallpaperchum/releases) page, unzip in a folder and you are good to go.

Open a command shell in the folder where you have unzipped the archive and type wallpaperbubby without parameters, this will show the help with the list of available parameters and options. For more information please refer to the [wiki](https://github.com/tommasodargenio/wallpaperchum/wiki)

### Quick Examples 

Download a random image from deviantArt (`-F D`) wallpapers (`--deviant-topic wallpapers`) topic page, save it to a folder on the desktop (`--save-to c:\users\johndoe\desktop\wallpapers`), set is as desktop background (`-W`) and lockscreen (`-L`)
`c:\>wallpaperchum.exe -W -L -F D --deviant-topic wallpapers --save-to c:\users\johndoe\desktop\wallpapers`

Download a random image from the subrreddit (`-F R`) channel Paintings (`-C Paintings`), make sure the image is in landscape orientation (`-A landscape`) and perform a file analysis (`-SI`) to make sure of this. Last store the file on a folder on the desktop (`--save-to c:\users\johndoe\desktop\wallpapers`)
`c:\>wallpaperchum.exe -F R -C Paintings -A landscape -SI --save-to c:\users\johndoe\desktop\wallpapers`

Download the daily image from Bing (`-F B`), save it to a folder on the desktop (`--save-to c:\users\johndoe\desktop\wallpapers`), if the folder doesn't exist create it (`-Y`), rename the file with the today's date (`-R d`), make sure the destination folder contains only max 30 files and delete the oldest (`-D 30`)
`c:\>wallpaperchum.exe -F B -Y --save-to c:\users\johndoe\desktop\wallpapers -R d -D 30`


# Acknowledgements

This work has been inspired, painstakingly tested, and refined by my brother. Thanks for all you support and ideas!

I owe a great deal to my partner Aisling who supported me throughout the development of this and other crazy ideas, and who helped me overcome the fear of publishing and releasing this work to the public.

To my dear and quirky auntie who always encouraged me and nurtured my brain, I always loved the way she used to say goodbye to me: Ad maiora! (to greater things).


# Disclaimers and Copyrights notice
This application and its code is provided *as-is* without warranty of any kind, either express or implied, including any implied warranties or fitness for a particular purpose, merchantability, or non-infringement.

This application depends on a number of libraries (all included in the binary releases) and their dependencies, all rights belong to their respective owners. See list below for more details

- [Costura.Fody](https://github.com/Fody/Costura) 5.3.0 MIT [License](https://github.com/Fody/Costura/blob/master/LICENSE)
- [Fody](https://github.com/Fody/Fody) 6.5.1 MIT [License](https://github.com/Fody/Fody/blob/master/License.txt)
- [HtmlAgilityPack](https://html-agility-pack.net/) 1.11.3 MIT [License](https://github.com/zzzprojects/html-agility-pack/blob/master/LICENSE)
- [McMaster.Extensions.CommandLineUtils](https://github.com/natemcmaster/CommandLineUtils) 3.1.0 Apache 2.0 [License](https://github.com/natemcmaster/CommandLineUtils/blob/main/LICENSE.txt)
- [Newtonsoft.Json](https://www.newtonsoft.com/json) 13.0.1 MIT [License](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md)
- [.NET](https://dotnet.microsoft.com/) dependencies MIT [License](https://github.com/dotnet/standard/blob/master/LICENSE.TXT)


**BING** is a copyright :copyright: of Microsoft Corporation, please refer to Microsoft service agreement [website](https://www.microsoft.com/en-gb/servicesagreement/) for further details

**Reddit** is a copyright :copyright: of Reddit Inc., please refer to Reddit user agreement [website](https://www.redditinc.com/policies/user-agreement) for further details

**DeviantArt** is a copyright :copyright: of DeviantArt Inc., please refer to DeviantArt service policy [website](https://www.deviantart.com/about/policy/service/) for further details
