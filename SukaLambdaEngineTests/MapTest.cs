using sukalambda;

namespace SukaLambdaEngineTests
{
    [TestClass]
    public class MapTest
    {
        //public Island68 map = new($"file:{nameof(MapTest)}?mode=memory&cache=shared", 10, 10);
        public Island68 map = new($"./{nameof(MapTest)}.db3");
        Lakhesh c = new("hecate2");

        [TestInitialize]
        public void AddCharacter()
        {
            map.conn.Database.EnsureDeleted();
            map.conn.Database.EnsureCreated();
            map.AddCharacter(c, 1, 2, HeadingDirection.East, Alignment.Blue);
            Assert.AreEqual(map.CharacterPosition(c), new Tuple<ushort, ushort>(1, 2));

            // AddDuplicatingCharacterAndOnOccupiedPosition
            try { map.AddCharacter(c, 0, 3, HeadingDirection.East, Alignment.Blue); } catch (ArgumentException) { }
            try { map.AddCharacter(new Lakhesh("hecate3"), 1, 2, HeadingDirection.East, Alignment.Blue); } catch (ArgumentException) { }

            // AddDuplicatingCharacterAndOnOccupiedPosition
            map.RemoveCharacter(c);
            Assert.AreEqual(map.CharacterPosition(c), null);
            Assert.AreEqual(map.CharacterPositionIncludingRemoved(c), new Tuple<ushort, ushort>(1, 2));
            try { map.AddCharacter(c, 1, 2, HeadingDirection.East); } catch (ArgumentException) { }
            map.AddCharacter(new Lakhesh("hecate3"), 1, 2, HeadingDirection.East);
        }
        [TestMethod]
        public void RemoveNonExistentCharacter()
        {
            try { map.RemoveCharacter(new Lakhesh("hecate3")); } catch(InvalidOperationException) { }
        }
    }
}