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
            string rendered = controller.vm.map.RenderAsText(Language.cn);
            Assert.AreEqual(rendered, "ＡＢＣＤＥＦＧＨＩＪＫＬ\r\n仓仓草草口口口口口口林森 0\r\n仓菈草草口口口口口口口口 1\r\n草草林森林口口森口森林森 2\r\n口林森林森口口林口林森林 3\r\n口口口口口口口森口森林森 4\r\n林森林森口森林森口水水水 5\r\n森林口口口林森林口水森林 6\r\n林森口森林口口森口口口口 7\r\n森林口口口口口口口水森林 8\r\n水水林水口水水水水水林森 9\r\n水水水水口林森水森林森林 10\r\n林森口口口森林水水水水水 11\r\n森林口林森林口口口林森林 12\r\n林森口口口口口口口森林森 13\r\n森林森林森林森口口口口口 14".Replace("\r", ""));
            _ = """
            ＡＢＣＤＥＦＧＨＩＪＫＬ
            仓仓草草口口口口口口林森 0
            仓菈草草口口口口口口口口 1
            草草林森林口口森口森林森 2
            口林森林森口口林口林森林 3
            口口口口口口口森口森林森 4
            林森林森口森林森口水水水 5
            森林口口口林森林口水森林 6
            林森口森林口口森口口口口 7
            森林口口口口口口口水森林 8
            水水林水口水水水水水林森 9
            水水水水口林森水森林森林 10
            林森口口口森林水水水水水 11
            森林口林森林口口口林森林 12
            林森口口口口口口口森林森 13
            森林森林森林森口口口口口 14
            """;
        }
    }
}
