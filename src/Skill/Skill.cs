using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace sukalambda
{
    public class SkillData
    {
        public Guid characterId { get; init; }
        public string skillClassName { get; init; }
    }

    public abstract class Skill : IRenderText
    {
        public Character owner;  // Maybe skills can be owned by one and executed from another?
        public uint rangeCommitted { get; private set; }
        public uint rangeTemporary { get; set; }
        public Skill(Character owner) { this.owner = owner; }

        /// <returns>null if the target is valid; A description for reason if the target is invalid</returns>
        public string? ReasonForInvalidTarget(Character fromCharacter, Character toCharacter, SukaLambdaEngine vm) => "";
        public HashSet<Character> ValidTargets(Character fromCharacter, SukaLambdaEngine vm) => new HashSet<Character>();
        public Character[] AutoSelectTargets(Character fromCharacter, SukaLambdaEngine vm) => new Character[0];
        public abstract SkillExecution PlanUseSkill(Character fromCharacter, List<Character> plannedTargets, SukaLambdaEngine vm, object[]? metaArgs = null);

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
        public abstract List<NumericEffect> Execute(SkillExecution skillExecution, SukaLambdaEngine vm, object[]? metaArgs=null);
        public string WriteFinalLog(NumericEffect effect, SukaLambdaEngine vm) => $"Fantastic logs from skill of !";
        /// <returns>Just the skill name in different languages</returns>
        public string RenderAsText(Language lang) => "A fantastic skill !";
    }

    /// <summary>
    /// <see cref="SkillExecution"/> is a plan to use a skill.
    /// Generated after a player decides to use a skill of a character, on a list of targets.
    /// </summary>
    public class SkillExecution
    {
        public Character fromCharacter { get; init; }  // Maybe skills can be owned by one and executed from another?
        public Skill skill { get; init; }
        public Character[] desiredTargets { get; init; }
        public uint roundPointer { get; set; }
        public object[]? metaArgs;
        public SkillExecution(Character fromCharacter, Skill skill, Character[] desiredTargets, object[]? metaArgs = null)
        {
            this.fromCharacter = fromCharacter;
            this.skill = skill;
            this.desiredTargets = desiredTargets;
            this.metaArgs = metaArgs;
        }
        public List<NumericEffect> Execute(SukaLambdaEngine vm)
        {
            vm.numericEffectsForSingleSkillExecution = skill.Execute(this, vm, metaArgs);
            HashSet<NumericEffect> executedNumericEffects = new();
            for (int numericEffectPointer=0; numericEffectPointer < vm.numericEffectsForSingleSkillExecution.Count; ++numericEffectPointer)
            {
                if (numericEffectPointer > PRODUCTION_CONFIG.MAX_NUMERIC_EFFECTS_IN_SINGLE_SKILL) throw new StackOverflowException("Too many NumericEffects! Probably too many targets.");
                if (executedNumericEffects.Contains(vm.numericEffectsForSingleSkillExecution[numericEffectPointer])) continue;
                executedNumericEffects.Add(vm.numericEffectsForSingleSkillExecution[numericEffectPointer]);
                // Search for all MetaEffects that will be triggered by this execution,
                // and sort meta effects by priority then by character speed.
                // Do not use foreach, because MetaEffect can add new MetaEffects.
                HashSet<MetaEffect> executedEffectsForThisTarget = new();
                vm.metaEffectsForSingleSkillExecution = new(vm.effectsByRound[vm.currentRoundPointer]);
                for (int metaEffectPointer = 0; metaEffectPointer < vm.metaEffectsForSingleSkillExecution.Count; ++metaEffectPointer)
                {
                    if (metaEffectPointer > PRODUCTION_CONFIG.MAX_META_EFFECTS_IN_SINGLE_NUMERIC_EFFECT) throw new StackOverflowException("Too many MetaEffects on a single NumericEffect!");
                    vm.effectsByRound[vm.currentRoundPointer].Sort((l, r) =>
                        l.priority != r.priority ? l.priority.CompareTo(r.priority) :
                        l.fromCharacter.statusTemporary.Speed != r.fromCharacter.statusTemporary.Speed ? l.fromCharacter.statusTemporary.Speed.CompareTo(r.fromCharacter.statusTemporary.Speed) :
                        l.fromSkillExecution.roundPointer != r.fromSkillExecution.roundPointer ? l.fromSkillExecution.roundPointer.CompareTo(r.fromSkillExecution.roundPointer) :
                        vm.rand.Next(3) - 1
                    );
                    MetaEffect metaEffect = vm.effectsByRound[vm.currentRoundPointer][metaEffectPointer];
                    if (!executedEffectsForThisTarget.Contains(metaEffect) && metaEffect.TriggeringCondition(vm.numericEffectsForSingleSkillExecution[numericEffectPointer], vm))
                    {
                        executedEffectsForThisTarget.Add(metaEffect);
                        vm.numericEffectsForSingleSkillExecution[numericEffectPointer] = metaEffect.Execute(vm.numericEffectsForSingleSkillExecution[numericEffectPointer], vm);
                    }
                }
                skill.WriteFinalLog(vm.numericEffectsForSingleSkillExecution[numericEffectPointer], vm);
            }
            return vm.numericEffectsForSingleSkillExecution;
        }
    }

    public class DummyVMSkillOnGameStart : Skill
    {
        public DummyVMSkillOnGameStart(Character owner) : base(owner) { }
        public override List<NumericEffect> Execute(SkillExecution skillExecution, SukaLambdaEngine vm, object[]? metaArgs) => new List<NumericEffect>();
        public override SkillExecution PlanUseSkill(Character fromCharacter, List<Character> plannedTargets, SukaLambdaEngine vm, object[]? metaArgs = null) => new(new DummyVMCharacter(), this, new Character[] { }, metaArgs);
    }
    public class DummyVMSkillOnRoundStart : Skill
    {
        public DummyVMSkillOnRoundStart(Character owner) : base(owner) { }
        public override List<NumericEffect> Execute(SkillExecution skillExecution, SukaLambdaEngine vm, object[]? metaArgs) => new List<NumericEffect>();
        public override SkillExecution PlanUseSkill(Character fromCharacter, List<Character> plannedTargets, SukaLambdaEngine vm, object[]? metaArgs = null) => new(new DummyVMCharacter(), this, new Character[] { }, metaArgs);
    }
    public class DummyVMSkillOnRoundEnd : Skill
    {
        public DummyVMSkillOnRoundEnd(Character owner) : base(owner) { }
        public override List<NumericEffect> Execute(SkillExecution skillExecution, SukaLambdaEngine vm, object[]? metaArgs) => new List<NumericEffect>();
        public override SkillExecution PlanUseSkill(Character fromCharacter, List<Character> plannedTargets, SukaLambdaEngine vm, object[]? metaArgs = null) => new(new DummyVMCharacter(), this, new Character[] { }, metaArgs);
    }
    public class DummyVMSkillOnGameEnd : Skill
    {
        public DummyVMSkillOnGameEnd(Character owner) : base(owner) { }
        public override List<NumericEffect> Execute(SkillExecution skillExecution, SukaLambdaEngine vm, object[]? metaArgs) => new List<NumericEffect>();
        public override SkillExecution PlanUseSkill(Character fromCharacter, List<Character> plannedTargets, SukaLambdaEngine vm, object[]? metaArgs = null) => new(new DummyVMCharacter(), this, new Character[] { }, metaArgs);
    }
    /// <summary>
    /// Used for <see cref="MetaEffect"/> triggered by <see cref="MapBlock"/>
    /// </summary>
    public class DummyMapSkill : Skill
    {
        public DummyMapSkill(Character owner) : base(owner) { }
        public override List<NumericEffect> Execute(SkillExecution skillExecution, SukaLambdaEngine vm, object[]? metaArgs) => new List<NumericEffect>();
        public override SkillExecution PlanUseSkill(Character fromCharacter, List<Character> plannedTargets, SukaLambdaEngine vm, object[]? metaArgs = null) => new(new DummyMapCharacter(), this, new Character[] { }, metaArgs);
    }
    public class MoveSkill : Skill
    {
        public MoveSkill(Character owner) : base(owner) { }
        public override List<NumericEffect> Execute(SkillExecution skillExecution, SukaLambdaEngine vm, object[]? metaArgs) => new List<NumericEffect>();
        public override SkillExecution PlanUseSkill(Character fromCharacter, List<Character> plannedTargets, SukaLambdaEngine vm, object[]? metaArgs = null) => new(fromCharacter, this, new Character[] { }, metaArgs);
    }
}
