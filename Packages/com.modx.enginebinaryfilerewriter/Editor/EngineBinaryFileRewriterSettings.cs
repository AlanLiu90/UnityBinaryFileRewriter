using System.IO;
using UnityEditorInternal;
using UnityEngine;

namespace EngineBinaryFileRewriter
{
    public sealed class EngineBinaryFileRewriterSettings : ScriptableObject
    {
        public static EngineBinaryFileRewriterSettings Instance
        {
            get
            {
                if (mInstance == null)
                    LoadOrCreate();

                return mInstance;
            }
        }

        public CodeRewriterFeature[] CodeRewriterFeatures;

        private const string FilePath = "ProjectSettings/EngineBinaryFileRewriterSettings.asset";

        private static EngineBinaryFileRewriterSettings mInstance;

        public static EngineBinaryFileRewriterSettings LoadOrCreate()
        {
            var arr = InternalEditorUtility.LoadSerializedFileAndForget(FilePath);
            mInstance = arr.Length > 0 ? arr[0] as EngineBinaryFileRewriterSettings : mInstance ?? CreateInstance<EngineBinaryFileRewriterSettings>();

            return mInstance;
        }

        public static void Save()
        {
            if (!mInstance)
            {
                Debug.LogError("Cannot save EngineBinaryFileRewriterSettings: no instance!");
                return;
            }

            string directoryName = Path.GetDirectoryName(FilePath);
            if (!Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);

            Object[] obj = new EngineBinaryFileRewriterSettings[1] { mInstance };
            InternalEditorUtility.SaveToSerializedFileAndForget(obj, FilePath, true);
        }
    }
}
