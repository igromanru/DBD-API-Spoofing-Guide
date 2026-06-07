# Dead by Daylight Items, Perks and Skins temporary Unlock Guide

In DBD you can hijack and spoof API responses to trick the game into thinking that you own certain Items, Perks, Skins, and have a certain character prestige.  
Currently the game just lets you roll with it and you can use them freely in online matches.  
They only check if you own the character, profile picture, and banner when you join a server, so don't try to use any of them that you haven't unlocked through normal methods.

## Index

## Required resources
- [Fiddler Classic](https://www.telerik.com/download/fiddler)
- [Melancholy (Market file generator) by OssieFromDK](https://github.com/igromanru/Melancholy) **OR** Market files aka. spoof API responses for latest version of the game
- My **MergeResponses.cs** FiddlerScript
- SSL Bypass (needed only for Steam)

## Download latest Fiddler
You can download the latest version of Fiddler Classic from the official telerik site.  
https://www.telerik.com/download/fiddler  
Just fill out the form with a random E-Mail; it doesn't have to be valid, it will lead directly to the download.
## Fiddler setup
After installing and launching Fiddler:  
1. In the Context Menu select **Tools**->**Options...**.
2. In Options dialog open the **HTTPS** tab.
3. Enable **Capture HTTPS CONNECTs** and **Decrypt HTTPS traffic**.
4. Press the **Actions** button and select **Trust Root Certificate**, then confirm any dialog that will pop up with **Yes**, to install Fiddler's certificate for HTTPS decryption.  
5. Now switch to the **Scripting** tab.
6. Copy and paste into the **References** following: `System.Core.dll;Newtonsoft.Json.dll`.
7. Select `C#` as **Language**.
8. Press the **OK** button of the Options dialog and restart Fiddler.

## FiddlerScript and Responses installation

1. Make sure you have done everything described in [Fiddler setup](#fiddler-setup) above.
2. Use the **Fiddler-Scripts-Directory** shortcut or navigate yourself to Fiddler's Scripts directory: `%USERPROFILE%\Documents\Fiddler2\Scripts\`
3. Copy the whole `MarketFiles` folder and the `CustomRules.cs` (replace the existing one) to the scripts directory.
4. Done. The script will be activated automatically and you can toggle it in  
   Context Menu -> **Rules** -> **Enable DBD Responses Merge**. 
5. Always start Fiddler before the game!


## Market aka. "/all" response options
`MarketFiles` has multiple options for the "/all" API endpoint, which basically contains most of the things that are getting unlocked. You can choose between the following files:  
- `Market.json`: Contains all inventory items (that you choose while generating with Melancholy). Items, Perks, Skins, profile pictures and banners.  
- `MarketDlcOnly.json`: Contains only characters. (It's an obsolete file, since you can't unlock characters this way anymore)
- `MarketNoSavefile.json`: Can't remember, but I think it contains everything besides Items and Addons. Or some specific Items and Addons are missing.
- `MarketTempWithNoCosmetics.json`: Only Items, Addons, Perks, and Characters 
- `MarketWithPerks.json`: Everything besides Items and Addons

**You can choose which one you want to use.**  
Per default the script uses full profile, the `Market.json`.  
If you want to change it:  

1. Open the `CustomRules.cs` script in any text editor of your choosing OR in Fiddler under **FiddlerScript**.
2. Replace `Market.json` (in line 94) with the file you want.
   e.g.
   ```csharp
   DeserializedResponseObject = JsonConvert.DeserializeObject<AllSchema.AllResponse>(File.ReadAllText(ScriptsDir + @"MarketFiles\Market.json")),`
   ```
   ->
   ```csharp
   DeserializedResponseObject = JsonConvert.DeserializeObject<AllSchema.AllResponse>(File.ReadAllText(ScriptsDir + @"MarketFiles\MarketTempWithNoCosmetics.json")),
   ```
3. Done. Restart the game for the effect to take.

## Market files aka. API Responses generation