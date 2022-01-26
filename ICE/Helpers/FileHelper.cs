using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Microsoft.Research.ICE.Helpers
{
    public sealed class FileHelper
    {
        Dictionary<string, HashSet<string>> supportedExtensions = new Dictionary<string, HashSet<string>>()
        {
            {"JPEG", new HashSet<string>(){".jpg", ".jpeg"} },
            {"PNG", new HashSet<string>(){".png"} }
        };

        private static FileHelper instance;

        private Regex imageExtensionRegex;

        private Regex videoExtensionRegex;

        public static FileHelper Instance => instance ?? (instance = new FileHelper());

        public string ImageFileFilter { get; private set; }

        public string VideoFileFilter { get; private set; }

        private FileHelper()
        {
            InitializeImageFileFilter();
            InitializeVideoFileFilter();
        }

        public bool IsImageFile(string filename)
        {
            string extension = Path.GetExtension(filename);
            return imageExtensionRegex.IsMatch(extension);
        }

        public bool IsVideoFile(string filename)
        {
            string extension = Path.GetExtension(filename);
            return videoExtensionRegex.IsMatch(extension);
        }

        public bool IsProjectFile(string filename)
        {
            string extension = Path.GetExtension(filename);
            return string.Equals(extension, ".spj", StringComparison.OrdinalIgnoreCase);
        }

        private void InitializeImageFileFilter()
        {
            imageExtensionRegex = null;
            ImageFileFilter = GenerateFileFilter(supportedExtensions, "Image", out imageExtensionRegex);
            
        }

        private void InitializeVideoFileFilter()
        {
            Dictionary<string, HashSet<string>> dictionary = new Dictionary<string, HashSet<string>>();
            dictionary.Add("WMV Files", new HashSet<string> { "*.wmv" });
            dictionary.Add("AVI Files", new HashSet<string> { "*.avi" });
            dictionary.Add("ASF Files", new HashSet<string> { "*.asf" });
            dictionary.Add("MPEG-4 Files", new HashSet<string> { "*.m4v", "*.mov", "*.mp4" });
            dictionary.Add("SAMI Files", new HashSet<string> { "*.sami", "*.smi" });
            dictionary.Add("3GP Files", new HashSet<string> { "*.3gp", "*.3g2", "*.3g2p", "*.3gpp" });
            VideoFileFilter = GenerateFileFilter(dictionary, "Video", out videoExtensionRegex);
        }

        private static string GenerateFileFilter(Dictionary<string, HashSet<string>> fileTypeToExtensionsMap, string titleType, out Regex fileExtensionRegex)
        {
            HashSet<string> hashSet = new HashSet<string>();
            StringBuilder stringBuilder = new StringBuilder();
            foreach (KeyValuePair<string, HashSet<string>> item in fileTypeToExtensionsMap.OrderBy((KeyValuePair<string, HashSet<string>> pair) => pair.Key))
            {
                stringBuilder.Append("|");
                stringBuilder.Append(item.Key);
                stringBuilder.Append(" (");
                string[] array = item.Value.OrderBy((string value) => value).ToArray();
                stringBuilder.Append(string.Join(", ", array));
                stringBuilder.Append(")|");
                stringBuilder.Append(string.Join(";", array));
                hashSet.UnionWith(array);
            }
            stringBuilder.Append("|");
            stringBuilder.Append("All Files (*.*)|*.*");
            StringBuilder stringBuilder2 = new StringBuilder();
            stringBuilder2.Append(titleType + " Files (");
            string[] value2 = hashSet.OrderBy((string value) => value).ToArray();
            stringBuilder2.Append(string.Join(", ", value2));
            stringBuilder2.Append(")|");
            stringBuilder2.Append(string.Join(";", value2));
            string[] value3 = hashSet.Select((string value) => value.TrimStart('*', '.')).ToArray();
            string pattern = "^\\.(" + string.Join("|", value3) + ")$";
            fileExtensionRegex = new Regex(pattern, RegexOptions.IgnoreCase);
            stringBuilder2.Append(stringBuilder);
            return stringBuilder2.ToString();
        }



        private static string GetFileType(string fileExtension)
        {
            string text = null;
            using (RegistryKey registryKey = Registry.ClassesRoot.OpenSubKey(fileExtension))
            {
                if (registryKey != null)
                {
                    text = registryKey.GetValue(null, null) as string;
                }
            }
            if (!string.IsNullOrEmpty(text))
            {
                RegistryKey registryKey2 = Registry.ClassesRoot.OpenSubKey(text);
                if (registryKey2 != null)
                {
                    string text2 = registryKey2.GetValue(null, null) as string;
                    registryKey2.Close();
                    if (!string.IsNullOrEmpty(text2))
                    {
                        return text2;
                    }
                   
                }
              ;
            }
            switch (fileExtension)
            {
                case ".exif":
                    return "JPEG Image";
                case ".icon":
                    return "Icon";
                case ".tif":
                case ".tiff":
                    return "TIFF Image";
                default:
                    return fileExtension.TrimStart('.').ToUpperInvariant() + " File";
            }
        }
    }
}