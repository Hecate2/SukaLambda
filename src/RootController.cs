using static sukalambda.Island68;

namespace sukalambda
{
    public enum GamePlatform
    {
        Undefined = 0,
        Telegram = 1,
    }
    public class RootController
    {
        public string chatId { get; init; }
        public GamePlatform gamePlatform { get; init; }
        public CommandRouter cmdRouter = new();
        public LogCollector logCollector = new();
        public SukaLambdaEngine? vm = null;

        public RootController(string chatId, GamePlatform gamePlatform = GamePlatform.Undefined)
        {
            this.chatId = chatId;
            this.gamePlatform = gamePlatform;
        }

        [OutGameCommand("exit", ".*", "Exit the game!")]
        public static bool Exit(string account, string commandBody, RootController controller)
        {
            if (controller.vm != null)
            {
                controller.vm.gameEnded = true;
            }
            controller.logCollector.Log(LogCollector.LogType.Map, "Will exit the game!");
            return true;
        }

        [OutGameCommand("pause", ".*", "Pause the game!")]
        public static bool Pause(string account, string commandBody, RootController controller)
        {
            if (controller.vm != null)
            {
                if (!controller.vm.gamePaused)
                {
                    controller.vm.gamePaused = true;
                    controller.logCollector.Log(LogCollector.LogType.Map, "Paused the game. Send /pause again to continue");
                }
                else
                {
                    controller.vm.gamePaused = false;
                    controller.logCollector.Log(LogCollector.LogType.Map, "Game continued.");
                }
            }
            return true;
        }
    }
}
