<div align="center">
  <img src="Pico2Dock/src/icon.ico" width="128" height="128"/>
  
  # Pico2Dock
  ### Convert the (X)APK(M, S) file for Pico 4 VR to change the application state from Floating Far app to Dashboard Near Dock, similar to File Manager.<br>Allow multitasking while in a full-screen immersive app.
  ### [Android Version](https://github.com/chaixshot/Pico2DockAndroid/tree/main)
  </div>
  
  ## 🖥️ Desktop screenshot
  <image src="Resource/Desktop_Pico2Dock.png" width="400">
    
  ## 👓 VR Headset screenshot
  <image src="Resource/Screenshot_pl.solidexplorer2.jpeg" width="400"> <image src="Resource/Screenshot_org.mozilla.firefox_beta.jpeg" width="400"> <image src="Resource/Screenshot_com.google.android.apps.translate.jpeg" width="400"> <image src="Resource/Screenshot_app.android.apps.youtube.music.jpeg" width="400">
  
  ## ⛏️ Prerequisites
<div align="center">

| Requirement                                                                                                           | Details                                     |
|-----------------------------------------------------------------------------------------------------------------------|---------------------------------------------|
| <a href="https://www.oracle.com/java/technologies/javase/jdk17-archive-downloads.html"><img src="https://img.shields.io/badge/Oracle-Java%2017-orange?style=for-the-badge&logo=oracle"></a> | [Java 17](https://www.oracle.com/java/technologies/javase/jdk17-archive-downloads.html)                  |
| <a href="https://dotnet.microsoft.com/en-us/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0+-512BD4?style=for-the-badge&logo=dotnet&logoColor=white"></a>             | [.NET Desktop Runtime 10+](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) |

</div>

You have to install **.NET runtime 10.0** or higher, and **Java 17** as recommended.

The file that [Pico 4](https://www.picoxr.com/products/pico4) supported architecture **arm64-v8a**, **armeabi-v7a**, and **armeabi**.\
It can be **.apk**, **.xapk**, **.apkm**, and **.apks**.

## 📐 How to use? 
1. Read and finish the [Prerequisites](#%EF%B8%8F-prerequisites)
2. Download the latest [Release](https://github.com/chaixshot/Pico2Dock/releases) from the GitHub repo
3. Run **Pico2Dock.exe**
4. Drag & Drop APK files to the drop space
5. Press the **Start** button and wait for the finish
6. Docked APK files are in **Pico** folder by the same folder as the original file, or right-click the file in the box to see the options.
7. Copy APK files to the headset and install or install via ``adb install`` command

## ⁉️ Can an app change state on the fly?
No, but you can install **Docked** alongside **Floating** by checking the **Random package name** box option, and you can also add text after the app name for classification.

<image src="Resource/Screenshot_both.jpeg" width="400">

## 🙏 Special thanks to:
- [apktool](https://github.com/iBotPeaches/Apktool) - Used for decompiling and recompiling Android Package
- [APKEditor](https://github.com/REAndroid/APKEditor) - Used for fallback decompiling and recompiling Android Package
- [uber-apk-signer](https://github.com/patrickfav/uber-apk-signer) - Used for signing
- [WPF-UI](https://github.com/lepoco/wpfui) - Fluent UI theme
- [MarkdView](https://github.com/hopesy/MarkdView) - Content textbox Markdown Syntax
