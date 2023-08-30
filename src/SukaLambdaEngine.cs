namespace sukalambda
{
    public class CONFIG
    {
        public const string DATABASE_PATH = "./SukaLambda.db3";
        public static PersistenceDbContext conn = new();
        public static DummyVMCharacter dummyVm = new();
        public static DummyMapCharacter dummyMap = new();
        public static DummyVMSkillOnGameStart gameStart = new();
        public static DummyVMSkillOnRoundStart roundStart = new();
        public static DummyVMSkillOnRoundEnd roundEnd = new();
        public static DummyVMSkillOnGameEnd gameEnd = new();
        public const uint MAX_ROUNDS = 128;
        public const uint MAX_SKILLS_IN_SINGLE_ROUND = 128;
        public const uint MAX_NUMERIC_EFFECTS_IN_SINGLE_SKILL = 1024;
        public const uint MAX_META_EFFECTS_IN_SINGLE_NUMERIC_EFFECT = 1024;
    }
    public class PRODUCTION_CONFIG : CONFIG
    {
        public new const string DATABASE_PATH = "./SukaLambdaProduction.db3";
    }
    public class TEST_CONFIG : CONFIG
    {
        public new const string DATABASE_PATH = "./SukaLambdaTest.db3";
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
        public RootController rootController { get; init; }
        public Semaphore semaphore = new(0, 1);

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

        /// <param name="map">For a fully-featured game, do not hurry to put a map here.
        /// First initialize <see cref="SukaLambdaEngine"/> without <see cref="Map"/>.
        /// Then initialize a map along with its <see cref="MapBlock"/>s.
        /// </param>
        public SukaLambdaEngine(RootController rootController, Map? map = null)
        {
            rand = new(timeStarted);
            this.rootController = rootController;
            rootController.vm = this;
            this.map = map;
            if (map != null) map.vm = this;
        }
        public void SetMap(Map map)
        {
            semaphore.WaitOne(5000);
            if (this.map != null) map.vm = null;
            this.map = map; map.vm = this;
            semaphore.Release();
        }

        public void AddCharacter(Character character, ushort x, ushort y, Heading heading, Alignment? alignment=null)
        {
            if (map == null) throw new InvalidOperationException("Map is null!");
            semaphore.WaitOne(5000);
            rootController.cmdRouter.RegisterCommandsForCharacter(character);
            characters[character.persistedStatus.id] = character;
            if (alignment != null) character.alignment = alignment;
            map.AddCharacter(character, x, y, heading, alignment ?? character.alignment);
            character.OnAddToMap(this);
            semaphore.Release();
        }

        public void AddCharacter(Character character, Alignment? alignment = null)
        {
            if (map != null) throw new InvalidOperationException("Map had been initialized!");
            semaphore.WaitOne(5000);
            rootController.cmdRouter.RegisterCommandsForCharacter(character);
            if (alignment != null) character.alignment = alignment;
            characters[character.persistedStatus.id] = character;
            semaphore.Release();
        }

        public void RemoveCharacter(Character character)
        {
            semaphore.WaitOne(5000);
            //characters.Remove(character.id);
            rootController.cmdRouter.UnregisterCommandsForCharacter(character);
            map?.RemoveCharacter(character);
            character.OnRemoveFromMap(this);
            character.removedFromMap = true;
            semaphore.Release();
        }

        public void PrepareSkill(SkillExecution execution)
        {
            semaphore.WaitOne(500);
            if (rounds[currentRoundPointer] == null) rounds[currentRoundPointer] = new();
            rounds[currentRoundPointer].Add(execution);
            semaphore.Release();
        }

        public void RemoveSkillOfCharacterAndType(Character character, Skill? type=null, uint roundBias=0)
        {
            semaphore.WaitOne(500);
            if (rounds[currentRoundPointer + roundBias] == null) rounds[currentRoundPointer + roundBias] = new();
            Round round = rounds[currentRoundPointer + roundBias];
            foreach (SkillExecution execution in round)
                if (execution.fromCharacter == character && (type == null || execution.skill.GetType() == type.GetType()))
                    round.Remove(execution);
            semaphore.Release();
        }

        public void AddEffectToRound(MetaEffect effect, uint roundBias=0)
        {
            semaphore.WaitOne(500);
            if (currentRoundPointer + roundBias < PRODUCTION_CONFIG.MAX_ROUNDS)
                effectsByRound[currentRoundPointer + roundBias].Add(effect);
            semaphore.Release();
        }

        public void AddEternalEffect(MetaEffect effect)
        {
            semaphore.WaitOne(500);
            for (uint i = currentRoundPointer; i < PRODUCTION_CONFIG.MAX_ROUNDS; ++i)
                effectsByRound[i].Add(effect);
            semaphore.Release();
        }

        public void ExecuteRound()
        {
            semaphore.WaitOne(5000);
            foreach (var kvp in characters)
                kvp.Value.statusTemporary = kvp.Value.statusCommitted.Clone();
            if (currentRoundPointer == 0)  OnStartGame();
            OnStartRound();
            HashSet<SkillExecution> executed = new();
            for (int currentSkillPointer = 0; currentRoundPointer < rounds[currentRoundPointer].Count; ++currentSkillPointer)
            {
                rounds[currentRoundPointer].Sort((l, r) =>
                    l.priority != r.priority ? l.priority.CompareTo(r.priority) :
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
            semaphore.Release();
        }

        private void OnStartRound()
        {
            foreach (NumericEffect effect in PRODUCTION_CONFIG.roundStart.PlanUseSkill(PRODUCTION_CONFIG.dummyVm, new(), this).Execute(this))
                effect.target.CommitNumericEffect(effect);
        }
        private void OnEndRound()
        {
            foreach (NumericEffect effect in PRODUCTION_CONFIG.roundEnd.PlanUseSkill(PRODUCTION_CONFIG.dummyVm, new(), this).Execute(this))
                effect.target.CommitNumericEffect(effect);
        }
        private void OnStartGame()
        {
            foreach (NumericEffect effect in PRODUCTION_CONFIG.gameStart.PlanUseSkill(PRODUCTION_CONFIG.dummyVm, new(), this).Execute(this))
                effect.target.CommitNumericEffect(effect);
        }
        private void OnEndGame()
        {
            foreach (NumericEffect effect in PRODUCTION_CONFIG.gameEnd.PlanUseSkill(PRODUCTION_CONFIG.dummyVm, new(), this).Execute(this))
                effect.target.CommitNumericEffect(effect);
            foreach (Character character in characters.Values)
                character.PersistEarnings();
        }
    }
}