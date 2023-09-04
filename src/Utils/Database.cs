using Microsoft.EntityFrameworkCore;

namespace sukalambda
{
    public class PersistenceDbContext : DbContext
    {
        public string dbPath { get; } = CONFIG.DATABASE_PATH;
        public DbSet<AccountData> Accounts { get; set; }
        public DbSet<CharacterData> Characters { get; set; }
        public DbSet<SkillData> Skills { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite($"Data Source={dbPath}");
        public PersistenceDbContext()
        {
            this.Database.EnsureCreated();  // works only when there is no table
        }
    }
    public class GameDbContext : DbContext
    {
        public string dbPath { get; init; }
        public DbSet<CharacterInMapData> characterInMap { get; set; }
        public GameDbContext(string dbPath)
        {
            this.dbPath = dbPath;
            this.Database.EnsureDeleted();
            this.Database.EnsureCreated();  // works only when there is no table
        }
        protected override void OnConfiguring(DbContextOptionsBuilder options) => options.UseSqlite($"Data Source={dbPath}");
    }
}
