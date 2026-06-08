# Dead by Daylight Items, Perks and Skins temporary Unlock Guide
In DBD you can hijack and spoof API responses to trick the game into thinking that you own certain/all Items, Perks, Skins, and characters, or have a custom prestige level.  
Currently, the game just lets you roll with it and you can use them freely in online matches.  
They only check if you own the character, profile picture, and banner when you join a server, so don't try to use any of them that you haven't unlocked through normal means.

This guide is built around my C# FiddlerScript that automatically merges original API responses with the "spoof" JSON files containing all items and stuff that will be unlocked.  
Merging preserves loadouts and everything else that wasn't covered by the JSON files.

## Index
- [Required resources](#required-resources)
   - [For unlocking](#for-unlocking)
   - [For Market Files update](#for-market-files-update)
- [Download latest Fiddler](#download-latest-fiddler)
- [Fiddler setup](#fiddler-setup)
- [FiddlerScript and Responses installation](#fiddlerscript-and-responses-installation)
- [Market aka. "/all" response options](#market-aka-all-response-options)
- [Market files aka. API responses generation](#market-files-aka-api-responses-generation)
- [FAQ](#faq)
   - [SSL Bypass / Fiddler doesn't work with the Steam version](#ssl-bypass--fiddler-doesnt-work-with-the-steam-version)
- [Credits](#credits)


## Required resources
### For unlocking
- [Fiddler Classic](https://www.telerik.com/download/fiddler)
- Market files, aka spoof API responses for the latest version of the game
- My **CustomRules.cs** FiddlerScript
- *SSL Bypass* (only needed for Steam)

### For Market Files update
- [Melancholy (Market file generator)](https://github.com/igromanru/Melancholy)
- [Dumper-7](https://github.com/Encryqed/Dumper-7)

## Download latest Fiddler
You can download the latest version of Fiddler Classic from the official Telerik site.  
https://www.telerik.com/download/fiddler  
Just fill out the form with a random e-mail; it doesn't have to be valid; it will lead directly to the download.
## Fiddler setup
After installing and launching Fiddler:  
1. In the Context Menu select **Tools**->**Options...**.
2. In Options dialog open the **HTTPS** tab.
3. Enable **Capture HTTPS CONNECTs** and **Decrypt HTTPS traffic**.
4. Press the **Actions** button and select **Trust Root Certificate**, then confirm any dialog that will pop up with **Yes**, to install Fiddler's certificate for HTTPS decryption.  
5. Now switch to the **Scripting** tab.
6. Copy and paste the following into **References**: `System.Core.dll;Newtonsoft.Json.dll`.
7. Select `C#` as **Language**.
8. Press the **OK** button of the Options dialog and restart Fiddler.

## FiddlerScript and Responses installation

1. Make sure you have done everything described in [Fiddler setup](#fiddler-setup) above.
2. Use the **Fiddler-Scripts-Directory** shortcut or navigate to Fiddler's Scripts directory: `%USERPROFILE%\Documents\Fiddler2\Scripts\`
3. Copy the whole `MarketFiles` folder and the `CustomRules.cs` (replace the existing one) to the scripts directory.
4. Done. The script will be activated automatically.  
   You can toggle it on/off in  
   Context Menu -> **Rules** -> **Enable DBD Responses Merge**. 
5. Always start Fiddler before the game!


## Market aka. "/all" response options
MarketFiles has multiple options for the "/all" API endpoint, which basically contains most of the things that get unlocked. You can choose between the following files:  
- `Market.json`: Contains all inventory items (that you choose while generating with Melancholy). Items, Perks, Skins, profile pictures and banners.  
- `MarketDlcOnly.json`: Contains only characters. (It's an obsolete file, since you can't unlock characters this way anymore.)
- `MarketNoSavefile.json`: Can't remember, but I think it contains everything besides Items and Addons, or some specific Items and Addons are missing.
- `MarketTempWithNoCosmetics.json`: Only Items, Addons, Perks, and Characters 
- `MarketWithPerks.json`: Everything besides Items and Addons

**You can choose which one you want to use.**  
By default, the script uses the full profile `Market.json`.  
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
3. Done. Restart the game for the change to take effect.

## Market files aka. API responses generation
*This section covers only the base steps; it's only for advanced cheaters, and you have to figure out the details yourself!*

With game updates, you will want to generate the new "MarketFiles" aka JSON files containing API responses.  
To do that you need the tool called [Melancholy](https://github.com/OssieFromDK/Melancholy), that was made by [OssieFromDK](https://github.com/OssieFromDK).  
The tool requires an up-to-date `.usmap` mapping file, which you have to find or dump yourself.

You can either try to compile the latest version of the open source tool and update/fix it, or, if it still works, use the pre-compiled version that is included in the release of this guide.

1. Clone and compile [Dumper-7](https://github.com/Encryqed/Dumper-7).
2. Start the game without EAC.
3. Inject `Dumper-7.dll` into the game process. (I recommend using System Informer to suspend the process first before injecting.)
4. Find the SDK dump in `C:\Dumper-7\`. There will be a `Mappings` folder with a `.usmap` file in it.
5. Copy the file and place it in the same directory as `Melancholy.exe`.  
6. Execute the `Melancholy.exe`. 
7. Follow its requests.
   1. Provide a path to the .paks files, then press Enter to continue. 
      e.g. for Epic: `C:\Program Files\Epic Games\DeadByDaylight\DeadByDaylight\Content\Paks`
   2. Provide the AES key, then press Enter to continue. (The AES key rarely changes.)  
      v9.6.2 AES key: `0x22B1639B548124925CF7B9CBAA09F9AC295FCF0324586D6B37EE1D42670B39B3`
   3. Press `1` to select the only `.usmap` that should be in the exe's directory, then press Enter to continue.
8. After setting up the Melancholy tool, it will start asking you about what you want to generate. Press `Y` or `N` to select your options, or simply keep the default by pressing `Enter` to continue.
9. Once done, the `Files` directory will contain new `MarketFiles`.

## FAQ
### SSL Bypass / Fiddler doesn't work with the Steam version
In the Steam version of the game, certificate validation is enabled.  
In order to be able to use Fiddler or any other HTTP proxy to decode encrypted requests, you need to disable the validation.  
For that you need an EAC-undetected "SSL Bypass".  

## Credits
- Ossie (OssieFromDK) for [Melancholy](https://github.com/OssieFromDK/Melancholy) (market files generator)
- GhostyPool for [working Fork of Melancholy](https://github.com/GhostyPool/Melancholy)
- Fischsalat for [Dumper-7](https://github.com/Encryqed/Dumper-7)
- Igromanru (me) for the FiddlerScript and the guide