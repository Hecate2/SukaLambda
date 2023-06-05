### A game skill engine!

-  In a functional programming style -> Reduces coupling

  ```
  Skill ->(Player Command)-> SkillExecution
        -> NumericEffect -> MetaEffect(NumericEffect) -> NumericEffect
        -> Committed to character
  
  SukaLambdaEngine: [Round, Round, ...]
  
  Round: [SkillExecution, SkillExecution, ...]
  sortBy(priority).thenBy(characterSpeed).thenBy(...)       
  
  SkillExecution: [MetaEffect -> ... -> SkillExecution -> ... -> MetaEffect]
  sortBy(priority).thenBy(characterSpeed).thenBy(...)
  
  MetaEffect: intakes NumericEffect, and returns a modified NumericEffect
  NumericEffect: Can be committed to characters
  ```

- 2-D text-based Map + detection mechanism!

  Happy playing electronic warfare.

- **Not thread-safe. Use your own locks!**