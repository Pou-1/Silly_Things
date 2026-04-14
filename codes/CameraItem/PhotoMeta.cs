using System;
using System.Collections.Generic;

namespace Silly_Things.codes.CameraItem
{
    [System.Serializable]
    internal class PhotoMeta
    {
        public int id;
        public string date = "";
        public string entities = "";
    }

    [Serializable]
    public class CameraMeta
    {
        public ulong id;
        public int colorVariant;
    }

    [Serializable]
    public class CameraMetaList
    {
        public List<CameraMeta> cameras = new List<CameraMeta>();
    }
}
