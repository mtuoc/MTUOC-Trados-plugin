using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using SDL.Trados.MTUOC.DTO;
using System;
using System.IO;

namespace SDL.Trados.MTUOC.Services.Cache
{
    public class CacheService : ICacheService
    {
        public CacheService()
        { }

        #region Fields/Constants
        
        public const string FILE_KEY = "{project}";
        public readonly string _fileName = $"SDL\\CACHE\\{FILE_KEY}_MTUOC.json";
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private string _memoryJson;

        #endregion

        #region ICacheService

        public string GetValue(string key, string project)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            var cacheItem = GetCacheItem(key, project);
            if (cacheItem == null)
                return string.Empty;

            _logger.Info($"CACHE GET. Source: {key}; Cache: {JsonConvert.SerializeObject(cacheItem)}");
            return cacheItem.Value;
        }

        public void SetValue(string key, string value, string project)
        {
            if (string.IsNullOrEmpty(key))
                return;
            if (string.IsNullOrEmpty(value))
                return;

            SaveCacheItem(key, new CacheItemDto
            {
                CreationDate = DateTime.Now,
                Value = value
            }, project);
        }

        public void ClearCaches(DateTime minValidDate)
        {
            try
            {
                var directory = Path.GetDirectoryName(GetCacheFilePath(string.Empty));
                if (!Directory.Exists(directory))
                    return;

                var files = Directory.EnumerateFiles(directory);
                foreach (var file in files)
                {
                    var date = File.GetLastWriteTime(file);
                    if (date.Date < minValidDate.Date)
                        File.Delete(file);
                }
                _memoryJson = string.Empty;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        #endregion

        #region Methods

        private string GetCacheFilePath(string project)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var relativeFilePath = _fileName.Replace(FILE_KEY, string.IsNullOrEmpty(project) ? "NA" : project);
            return Path.Combine(appData, relativeFilePath);
        }

        private CacheItemDto GetCacheItem(string key, string project)
        {
            string json = string.IsNullOrEmpty(_memoryJson) ? GetJson(project) : _memoryJson;
            if (string.IsNullOrEmpty(json))
                return null;

            var jObject = JObject.Parse(json);
            if (jObject.TryGetValue(key, out JToken value))
                return value.ToObject<CacheItemDto>();

            return null;
        }

        private string GetJson(string project)
        {
            try
            {
                CreateFileIfNotExits(project);
                _memoryJson = File.ReadAllText(GetCacheFilePath(project));
            }
            catch { /* It may fail because it is opened by another process */ }

            return _memoryJson;
        }

        private void SaveCacheItem(string key, CacheItemDto value, string project)
        {
            try
            {
                var json = string.IsNullOrEmpty(_memoryJson) ? GetJson(project) : _memoryJson;
                var cacheObject = string.IsNullOrEmpty(json) ? new JObject() : JObject.Parse(json);

                var newCacheItem = JToken.FromObject(value);

                _logger.Info($"CACHE SAVE. Source: {key}; Cache: {JsonConvert.SerializeObject(value)}");
                if (cacheObject.TryGetValue(key, out JToken oldCacheItem))
                {
                    var oldValue = oldCacheItem.ToObject<CacheItemDto>();
                    cacheObject[key] = newCacheItem;
                }
                else
                {
                    cacheObject.Add(key, newCacheItem);
                }

                _memoryJson = cacheObject.ToString(Formatting.Indented);
                File.WriteAllText(GetCacheFilePath(project), _memoryJson);
            }
            catch { /* It may fail because it is opened by another process */ }
        }

        private void CreateFileIfNotExits(string project)
        {
            var path = GetCacheFilePath(project);
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (!File.Exists(path))
            {
                using (File.Create(path))
                { }
            }
        }

        #endregion
    }
}
