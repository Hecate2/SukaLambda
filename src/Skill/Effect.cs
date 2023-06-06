using System.Runtime.CompilerServices;

namespace sukalambda
{
    public enum MetaEffectPriority
    {
        onFire = 0, onHit = short.MaxValue / 4 + 1,
    }

    public class NumericEffect : IRenderText
    {
        public SkillExecution skillExecution { get; init; }
        public Character target { get; init; }
        public NumericStatus statusChange { get; set; }
        public Dictionary<MetaEffect, List<string>> logs { get; set; } = new();
        public List<string> initialLogsFromSkill = new();
        public List<string> intermediateLogsFromSkill = new();
        public List<string> finalLogsFromSkill = new();
        public bool willCommit { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="skillExecution"></param>
        /// <param name="target"></param>
        /// <param name="statusChange"></param>
        /// <param name="willCommit">The effect will actually change <see cref="Character.statusCommitted"/> after executed</param>
        public NumericEffect(SkillExecution skillExecution, Character target, NumericStatus? statusChange = null, bool willCommit = true)
        {
            this.skillExecution=skillExecution;
            this.target = target;
            this.statusChange=statusChange ?? new();
            this.willCommit = willCommit;
        }
        public void AppendLog(MetaEffect effect, string log)
        {
            if (logs.ContainsKey(effect)) logs[effect] = new List<string> { log }; else logs[effect].Append(log);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal string AppendLogsFromList(string finalLogs, List<string> logs)
        {
            foreach (string log in logs)
            {
                finalLogs += log;
                if (!log.EndsWith("\n"))
                    finalLogs += "\n";
            }
            return finalLogs;
        }
        public string RenderAsText(Language lang)
        {
            string finalLogs = "";
            finalLogs = AppendLogsFromList(finalLogs, initialLogsFromSkill);
            bool intermediateLogsFromSkillAppended = false;
            foreach (var kv in logs.OrderBy(kv => kv.Key.priority).ThenBy(kv => -kv.Key.fromCharacter?.statusTemporary.Speed))
            {
                if (intermediateLogsFromSkillAppended == false && (kv.Key.priority > 0 ||
                    kv.Key.priority == 0 && kv.Key.fromCharacter?.statusTemporary.Speed < skillExecution.fromCharacter?.statusTemporary.Speed))
                {
                    finalLogs = AppendLogsFromList(finalLogs, intermediateLogsFromSkill);
                    intermediateLogsFromSkillAppended = true;
                }
                finalLogs = AppendLogsFromList(finalLogs, kv.Value);
            }
            finalLogs = AppendLogsFromList(finalLogs, finalLogsFromSkill);
            return finalLogs;
        }
    }

    /// <summary>
    /// For example, if you want your effect to let a skill miss the target,
    /// just return null in `Execute`, or
    /// just set the <see cref="NumericEffect.willCommit"/> = false;
    /// and remove all the <see cref="MetaEffect"/> in <see cref="SukaLambdaEngine.metaEffectsForSingleSkillExecution"/>
    /// with priority >= <see cref="MetaEffectPriority.onHit"/>, or priority > your effect 
    /// For another example, if you want to increase the <see cref="NumericStatus"/> of a Character temporarily,
    /// Define priority < 0 and re-compute the numeric effects with <see cref="Skill.Execute"/>.
    /// </summary>
    public abstract class MetaEffect
    {
        public Character fromCharacter { get; set; }
        public SkillExecution fromSkillExecution { get; set; }
        public HashSet<Character> toCharacters { get; set; }
        public short priority { get; set; }
        public object[] metaArgs { get; set; }
        public Func<NumericEffect?, SukaLambdaEngine, bool> TriggeringCondition { get; init; }
        public Func<NumericEffect?, SukaLambdaEngine, NumericEffect?> Execute { get; init; }
        public MetaEffect(Character fromCharacter, SkillExecution fromSkillExecution, HashSet<Character> toCharacters, short priority, object[] metaArgs,
            Func<NumericEffect?, SukaLambdaEngine, bool> triggeringCondition, Func<NumericEffect?, SukaLambdaEngine, NumericEffect?> execute)
        {
            this.fromCharacter=fromCharacter;
            this.fromSkillExecution=fromSkillExecution;
            this.toCharacters=toCharacters;
            this.priority=priority;
            this.metaArgs=metaArgs;
            this.TriggeringCondition=triggeringCondition;
            this.Execute=execute;
        }
    }

    public abstract class ActiveEffect : MetaEffect
    {
        public ActiveEffect(Character fromCharacter, SkillExecution fromSkillExecution, HashSet<Character> toCharacters, short priority, object[] metaArgs, Func<NumericEffect?, SukaLambdaEngine, NumericEffect?> execute)
            : base(fromCharacter, fromSkillExecution, toCharacters, priority, metaArgs,
            (effect, vm) => effect?.skillExecution.fromCharacter == fromCharacter ,
            execute)
        { }
    }

    public abstract class PassiveEffect : MetaEffect
    {
        public PassiveEffect(Character fromCharacter, SkillExecution fromSkillExecution, HashSet<Character> toCharacters, short priority, object[] metaArgs, Func<NumericEffect?, SukaLambdaEngine, NumericEffect?> execution)
            : base(fromCharacter, fromSkillExecution, toCharacters, priority, metaArgs,
            (effect, vm) => effect != null && toCharacters.Contains(effect.target),
            execution)
        { }
    }

}
