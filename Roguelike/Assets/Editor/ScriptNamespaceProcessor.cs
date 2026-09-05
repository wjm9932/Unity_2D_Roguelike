using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Editor
{
    /// <summary>Replaces the namespace marker in this project's script templates.</summary>
    public sealed class ScriptNamespaceProcessor : AssetModificationProcessor
    {
        private const string namespaceMarker = "ProjectFolderNamespacePlaceholder";
        private const string keywords = " abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null object operator out override params private protected public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while ";

        private static void OnWillCreateAsset(string assetPath)
        {
            assetPath = assetPath.Replace('\\', '/');
            if (assetPath.EndsWith(".meta", StringComparison.Ordinal))
            {
                assetPath = assetPath.Substring(0, assetPath.Length - 5);
            }

            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal) ||
                !assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Unity can notify before writing the script, or when creating its meta file.
            if (File.Exists(assetPath))
            {
                ReplaceNamespace(assetPath);
            }
            else
            {
                EditorApplication.delayCall += () =>
                {
                    if (File.Exists(assetPath) && ReplaceNamespace(assetPath))
                    {
                        AssetDatabase.ImportAsset(assetPath);
                    }
                };
            }
        }

        private static bool ReplaceNamespace(string assetPath)
        {
            string source = File.ReadAllText(assetPath);
            if (!source.Contains(namespaceMarker))
            {
                return false;
            }

            string folder = assetPath.Substring("Assets/".Length);
            folder = folder.Substring(0, Math.Max(0, folder.LastIndexOf('/')));
            if (folder.StartsWith("Scripts/", StringComparison.Ordinal))
            {
                folder = folder.Substring("Scripts/".Length);
            }

            string[] segments = folder.Length == 0 ? new[] { "Assets" } : folder.Split('/');
            for (int index = 0; index < segments.Length; index++)
            {
                // Keep Korean and other Unicode letters; replace spaces and punctuation.
                string segment = Regex.Replace(segments[index], @"[^\p{L}\p{Nl}\p{Nd}\p{Pc}\p{Mn}\p{Mc}\p{Cf}]", "_");
                if (!Regex.IsMatch(segment, @"^[\p{L}\p{Nl}_]"))
                {
                    segment = "_" + segment;
                }

                segments[index] = keywords.Contains(" " + segment + " ") ? "@" + segment : segment;
            }

            File.WriteAllText(assetPath, source.Replace(namespaceMarker, string.Join(".", segments)), new UTF8Encoding(false));
            return true;
        }
    }
}
