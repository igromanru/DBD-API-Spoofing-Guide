/// 
/// FiddleScript that merges original DBD API responses with spoofed responses from files.
/// Author: Igromanru
/// Created: 2026-06-07
/// Version: 1.0.0
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
        public class CharacterEntry
        {
            [JsonProperty("characterName")]
            public string CharacterName { get; set; }

            [JsonProperty("legacyPrestigeLevel")]
            public int LegacyPrestigeLevel { get; set; }

            [JsonProperty("prestigeLevel")]
            public int PrestigeLevel { get; set; }

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
            [JsonExtensionData]
            public IDictionary<string, JToken> AdditionalData { get; set; }
        }
    }

    public static class Handlers
    {
        public delegate string MergeResponsesDelegate(string originalJson, string spoofJson);

        struct MergeTarget
        {
            public string ApiPath;
            public string FilePath;
            public MergeResponsesDelegate MergeResponses;
        }

        [RulesOption("Enable DBD Responses Merge")]
        public static bool EnableResponsesMerge = true;

        static readonly List<MergeTarget> MergeTargets = new List<MergeTarget>()
        {
            new MergeTarget
            {
                ApiPath = "/api/v1/dbd-character-data/get-all",
                FilePath = @"D:\Git\Cpp\Dead-By-Daylight-D7-Internal-v2\Fiddler\MarketFiles\GetAll.json",
                MergeResponses = MergeGetAllResponses
            },
            new MergeTarget
            {
                ApiPath = "/api/v1/dbd-inventories/all",
                FilePath = @"D:\Git\Cpp\Dead-By-Daylight-D7-Internal-v2\Fiddler\MarketFiles\MarketTempWithNoCosmetics.json",
                MergeResponses = MergeAllResponses
            },
            new MergeTarget
            {
                ApiPath = "/api/v1/dbd-character-data/bloodweb/v2",
                FilePath = @"D:\Git\Cpp\Dead-By-Daylight-D7-Internal-v2\Fiddler\MarketFiles\Bloodweb.json",
                MergeResponses = MergeBloodwebResponses
            }
        };

        public static void OnBeforeResponse(Session oSession)
        {
            if (!EnableResponsesMerge || oSession == null)
                return;

            foreach (var mergeTarget in MergeTargets)
            {
                if (!oSession.uriContains(mergeTarget.ApiPath) || mergeTarget.MergeResponses == null) 
                    continue;

                try
                {
                    if (!File.Exists(mergeTarget.FilePath))
                    {
                        FiddlerApplication.Log.LogFormat("[Merge] Missing file: {0}", mergeTarget.FilePath);
                        return;
                    }

                    oSession.utilDecodeResponse();

                    string responseBody = oSession.GetResponseBodyAsString();
                    string fakeResponseJson = File.ReadAllText(mergeTarget.FilePath, Encoding.UTF8);

                    string merged = mergeTarget.MergeResponses(responseBody, fakeResponseJson);

                    oSession.utilSetResponseBody(merged);
                    
                    FiddlerApplication.Log.LogFormat("[Merge] Success: {0}", mergeTarget.ApiPath);
                }
                catch (Exception ex)
                {
                    FiddlerApplication.Log.LogFormat("[Merge] Error {0}: {1}", mergeTarget.ApiPath, ex.Message);
                }

                break;
            }
        }

        private static string MergeGetAllResponses(string originalJson, string spoofJson)
        {
            var originalJsonResponse = JsonConvert.DeserializeObject<GetAllSchema.GetAllResponse>(originalJson) ?? new GetAllSchema.GetAllResponse();
            var spoofJsonResponse = JsonConvert.DeserializeObject<GetAllSchema.GetAllResponse>(spoofJson) ?? new GetAllSchema.GetAllResponse();

            if (originalJsonResponse.Entries == null) originalJsonResponse.Entries = new List<GetAllSchema.CharacterEntry>();
            if (spoofJsonResponse.Entries == null) return originalJson;

            foreach (var entry in spoofJsonResponse.Entries)
            {
                if (string.IsNullOrEmpty(entry.CharacterName))
                    continue;

                var existingEntry = originalJsonResponse.Entries.Find(e => e.CharacterName == entry.CharacterName);
                if (existingEntry == null)
                {
                    originalJsonResponse.Entries.Add(entry);
                }
                else
                {
                    existingEntry.PrestigeLevel = entry.PrestigeLevel;
                    existingEntry.LegacyPrestigeLevel = entry.LegacyPrestigeLevel;

                    if (entry.AdditionalData != null)
                    {
                        if (existingEntry.AdditionalData == null)
                        {
                            existingEntry.AdditionalData = new Dictionary<string, JToken>();
                        }

                        foreach (var kvp in entry.AdditionalData)
                        {
                            existingEntry.AdditionalData[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(originalJsonResponse);
        }

        private static string MergeAllResponses(string originalJson, string spoofJson)
        {
            var originalJsonResponse = JsonConvert.DeserializeObject<AllSchema.AllResponse>(originalJson) ?? new AllSchema.AllResponse();
            var spoofJsonResponse = JsonConvert.DeserializeObject<AllSchema.AllResponse>(spoofJson) ?? new AllSchema.AllResponse();

            if (originalJsonResponse.InventoryItems == null) originalJsonResponse.InventoryItems = new List<AllSchema.InventoryItem>();
            if (spoofJsonResponse.InventoryItems == null) return originalJson;

            foreach (var item in spoofJsonResponse.InventoryItems)
            {
                if (string.IsNullOrEmpty(item.ObjectId))
                    continue;

                var existingItem = originalJsonResponse.InventoryItems.Find(i => i.ObjectId == item.ObjectId);
                if (existingItem == null)
                {
                    originalJsonResponse.InventoryItems.Add(item);
                }
                else
                {
                    existingItem.Quantity = item.Quantity;

                    if (item.AdditionalData != null)
                    {
                        if (existingItem.AdditionalData == null)
                        {
                            existingItem.AdditionalData = new Dictionary<string, JToken>();
                        }

                        foreach (var kvp in item.AdditionalData)
                        {
                            existingItem.AdditionalData[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(originalJsonResponse);
        }

        private static string MergeBloodwebResponses(string originalJson, string spoofJson)
        {
            var originalJsonResponse = JsonConvert.DeserializeObject<BloodwebSchema.BloodwebResponse>(originalJson) ?? new BloodwebSchema.BloodwebResponse();
            var spoofJsonResponse = JsonConvert.DeserializeObject<BloodwebSchema.BloodwebResponse>(spoofJson) ?? new BloodwebSchema.BloodwebResponse();

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