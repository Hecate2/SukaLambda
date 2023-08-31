using static sukalambda.SukaLambdaEngine;

namespace sukalambda
{
    public class LogCollector
    {
        public enum LogType {
            Trace, Debug, Info, Warn, Error, Fatal,
            OutGame,
            Engine, Map, Character, Round, Skill, NumericEffect, MetaEffect
        }
        public static readonly HashSet<LogType> inGameLogs = new HashSet<LogType> { LogType.Engine, LogType.Map, LogType.Character, LogType.Round, LogType.Skill, LogType.NumericEffect, LogType.MetaEffect };
        public static readonly HashSet<LogType> outGameLogs = new HashSet<LogType> { LogType.OutGame };
        public static readonly HashSet<LogType> gameLogs = inGameLogs.Union(outGameLogs).ToHashSet();
        internal List<Tuple<LogType, string>> logs = new();
        public void Log(LogType type, string message)
        {
            if (message != "")
                logs.Add(new(type, message));
        }
        public List<string> ViewLog(LogType type) => logs.FindAll(v => v.Item1 == type).Select(v => v.Item2).ToList();
        public List<string> ViewLog(IEnumerable<LogType> types) => logs.FindAll(v => types.Contains(v.Item1)).Select(v => v.Item2).ToList();
        public void DeleteLog(LogType type) => logs = logs.Where(v => v.Item1 != type).ToList();
        public void DeleteLog(IEnumerable<LogType> types) => logs = logs.Where(v => !types.Contains(v.Item1)).ToList();
        public List<string> PopLog(LogType type)
        {
            lock(logs){
                List<string> result = ViewLog(type);
                DeleteLog(type);
                return result;
            }
        }
        public List<string> PopLog(IEnumerable<LogType> types)
        {
            lock (logs)
            {
                List<string> result = ViewLog(types);
                DeleteLog(types);
                return result;
            }
        }
        public List<string> PopGameLog() => PopLog(gameLogs);
    }
}
