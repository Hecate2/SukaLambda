using System.ComponentModel.DataAnnotations;

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

    public class CharacterData
    {
        [Key]
        public Guid id { get; set; }
        public string accountId { get; init; }
        public string characterName { get; init; }
        public uint experience { get; set; }
    }

    public abstract class Character : IRenderText
    {
        public string accountId { get; init; }
        public List<Skill> skills { get; set; } = new();
        /// <summary>
        /// Call <see cref="SukaLambdaEngine.RemoveCharacter"/>!
        /// Or <see cref="Map.RemoveCharacter"/> if there is no <see cref="SukaLambdaEngine"/>!
        /// Do not remove characters just by setting this field, if there is a vm and a map.
        /// </summary>
        public bool removedFromMap { get; set; } = false;

        public CharacterData persistedStatus { get; set; }  // state in database
        public NumericStatus statusCommitted { get; internal set; }  // state when a round is finished
        public NumericStatus statusTemporary { get; set; }  // state in a round
        public Alignment? alignment = null;

        // Typically for flying units
        public Altitude altitude = Altitude.Surface;  // on the ground by default
        public Dictionary<Altitude, ulong> minSpeedAbs = new()
        {
            { Altitude.Surface, 0 },  // Other altitudes are not supported by default!
        };

        private void LoadFromDatabase(string accountId, string characterName)
        {
            using var tx = PRODUCTION_CONFIG.conn.Database.BeginTransaction();
            {
                this.persistedStatus = PRODUCTION_CONFIG.conn.Characters.Where(c => c.id == this.persistedStatus.id).First();
                GetSkillsFromDb();
            }
        }
        public void PersistEarnings()
        {
            CharacterData? originalCharacter = PRODUCTION_CONFIG.conn.Characters.Where(c => c.id == this.persistedStatus.id).FirstOrDefault();
            if (originalCharacter == null)
                PRODUCTION_CONFIG.conn.Characters.Add(this.persistedStatus);
            else
                if (originalCharacter.experience < this.persistedStatus.experience)
                {
                    originalCharacter.experience = this.persistedStatus.experience;
                    PRODUCTION_CONFIG.conn.Characters.Update(originalCharacter);
                }
            PRODUCTION_CONFIG.conn.SaveChanges();
        }
        private void GetSkillsFromDb()
        {
            List<SkillData> skills = PRODUCTION_CONFIG.conn.Skills.Where(sk => sk.characterId == this.persistedStatus.id).ToList();
            foreach (SkillData skillData in skills)
            {
                Type? t = Type.GetType(skillData.skillClassName);
                if (t == null) continue;
                Skill? skill = (Skill?)Activator.CreateInstance(t, this);
                if (skill != null)
                    this.skills.Add(skill);
            }
        }
        public void ComputeNumericStatus(SukaLambdaEngine vm) { }
        public void OnAddToMap(SukaLambdaEngine vm) { }
        public void OnMoveInMap(SukaLambdaEngine vm, Tuple<ushort, ushort> src, Heading srcHeading, Tuple<ushort, ushort> dst, Heading dstHeading) { }
        public void OnRemoveFromMap(SukaLambdaEngine vm) { }
        public abstract string RenderAsText(Language lang);

        public Character(string accountId, string? characterName = null)
        {
            this.accountId = accountId;
            if (characterName == null)  characterName = this.GetType().Name;
            //LoadFromDatabase(accountId, characterName);
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
