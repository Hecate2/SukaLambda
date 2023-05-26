namespace sukalambda
{
    public abstract class CustomCharacter : Character
    {
        public CustomCharacter(string accountId, string characterName): base(accountId, characterName) { }

        public abstract override string RenderAsText(Language lang);
    }
}
