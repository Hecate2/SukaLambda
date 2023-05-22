using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sukalambda
{
    public enum Alignment  // 阵营
    {
        Red = 0,
        Blue = 1,
    }
    public class Character
    {
        public readonly string id;
        public Alignment alignment;
        public Character(string id, Alignment alignment)
        {
            this.id = id;
            this.alignment = alignment;
        }
    }
}
