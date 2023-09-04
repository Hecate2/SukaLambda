using System.ComponentModel.DataAnnotations;

namespace sukalambda
{
    public class SkillData
    {
        [Key]
        public Guid characterId { get; init; }
        public string skillClassName { get; init; }
    }

    public abstract class Skill : IRenderText
    {
        public ushort priority = 0;
        public Character owner;  // Maybe skills can be owned by one and executed from another?
        public uint rangeCommitted { get; private set; }
        public uint rangeTemporary { get; set; }
        public Skill(Character owner) { this.owner = owner; }

        /// <returns>null if the target is valid; A description for reason if the target is invalid</returns>
        public virtual string? ReasonForInvalidTarget(Character fromCharacter, Character toCharacter, SukaLambdaEngine vm) => "";
        public virtual HashSet<Character> ValidTargets(Character fromCharacter, SukaLambdaEngine vm) => new HashSet<Character>();
        public virtual Character[] AutoSelectTargets(Character fromCharacter, SukaLambdaEngine vm) => new Character[0];
        
        /// <summary>
        /// For vm to plan a skill.
        /// No need to be implemented for human skills
        /// </summary>
        /// <param name="fromCharacter"></param>
        /// <param name="plannedTargets"></param>
        /// <param name="vm"></param>
        /// <param name="metaArgs"></param>
        /// <returns></returns>
        public virtual SkillExecution PlanUseSkill(Character fromCharacter, List<Character> plannedTargets, SukaLambdaEngine vm, object[]? metaArgs = null) => new(fromCharacter, this, plannedTargets.ToArray(), metaArgs);

        /// <summary>
        /// Used by human to input commands.
        /// Translates human commands to SkillExecution, and send to VM
        /// </summary>
        /// <param name="account"></param>
        /// <param name="command">mv NNESWWW</param>
        /// <returns></returns>
        public abstract bool PlanUseSkill(string command, SukaLambdaEngine vm);

        /// <summary>
        /// Write arbitrary codes in a inherited class overriding <see cref="Execute"/>
        /// to program the actual execution of skill.
        /// Add <see cref="MetaEffect"/> with <see cref="SukaLambdaEngine.AddEffectToRound"/>,
        /// and generate <see cref="NumericEffect"/>
        /// For example of a delicate temporary shield, activated after my SkillExecution,
        /// and deactivated before my SkillExecution in the next round:
        /// Build a MetaEffect in this round for the shield,
        /// another MetaEffect at the end of this round to move the remaining shield life to the next round,
        /// and a third MetaEffect in the next round to deactivate it.
        /// </summary>
        /// <param name="metaArgs"></param>
        /// <returns>The planned numeric effects. The length can be shorter than <see cref="SkillExecution.desiredTargets"/></returns>
        public abstract List<NumericEffect> Execute(SkillExecution skillExecution, SukaLambdaEngine vm, object[]? metaArgs = null);
        public virtual string WriteLogAtStart(SukaLambdaEngine vm) => "";
        public virtual string WriteLogForEffect(NumericEffect effect, SukaLambdaEngine vm) => "";
        public virtual string WriteLogAtEnd(SukaLambdaEngine vm) => "";
        /// <returns>Just the skill name in different languages</returns>
        public virtual string RenderAsText(Language lang) => $"A fantastic skill {this.GetType().Name}!";
    }

    /// <summary>
    /// <see cref="SkillExecution"/> is a plan to use a skill.
    /// Generated after a player decides to use a skill of a character, on a list of targets.
    /// Usually, do not inherit this class
    /// </summary>
    public class SkillExecution
    {
        public ushort priority;
        public Character fromCharacter { get; init; }  // Maybe skills can be owned by one and executed from another?
        public Skill skill { get; init; }
        public Character[] desiredTargets { get; init; }
        public uint roundPointer { get; set; }
        public object[]? metaArgs;
        public SkillExecution(Character fromCharacter, Skill skill, Character[] desiredTargets, object[]? metaArgs = null)
        {
            this.fromCharacter = fromCharacter;
            this.skill = skill;
            this.priority = skill.priority;
            this.desiredTargets = desiredTargets;
            this.metaArgs = metaArgs;
        }
        public List<NumericEffect> Execute(SukaLambdaEngine vm)
        {
            vm.rootController.logCollector.Log(LogCollector.LogType.Skill, skill.WriteLogAtStart(vm));

            vm.numericEffectsForSingleSkillExecution = new();
            if (vm.effectsByRound[vm.currentRoundPointer] == null) vm.effectsByRound[vm.currentRoundPointer] = new();
            vm.metaEffectsForSingleSkillExecution = new(vm.effectsByRound[vm.currentRoundPointer]);
            HashSet<MetaEffect> executedMetaEffectsForThisSkill = new();

            // Before exeuction of skill: execute MetaEffects of priority < 0
            int metaEffectPointerBeforeSkill = 0;
            for (; metaEffectPointerBeforeSkill < vm.metaEffectsForSingleSkillExecution.Count; ++metaEffectPointerBeforeSkill)
            {
                vm.metaEffectsForSingleSkillExecution.Sort((l, r) =>
                    l.priority != r.priority ? l.priority.CompareTo(r.priority) :
                    l.fromCharacter.statusTemporary.Speed != r.fromCharacter.statusTemporary.Speed ? l.fromCharacter.statusTemporary.Speed.CompareTo(r.fromCharacter.statusTemporary.Speed) :
                    l.fromSkillExecution.roundPointer != r.fromSkillExecution.roundPointer ? l.fromSkillExecution.roundPointer.CompareTo(r.fromSkillExecution.roundPointer) :
                    vm.rand.Next(3) - 1
                );
                if (vm.metaEffectsForSingleSkillExecution[metaEffectPointerBeforeSkill].priority >= 0) break;
                MetaEffect metaEffect = vm.effectsByRound[vm.currentRoundPointer][metaEffectPointerBeforeSkill];
                if (!executedMetaEffectsForThisSkill.Contains(metaEffect) && metaEffect.TriggeringCondition(null, vm))
                {
                    executedMetaEffectsForThisSkill.Add(metaEffect);
                    NumericEffect? numericEffect = metaEffect.Execute(null, vm);
                    if (numericEffect != null)
                        vm.numericEffectsForSingleSkillExecution.Add(numericEffect);
                }
            }

            // Execute our skill!
            vm.numericEffectsForSingleSkillExecution =
                vm.numericEffectsForSingleSkillExecution.Concat(skill.Execute(this, vm, metaArgs)).ToList();

            // Modify the resulting NumericEffects, or set to null to let the effect miss the target
            HashSet<NumericEffect> executedNumericEffects = new();
            for (int numericEffectPointer = 0; numericEffectPointer < vm.numericEffectsForSingleSkillExecution.Count; ++numericEffectPointer)
            {
                if (numericEffectPointer > PRODUCTION_CONFIG.MAX_NUMERIC_EFFECTS_IN_SINGLE_SKILL) throw new StackOverflowException("Too many NumericEffects! Probably too many targets.");
                NumericEffect validNumericEffect = vm.numericEffectsForSingleSkillExecution[numericEffectPointer];
                if (executedNumericEffects.Contains(validNumericEffect)) continue;
                executedNumericEffects.Add(validNumericEffect);
                // Search for all MetaEffects that will be triggered by this execution,
                // and sort meta effects by priority then by character speed.
                // Do not use foreach, because MetaEffect can add new MetaEffects.
                HashSet<MetaEffect> executedMetaEffectsForThisTarget = new();
                vm.metaEffectsForSingleSkillExecution = new(vm.effectsByRound[vm.currentRoundPointer]);
                for (int metaEffectPointer = metaEffectPointerBeforeSkill; metaEffectPointer < vm.metaEffectsForSingleSkillExecution.Count; ++metaEffectPointer)
                {
                    if (metaEffectPointer > PRODUCTION_CONFIG.MAX_META_EFFECTS_IN_SINGLE_NUMERIC_EFFECT) throw new StackOverflowException("Too many MetaEffects on a single NumericEffect!");
                    if (vm.metaEffectsForSingleSkillExecution[metaEffectPointer].priority < 0) continue;
                    vm.effectsByRound[vm.currentRoundPointer].Sort((l, r) =>
                        l.priority != r.priority ? l.priority.CompareTo(r.priority) :
                        l.fromCharacter.statusTemporary.Speed != r.fromCharacter.statusTemporary.Speed ? l.fromCharacter.statusTemporary.Speed.CompareTo(r.fromCharacter.statusTemporary.Speed) :
                        l.fromSkillExecution.roundPointer != r.fromSkillExecution.roundPointer ? l.fromSkillExecution.roundPointer.CompareTo(r.fromSkillExecution.roundPointer) :
                        vm.rand.Next(3) - 1
                    );
                    MetaEffect metaEffect = vm.effectsByRound[vm.currentRoundPointer][metaEffectPointer];
                    if (!executedMetaEffectsForThisSkill.Contains(metaEffect) &&
                        !executedMetaEffectsForThisTarget.Contains(metaEffect) && metaEffect.TriggeringCondition(validNumericEffect, vm))
                    {
                        // DO NOT executedMetaEffectsForThisSkill.Add(metaEffect);
                        executedMetaEffectsForThisTarget.Add(metaEffect);
                        NumericEffect? newNumericEffect = metaEffect.Execute(validNumericEffect, vm);
                        if (newNumericEffect != null)
                        {
                            validNumericEffect = newNumericEffect;
                            vm.numericEffectsForSingleSkillExecution[numericEffectPointer] = newNumericEffect;
                        }
                        else
                        {
                            vm.numericEffectsForSingleSkillExecution.Remove(validNumericEffect);
                            --numericEffectPointer;
                            break;
                        }
                    }
                }
                string log = skill.WriteLogForEffect(validNumericEffect, vm);
                vm.rootController.logCollector.Log(LogCollector.LogType.Skill, log);
            }

            vm.rootController.logCollector.Log(LogCollector.LogType.Skill, skill.WriteLogAtEnd(vm));

            return vm.numericEffectsForSingleSkillExecution;
        }
    }

    public class DummyVMSkillOnGameStart : Skill
    {
        public new ushort priority = ushort.MinValue;
        public DummyVMSkillOnGameStart() : base(PRODUCTION_CONFIG.dummyVm) { }
        public override List<NumericEffect> Execute(SkillExecution skillExecution, SukaLambdaEngine vm, object[]? metaArgs) => new List<NumericEffect>();
        public new SkillExecution PlanUseSkill(Character fromCharacter, List<Character> plannedTargets, SukaLambdaEngine vm, object[]? metaArgs = null) => new(new DummyVMCharacter(), this, new Character[] { }, metaArgs);
        public override bool PlanUseSkill(string command, SukaLambdaEngine vm) { throw new InvalidOperationException("This is a dummy skill commanded by the game itself"); }
        public override string WriteLogAtStart(SukaLambdaEngine vm) => "Game started.";
        public override string WriteLogForEffect(NumericEffect effect, SukaLambdaEngine vm) => "";
        public override string WriteLogAtEnd(SukaLambdaEngine vm) => "";
    }
    public class DummyVMSkillOnRoundStart : Skill
    {
        public new ushort priority = ushort.MinValue;
        public DummyVMSkillOnRoundStart() : base(PRODUCTION_CONFIG.dummyVm) { }
        public override List<NumericEffect> Execute(SkillExecution skillExecution, SukaLambdaEngine vm, object[]? metaArgs) => new List<NumericEffect>();
        public new SkillExecution PlanUseSkill(Character fromCharacter, List<Character> plannedTargets, SukaLambdaEngine vm, object[]? metaArgs = null) => new(new DummyVMCharacter(), this, new Character[] { }, metaArgs);
        public override bool PlanUseSkill(string command, SukaLambdaEngine vm) { throw new InvalidOperationException("This is a dummy skill commanded by the game itself"); }
        public override string WriteLogAtStart(SukaLambdaEngine vm) => $"Round #{vm.currentRoundPointer} started.";
        public override string WriteLogForEffect(NumericEffect effect, SukaLambdaEngine vm) => "";
        public override string WriteLogAtEnd(SukaLambdaEngine vm) => "";
    }
    public class DummyVMSkillOnRoundEnd : Skill
    {
        public new ushort priority = ushort.MaxValue;
        public DummyVMSkillOnRoundEnd() : base(PRODUCTION_CONFIG.dummyVm) { }
        public override List<NumericEffect> Execute(SkillExecution skillExecution, SukaLambdaEngine vm, object[]? metaArgs) => new List<NumericEffect>();
        public new SkillExecution PlanUseSkill(Character fromCharacter, List<Character> plannedTargets, SukaLambdaEngine vm, object[]? metaArgs = null) => new(new DummyVMCharacter(), this, new Character[] { }, metaArgs);
        public override bool PlanUseSkill(string command, SukaLambdaEngine vm) { throw new InvalidOperationException("This is a dummy skill commanded by the game itself"); }
        public override string WriteLogAtStart(SukaLambdaEngine vm) => "";
        public override string WriteLogForEffect(NumericEffect effect, SukaLambdaEngine vm) => "";
        public override string WriteLogAtEnd(SukaLambdaEngine vm) => $"Round #{vm.currentRoundPointer} ended.";
    }
    public class DummyVMSkillOnGameEnd : Skill
    {
        public new ushort priority = ushort.MaxValue;
        public DummyVMSkillOnGameEnd() : base(PRODUCTION_CONFIG.dummyVm) { }
        public override List<NumericEffect> Execute(SkillExecution skillExecution, SukaLambdaEngine vm, object[]? metaArgs) => new List<NumericEffect>();
        public new SkillExecution PlanUseSkill(Character fromCharacter, List<Character> plannedTargets, SukaLambdaEngine vm, object[]? metaArgs = null) => new(PRODUCTION_CONFIG.dummyVm, this, new Character[] { }, metaArgs);
        public override bool PlanUseSkill(string command, SukaLambdaEngine vm) { throw new InvalidOperationException("This is a dummy skill commanded by the game itself"); }
        public override string WriteLogAtStart(SukaLambdaEngine vm) => "";
        public override string WriteLogForEffect(NumericEffect effect, SukaLambdaEngine vm) => "";
        public override string WriteLogAtEnd(SukaLambdaEngine vm) => $"Game ended in {vm.currentRoundPointer} rounds.";
    }
    /// <summary>
    /// Used for <see cref="MetaEffect"/> triggered by <see cref="MapBlock"/>
    /// </summary>
    public class DummyMapSkill : Skill
    {
        public DummyMapSkill() : base(PRODUCTION_CONFIG.dummyMap) { }
        public override List<NumericEffect> Execute(SkillExecution skillExecution, SukaLambdaEngine vm, object[]? metaArgs) => new List<NumericEffect>();
        public new SkillExecution PlanUseSkill(Character fromCharacter, List<Character> plannedTargets, SukaLambdaEngine vm, object[]? metaArgs = null) => new(PRODUCTION_CONFIG.dummyMap, this, new Character[] { }, metaArgs);
        public override bool PlanUseSkill(string command, SukaLambdaEngine vm) { throw new InvalidOperationException("This is a dummy skill commanded by the map itself"); }
        public override string WriteLogAtStart(SukaLambdaEngine vm) => "";
        public override string WriteLogForEffect(NumericEffect effect, SukaLambdaEngine vm) => "Skill from map.";
        public override string WriteLogAtEnd(SukaLambdaEngine vm) => "";
    }
    public class MoveSkill : Skill
    {
        ushort srcX, srcY;
        public MoveSkill(Character owner) : base(owner) { }
        public override List<NumericEffect> Execute(SkillExecution skillExecution, SukaLambdaEngine vm, object[]? metaArgs)
        {
            if (vm.map == null || metaArgs == null) return new List<NumericEffect>();
            vm.map.CharacterMove(skillExecution.fromCharacter,
                (Heading[])metaArgs, Enumerable.Repeat((ushort)1, metaArgs.Length).ToArray());
            return new List<NumericEffect>();
        }

        [InGameCommand("mv", @"^[↑↓←→NSWEnsweUDLRudlr]+$",
            "`/mv NNESWWW` for moving 2 blks up, 1 right, 1 down, 3 left")]
        public override bool PlanUseSkill(string commandBody, SukaLambdaEngine vm)
        {
            if (owner.altitude != Altitude.Surface) throw new NotImplementedException();
            if (vm.map == null) return false;
            Tuple<ushort, ushort>? position = vm.map.CharacterPosition(owner, out _);
            if (position == null) return false;
            srcX = position.Item1;
            srcY = position.Item2;
            List<Heading> plannedMove = new();
            foreach (char c in commandBody)
                switch (c)
                {
                    case '↑': case 'N': case 'n': case 'U': case 'u': plannedMove.Add(new Heading(HeadingDirection.Up)); break;
                    case '→': case 'E': case 'e': case 'R': case 'r': plannedMove.Add(new Heading(HeadingDirection.Right)); break;
                    case '↓': case 'S': case 's': case 'D': case 'd': plannedMove.Add(new Heading(HeadingDirection.Down)); break;
                    case '←': case 'W': case 'w': case 'L': case 'l': plannedMove.Add(new Heading(HeadingDirection.Left)); break;
                    default: break;
                }
            vm.RemoveSkillOfCharacterAndType(owner, this);
            vm.PrepareSkill(new SkillExecution(owner, this, new Character[] {}, plannedMove.ToArray()));
            return true;
        }

        public override string WriteLogAtStart(SukaLambdaEngine vm) => "";
        public override string WriteLogForEffect(NumericEffect effect, SukaLambdaEngine vm) => "";
        public override string WriteLogAtEnd(SukaLambdaEngine vm)
        {
            Tuple<ushort, ushort>? currentPosition = vm.map!.CharacterPositionIncludingRemoved(owner, out _);
            if (currentPosition == null) return "";
            return $"{owner.GetType().Name} {(char)('A'+srcX)}{srcY} -> {(char)('A'+currentPosition.Item1)}{currentPosition.Item2}";
        }
    }
}