/// 
/// My FiddlerScript that merges original DBD API responses with spoofed responses from files.
/// And blocks game log requests to prevent them from being sent to the server. (no idea if it does anything)
/// With v1.3.0 the script now saves your customization presets to a file and tries to restore them.
/// Author: Igromanru
/// Created: 2026-06-07
/// LastModified: 2026-07-20
/// Version: 1.3.1
/// Instructions:
/// - Set Scripting language to "C#" in Tools->Options->Scripting->Language
/// - Set references to "System.Core.dll;Newtonsoft.Json.dll" in Tools->Options->Scripting->References
/// - Enable "Capture HTTPS CONNECTs" and "Decrypt HTTPS traffic" in Tools->Options->HTTPS
/// - Install Root Certificate if not already done (Tools->Options->HTTPS->Actions->Trust Root Certificate)
/// Hints:
/// - For Steam version of the game you need a EAC undetected SSL Bypass, otherwise Fiddler can't decrypt HTTPS traffic.
/// 
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Fiddler;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fiddler
{
    namespace GetAllSchema
    {
        public class Charm
        {
            [JsonProperty("charmId")]
            public string CharmId { get; set; }

            [JsonProperty("slotIndex")]
            public int SlotIndex { get; set; }
        }

        public class Preset
        {
            [JsonProperty("head")]
            public string Head { get; set; }

            [JsonProperty("legsOrWeapon")]
            public string LegsOrWeapon { get; set; }

            [JsonProperty("torsoOrBody")]
            public string TorsoOrBody { get; set; }

            [JsonProperty("charms")]
            public List<Charm> Charms { get; set; }
        }

        public class Online
        {
            [JsonProperty("active")]
            public int Active { get; set; }

            [JsonProperty("presets")]
            public List<Preset> Presets { get; set; }

            [JsonExtensionData]
            public IDictionary<string, JToken> AdditionalData { get; set; }

            public Online()
            {
                Active = 0;
                Presets = new List<Preset>();
            }
        }

        public class Customizations
        {
            [JsonProperty("Online")]
            public Online Online { get; set; }

            public Customizations()
            {
                this.Online = new Online();
            }
        }

        public class CharacterEntry
        {
            [JsonProperty("characterName")]
            public string CharacterName { get; set; }

            [JsonProperty("legacyPrestigeLevel")]
            public int LegacyPrestigeLevel { get; set; }

            [JsonProperty("prestigeLevel")]
            public int PrestigeLevel { get; set; }

            [JsonProperty("isEntitled")]
            public bool IsEntitled { get; set; }

            [JsonProperty("customizations")]
            public Customizations Customizations { get; set; }

            [JsonExtensionData]
            public IDictionary<string, JToken> AdditionalData { get; set; }
        }

        public class GetAllResponse
        {
            [JsonProperty("list")]
            public List<CharacterEntry> Entries { get; set; }
        }
    }

    namespace AllSchema
    {
        public class InventoryItem
        {
            [JsonProperty("objectId")]
            public string ObjectId { get; set; }

            [JsonProperty("quantity")]
            public int Quantity { get; set; }

            [JsonExtensionData]
            public IDictionary<string, JToken> AdditionalData { get; set; }
        }

        public class AllResponse
        {
            [JsonProperty("inventoryItems")]
            public List<InventoryItem> InventoryItems { get; set; }
        }
    }

    namespace BloodwebSchema
    {
        public class BloodwebResponse
        {
            [JsonProperty("characterName")]
            public string CharacterName { get; set; }

            [JsonExtensionData]
            public IDictionary<string, JToken> AdditionalData { get; set; }
        }
    }

    namespace LoadoutSchema
    {
        public class Customizations
        {
            [JsonProperty("active")]
            public int Active { get; set; }

            [JsonProperty("presets")]
            public List<GetAllSchema.Preset> Presets { get; set; }
        }

        public class Root
        {
            [JsonProperty("character")]
            public string Character { get; set; }

            [JsonProperty("customizations")]
            public Customizations Customizations { get; set; }

            [JsonExtensionData]
            public IDictionary<string, JToken> AdditionalData { get; set; }
        }
    }

    public static class Handlers
    {
        public delegate string MergeResponsesDelegate(string originalJson, object responseObject);

        private static readonly string ScriptsDir = Fiddler.CONFIG.GetPath("Scripts");

        struct MergeTarget
        {
            public object DeserializedResponseObject;
            public MergeResponsesDelegate MergeResponses;
        }

        private static readonly IDictionary<string, MergeTarget> MergeTargets = new Dictionary<string, MergeTarget>()
        {
            { "/api/v1/dbd-inventories/all", new MergeTarget
                {
                    DeserializedResponseObject = JsonConvert.DeserializeObject<AllSchema.AllResponse>(File.ReadAllText(ScriptsDir + @"MarketFiles\Market.json")),
                    MergeResponses = MergeAllResponses
                }
            },
            { "/api/v1/dbd-character-data/get-all", new MergeTarget
                {
                    DeserializedResponseObject = JsonConvert.DeserializeObject<GetAllSchema.GetAllResponse>(File.ReadAllText(ScriptsDir + @"MarketFiles\GetAll.json")),
                    MergeResponses = MergeGetAllResponses
                }
            },
            { "/api/v1/dbd-character-data/bloodweb/v2", new MergeTarget
                {
                    DeserializedResponseObject = JsonConvert.DeserializeObject<BloodwebSchema.BloodwebResponse>(File.ReadAllText(ScriptsDir + @"MarketFiles\Bloodweb.json")),
                    MergeResponses = MergeBloodwebResponses
                }
            }
        };

        [RulesOption("Enable DBD Custom Rules (disabled all options)")]
        public static bool EnableDBDCustomRules = true;

        [RulesOption("Enable DBD Responses Merge")]
        public static bool EnableResponsesMerge = true;

        [RulesOption("Enable DBD Block Log Requests")]
        public static bool EnableBlockLogRequests = false;

        private static readonly string ScriptName = "Igromanru's DBD FiddlerScript";

        private static readonly string CharacterPresetsFile = ScriptsDir + @"CharacterPresetsDb.json";

        private static Dictionary<string, List<GetAllSchema.Preset>> CharacterPresets { get; set; }

        private static void LoadCharacterPresetsFromFile()
        {
            try
            {
                CharacterPresets = JsonConvert.DeserializeObject<Dictionary<string, List<GetAllSchema.Preset>>>(File.ReadAllText(CharacterPresetsFile));
            }
            catch (Exception ex)
            {
                CharacterPresets = new Dictionary<string, List<GetAllSchema.Preset>>();
                FiddlerApplication.Log.LogFormat("[{0}] Error: Failed to load {1}.\n{2}", ScriptName, CharacterPresetsFile, ex.Message);
            }
        }

        private static void SaveCharacterPresetsToFile()
        {
            try
            {
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore
                };
                string json = JsonConvert.SerializeObject(CharacterPresets, settings);
                File.WriteAllText(CharacterPresetsFile, json);
            }
            catch (Exception ex)
            {
                FiddlerApplication.Log.LogFormat("[{0}] Error: Failed to write to {1}.\n{2}", ScriptName, CharacterPresetsFile, ex.Message);
            }
        }

        public static void Main()
        {
            FiddlerApplication.Log.LogFormat("[{0}] Scripts directory: {1}", ScriptName, ScriptsDir);

            LoadCharacterPresetsFromFile();
        }

        public static void OnBeforeRequest(Session oSession)
        {
            if (oSession == null || !EnableDBDCustomRules)
                return;

            if (oSession.HTTPMethodIs("CONNECT") && oSession.hostname != "bhvrdbd.com" && !oSession.hostname.EndsWith(".bhvrdbd.com"))
            {
                oSession["x-no-decrypt"] = "true";
                oSession.bypassGateway = true;
            }

            if (EnableBlockLogRequests && oSession.uriContains("/api/v1/gameLogs/batch"))
            {
                oSession.utilCreateResponseAndBypassServer();
                oSession.oResponse.headers.HTTPResponseStatus = "200 OK";
                oSession.oResponse["Content-Type"] = "application/json";
                oSession.utilSetResponseBody("{\"Encrypted\":false,\"FailedPutCount\":0,\"RequestResponses\":[{\"RecordId\":\"\"}]}");
            }
        }

        public static void OnBeforeResponse(Session oSession)
        {
            if (oSession == null || !EnableDBDCustomRules)
                return;

            if (oSession.uriContains("/api/v1/dbd-character-data/loadout"))
            {
                try
                {
                    oSession.utilDecodeResponse();
                    string responseBody = oSession.GetResponseBodyAsString();
                    LoadoutSchema.Root loadout = JsonConvert.DeserializeObject<LoadoutSchema.Root>(responseBody);
                    if (loadout.Character == null || loadout.Customizations == null) return;

                    CharacterPresets[loadout.Character] = loadout.Customizations.Presets;
                    SaveCharacterPresetsToFile();
                }
                catch (Exception ex)
                {
                    FiddlerApplication.Log.LogFormat("[{0}] Error loadout: {1}", ScriptName, ex.Message);
                }
                return;
            }

            if (!EnableResponsesMerge)
                return;

            foreach (var kvp in MergeTargets)
            {
                var apiPath = kvp.Key;
                var mergeTarget = kvp.Value;
                if (!oSession.uriContains(apiPath) || mergeTarget.DeserializedResponseObject == null || mergeTarget.MergeResponses == null)
                    continue;

                try
                {
                    oSession.utilDecodeResponse();

                    string responseBody = oSession.GetResponseBodyAsString();
                    string merged = mergeTarget.MergeResponses(responseBody, mergeTarget.DeserializedResponseObject);

                    oSession.utilSetResponseBody(merged);

                    FiddlerApplication.Log.LogFormat("[{0}] Success: {1}", ScriptName, apiPath);
                }
                catch (Exception ex)
                {
                    FiddlerApplication.Log.LogFormat("[{0}] Error {1}: {2}", ScriptName, apiPath, ex.Message);
                }

                break;
            }
        }

        private static string MergeGetAllResponses(string originalJson, object responseObject)
        {
            GetAllSchema.GetAllResponse originalJsonResponse = JsonConvert.DeserializeObject<GetAllSchema.GetAllResponse>(originalJson) ?? new GetAllSchema.GetAllResponse();
            GetAllSchema.GetAllResponse spoofJsonResponse = responseObject as GetAllSchema.GetAllResponse ?? new GetAllSchema.GetAllResponse();

            if (originalJsonResponse.Entries == null) originalJsonResponse.Entries = new List<GetAllSchema.CharacterEntry>();
            if (spoofJsonResponse.Entries == null) return originalJson;

            foreach (GetAllSchema.CharacterEntry spoofEntry in spoofJsonResponse.Entries)
            {
                if (string.IsNullOrEmpty(spoofEntry.CharacterName))
                    continue;

                // GetAllSchema.CharacterEntry existingEntry = originalJsonResponse.Entries.Find(e => e.CharacterName == spoofEntry.CharacterName);
                // if (existingEntry == null)
                // {
                //     originalJsonResponse.Entries.Add(spoofEntry);
                // }
                // else
                GetAllSchema.CharacterEntry existingEntry = originalJsonResponse.Entries.Find(e => e.CharacterName == spoofEntry.CharacterName);
                if (existingEntry != null && existingEntry.IsEntitled)
                {
                    existingEntry.PrestigeLevel = spoofEntry.PrestigeLevel;
                    existingEntry.LegacyPrestigeLevel = spoofEntry.LegacyPrestigeLevel;
                    existingEntry.Customizations = spoofEntry.Customizations;

                    List<GetAllSchema.Preset> presets;
                    if (CharacterPresets.TryGetValue(existingEntry.CharacterName, out presets))
                    {
                        if (presets != null && presets.Count > 0)
                        {
                            if (existingEntry.Customizations == null) existingEntry.Customizations = new GetAllSchema.Customizations();
                            existingEntry.Customizations.Online.Presets = presets;

                            FiddlerApplication.Log.LogFormat("[{0}] {1} Presets found for {2}", ScriptName, presets.Count, existingEntry.CharacterName);
                        }
                    }

                    if (spoofEntry.AdditionalData != null)
                    {
                        if (existingEntry.AdditionalData == null) existingEntry.AdditionalData = new Dictionary<string, JToken>();
                        foreach (var kvp in spoofEntry.AdditionalData)
                        {
                            existingEntry.AdditionalData[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(originalJsonResponse);
        }

        private static string MergeAllResponses(string originalJson, object responseObject)
        {
            AllSchema.AllResponse originalJsonResponse = JsonConvert.DeserializeObject<AllSchema.AllResponse>(originalJson) ?? new AllSchema.AllResponse();
            AllSchema.AllResponse spoofJsonResponse = responseObject as AllSchema.AllResponse ?? new AllSchema.AllResponse();

            if (originalJsonResponse.InventoryItems == null) originalJsonResponse.InventoryItems = new List<AllSchema.InventoryItem>();
            if (spoofJsonResponse.InventoryItems == null) return originalJson;

            foreach (var item in spoofJsonResponse.InventoryItems)
            {
                if (string.IsNullOrEmpty(item.ObjectId))
                    continue;

                AllSchema.InventoryItem existingItem = originalJsonResponse.InventoryItems.Find(i => i.ObjectId == item.ObjectId);
                if (existingItem == null)
                {
                    originalJsonResponse.InventoryItems.Add(item);
                }
                else
                {
                    existingItem.Quantity = item.Quantity;

                    if (item.AdditionalData != null)
                    {
                        if (existingItem.AdditionalData == null) existingItem.AdditionalData = new Dictionary<string, JToken>();

                        foreach (var kvp in item.AdditionalData)
                        {
                            existingItem.AdditionalData[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(originalJsonResponse);
        }

        private static string MergeBloodwebResponses(string originalJson, object responseObject)
        {
            BloodwebSchema.BloodwebResponse originalJsonResponse = JsonConvert.DeserializeObject<BloodwebSchema.BloodwebResponse>(originalJson) ?? new BloodwebSchema.BloodwebResponse();
            BloodwebSchema.BloodwebResponse spoofJsonResponse = responseObject as BloodwebSchema.BloodwebResponse ?? new BloodwebSchema.BloodwebResponse();

            if (originalJsonResponse.AdditionalData == null) originalJsonResponse.AdditionalData = new Dictionary<string, JToken>();
            if (spoofJsonResponse.AdditionalData == null) return originalJson;

            foreach (var kvp in spoofJsonResponse.AdditionalData)
            {
                originalJsonResponse.AdditionalData[kvp.Key] = kvp.Value;
            }

            return JsonConvert.SerializeObject(originalJsonResponse);
        }
    }
}