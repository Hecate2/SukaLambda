using System.Data;

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

    public class Map
    {
        public ushort width, height;
        public DataTable table = new();
        public DataColumn characterID = new("characterId", typeof(string));
        public DataColumn alignment = new("alignment", typeof(ushort));
        public DataColumn positionX = new("positionX", typeof(ushort));
        public DataColumn positionY = new("positionY", typeof(ushort));
        public DataColumn heading = new("heading", typeof(ushort));

        public Map(ushort width, ushort height)
        {
            this.width = width;
            this.height = height;
            table.Columns.Add(characterID);
            table.Columns.Add(alignment);
            table.Columns.Add(positionX);
            table.Columns.Add(positionY);
            table.Columns.Add(heading);
        }
        public void AddCharacter(Character character, ushort x, ushort y, Heading heading)
        {
            if (table.Rows.Count >= int.MaxValue)
                throw new IndexOutOfRangeException($"Too many characters: {table.Rows.Count}");
            if (x >= width)
                throw new ArgumentException($"({x}, {y}); x={x} larger than the width of map {width}");
            if (y >= height)
                throw new ArgumentException($"({x}, {y}); y={y} larger than the height of map {height}");
            if (table.Select($"characterId='{character.accountId}'").Length > 0)
                throw new ArgumentException($"This character {character.accountId} had been added before");
            DataRow[] row = table.Select($"positionX={x} AND positionY={y}");
            if (row.Length > 0)
                throw new ArgumentException($"Another character {row[0][characterID]} at ({row[0][positionX]}, {row[0][positionY]}) had been added before");
            table.Rows.Add(character.accountId, x, y, heading);
        }

        public void RemoveCharacter(Character character)
        {
            table.Rows.Remove(table.Select($"characterId='{character.accountId}'")[0]);
            table.AcceptChanges();
        }

        public Tuple<ushort, ushort> GetCharacterPosition(Character character)
        {
            DataRow[] rows = table.Select($"characterId='{character.accountId}'");
            int count = rows.Length;
            if (count > 1) throw new InvalidDataException($"Multiple character {character.accountId} in {nameof(Map)}!");
            return new Tuple<ushort, ushort>((ushort)rows[0][1], (ushort)rows[0][2]);
        }
    }
}
