# Dead by Daylight Items, Perks and Skins temporary Unlock Guide

In DBD you can hijack and spoof API responses to trick the game into thinking that you own certain Items, Perks, Skins and have a certain character prestige.  
Currently the game just let you role with it and you can use them freely in online matches.  
They only check if you own the character when you join a server, so don't try to use any character that you haven't unlocked through normal methods.

## Index

## Required resources
- [Fiddler Classic](https://www.telerik.com/download/fiddler)
- [Melancholy (Market file generator) by OssieFromDK](https://github.com/igromanru/Melancholy) **OR** Market files aka. spoof API responses for latest version of the game
- My **MergeResponses.cs** FiddlerScript
- SSL Bypass (needed only for Steam)

## Download latest Fiddler
You can download latest version of Fiddler Classic from the official telerik site.  
https://www.telerik.com/download/fiddler  
Just fill out the form with a random E-Mail, it doesn't has to be valid, it will lead directly to the download.
## Fiddler setup
After installing and launching Fiddler:  
1. In the Context Menu select **Tools**->**Options...**.
2. In Options dialog open the **HTTPS** tab.
3. Enable **Capture HTTPS CONNECTs** and **Decrypt HTTPS traffic**.
4. Press the **Actions** button and select **Trust Root Certificate**, then confirm any dialog that will pop-up with **Yes**, to install Fiddlers certificate for HTTPS decryption.  
5. Now switch to the **Scripting** tab.
6. Copy and paste into the **References** following: `System.Core.dll;Newtonsoft.Json.dll`.
7. Select `C#` as **Language**.
8. Press **OK** button  of the Options dialog and restart Fiddler.

## FiddlerScript and Responses setup

1. Make sure you have done everything described in [Fiddler setup](#fiddler-setup) above.
2. 

## Market files aka. API Responses generation