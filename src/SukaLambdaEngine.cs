using System.Collections.Concurrent;

namespace sukalambda
{
    public class CONFIG
    {
        public const string DATABASE_PATH = "./SukaLambda.db3";
        public static PersistenceDbContext conn = new();
        public const uint MAX_ROUNDS = 128;
        public const uint MAX_SKILLS_IN_SINGLE_ROUND = 128;
        public const uint MAX_NUMERIC_EFFECTS_IN_SINGLE_SKILL = 1024;
        public const uint MAX_META_EFFECTS_IN_SINGLE_NUMERIC_EFFECT = 1024;
    }
    public class PRODUCTION_CONFIG : CONFIG { }

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

        public int timeStarted = DateTime.Now.Second;
        public Random rand { get; init; }
        public class Round : List<SkillExecution> { }
        public uint currentRoundPointer { get; private set; } = 0;

        public readonly Round[] rounds = new Round[PRODUCTION_CONFIG.MAX_ROUNDS];
        public readonly List<MetaEffect>[] effectsByRound = new List<MetaEffect>[PRODUCTION_CONFIG.MAX_ROUNDS];
        public List<MetaEffect> metaEffectsForSingleSkillExecution { get; set; } = new();
        public List<NumericEffect> numericEffectsForSingleSkillExecution { get; set; } = new();
        public readonly Dictionary<Guid, Character> characters = new();
        public Map? map;
        public LogCollector logCollector = new();

        public DummyVMCharacter dummyVmCharacter = new();

        /// <param name="map">For a fully-featured game, do not hurry to put a map here.
        /// First initialize <see cref="SukaLambdaEngine"/> without <see cref="Map"/>.
        /// Then initialize a map along with its <see cref="MapBlock"/>s.
        /// </param>
        public SukaLambdaEngine(CONFIG? config=null, Map? map = null)
        {
            rand = new(timeStarted);
            this.map = map;
            if (map != null) map.vm = this;
        }
        public void SetMap(Map map)
        {
            this.map = map; map.vm = this;
        }

        public void AddCharacter(Character character, ushort x, ushort y, Heading heading, Alignment alignment)
        {
            if (map == null) throw new InvalidOperationException("Map is null!");
            characters[character.persistedStatus.id] = character;
            map.AddCharacter(character, x, y, heading, alignment);
            character.OnAddToMap(this);
        }

        public void AddCharacter(Character character, Alignment alignment)
        {
            characters[character.persistedStatus.id] = character;
        }

        public void RemoveCharacter(Character character)
        {
            //characters.Remove(character.id);
            map?.RemoveCharacter(character);
            character.OnRemoveFromMap(this);
            character.removedFromMap = true;
        }

        public void PrepareSkill(SkillExecution execution)
        {
            rounds[currentRoundPointer].Add(execution);
        }

        public void AddEffectToRound(MetaEffect effect, uint roundBias=0)
        {
            if (currentRoundPointer + roundBias < PRODUCTION_CONFIG.MAX_ROUNDS)
                effectsByRound[currentRoundPointer + roundBias].Add(effect);
        }

        public void AddEternalEffect(MetaEffect effect)
        {
            for (uint i = currentRoundPointer; i < PRODUCTION_CONFIG.MAX_ROUNDS; ++i)
                effectsByRound[i].Add(effect);
        }

        public void ExecuteRound()
        {
            foreach (var kvp in characters)
                kvp.Value.statusTemporary = kvp.Value.statusCommitted.Clone();
            if (currentRoundPointer == 0)  OnStartGame();
            OnStartRound();
            HashSet<SkillExecution> executed = new();
            for (int currentSkillPointer = 0; currentRoundPointer < rounds[currentRoundPointer].Count; ++currentSkillPointer)
            {
                rounds[currentRoundPointer].Sort((l, r) =>
                    l.fromCharacter.statusTemporary.Speed != r.fromCharacter.statusTemporary.Speed ?
                    l.fromCharacter.statusTemporary.Speed.CompareTo(r.fromCharacter.statusTemporary.Speed) :
                    rand.Next(3) - 1);
                if (currentRoundPointer > PRODUCTION_CONFIG.MAX_SKILLS_IN_SINGLE_ROUND) throw new StackOverflowException("Too many skills!");
                SkillExecution execution = rounds[currentRoundPointer][currentSkillPointer];
                if (executed.Contains(execution)) continue;
                executed.Add(execution);
                List<NumericEffect> numericEffects = execution.Execute(this);
                foreach (NumericEffect effect in numericEffects)
                    effect.target.CommitNumericEffect(effect);
            }
            OnEndRound();
            // TODO: some baseline codes to judge end of game without map
            if (map?.JudgeEndGame(this) == true || ++currentRoundPointer >= PRODUCTION_CONFIG.MAX_ROUNDS)
                OnEndGame();
        }

        private void OnStartRound()
        {
            foreach (NumericEffect effect in new DummyVMSkillOnRoundStart(dummyVmCharacter).PlanUseSkill(dummyVmCharacter, new(), this).Execute(this))
                effect.target.CommitNumericEffect(effect);
        }
        private void OnEndRound()
        {
            foreach (NumericEffect effect in new DummyVMSkillOnRoundEnd(dummyVmCharacter).PlanUseSkill(dummyVmCharacter, new(), this).Execute(this))
                effect.target.CommitNumericEffect(effect);
        }
        private void OnStartGame()
        {
            foreach (NumericEffect effect in new DummyVMSkillOnGameStart(dummyVmCharacter).PlanUseSkill(dummyVmCharacter, new(), this).Execute(this))
                effect.target.CommitNumericEffect(effect);
        }
        private void OnEndGame()
        {
            foreach (NumericEffect effect in new DummyVMSkillOnGameEnd(dummyVmCharacter).PlanUseSkill(dummyVmCharacter, new(), this).Execute(this))
                effect.target.CommitNumericEffect(effect);
            foreach (Character character in characters.Values)
                character.PersistEarnings();
        }
    }
}