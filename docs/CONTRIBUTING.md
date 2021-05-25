# Contributing to this project

First off, thanks for taking the time considering to contribute to the code. I welcome all contribution, being a solo developer and this being a little project I haven't set any hard requirements or guidelines for pull requests. However I'd appreciate if you could follow the directions set below:

- fork the repository into your own github account
- then clone the repository locally on your computer
- I use the development branch for all in-progress coding, and merge into master eventually - so please checkout this branch
- make sure to always pull latest changes
- create a new branch
- call the branch with a meaningful name, i.e.: feature-megawallpaper-connector
- comment your code as much as possible, I'm not fussy about the style you choose to use
- try to use the existing methods and properties as much as possible (DRY)[https://en.wikipedia.org/wiki/Don%27t_repeat_yourself]
- if you want to add a new command line parameter please follow the same style as the rest of the parameters (I use [McMaster Commandline Utility library](https://github.com/natemcmaster/CommandLineUtils))
- test, test, test
- if all works, push your repository up (do not rebase or squash your commits please)
- create a pull request and explain what your code is about

I'll review and if possible will approve and merge your code (I may ask you to do changes and push more commits to the same pull request). 

## IMPORTANT ##
Some methods (like processDeviantAPI) require the use of client ID and client secrets to access 3rd party API. The ID and Secret are not included in the public repository for obvious security reasons, but attached as environment variables during the build process directly via GitHub CI pipeline (i.e. Actions). If you want to test this specific method you need to request your own ClientId and ClientSecret, create a file called development.env in the project main folder (add it to your .gitignore so it doesn't get pushed in your commits), add the the file the required parameters as follow:

### DeviantArt ###
`deviantArtClientId=xxxxxx
deviantArtClientSecret=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx`

Would you have any questions or doubts about this process, do not hesitate to [open](https://github.com/tommasodargenio/wallpaperbuddy/issues/new) an issue on the repository.

If this is the first time you contribute to a project, consider getting some practice [here](https://github.com/firstcontributions/first-contributions)