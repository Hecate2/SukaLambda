using SQLite;
using System.Collections.Concurrent;

namespace sukalambda
{
    public static class CONFIG
    {
        public const string DATABASE_PATH = "./SukaLambda.db3";
        public static SQLiteConnection conn = new(new SQLiteConnectionString(DATABASE_PATH));
        public const uint MAX_ROUNDS = 1024;
    }

    /// <summary>
    /// <see cref="Round">: Priority queue; [SkillExecution (by a Character), SkillExecution (by a Character), ...]
    ///     typically ordered by Character speed
    /// <see cref="SkillExecution">: Generates Effect.
    /// <see cref="NumericEffect">: only numeric effects such as HitPoint changes! Meta effects should be programmed as Modifiers.
    /// <see cref="ActiveEffect">: With each SkillExecution, we head to this variable, looking for the calling character of the skill.
    ///     The Effect of the SkillExecution is then modified.
    /// <see cref="PassiveEffect">: Similar; look for the target character(s) of an SkillExecution
    /// <see cref="MetaEffect">: Similar; triggered by arbitrary custom events or complex lambda functions, not only characters.
    ///     For better performance, simple and frequent lambda functions should be grouped together for a new type of Modifier
    /// MetaEffects are also ordered, by priority of short integer (-32768~32767).
    ///     Priority 0 is the time when the skill is executed; smaller nums for earlier execution.
    ///     For example, you should increase character status before a skill, and increase the damage after a skill
    /// </summary>
    public class SukaLambdaEngine
    {
        public class LogCollector
        {
            public enum LogLevel { Trace, Debug, Info, Gameplay, Warn, Error, Fatal }
            internal readonly ConcurrentDictionary<LogLevel, string> logs = new();
            public void Log(LogLevel level, string message) => logs.AddOrUpdate(level, message, (level, oldMessage) => oldMessage + message);
            public string ViewLog(LogLevel level) => logs.TryGetValue(level, out string? value) ? value : "";
            public string PopLog(LogLevel level) => logs.Remove(level, out string? value) ? value : "";
        }

        public class Round : List<SkillExecution> { }
        public uint currentRoundPointer { get; private set; } = 0;

        public readonly Round[] rounds = new Round[CONFIG.MAX_ROUNDS];
        public readonly List<MetaEffect>[] effectsByRound = new List<MetaEffect>[CONFIG.MAX_ROUNDS];
        public Map? map;
        public LogCollector logCollector = new();

        public SukaLambdaEngine(Map? map = null)
        {
            this.map = map;
        }

        public void AddCharacter(Character character)
        {
        }

        public void PrepareSkill(SkillExecution execution)
        {
            execution.roundPointer = currentRoundPointer;
        }

        public void AddEffectToRound(MetaEffect effect, uint roundBias=0)
        {
            if (currentRoundPointer + roundBias < CONFIG.MAX_ROUNDS)
                effectsByRound[currentRoundPointer + roundBias].Add(effect);
        }
    }
}