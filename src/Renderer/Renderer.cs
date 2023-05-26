namespace sukalambda
{
    public enum Language
    {
        cn, en, jp
    }
    public interface IRenderText
    {
        public string RenderAsText(Language lang);
    }
}
