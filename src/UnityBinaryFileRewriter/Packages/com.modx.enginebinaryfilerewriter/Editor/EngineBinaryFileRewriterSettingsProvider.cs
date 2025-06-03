using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace EngineBinaryFileRewriter
{
    public sealed class EngineBinaryFileRewriterSettingsProvider : SettingsProvider
    {
        private static EngineBinaryFileRewriterSettingsProvider mInstance;

        private SerializedObject mSettings;
        private SerializedProperty mCodeRewriterFeaturesProp;
        private readonly StringBuilder mStringBuilder = new StringBuilder();
        private string mErrorMessage;

        public EngineBinaryFileRewriterSettingsProvider()
            : base("Project/Engine Binary File Rewriter", SettingsScope.Project) 
        {
        }

        public override void OnGUI(string searchContext)
        {
            if (mSettings == null || mSettings.targetObject == null)
                CreateSettings();

            mSettings.Update();

            if (!string.IsNullOrEmpty(mErrorMessage))
                EditorGUILayout.HelpBox(mErrorMessage, MessageType.Error);
            else
                EditorGUILayout.HelpBox(GetInformation(), MessageType.Info);

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(mCodeRewriterFeaturesProp);

            if (EditorGUI.EndChangeCheck())
            {
                mSettings.ApplyModifiedPropertiesWithoutUndo();

                if (Utility.ValidateEngineBinaryFileRewriterSettings(out mErrorMessage))
                    EngineBinaryFileRewriterSettings.Save();
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            if (EngineBinaryFileRewriterSettings.Instance != null && mInstance == null)
            {
                mInstance = new EngineBinaryFileRewriterSettingsProvider();
                using (var so = new SerializedObject(EngineBinaryFileRewriterSettings.Instance))
                {
                    mInstance.keywords = GetSearchKeywordsFromSerializedObject(so);
                }
            }

            return mInstance;
        }

        private void CreateSettings()
        {
            mSettings = new SerializedObject(EngineBinaryFileRewriterSettings.Instance);

            mCodeRewriterFeaturesProp = mSettings.FindProperty("CodeRewriterFeatures");
        }

        private string GetInformation()
        {
            var features = GetEnabledFeatures();

            if (!features.Any())
                return $"No feature is enabled for {Utility.GetUnityVersion()}";

            var sb = mStringBuilder;
            sb.Clear();

            sb.AppendFormat("The following feature(s) are enabled for {0}:", Utility.GetUnityVersion());
            sb.AppendLine();

            int index = 1;

            foreach (var feature in features)
            {
                sb.AppendFormat("{0}. {1}:", index++, feature.Item1);
                sb.AppendLine();

                foreach (var target in feature.Item2)
                {
                    sb.AppendFormat("    {0}", target);
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private IEnumerable<(string, string[])> GetEnabledFeatures()
        {
            var unityVersion = Utility.GetUnityVersion();
            var features = EngineBinaryFileRewriterSettings.Instance.CodeRewriterFeatures;

            if (features == null)
                yield break;

            foreach (var feature in features)
            {
                string[] targets;

                try
                {
                    if (!feature.Enable)
                        continue;

                    if (feature.RuleSets == null)
                        continue;

                    var ruleSet = feature.RuleSets.Where(x => Regex.IsMatch(unityVersion, x.UnityVersion)).FirstOrDefault();
                    if (ruleSet == null)
                        continue;

                    if (ruleSet.Rules == null)
                        continue;

                    targets = ruleSet.Rules
                        .Select(x => $"{x.BuildTarget}+{x.Architecture}+{GetText(x.Development)}")
                        .ToArray();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    continue;
                }

                if (targets.Length > 0)
                    yield return (feature.Name, targets);
            }
        }

        private static string GetText(bool development)
        {
            return development ? "Development" : "Release";
        }
    }
}
