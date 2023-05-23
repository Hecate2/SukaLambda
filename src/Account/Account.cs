using SQLite;

namespace sukalambda
{
    [Table("account")]
    public class Account
    {
        [PrimaryKey]
        [Column("account")]
        public string account { get; init; }

        [Column("nickname")]
        [Indexed]
        public string nickname { get; init; }

        public static void ChangeNickname(string account, string nickname) => CONFIG.conn.Update(new Account { account=account, nickname=nickname });
    }
}
