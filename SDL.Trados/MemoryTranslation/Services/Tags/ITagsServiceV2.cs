using Sdl.LanguagePlatform.Core;

namespace SDL.Trados.MTUOC.Services.Tags
{
    internal interface ITagsServiceV2
    {
        string PreparedSourceText { get; }
        Segment GetTaggedSegment(string returnedText);
        void SetOriginSegment(Segment source);
}
}
