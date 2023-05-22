using sukalambda;

namespace SukaLambdaEngineTests
{
    [TestClass]
    public class MapTest
    {
        public Map map = new(10, 10);
        Character c = new("hecate2", Alignment.Blue);

        [TestInitialize]
        public void AddCharacter()
        {
            map.AddCharacter(c, 1, 2, Heading.East);
            Assert.AreEqual(map.GetCharacterPosition(c), new Tuple<ushort, ushort>(1, 2));
        }
        [TestMethod]
        public void AddDuplicatingCharacterAndOnOccupiedPosition()
        {
            try { map.AddCharacter(c, 0, 3, Heading.East); } catch(ArgumentException) { }
            try { map.AddCharacter(new("hecate3", Alignment.Blue), 1, 2, Heading.East); } catch(ArgumentException) { }
        }
        [TestMethod]
        public void RemoveCharacter()
        {
            map.RemoveCharacter(c);
            try { map.RemoveCharacter(c); } catch (IndexOutOfRangeException) { }
            map.AddCharacter(c, 1, 2, Heading.East);
        }
        [TestMethod]
        public void RemoveNonExistentCharacter()
        {
            try { map.RemoveCharacter(new("hecate3", Alignment.Blue)); } catch(IndexOutOfRangeException) { }
        }
    }
}