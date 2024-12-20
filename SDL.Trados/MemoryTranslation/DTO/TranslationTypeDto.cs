using Sdl.LanguagePlatform.TranslationMemory;

namespace SDL.Trados.MTUOC.DTO
{
    public class TranslationTypeDto
    {
        public string Name
        {
            get
            {
                return Value.ToString();
            }
        }

        public TranslationUnitOrigin Value { get; set; }
    }
}
