using SQLite;
using static sukalambda.Utils;
using static sukalambda.Map.CharacterMap;
using System.Security.Cryptography.X509Certificates;

namespace sukalambda
{
    public enum Alignment  // 阵营
    {
        Red = 0,
        Blue = 1,
    }

    public enum Heading
    {
        Up = 0,      North = 0,    N = 0,
        Right = 90,  East = 90,    E = 90,
        Down = 180,  South = 180,  S = 180,
        Left = 270,  West = 270,   W = 270,
        NE = 45,
        SE = 135,
        SW = 225,
        NW = 315,
    }

    public class MapBlock
    {
        public ushort x { get; init; }
        public ushort y { get; init; }
        public SukaLambdaEngine? vm { get; set; }  // You can do something when the vm is set
        public MapBlock(ushort x, ushort y, SukaLambdaEngine? vm)
        {
            this.x = x;  this.y = y;
            this.vm = vm;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="movements"></param>
        /// <param name="movementIndexEnteringThisBlock">0 if the movement starts from this block</param>
        /// <param name="vm"></param>
        public void OnCharacterMovingIn(Character character,
            Heading[] movements, ushort movementIndexEnteringThisBlock)
        { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="movements"></param>
        /// <param name="movementIndexLeavingThisBlock">0 if the movement starts from this block</param>
        /// <param name="vm"></param>
        public void OnCharacterMovingOut(Character character,
            Heading[] movements, ushort movementIndexLeavingThisBlock)
        { }
        public void OnCharacterMovingOutOfMapFromThisBlock(Character character,
            Heading[] movements, ushort movementIndexLeavingThisBlock)
        { }
    }

    public class Map
    {
        public string databasePath { get; init; }
        internal SQLiteConnection conn;
        public SukaLambdaEngine? vm { get; set; }  // You can do something when the vm is set
        internal Dictionary<Tuple<ushort, ushort>, MapBlock> mapBlocks = new();
        public ushort width, height;

        [Table("characterMap")]
        public class CharacterMap
        {
            [PrimaryKey]
            [Column("characterId")]
            public Guid characterID { get; init; }
            [Column("positionX")]
            [Indexed]
            public ushort positionX { get; init; }
            [Column("positionY")]
            [Indexed]
            public ushort positionY { get; init; }
            [Column("heading")]
            [Indexed]
            public ushort heading { get; init; }

            [Column("alignment")]  // 阵营
            public ushort alignment { get; init; }
            [Column("removed")]    // wheter the character had been removed from the map
            public bool removed { get; init; }

            public static int CountCharacterIncludingRemoved(SQLiteConnection conn) => conn.Table<CharacterMap>().Count();
            public static int CountCharacter(SQLiteConnection conn) => conn.Table<CharacterMap>().Where(c => c.removed == false).Count();
            public static Tuple<ushort, ushort>? CharacterPosition(SQLiteConnection conn, Character character)
            {
                CharacterMap? characterInDatabase = conn.Table<CharacterMap>().Where(c => c.characterID == character.id).FirstOrDefault();
                return (characterInDatabase == null) ? null :
                    new Tuple<ushort, ushort>(characterInDatabase.positionX, characterInDatabase.positionY);
            } 
            public static Guid? HasCharacterAt(SQLiteConnection conn, ushort x, ushort y) => conn.Table<CharacterMap>().Where(c => c.removed == false && c.positionX == x && c.positionY == y).FirstOrDefault()?.characterID;
            public static void AddCharacter(SQLiteConnection conn, Character character, ushort x, ushort y, Heading heading, Alignment alignment, SukaLambdaEngine? vm)
            {
                conn.RunInTransaction(() =>
                {
                    int count = CountCharacterIncludingRemoved(conn);
                    if (count >= int.MaxValue) throw new ArgumentException($"Too many characters!");
                    if (CharacterPosition(conn, character) != null) throw new ArgumentException($"This character {character.accountId} had been added before");
                    Guid? anotherCharacterId = HasCharacterAt(conn, x, y);
                    if (anotherCharacterId != null)
                        throw new ArgumentException( (vm != null) ?
                            $"Another character [{vm.characters[(Guid)anotherCharacterId].characterName}]({anotherCharacterId}) of {vm.characters[(Guid)anotherCharacterId].accountId} at ({x}, {y}) had been added before"
                        :
                            $"Another character ({anotherCharacterId}) at ({x}, {y}) had been added before; no {nameof(SukaLambdaEngine)} had been specified for this map"
                        );
                    conn.Insert(new CharacterMap { characterID=character.id, positionX=x, positionY=y, heading=(ushort)heading, alignment=(ushort)alignment, removed=false });
                });
            }

            public static void RemoveCharacter(SQLiteConnection conn, Character character)
            {
                conn.Update(new CharacterMap { characterID=character.id, removed=true });
            }
        }
        // Define the special effects for blocks if needed
        public Dictionary<Tuple<ushort, ushort>, MapBlock> blocks = new();

        public Map(string databasePath, ushort width, ushort height, SukaLambdaEngine? vm=null)
        {
            // TODO: Check if the database exists?
            this.databasePath = databasePath;
            conn = new(new SQLiteConnectionString(databasePath));
            this.vm = vm;
            this.width = width;
            this.height = height;
        }

        public void InsertMapBlock(ushort x, ushort y, MapBlock mapBlock)
        {
            mapBlocks[new Tuple<ushort, ushort>(x, y)] = mapBlock;
            mapBlock.vm = vm;
        }
        public void RemoveMapBlock(ushort x, ushort y)
        {
            if (mapBlocks.Remove(new Tuple<ushort, ushort>(x, y), out MapBlock? block))
                block.vm = null;
        }

        public void AddCharacter(Character character, ushort x, ushort y, Heading heading, Alignment alignment)
        {
            if (x >= width)   throw new ArgumentException($"({x}, {y}); x={x} larger than the width of map {width}");
            if (y >= height)  throw new ArgumentException($"({x}, {y}); y={y} larger than the height of map {height}");
            CharacterMap.AddCharacter(conn, character, x, y, heading, alignment, vm);
        }
        public void RemoveCharacter(Character character) => CharacterMap.RemoveCharacter(conn, character);
        /// <returns>true if in map but removed; null if not in map; false if in map and not removed</returns>
        public bool? CharacterRemoved(Character character) => conn.Table<CharacterMap>().Where(c => c.characterID == character.id).FirstOrDefault()?.removed;
        public Tuple<ushort, ushort>? CharacterPosition(Character character) => CharacterMap.CharacterPosition(conn, character);

        public void CharacterMove(Character character, Heading[] movements)
        {
            // Execute effects caused by MapBlocks...
        }
    }
}
