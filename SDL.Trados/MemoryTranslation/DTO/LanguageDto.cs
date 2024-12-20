namespace SDL.Trados.MTUOC.DTO
{
    public class LanguageDto
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public string FullName
        {
            get { return string.Format("{0} ({1})", Name, Code); }
        }
    }
}
