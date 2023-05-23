using SQLite;

namespace sukalambda
{
    public static class CONFIG
    {
        public const string DATABASE_PATH = "./SukaLambda.db3";
        public static SQLiteConnection conn = new(new SQLiteConnectionString(DATABASE_PATH));
    }

    class SkillVM
    {
        class SkillExecution
        {
            public Character character;
            public Skill skill;
            public SkillExecution(Character character, Skill skill)
            {
                this.character = character;
                this.skill = skill;
            }
        }

        public SkillVM(Map? map = null)
        {
            
        }

        public void AddCharacter(Character character)
        {

        }

        public void PrepareSkill(Character character)
        {

        }
    }
}