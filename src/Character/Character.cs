using SQLite;

namespace sukalambda
{
    public enum CharacterName
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
        Lillia,
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

    [Table("character")]
    public abstract class Character
    {
        [PrimaryKey]
        [Column("id")]
        public Guid id { get; set; }
        
        [Column("accountId")]
        [Indexed]
        public string accountId { get; init; }

        [Column("characterName")]
        public string characterName { get; init; }

        [Column("level")]
        public int level { get; init; }

        public Character(string accountId)
        {
            this.accountId = accountId;
            characterName = GetType().Name;
            level = 1;
        }
        public Character(string accountId, string characterName)
        {
            this.accountId = accountId;
            this.characterName = characterName;
            level = 1;
        }
    }
}
