using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TekaTeka.Plugins
{
    internal class CustomSongLoader
    {
        const string CHARTS_FOLDER = "fumen";
        const string PRACTICE_DIVISIONS_FOLDER = "fumencsv";
        const string ASSETS_FOLDER = "ReadAssets";

        static readonly string songsPath = Path.Combine(BepInEx.Paths.GameRootPath, "TekaSongs");

        static Dictionary<int, int> musicIdToMusicInfo = new Dictionary<int, int>();
        static Dictionary<string, int> musicFileToMusicInfo = new Dictionary<string, int>();
        static List<MusicDataInterface.MusicInfo> customSongsList = new List<MusicDataInterface.MusicInfo>();

#region Append Custom Songs DB

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MusicDataInterface), "LoadDataFromFile")]
        static void LoadSongsDatabase_Postfix(MusicDataInterface __instance, ref string path)
        {
            string musicDataPath = Path.Combine(songsPath, ASSETS_FOLDER, "musicinfo");
            string jsonString;
            if (File.Exists(musicDataPath + ".json"))
            {
                musicDataPath += ".json";
                jsonString = File.ReadAllText(musicDataPath);
            }
            else if (File.Exists(musicDataPath + ".bin"))
            {
                musicDataPath += ".bin";
                var bytes = Cryptgraphy.ReadAllAesAndGZipBytes(musicDataPath, Cryptgraphy.AesKeyType.Type2);
                jsonString = Encoding.UTF8.GetString(bytes);
            }
            else
            {
                Logger.Log($"File \"musicinfo\" at {musicDataPath + "{.bin/.json}"} not present", LogType.Warning);
                return;
            }

            try
            {
                var musicInfolist = JsonSupports.ReadJson<MusicDataInterface.MusicInfo>(jsonString);
                for (int i = 0; i < musicInfolist.Count; i++)
                {
                    MusicDataInterface.MusicInfo song = musicInfolist[i];
                    customSongsList.Add(song);
                    musicFileToMusicInfo.TryAdd(song.SongFileName, customSongsList.Count);
                    musicIdToMusicInfo.TryAdd(song.UniqueId, customSongsList.Count);

                    __instance.AddMusicInfo(ref song);
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Got error {e.Message} while reading {musicDataPath}", LogType.Error);
            }
        }

#endregion

#region Load Custom Chart

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FumenLoader.PlayerData), "ReadCoroutine")]
        static void ReadSongChart_Prefix(FumenLoader.PlayerData __instance, ref string filePath)
        {
            if (File.Exists(filePath))
            {
                return;
            };
            string fileName = Path.GetFileName(filePath);
            filePath = Path.Combine(songsPath, CHARTS_FOLDER, fileName);
        }

#endregion

#region Load Custom Practice Chart

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FumenDivisionManager), nameof(FumenDivisionManager.Load))]
        static bool ReadSongPracticeDivision_Prefix(FumenDivisionManager __instance, ref string musicuid)
        {
            string originalFile =
                Path.Combine(UnityEngine.Application.streamingAssetsPath, PRACTICE_DIVISIONS_FOLDER, musicuid + ".bin");
            if (File.Exists(originalFile))
            {
                return true;
            };
            string filePath = Path.Combine(songsPath, PRACTICE_DIVISIONS_FOLDER, musicuid + ".bin");

            var bytes = Cryptgraphy.ReadAllAesAndGZipBytes(filePath, Cryptgraphy.AesKeyType.Type2);
            string csvString = Encoding.UTF8.GetString(bytes);
            var datas = __instance.loader_.CreateDivisionData(csvString);
            __instance.datas_ = datas;
            __instance.IsLoadFinished = true;
            __instance.musicuId_ = musicuid;
            return false;
        }

#endregion

#region Load Custom Song file

        // TODO

#endregion
    }
}
