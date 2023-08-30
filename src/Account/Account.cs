using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sukalambda
{
    public class AccountData
    {
        [Key]
        public string account { get; set; }
        public string nickname { get; set; }
        [NotMapped]
        public List<Character> characters { get; } = new();
        public static void ChangeNickname(string account, string nickname)
        {
            AccountData acc = PRODUCTION_CONFIG.conn.Accounts.First(x => x.account == account);
            acc.nickname = nickname;
            PRODUCTION_CONFIG.conn.Update(acc);
            PRODUCTION_CONFIG.conn.SaveChanges();
        }
    }
}
