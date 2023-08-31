using sukalambda;

namespace SukaLambdaEngineTests
{
    [TestClass]
    public class StartGameTest
    {
        [TestMethod]
        public void StartGame()
        {
            RootController controller = new RootController(GamePlatform.Undefined);
            controller.cmdRouter.ExecuteCommand(
                "TestAccount",
                "/i68".TrimStart().TrimStart('/'),
                controller
            );
            Assert.IsNotNull( controller.vm!.map );
            Character lakhesh = controller.vm.characters.First().Value;
            Assert.AreEqual(lakhesh.statusCommitted.Mobility, 2);
            Tuple<ushort, ushort>? position = controller.vm.map.CharacterPosition(lakhesh, out _);
            Assert.IsTrue(position != null && position.Item1 == 0 && position.Item2 == 0);
            controller.cmdRouter.ExecuteCommand(
                "TestAccount",
                "/mv ESW".TrimStart().TrimStart('/'),
                controller
            );
            controller.vm.ExecuteRound();
            Assert.AreEqual(lakhesh.statusCommitted.Mobility, 2);
            Assert.AreEqual(lakhesh.statusTemporary.Mobility, 0);
            position = controller.vm.map.CharacterPosition(lakhesh, out _);
            Assert.IsTrue(position != null && position.Item1 == 1 && position.Item2 == 1);
        }
    }
}
