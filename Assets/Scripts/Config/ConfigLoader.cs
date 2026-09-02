using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GameDevStudio.Config
{
    public static class ConfigLoader
    {
        public const string FileName = "GameData.json";

        public static IEnumerator Load(Action<GameDataDto> onLoaded, Action<string> onError)
        {
            string path = Path.Combine(Application.streamingAssetsPath, FileName);
            string json = null;

#if (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
            string url = path;
            if (url.IndexOf("://", StringComparison.Ordinal) < 0)
            {
                url = "file://" + url;
            }

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke("Не удалось загрузить " + FileName + ": " + request.error);
                    yield break;
                }

                json = request.downloadHandler.text;
            }
#else
            if (!File.Exists(path))
            {
                onError?.Invoke("Нет файла настроек: " + path);
                yield break;
            }

            json = File.ReadAllText(path, Encoding.UTF8);
            yield return null;
#endif

            json = StripComments(json);
            if (!string.IsNullOrEmpty(json) && json[0] == '\uFEFF')
            {
                json = json.Substring(1);
            }
            GameDataDto data = JsonUtility.FromJson<GameDataDto>(json);
            if (data == null || data.genres == null || data.roles == null)
            {
                onError?.Invoke("GameData.json разобран, но данные пустые или повреждены.");
                yield break;
            }

            onLoaded?.Invoke(data);
        }

        public static string StripComments(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return json;
            }

            var builder = new StringBuilder(json.Length);
            bool inString = false;
            bool escape = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (inString)
                {
                    builder.Append(c);
                    if (escape)
                    {
                        escape = false;
                    }
                    else if (c == '\\')
                    {
                        escape = true;
                    }
                    else if (c == '"')
                    {
                        inString = false;
                    }

                    continue;
                }

                if (c == '"')
                {
                    inString = true;
                    builder.Append(c);
                    continue;
                }

                if (c == '/' && i + 1 < json.Length && json[i + 1] == '/')
                {
                    i += 2;
                    while (i < json.Length && json[i] != '\n')
                    {
                        i++;
                    }

                    if (i < json.Length)
                    {
                        builder.Append('\n');
                    }

                    continue;
                }

                if (c == '/' && i + 1 < json.Length && json[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < json.Length && !(json[i] == '*' && json[i + 1] == '/'))
                    {
                        i++;
                    }

                    i++;
                    continue;
                }

                builder.Append(c);
            }

            return builder.ToString();
        }
    }
}
