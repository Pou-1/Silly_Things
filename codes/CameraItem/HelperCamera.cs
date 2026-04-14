using BepInEx;
using Newtonsoft.Json;
using Silly_Things.Codes.CameraItem;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Silly_Things.codes.CameraItem
{
    internal class HelperCamera
    {
        public static HashSet<ulong> clientsLoadedPhotos = new HashSet<ulong>();
        public static bool canLoadPictures = true;

        // _____________PICTURE_____________ \\
        public static void DeletePictures()
        {
            try
            {
                string folder = Path.Combine(Paths.GameRootPath, "CameraPictures");

                if (!Directory.Exists(folder))
                    return;

                Directory.GetFiles(folder).ToList().ForEach(File.Delete);
            }
            catch (System.Exception e)
            {
                Plugin.Logger.LogError("Failed to delete pictures" + e);
            }
        }

        public static Texture2D DownscaleTexture(Texture2D source, int width, int height, Material photoMat)
        {
            RenderTexture rt = RenderTexture.GetTemporary(width, height);

            if (photoMat != null)
                Graphics.Blit(source, rt, photoMat);
            else
                Graphics.Blit(source, rt);

            RenderTexture current = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            RenderTexture.active = current;

            RenderTexture.ReleaseTemporary(rt);

            return tex;
        }

        public static void SavePhotoToDisk(Texture2D tex, string username)
        {
            try
            {
                string folder = Path.Combine(Paths.GameRootPath, "CameraPictures");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string date = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string fileName = date + "_" + username + ".png";
                string path = Path.Combine(folder, fileName);

                byte[] png = tex.EncodeToPNG();

                File.WriteAllBytes(path, png);

                Plugin.Logger.LogInfo("Picture saved: " + path);
            }
            catch (System.Exception e)
            {
                Plugin.Logger.LogError("Failed to save photo: " + e);
            }
        }

        // _____________PHOTO SAVE AND LOAD_____________ \\
        public static void LoadAllPhotosFromDisk()
        {
            if (!NetworkManager.Singleton.IsHost)
                return;

            string saveName = GameNetworkManager.Instance.currentSaveFileName;
            string folder = Path.Combine(Paths.GameRootPath, "TempSillyThings", saveName);

            if (!Directory.Exists(folder))
                return;

            foreach (PhotoItem photo in PhotoItem.Instances)
            {
                if (photo == null)
                    continue;

                if (photo.entityNamesText?.text != "Trans puppy girl")
                    continue;

                int uniqId = photo.UniqueIdNet.Value;
                string filePath = Path.Combine(folder, uniqId + ".jpg");

                if (!File.Exists(filePath))
                    continue;

                byte[] data = File.ReadAllBytes(filePath);

                string basePath = Path.Combine(folder, uniqId.ToString());
                string metaPath = basePath + ".json";
                string date = "";
                string entityNamesText = "";

                if (File.Exists(metaPath))
                {
                    string json = File.ReadAllText(metaPath);
                    PhotoMeta? meta = JsonConvert.DeserializeObject<PhotoMeta>(json);

                    if (meta != null)
                    {
                        date = meta.date;
                        entityNamesText = meta.entities;
                    }
                }

                photo.ApplyPhotoToExistingItem(uniqId, data, date, entityNamesText);
            }
            Plugin.Logger.LogInfo("Loaded all photos from disk (HOST)");
        }

        public static void SaveTemp(byte[] jpg, int uniqId)
        {
            try
            {
                string folder = Path.Combine(Paths.GameRootPath, "TempSillyThings");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string saveName = GameNetworkManager.Instance.currentSaveFileName;
                string folderSave = Path.Combine(Paths.GameRootPath, "TempSillyThings", saveName);

                if (!Directory.Exists(folderSave))
                    Directory.CreateDirectory(folderSave);

                string path = Path.Combine(folderSave, uniqId + ".jpg");

                File.WriteAllBytes(path, jpg);
            }
            catch (System.Exception e)
            {
                Plugin.Logger.LogError("Failed to save FULL photo: " + e);
            }
        }
    }
}
