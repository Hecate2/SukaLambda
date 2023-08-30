using System.Collections.Concurrent;

namespace sukalambda
{
    public class LogCollector
    {
        public enum LogLevel { Trace, Debug, Info, Warn, Error, Fatal,
            OutGame,
            Engine, Map, Character, Round, Skill, NumericEffect, MetaEffect }
        internal readonly ConcurrentDictionary<LogLevel, string> logs = new();
        public void Log(LogLevel level, string message) => logs.AddOrUpdate(level, message, (level, oldMessage) => oldMessage + message);
        public string ViewLog(LogLevel level) => logs.TryGetValue(level, out string? value) ? value : "";
        public string PopLog(LogLevel level) => logs.Remove(level, out string? value) ? value : "";
    }
}
