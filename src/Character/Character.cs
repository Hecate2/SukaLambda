using SQLite;

namespace sukalambda
{
    public enum PreDefinedCharacterName
    {
        // ALWAYS USE STRING OF CHARACTER NAMES IN DATABASE!
        // NOT NUMERIC VALUES!
        Almaria,
        Almita,
        Buronny,
        Chtholly,
        Collon,
        EbonCandle,
        Elba,  // non-official?
        Elq,
        Eudea,
        Godley,
        Grick,
        Ithea,
        Lakhesh,
        Lilya,
        Limeskin,
        Margomedari,
        Nasania,  // non-official?
        Nephren,
        Nopht,
        Nygglatho,
        Pannibal,
        Phyracorlybia,
        Rhantolk,
        SilverClover,
        Suowong,
        Tiat,
        Willem,
    }

    public abstract class Character
    {
        [Table("character")]
        public class CharacterData
        {
            [PrimaryKey]
            [Column("id")]
            public Guid id { get; set; }

            [Column("accountId")]
            [Indexed]
            public string accountId { get; init; }

            [Column("characterName")]
            [Indexed]
            public string characterName { get; init; }

            [Column("level")]
            public int level { get; init; }
        }

        public Guid id { get; set; }
        public string accountId { get; init; }
        public string characterName { get; init; }
        public int level { get; init; }
        public List<Skill> skills { get; set; } = new();
        public bool removedFromMap { get; private set; } = false;

        public int speed { get; init; }

        public void GetSkills(SukaLambdaEngine vm) { }
        public void ComputeStatus(SukaLambdaEngine vm) { }
        public void OnAddToMap(SukaLambdaEngine vm) { }
        public void OnRemoveFromMap(SukaLambdaEngine vm) { removedFromMap = true; }

        public Character(string accountId)
        {
            this.accountId = accountId;
            characterName = GetType().Name;
            level = 1;
            speed = 1;  // TODO: compute speed
        }
        public Character(string accountId, string characterName)
        {
            this.accountId = accountId;
            this.characterName = characterName;
            level = 1;
        }
    }
}
