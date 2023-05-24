using System.Runtime.CompilerServices;

namespace sukalambda
{
    public class NumericEffect
    {
        public SkillExecution skillExecution { get; init; }
        public Character target { get; init; }
        public decimal hitPointChange;
        public Dictionary<MetaEffect, List<string>> logs { get; set; } = new();
        public List<string> initialLogsFromSkill = new();
        public List<string> intermediateLogsFromSkill = new();
        public List<string> finalLogsFromSkill = new();
        public NumericEffect(SkillExecution skillExecution, Character target, decimal hitPointChange = 0)
        {
            this.skillExecution=skillExecution;
            this.target = target;
            this.hitPointChange=hitPointChange;
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
        public string GetLogs()
        {
            string finalLogs = "";
            finalLogs = AppendLogsFromList(finalLogs, initialLogsFromSkill);
            bool intermediateLogsFromSkillAppended = false;
            foreach (var kv in logs.OrderBy(kv => kv.Key.priority).ThenBy(kv => -kv.Key.fromCharacter?.speed))
            {
                if (intermediateLogsFromSkillAppended == false && (kv.Key.priority > 0 ||
                    kv.Key.priority == 0 && kv.Key.fromCharacter?.speed < skillExecution.fromCharacter.speed))
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

    public class MetaEffect
    {
        public Character fromCharacter { get; init; }
        public SkillExecution fromSkillExecution { get; init; }
        public Character toCharacter { get; init; }
        public short priority;
        public Func<NumericEffect, SukaLambdaEngine, bool> triggeringCondition { get; init; }
        public Func<NumericEffect, SukaLambdaEngine, NumericEffect> execution { get; init; }
        public MetaEffect(Character fromCharacter, SkillExecution fromSkillExecution, Character toCharacter, short priority, HashSet<uint> ApplicableRoundsBias, 
            Func<NumericEffect, SukaLambdaEngine, bool> triggeringCondition, Func<NumericEffect, SukaLambdaEngine, NumericEffect> execution)
        {
            this.fromCharacter=fromCharacter;
            this.fromSkillExecution=fromSkillExecution;
            this.toCharacter=toCharacter;
            this.priority=priority;
            this.triggeringCondition=triggeringCondition;
            this.execution=execution;
        }
    }

    public class ActiveEffect : MetaEffect
    {
        public ActiveEffect(Character fromCharacter, SkillExecution fromSkillExecution, Character toCharacter, short priority, HashSet<uint> ApplicableRoundsBias, Func<NumericEffect, SukaLambdaEngine, NumericEffect> execution)
            : base(fromCharacter, fromSkillExecution, toCharacter, priority, ApplicableRoundsBias, 
            (effect, vm) => effect.skillExecution.fromCharacter == fromCharacter ,
            execution)
        { }
    }

    public class PassiveEffect : MetaEffect
    {
        public PassiveEffect(Character fromCharacter, SkillExecution fromSkillExecution, Character to, short priority, HashSet<uint> ApplicableRoundsBias, Func<NumericEffect, SukaLambdaEngine, NumericEffect> execution)
            : base(fromCharacter, fromSkillExecution, to, priority, ApplicableRoundsBias, 
            (effect, vm) => effect.target == to,
            execution)
        { }
    }

}
