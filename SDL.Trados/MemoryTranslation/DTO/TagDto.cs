using Sdl.LanguagePlatform.Core;

namespace SDL.Trados.MTUOC.DTO
{
    internal class TagDto
    {
        internal TagDto(Tag tag)
        {
            SdlTag = tag;
            PadLeft = string.Empty;
            PadRight = string.Empty;
        }

        internal string PadLeft { get; set; }

        internal string PadRight { get; set; }

        internal Tag SdlTag { get; }
    }
}
