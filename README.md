### A war-chess game skill engine, designed for bots to host MMO games in chat groups, with text only.

-  In a functional programming style -> Reduces coupling

  ```
  (Player Command) -> PrepareSkill -> SkillExecution
        -> NumericEffect -> MetaEffect(NumericEffect) -> NumericEffect
        -> Committed to character
  
  SukaLambdaEngine: [Round, Round, ...]
  Round: [SkillExecution, SkillExecution, ...]
  sortBy(priority).thenBy(characterSpeed).thenBy(...)       
  
  SkillExecution: [MetaEffect -> ... -> MetaEffect(NumericEffect) -> ... -> MetaEffect]
  sortBy(priority).thenBy(characterSpeed).thenBy(...)
  
  MetaEffect: intakes NumericEffect, and returns a modified NumericEffect
  NumericEffect: Can be committed to characters
  ```
  
- 2-D text-based Map + (potential) detection mechanism!

  ```
  # (Lakhesh菈 at B1)
  ＡＢＣＤＥＦＧＨＩＪＫＬ
  仓仓草草口口口口口口林森 0
  仓菈草草口口口口口口口口 1
  草草林森林口口森口森林森 2
  口林森林森口口林口林森林 3
  口口口口口口口森口森林森 4
  林森林森口森林森口水水水 5
  森林口口口林森林口水森林 6
  林森口森林口口森口口口口 7
  森林口口口口口口口水森林 8
  水水林水口水水水水水林森 9
  水水水水口林森水森林森林 10
  林森口口口森林水水水水水 11
  森林口林森林口口口林森林 12
  林森口口口口口口口森林森 13
  森林森林森林森口口口口口 14
  ```

  Happy playing electronic warfare.

- **Not thread-safe. Use your own locks!**