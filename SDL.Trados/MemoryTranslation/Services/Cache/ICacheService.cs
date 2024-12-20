using System;

namespace SDL.Trados.MTUOC.Services.Cache
{
    public interface ICacheService
    {
        void ClearCaches(DateTime minValidDate);
        string GetValue(string key, string project);
        void SetValue(string key, string value, string project);
    }
}
