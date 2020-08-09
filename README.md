# wallpaperbuddy

### In a nutshell
Wallpaper Buddy it's a small desktop app utility to download random wallpapers from a number of sources. It allows you to save the images, set them as wallpaper and much more

### What it does
Download image backgrounds from BING or Reddit RSS Feeds, store it in a folder so it can be used as a wallpaper or lockscreen.

You can set a task in the Windows Scheduler to run the app and download the image daily, this can be either stored in a folder or in the Temp folder. The image can be renamed using a number of methods.

You can also specify whether you want the last downloaded image to become your wallpaper.

You can choose to download images from different regions of BING as they all have different and wonderful images, just use the region/culture option. Or you can download the images from the many subreddits (a number of them are provided in the sample library file already) that have photos, wallpapers, etc.

The utility does not need elevated privileges (i.e. Run As Administrator) to download and set the wallpaper. It may require it for setting the lockscreen (WIP)

### Work in Progress
- Implement ability to set the downloaded image as lockscreen (wallpaper and lockscreen will be the same)
- Implement configuration file (to avoid using command line parameters every time)

### How it works
Open a command shell and type wallpaperbubby without parameters to show the help
