namespace sukalambda
{
    public enum GamePlatform
    {
        Undefined = 0,
        Telegram = 1,
    }
    public class RootController
    {
        public GamePlatform gamePlatform;
        public CommandRouter cmdRouter = new();
        public LogCollector logCollector = new();
        public SukaLambdaEngine? vm = null;

        public RootController(GamePlatform gamePlatform = GamePlatform.Undefined)
        {
            this.gamePlatform = gamePlatform;
        }
    }
}
