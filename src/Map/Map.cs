using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace sukalambda
{
    public enum HeadingDirection
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
    public class Heading : IRenderText
    {
        public HeadingDirection heading;
        public Heading(HeadingDirection heading)  { this.heading=heading; }
        public Heading(long heading)  { this.heading=(HeadingDirection)heading; }
        public static Heading operator +(Heading lhs, Heading rhs) => new Heading(((int)lhs.heading + (int)rhs.heading) % 360);
        public static Heading operator -(Heading lhs, Heading rhs) => new Heading(((int)lhs.heading - (int)rhs.heading) % 360);
        public override string ToString() => $"Heading[{string.Format("{0:000}", heading)}]";
        public static implicit operator HeadingDirection(Heading h) => h.heading;
        public static implicit operator int(Heading h) => (int)h.heading % 360;
        public string RenderAsText(Language lang)
        {
            switch ((ushort)heading)
            {
                case (ushort)HeadingDirection.Left:   return "←";
                case (ushort)HeadingDirection.Right:  return "→";
                case (ushort)HeadingDirection.Up:     return "↑";
                case (ushort)HeadingDirection.Down:   return "↓";
                case (ushort)HeadingDirection.NE:     return "↗";
                case (ushort)HeadingDirection.SE:   return "↘";
                case (ushort)HeadingDirection.SW:   return "↙";
                case (ushort)HeadingDirection.NW:   return "↖";
                default: return "";
            }
        }
    }

    public enum Altitude
    {
        Surface = 0, Air = 16, Space = 32, Underwater = -16,
    }

    public abstract class MapBlock : IRenderText
    {
        public string blockAsText = "〇";  // ☉☆★◇◆◎△▲▼▽
        public ushort x { get; init; }
        public ushort y { get; init; }
        public SukaLambdaEngine? vm { get; set; }  // You can do something when the vm is set
        public List<MetaEffect> metaEffects = new();
        public static Dictionary<Altitude, ushort> defaultMobilityCost = new()
        {
            {Altitude.Underwater, 100},{Altitude.Surface, 1},{Altitude.Air, 0},{Altitude.Space, 0},
        };
        public Dictionary<Altitude, ushort> mobilityCost = defaultMobilityCost;
        public MapBlock(ushort x, ushort y, SukaLambdaEngine? vm)
        {
            this.x = x;  this.y = y;
            this.vm = vm;
        }

        public virtual bool AllowEntrancy(Character character,
            Heading?[] movements, ushort movementIndexEnteringThisBlock) => true;
        public virtual bool AllowDeparture(Character character,
            Heading?[] movements, ushort movementIndexEnteringThisBlock) => true;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="movements"></param>
        /// <param name="movementIndexEnteringThisBlock">0 if the movement starts from this block</param>
        /// <param name="vm"></param>
        public virtual void OnCharacterMovingIn(Character character,
            Heading?[] movements, ushort movementIndexEnteringThisBlock)
        { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="movements"></param>
        /// <param name="movementIndexLeavingThisBlock">0 if the movement starts from this block</param>
        /// <param name="vm"></param>
        public virtual void OnCharacterMovingOut(Character character,
            Heading?[] movements, ushort movementIndexLeavingThisBlock)
        { }
        public virtual void OnCharacterMovingOutOfMapFromThisBlock(Character character,
            Heading?[] movements, ushort movementIndexLeavingThisBlock)
        { }

        public virtual string RenderAsText(Language lang) => blockAsText;
    }

    [Index(nameof(positionX))]
    [Index(nameof(positionY))]
    [Index(nameof(alignment))]
    [Index(nameof(removed))]
    public class CharacterInMapData
    {
        [Key]
        public Guid characterID { get; init; }
        public ushort positionX { get; set; }
        public ushort positionY { get; set; }
        //public short altitude { get; set; }
        public ushort heading { get; set; }
        public ushort? alignment { get; set; }  // 阵营
        public bool removed { get; set; }  // whether the character had been removed from the map
    }

    /// <summary>
    /// A grid with index (x=0,y=0) at top-left, and (x=width-1, y=height-1) at bottom-right.
    /// </summary>
    public abstract class Map : IRenderText
    {
        public string basicBlockAsText = "口";
        public string databasePath { get; init; }
        public GameDbContext conn;
        private SukaLambdaEngine? _vm;
        public SukaLambdaEngine? vm { get => _vm; set
            {
                _vm = value;
                foreach (var kv in blocks)
                    kv.Value.vm = _vm;
            } }  // You can do something when the vm is set
        public ushort width, height;
        public Func<SukaLambdaEngine?, bool> JudgeEndGame;
        public Func<SukaLambdaEngine?, Alignment?> JudgeWinningAlignment;
        public Func<SukaLambdaEngine?, Character?> JudgeWinningCharacter;
        public string WinningConditions = "Describe how to win in this map!";

        // Define the special effects for blocks if needed
        public Dictionary<Tuple<ushort, ushort>, MapBlock> blocks = new();

        public Tuple<ushort, ushort>? ComputeCoordinate(int guessX, int guessY, bool allowOverflow)
        {
            if (allowOverflow)
            {
                guessX = guessX % this.width;
                guessY = guessY % this.height;
            }
            if (guessX >= 0 && guessX < this.width && guessY >= 0 && guessY < this.height)
                return new((ushort)guessX, (ushort)guessY);
            return null;
        }

        public List<Tuple<ushort, ushort>> AllCoordinatesWithinManhattanDistance(Tuple<ushort, ushort> center, ushort distance, bool allowOverflow = false) => AllCoordinatesWithinManhattanDistance(center.Item1, center.Item2, distance, allowOverflow);
        public List<Tuple<ushort, ushort>> AllCoordinatesWithinManhattanDistance(ushort x, ushort y, ushort distance, bool allowOverflow = false)
        {
            List<Tuple<ushort, ushort>> result = new();
            if ((x >= this.width || y >= this.height) && allowOverflow == false) return result;
            x = (ushort)(x % this.width); y = (ushort)(y % this.width);
            int guessX = x, guessY = y - distance;  // start guessing from the top
            Tuple<ushort, ushort>? coordinate;
            while (guessX < x + distance)  // top-right side
            {
                coordinate = ComputeCoordinate(guessX, guessY, allowOverflow);
                if (coordinate != null) result.Add(coordinate);
                guessX += 1;  guessY += 1;
            }
            while (guessY < y + distance)  // bottom-right side
            {
                coordinate = ComputeCoordinate(guessX, guessY, allowOverflow);
                if (coordinate != null) result.Add(coordinate);
                guessX -= 1; guessY += 1;
            }
            while (guessX > x - distance)  // bottom-left side
            {
                coordinate = ComputeCoordinate(guessX, guessY, allowOverflow);
                if (coordinate != null) result.Add(coordinate);
                guessX -= 1; guessY -= 1;
            }
            while (guessY > y - distance)  // top-left side
            {
                coordinate = ComputeCoordinate(guessX, guessY, allowOverflow);
                if (coordinate != null) result.Add(coordinate);
                guessX += 1; guessY -= 1;
            }
            return distance > 0 ? result.Concat(AllCoordinatesWithinManhattanDistance(x, y, (ushort)(distance - 1), allowOverflow)).ToList() : result;
        }

        public Dictionary<Alignment, HashSet<Character>> charactersNotDetectedByAlignment = new();
        public void ClearUndetection()
        {
            foreach (var k in charactersNotDetectedByAlignment.Keys)
                charactersNotDetectedByAlignment[k].Clear();
        }

        public Map(string databasePath, ushort width, ushort height, SukaLambdaEngine? vm = null)
        {
            this.databasePath = databasePath;
            conn = new(databasePath);
            this.vm = vm;
            this.width = width;
            this.height = height;
        }

        public void InsertMapBlock(MapBlock mapBlock)
        {
            blocks[new Tuple<ushort, ushort>(mapBlock.x, mapBlock.y)] = mapBlock;
            mapBlock.vm = vm;
        }
        public void RemoveMapBlock(ushort x, ushort y)
        {
            if (blocks.Remove(new Tuple<ushort, ushort>(x, y), out MapBlock? block))
                block.vm = null;
        }

        public int CountCharacterIncludingRemoved() => conn.characterInMap.Count();
        public int CountCharacter() => conn.characterInMap.Where(c => c.removed == false).Count();
        public Tuple<ushort, ushort>? CharacterPosition(Character character, out CharacterInMapData? characterInDatabase)
        {
            //CharacterMap? characterInDatabase = conn.Table<CharacterMap>().Where(c => c.characterID == character.persistedStatus.id).FirstOrDefault();
            characterInDatabase = conn.characterInMap.Where(c => c.characterID == character.persistedStatus.id).FirstOrDefault();
            return (characterInDatabase == null || characterInDatabase.removed == true) ? null :
                new Tuple<ushort, ushort>(characterInDatabase.positionX, characterInDatabase.positionY);
        }
        public Tuple<ushort, ushort>? CharacterPositionIncludingRemoved(Character character, out CharacterInMapData? characterInDatabase)
        {
            characterInDatabase = conn.characterInMap.Where(c => c.characterID == character.persistedStatus.id).FirstOrDefault();
            return (characterInDatabase == null) ? null :
                new Tuple<ushort, ushort>(characterInDatabase.positionX, characterInDatabase.positionY);
        }
        public List<CharacterInMapData> AllCharacters() => conn.characterInMap.Where(c => c.removed == false).ToList();
        public Guid? HasCharacterAt(ushort x, ushort y) => conn.characterInMap.Where(c => c.removed == false && c.positionX == x && c.positionY == y).FirstOrDefault()?.characterID;
        public Guid? HasCharacterAtIncludingRemoved(ushort x, ushort y) => conn.characterInMap.Where(c => c.positionX == x && c.positionY == y).FirstOrDefault()?.characterID;
        public void RemoveCharacter(Character character)
        {
            CharacterInMapData characterInMap = conn.characterInMap.Where(c => c.characterID == character.persistedStatus.id).First();
            characterInMap.removed = true;
            conn.characterInMap.Update(characterInMap);
            conn.SaveChanges();
        }

        public void AddCharacter(Character character, ushort x, ushort y, Heading heading, Alignment? alignment=null) =>
            AddCharacter(character, x, y, heading.heading, alignment);
        public void AddCharacter(Character character, ushort x, ushort y, HeadingDirection heading, Alignment? alignment=null)
        {
            if (x >= width)   throw new ArgumentException($"({x}, {y}); x={x} larger than the width of map {width}");
            if (y >= height)  throw new ArgumentException($"({x}, {y}); y={y} larger than the height of map {height}");
            using var tx = conn.Database.BeginTransaction();
            {
                int count = CountCharacterIncludingRemoved();
                if (count >= int.MaxValue) throw new ArgumentException($"Too many characters!");
                if (CharacterPositionIncludingRemoved(character, out _) != null) throw new ArgumentException($"This character {character.persistedStatus.accountId} had been added before");
                Guid? anotherCharacterId = HasCharacterAt(x, y);
                if (anotherCharacterId != null)
                    throw new ArgumentException((vm != null) ?
                        $"Another character [{vm.characters[(Guid)anotherCharacterId].persistedStatus.characterName}]({anotherCharacterId}) of {vm.characters[(Guid)anotherCharacterId].persistedStatus.accountId} at ({x}, {y}) had been added before"
                    :
                        $"Another character ({anotherCharacterId}) at ({x}, {y}) had been added before; no {nameof(SukaLambdaEngine)} had been specified for this map"
                    );
                conn.Add(new CharacterInMapData { characterID=character.persistedStatus.id, positionX=x, positionY=y, heading=(ushort)heading, alignment=(ushort?)alignment, removed=false });
                conn.SaveChanges();
            }
            tx.Commit();
        }
        /// <returns>true if in map but removed; null if not in map; false if in map and not removed</returns>
        public bool? IsCharacterRemoved(Character character) => conn.characterInMap.Where(c => c.characterID == character.persistedStatus.id).FirstOrDefault()?.removed;

        /// <summary>
        /// Change the heading, cutting off the heading projection leaving the map
        /// For example, if we are leaving the bottom line of the map, heading <see cref="HeadingDirection.SE"/>,
        /// cut the component at direction <see cref="HeadingDirection.South"/>, and return <see cref="HeadingDirection.East"/>.
        /// Return null if there can be no movement.
        /// </summary>
        /// <param name="fromX"></param>
        /// <param name="fromY"></param>
        /// <param name="heading"></param>
        /// <returns></returns>
        public Heading? CheckMovingOutOfMap(ushort fromX, ushort fromY, Heading? heading)
        {
            if (heading == null) return null;
            if (heading > 360) throw new ArgumentException($"Invalid heading {(ushort)heading}!");
            /*N*/if (fromY <= 0)
            {
                if (heading < 45 || heading > 315) return null;
                if (heading >= 45 && heading < 90) heading.heading=(HeadingDirection)90;
                if (heading > 270 && heading <= 315) heading.heading=(HeadingDirection)270;
            }
            /*S*/if (fromY >= height - 1)
            {
                if (heading > 135 && heading < 225) return null;
                if (heading > 90 && heading <= 135) heading.heading=(HeadingDirection)90;
                if (heading >= 225 && heading < 270) heading.heading=(HeadingDirection)270;
            }
            /*W*/if (fromX <= 0)
            {
                if (heading > 225 && heading < 315) return null;
                if (heading > 180 && heading <= 225) heading.heading=(HeadingDirection)180;
                if (heading >= 315) heading.heading=(HeadingDirection)0;
            }
            /*E*/
            if (fromX >= width - 1)
            {
                if (heading < 135 && heading > 45) return null;
                if (heading >= 135 && heading < 180) heading.heading=(HeadingDirection)180;
                if (heading <= 45) heading.heading=(HeadingDirection)0;
            }
            return heading;
        }

        public static double ToRadian(double headingDegree)
        {
            headingDegree = headingDegree - 90;  // convert from 0 degree at north to 0 degree at east
            return headingDegree * Math.PI / 180;
        }

        /// <returns>Changes of x and y</returns>
        public Tuple<ushort, ushort> ComputeMovement(Character character, Heading heading, ushort distance)
        {
            double radianHeading = ToRadian(heading);
            return new Tuple<ushort, ushort>((ushort)Math.Round(distance * Math.Cos(radianHeading)), (ushort)Math.Round(distance * Math.Sin(radianHeading)));
        }

        /// <summary>
        /// Typically, use distances of only 1, and headings of only NWSE for ordinary characters.
        /// Use large values only for very fast ones. E.g. missiles.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="headings"></param>
        /// <param name="distances"></param>
        public void CharacterMove(Character character, Heading[] headings, ushort[] distances)
        {
            if (headings.Length != distances.Length) throw new ArgumentException($"Different length of headings {headings.Length} and destinations {distances.Length}!");
            // Execute effects caused by MapBlocks...
            using var tx = conn.Database.BeginTransaction();
            {
                ushort x, y;
                Tuple<ushort, ushort>? src = CharacterPosition(character, out CharacterInMapData? charData);
                if (src == null || charData == null) return;
                x = src.Item1; y = src.Item2;
                for (ushort i = 0; i < headings.Length; ++i )
                {
                    bool outOfMap = false;
                    Heading plannedHeading = headings[i];
                    Heading? headingResult = CheckMovingOutOfMap(x, y, plannedHeading);
                    if (headingResult != plannedHeading) outOfMap = true;
                    // You can also change distances by yourself!

                    Tuple<ushort, ushort> currentPosition = new(x, y);
                    if (this.blocks.TryGetValue(currentPosition, out MapBlock? block))
                    {
                        if (outOfMap)
                            block.OnCharacterMovingOutOfMapFromThisBlock(character, headings, i);
                        if (character.removedFromMap || character.statusTemporary.Mobility <= 0) break;
                        if (headingResult != null)
                            block.OnCharacterMovingOut(character, headings, i);
                        if (character.removedFromMap || character.statusTemporary.Mobility <= 0) break;
                    }
                    if (headingResult == null) continue;
                    Tuple<ushort, ushort> movement = ComputeMovement(character, headingResult, distances[i]);
                    
                    int mobilityCost;
                    if (block != null && block.mobilityCost.ContainsKey(character.altitude))
                        mobilityCost = distances[i] * block.mobilityCost[character.altitude];
                    else
                        mobilityCost = distances[i] * MapBlock.defaultMobilityCost[character.altitude];
                    character.statusTemporary.Mobility -= mobilityCost;

                    if (block == null || block.AllowDeparture(character, headings, i))
                    {
                        x += movement.Item1;
                        y += movement.Item2;
                    }
                    Tuple<ushort, ushort> destination = new(x, y);
                    if (vm == null) break;
                    character.OnMoveInMap(vm, currentPosition, plannedHeading, destination, headingResult);
                    if (character.removedFromMap || character.statusTemporary.Mobility <= 0) break;
                    if (this.blocks.TryGetValue(destination, out block))  // blockTo
                    {
                        if (block != null && !block.AllowEntrancy(character, headings, i))
                        {   // cancel movement
                            x -= movement.Item1;
                            y -= movement.Item2;
                            break;
                        }
                        if (block != null)
                            block.OnCharacterMovingIn(character, headings, i);
                    }
                    if (character.removedFromMap || character.statusTemporary.Mobility <= 0) break;
                }
                charData.positionX = x; charData.positionY = y;
                conn.Update(charData);
                conn.SaveChanges();
            }
            tx.Commit();
        }
        public string RenderAsText(Language lang) => RenderAsText(lang, null);
        public string RenderAsText(Language lang, Alignment? alignment = null)
        {
            // TODO: use renderPriority for each kind of objects?
            int basicBlockOccupiesColumns = basicBlockAsText.Count();
            int basicBlockOccupiesRows = basicBlockAsText.Count(ch => ch == '\n') + 1;
            string[] singleRow = Enumerable.Repeat(basicBlockAsText, width).ToArray();
            string[][] wholeMap = new string[height][];
            for (int rowIndex = 0; rowIndex < height; rowIndex++)
            {
                wholeMap[rowIndex] = new string[width];
                singleRow.CopyTo(wholeMap[rowIndex], 0);
            }
            foreach (var kvp in blocks)
            {
                if (kvp.Key.Item1 >= width || kvp.Key.Item2 >= height) continue;
                string text = kvp.Value.RenderAsText(lang);
                if (text == "") continue;
                if (text.Count(ch => ch == '\n') == 0)
                    text = string.Join("\n", Enumerable.Repeat(text, basicBlockOccupiesRows));
                wholeMap[kvp.Key.Item2][kvp.Key.Item1] = text;
            }

            if (vm != null)
            {
                List<CharacterInMapData> allCharacters = AllCharacters();
                // render characters
                foreach (CharacterInMapData characterInMap in allCharacters)
                {
                    Character character = vm.characters[characterInMap.characterID];
                    if (alignment != null && charactersNotDetectedByAlignment.ContainsKey((Alignment)alignment)
                        && charactersNotDetectedByAlignment[(Alignment)alignment].Contains(character))
                        continue;
                    string text = string.Join("", Enumerable.Repeat(character.RenderAsText(lang), basicBlockOccupiesColumns));
                    if (text == "") continue;
                    if (text.Count(ch => ch == '\n') == 0)
                        text = string.Join("\n", Enumerable.Repeat(text, basicBlockOccupiesRows));
                    wholeMap[characterInMap.positionY][characterInMap.positionX] = text;
                }
            }

            string finalText = "";
            for (int column = 0; column < wholeMap[0].Length; column++)
                for (int i = 0; i < basicBlockOccupiesColumns; i++)
                    if (i == basicBlockOccupiesColumns / 2)
                        finalText += (char)('Ａ' + column);  // full-width letter
                    else
                        finalText += ' ';
            finalText += "\n";
            for (int rowIndex = 0; rowIndex < wholeMap.Length; ++rowIndex)
                for (int i = 0; i < basicBlockOccupiesRows; i++)
                {
                    foreach (string block in wholeMap[rowIndex])
                        finalText += block.Split('\n')[i];
                    if (i == basicBlockOccupiesRows / 2)
                        finalText += $" {rowIndex}";
                    finalText += '\n';
                }
            return finalText.TrimEnd('\n');
        }
    }
}
