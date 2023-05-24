namespace sukalambda
{
    public abstract class Skill
    {
        public Skill() { }

        /// <returns>null if the target is valid; A description for reason if the target is invalid</returns>
        public abstract string? ReasonForInvalidTarget(Character fromCharacter, Character toCharacter, SukaLambdaEngine vm);
        public abstract HashSet<Character> ValidTargets(Character fromCharacter, SukaLambdaEngine vm);
        public abstract Character[] AutoSelectTargets(Character fromCharacter, SukaLambdaEngine vm);

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
        /// <param name="fromCharacter"></param>
        /// <param name="target"></param>
        /// <param name="vm"></param>
        /// <param name="metaArgs"></param>
        /// <returns>The planned numeric effects. The length can be shorter than <see cref="SkillExecution.desiredTargets"/></returns>
        public abstract List<NumericEffect> Execute(SkillExecution skillExecution, SukaLambdaEngine vm, object[] metaArgs);
        public abstract string WriteFinalLog(NumericEffect effect, SukaLambdaEngine vm);
    }

    /// <summary>
    /// <see cref="SkillExecution"/> is a plan to use a skill.
    /// Generated after a player decides to use a skill of a character, on a list of targets.
    /// </summary>
    public class SkillExecution
    {
        public Character fromCharacter { get; init; }
        public Skill skill { get; init; }
        public Character[] desiredTargets { get; init; }
        public uint roundPointer { get; set; }
        public object[] metaArgs;
        public SkillExecution(Character fromCharacter, Skill skill, Character[] desiredTargets, object[] metaArgs)
        {
            this.fromCharacter = fromCharacter;
            this.skill = skill;
            this.desiredTargets = desiredTargets;
            this.metaArgs = metaArgs;
        }
        public List<NumericEffect> Execute(SukaLambdaEngine vm)
        {
            List<NumericEffect> numericEffects = skill.Execute(this, vm, metaArgs);
            for (int i=0; i<numericEffects.Count; ++i)
            {
                // Search for all MetaEffects that will be triggered by this execution,
                // and sort meta effects by priority then by character speed.
                // Do not use foreach, because MetaEffect can add new MetaEffects.
                int effectPointer = 0;
                HashSet<MetaEffect> executedEffectsForThisTarget = new();
                while (effectPointer < vm.effectsByRound[vm.currentRoundPointer].Count)
                {
                    vm.effectsByRound[vm.currentRoundPointer].Sort((l, r) =>
                        l.priority != r.priority ? l.priority.CompareTo(r.priority) :
                        l.fromCharacter?.speed != r.fromCharacter?.speed ? l.fromCharacter.speed.CompareTo(r.fromCharacter.speed) :
                        l.fromSkillExecution.roundPointer.CompareTo(r.fromSkillExecution.roundPointer)
                    );
                    MetaEffect metaEffect = vm.effectsByRound[vm.currentRoundPointer][effectPointer];
                    if (!executedEffectsForThisTarget.Contains(metaEffect) && metaEffect.triggeringCondition(numericEffects[i], vm))
                    {
                        executedEffectsForThisTarget.Add(metaEffect);
                        numericEffects[i] = metaEffect.execution(numericEffects[i], vm);
                    }
                    effectPointer++;
                }
                skill.WriteFinalLog(numericEffects[i], vm);
            }
            return numericEffects;
        }
    }
}
