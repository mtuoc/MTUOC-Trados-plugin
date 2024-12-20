using Sdl.LanguagePlatform.Core;
using System;

namespace SDL.Trados.MTUOC.Services.Tags
{
    [Obsolete("service used by old websocket protocol")]
    internal interface ITagsService
    {
        Segment GetTaggedSegment(string text);
        string GetSourceText(Segment sourceSegment);
    }
}
