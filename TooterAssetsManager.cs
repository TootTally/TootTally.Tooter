using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TootTally.Utils;
using UnityEngine;

namespace TootTally.Tooter
{
    public static class TooterAssetsManager
    {
        public static int coroutineCount;

        //Add your asset names here:
        //Assets are attempted to be loaded locally. If the assets aren't found, it downloads them from the server's assets API link into the Bepinex/Assets folder and tries to reload them.
        public static readonly List<string> requiredAssetNames = new List<string>
        {
            "TooterButton.png",
            "TooterText.png",

            "SodaShadow.png",
            "SodaNeutral.png",
            "SodaDeepSmug.png",
            "SodaEh.png",
            "SodaEmbarrassedLight.png",
            "SodaNeutralTalk.png",
            "SodaShock.png",
            "SodaStressLight.png",
            "SodaWheezeRW.png",
            "SodaAgree.png",
            "SodaThinking.png",
            "SodaPlead.png",
            "SodaBeg.png",
            "SodaBleh.png",
            "SodaEat.png",
            "SodaHoldCookie.png",
            "SodaMunch.png",
            "SodaWow.png",
            "SodaInLove.png",
            "SodaCall.png",
            "SodaHype.png",
            "SodaFightMe.png",

            "TrixieBag.png",
            "TrixieShadow.png",
            "TrixieNeutral.png",
            "TrixieNeutralTalk.png",
            "TrixieAnxious.png",
            "TrixieCompliment1.png",
            "TrixieCompliment2.png",
            "TrixieCompliment3.png",
            "TrixiePanic.png",
            "TrixiePleased.png",
            "TrixieSadge.png",
            "TrixieAgree.png",
            "TrixieAmaze.png",
            "TrixieEat.png",
            "TrixieHoldCookie.png",
            "TrixieSurprise.png",
            "TrixieInLove.png",

            "AppaloosaShadow.png",
            "AppaloosaNeutral.png",
            "AppaloosaNeutralTalk.png",
            "AppaloosaCall.png",
            "AppaloosaLeanAway.png",
            "AppaloosaLOL.png",

            "KaizyleShadow.png",
            "KaizyleNeutral.png",
            "KaizyleNeutralTalk.png",
            "KaizyleDispleased.png",
            "KaizyleWTF.png",
            "KaizyleConcern.png",
            "KaizyleBrag.png",
            "KaizylePissed.png",
            "KaizyleUm.png",
            "KaizyleFightMe.png",
            "KaizyleCat.png",

            "BeezerlyShadow.png",
            "BeezerlyNeutral.png",
            "BeezerlyNeutralTalk.png",
            "BeezerlyAggro.png",
            "BeezerlyMock.png",
            "BeezerlyBump.png",
            "BeezerlyImpressed.png",
            "BeezerlyThinking.png",
            "BeezerlyUh.png",
            "BeezerlyChallenge.png",
            "BeezerlyInLove.png",
            "BeezerlyHype.png",
            "BeezerlyPassion.png",
            "BeezerlyWhat.png",
            "BeezerlyPoint.png",
            "BeezJam1.png",
            "BeezJam2.png",
            "BeezJam3.png",

            "HornLordNeutral.png",
            "HornLordTalk.png",
            "HornLordYeah.png",

            "AppaloosaRoom.png",
            "Backstage.png",
            "BeezerlyRoomLightsOff.png",
            "BeezerlyRoomLightsOn.png",
            "ClassroomEvening.png",
            "CompetitionStage.png",
            "Downtown.png",
            "HardRockCafe.png",
            "HardRockCafeOutside.png",
            "HardRockCafeTableAndStage.png",
            "IceCreamCafe.png",
            "JazzClub.png",
            "KaizyleRoomEvening.png",
            "KaizyleRoomNight.png",
            "MusicRoom.png",
            "OutsideCompetitionVenue.png",
            "PenguinCafe.png",
            "TrixiebellRoom.png",
            "StreetNight.png",
            "TrixieHouseNight.png",
            "SpecialMomentTrixie.png",
            "SpecialMomentBeezerly.png",
            "SpecialMomentAppoloosa.png",
            "SpecialMomentKaizyle.png",
            "ParkBench.png",

        };

        public static Dictionary<string, Texture2D> textureDictionary;

        public static void LoadAssets()
        {
            coroutineCount = 0;
            string assetDir = Path.Combine(Path.GetDirectoryName(Plugin.Instance.Info.Location), "Assets");
            if (!Directory.Exists(assetDir)) Directory.CreateDirectory(assetDir);

            textureDictionary = new Dictionary<string, Texture2D>();

            foreach (string assetName in requiredAssetNames)
            {
                string assetPath = Path.Combine(assetDir, assetName);
                Plugin.Instance.StartCoroutine(TootTallyAPIService.TryLoadingTextureLocal(assetPath, (texture) =>
                {
                    if (texture != null)
                    {
                        textureDictionary.Add(assetName, texture);
                    }
                    else
                        DownloadAssetFromServer("http://cdn.toottally.com/assets/" + assetName, assetDir, assetName);
                }));
            }
        }

        public static void DownloadAssetFromServer(string apiLink, string assetDir, string assetName)
        {
            coroutineCount++;
            Plugin.Instance.LogInfo("Downloading asset " + assetName);
            string assetPath = Path.Combine(assetDir, assetName);
            Plugin.Instance.StartCoroutine(TootTallyAPIService.DownloadTextureFromServer(apiLink, assetPath, (success) =>
            {
                ReloadTextureLocal(assetDir, assetName);
            }));
        }

        public static void ReloadTextureLocal(string assetDir, string assetName)
        {
            string assetPath = Path.Combine(assetDir, assetName);
            Plugin.Instance.StartCoroutine(TootTallyAPIService.TryLoadingTextureLocal(assetPath, (texture) =>
            {
                coroutineCount--;
                if (texture != null)
                {
                    Plugin.Instance.LogInfo("Asset " + assetName + " Reloaded");
                    textureDictionary.Add(assetName, texture);
                }
                if (coroutineCount == 0)
                {
                    List<string> missingAssetList = GetMissingAssetsName();
                    if (missingAssetList.Count > 0)
                    {
                        Plugin.Instance.LogError("Missing Asset(s):");
                        foreach (string missingAsset in missingAssetList)
                            Plugin.Instance.LogError("    " + missingAsset);
                    }
                    else
                        Plugin.Instance.LogInfo("All Assets Loaded Correctly");
                }
            }));
        }

        public static List<string> GetMissingAssetsName()
        {
            List<string> missingAssetsNames = new List<string>();
            foreach (string assetName in requiredAssetNames)
                if (!textureDictionary.ContainsKey(assetName)) missingAssetsNames.Add(assetName);
            return missingAssetsNames;
        }

        public static Texture2D GetTexture(string assetKey) => textureDictionary[assetKey];
        public static Sprite GetSprite(string assetKey)
        {
            try
            {
                Texture2D texture = textureDictionary[assetKey];
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one / 2f, 300f);
            }
            catch (Exception ex)
            {
                Plugin.Instance.LogError("TextureName: " + assetKey);
                Plugin.Instance.LogInfo("Error: " + ex.Message);
            }
            return null;
        }
    }
}
