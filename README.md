![Woah builder woah](https://cdn.discordapp.com/attachments/452940237363216415/529566642070618112/unknown.png)

# Builder
An editor only utility that allows for building a game directly to a zip file. With support for uploading to itch.io and github releases for both private and public repositories.

## Features
- Archives builds to a zip file after building
- Can upload directly to GitHub releases or itch.io
- Auto called `OnPreBuild` method before building
- Auto called `OnPostBuild` method after building but before archiving

## Requirements
- .NET Framework 4.5
- [SharpZipLib](https://github.com/icsharpcode/SharpZipLib) for compression during builds

## Installation
To install for use in Unity, copy everything from this repository to `<YourNewUnityProject>/Packages/Popcron.Builder` folder.

If using 2018.3.x, you can add a new entry to the manifest.json file in your Packages folder:
```json
"com.popcron.builder": "https://github.com/popcron/builder.git"
```

Also make sure that a SharpZipLib.dll is present in your `Assets/Plugins` folder with editor only setting enabled. A copy of this library already compiled as a .dll is available [here](https://github.com/icsharpcode/SharpZipLib/releases/tag/0.86.0.518).

## Uploading to GitHub
To set this up, go to `Popcron/Builder/Settings` menu. From here, add a new service with the `GitHub` type.
- **Owner**: This the github owner name. (eg. https://github.com/popcron/rocket-jump its `popcron`)
- **Repository**: This is the project name used in the url. (eg. https://github.com/popcron/rocket-jump its `rocket-jump`)
- **Prefix**: This is the prefix to use in the tag name for when a release is uploaded. An example of this will be shown.
- **Token**: A private application token that is retrieved from your GitHub account settings, this is required. The GitHub Token key is not stored with the project, it is stored locally in the registry of the machine. DO NOT SHARE THIS TOKEN WITH ANYONE.

## Uploading to itch.io
To set this up, go to `Popcron/Builder/Settings` menu. From here, add a new service with the `Itch` type.
- **Account**: This the itch.io account name. (eg. https://popcron.itch.io/rj its `popcron`)
- **Name**: This is the project name used in the url. (eg. https://popcron.itch.io/rj its `rj`)
- **Butler path**: Path to the folder containing the butler.exe file. If the itch.io app is installed, it will use the butler.exe that is provided with it. If one isn't found, then it will give you the option to download a new copy to your project.

![Settings window](https://cdn.discordapp.com/attachments/452940237363216415/529566234098794516/unknown.png)
