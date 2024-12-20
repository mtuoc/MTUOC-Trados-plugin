using Sdl.LanguagePlatform.Core;
using Sdl.LanguagePlatform.TranslationMemory;

namespace SDL.Trados.MTUOC.DTO
{
    public class PreTranslateSegmentDto
    {
        public string PlainTranslation { get; set; }
        public SearchSettings SearchSettings { get; set; }
        public string SourceText { get; set; }
        public Segment TranslationSegment { get; set; }
        public TranslationUnit TranslationUnit { get; set; }
    }
}
