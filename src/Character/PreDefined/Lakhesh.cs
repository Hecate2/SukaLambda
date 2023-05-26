namespace sukalambda
{
    public class Lakhesh : Character
    {
        public Lakhesh(string accountId) : base(accountId)
        {
            this.persistedStatus = new CharacterDataPersisted { id = Guid.NewGuid(), accountId = accountId, characterName = nameof(Lakhesh), experience=0 };
        }

        public override string RenderAsText(Language lang)
        {
            if (removedFromMap) return "";
            switch (lang)
            {
                case Language.en: return "La";
                case Language.jp: return "ラ";
                case Language.cn: default: return "菈";
            }
        }
    }
}
