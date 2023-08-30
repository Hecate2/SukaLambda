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
            controller.cmdRouter.ExecuteCommand(
                "TestAccount",
                "/mv ES".TrimStart().TrimStart('/'),
                controller
            );
            controller.vm.ExecuteRound();
            ;
        }
    }
}
