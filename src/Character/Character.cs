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

    public enum Alignment  // 阵营
    {
        Red = 0,
        Blue = 1,
    }

    /// <summary>
    /// The data in <see cref="NumericStatus"/> is used only in games,
    /// never persisted to the database.
    /// Always compute <see cref="NumericStatus"/> with the data in the database and <see cref="NumericEffect"/>
    /// </summary>
    public class NumericStatus
    {
        public long HitPoint = 0;
        public long Venenom = 0;
        public long ActionPoint = 0;
        public long Speed = 0;
        public long Manuverability = 0;
        public long Mobility = 0;

        public NumericStatus()
        {
        }
        public NumericStatus Clone() => new NumericStatus { HitPoint = this.HitPoint, Venenom = this.Venenom, ActionPoint = this.ActionPoint, Speed = this.Speed, Manuverability = this.Manuverability, Mobility = this.Mobility };

        public static NumericStatus operator +(NumericStatus lhs, NumericStatus rhs) => new NumericStatus { HitPoint = lhs.HitPoint + rhs.HitPoint, Venenom = lhs.Venenom + rhs.Venenom, ActionPoint = lhs.ActionPoint + rhs.ActionPoint, Speed = lhs.Speed + rhs.Speed, Manuverability = lhs.Manuverability + rhs.Manuverability, Mobility = lhs.Mobility + rhs.Mobility };
        public static NumericStatus operator -(NumericStatus lhs, NumericStatus rhs) => new NumericStatus { HitPoint = lhs.HitPoint - rhs.HitPoint, Venenom = lhs.Venenom - rhs.Venenom, ActionPoint = lhs.ActionPoint - rhs.ActionPoint, Speed = lhs.Speed - rhs.Speed, Manuverability = lhs.Manuverability - rhs.Manuverability, Mobility = lhs.Mobility - rhs.Mobility };
        public static NumericStatus operator *(NumericStatus lhs, decimal rhs) => new NumericStatus { HitPoint = (long)(lhs.HitPoint * rhs), Venenom = (long)(lhs.Venenom * rhs), ActionPoint = (long)(lhs.ActionPoint * rhs), Speed = (long)(lhs.Speed * rhs), Manuverability = (long)(lhs.Manuverability * rhs), Mobility = (long)(lhs.Mobility * rhs) };
        public static NumericStatus operator *(decimal lhs, NumericStatus rhs) => rhs * lhs;
        public static NumericStatus operator /(NumericStatus lhs, long rhs) => new NumericStatus { HitPoint = lhs.HitPoint / rhs, Venenom = lhs.Venenom / rhs, ActionPoint = lhs.ActionPoint / rhs, Speed = lhs.Speed / rhs, Manuverability = lhs.Manuverability / rhs };
    }

    public abstract class Character : IRenderText
    {
        [Table("character")]
        public class CharacterDataPersisted
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

            [Column("experience")]
            [Indexed]
            public uint experience { get; init; }
        }

        public List<Skill> skills { get; set; } = new();
        /// <summary>
        /// Call <see cref="SukaLambdaEngine.RemoveCharacter"/>!
        /// Or <see cref="Map.RemoveCharacter"/> if there is no <see cref="SukaLambdaEngine"/>!
        /// Do not remove characters just by setting this field, if there is a vm and a map.
        /// </summary>
        public bool removedFromMap { get; set; } = false;

        public CharacterDataPersisted persistedStatus { get; set; }
        public NumericStatus statusCommitted { get; private set; }
        public NumericStatus statusTemporary { get; set; }
        public Alignment? defaultAlignment = null;

        public void LoadFromDatabase(SQLiteConnection conn, string accountId, string characterName) { throw new NotImplementedException(); }
        public void PersistEarnings(SQLiteConnection conn) { throw new NotImplementedException(); }  // Write persistedStatus
        public void GetSkills(SukaLambdaEngine vm) { }
        public void ComputeStatus(SukaLambdaEngine vm) { }
        public void OnAddToMap(SukaLambdaEngine vm) { }
        public void OnMoveInMap(SukaLambdaEngine vm, Tuple<ushort, ushort> src, Heading srcHeading, Tuple<ushort, ushort> dst, Heading dstHeading) { }
        public void OnRemoveFromMap(SukaLambdaEngine vm) { }
        public abstract string RenderAsText(Language lang);

        public Character(string accountId)
        {
        }
        public Character(string accountId, string characterName)
        {
        }
        public void CommitNumericEffect(NumericEffect numericEffect) { throw new NotImplementedException(); }
    }

    public class DummyVMCharacter : Character
    {
        public DummyVMCharacter() : base("#SukaLambdaEngine") { }
        public override string RenderAsText(Language lang) => "#SukaLambdaEngine";
    }

    public class DummyMapCharacter : Character
    {
        public DummyMapCharacter() : base("#Map") { }
        public override string RenderAsText(Language lang) => "#Map";
    }

}
