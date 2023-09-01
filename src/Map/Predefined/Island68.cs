namespace sukalambda
{
    public class Island68 : Map
    {
        [OutGameCommand("i68", "i68", "Start game on island 68")]
        public static bool Start(string account, string commandBody, RootController controller)
        {
            if (controller.vm != null)  return false;
            Map map = new Island68($"file:{nameof(Island68)}?mode=memory&cache=shared");
            Lakhesh lakhesh = new Lakhesh(account);
            GetWater getWater = new GetWater(lakhesh);
            lakhesh.skills.Add(getWater);
            map.JudgeEndGame = (SukaLambdaEngine? vm) =>
            {
                // lose
                if (vm == null || vm.map == null) return true;
                Tuple<ushort, ushort>? position = vm.map.CharacterPosition(lakhesh, out _);
                if (position == null)  return true;

                // win
                if (map.blocks.TryGetValue(position, out MapBlock? b)
                 && b.GetType() == typeof(Warehouse) && getWater.hasWater)
                    return true;

                return false;
            };

            SukaLambdaEngine vm = new(controller, map: map);
            vm.AddCharacter(lakhesh, 0, 0, new Heading(HeadingDirection.E));
            return true;
        }

        public class GetWater : Skill
        {
            public bool hasWater = false;
            public bool executionSuccess = false;
            public GetWater(Character owner) : base(owner) { }

            public override List<NumericEffect> Execute(SkillExecution skillExecution, SukaLambdaEngine vm, object[]? metaArgs = null)
            {
                List<NumericEffect> result = new();
                if (vm.map == null || hasWater) return result;
                Tuple<ushort, ushort>? position = vm.map.CharacterPosition(owner, out _);
                if (position == null) return result;
                MapBlock? b;
                foreach (Tuple<ushort, ushort> coordinate in vm.map.AllCoordinatesWithinManhattanDistance(position, 1))
                    if (vm.map.blocks.TryGetValue(coordinate, out b)
                      && b.GetType() == typeof(Water))
                    {
                        hasWater = true;
                        executionSuccess = true;
                        owner.statusCommitted.Mobility -= (long)Math.Abs(owner.statusCommitted.Mobility * 0.4);
                        break;
                    }
                return result;
            }

            [InGameCommand("water", ".*", "Get water within distance 1; Mobility -40%")]
            public override bool PlanUseSkill(string commandBody, SukaLambdaEngine vm)
            {
                if (hasWater || vm.map?.GetType() != typeof(Island68)) return false;
                Tuple<ushort, ushort>? position = vm.map.CharacterPosition(owner, out _);
                if (position == null) return false;
                vm.PrepareSkill(new SkillExecution(owner, this, new Character[] { }, null));
                return true;
            }

            public override string WriteLogAtStart(SukaLambdaEngine vm) => "";
            public override string WriteLogAtEnd(SukaLambdaEngine vm)
            {
                if (executionSuccess)
                {
                    executionSuccess = false;
                    return "水を得る！";
                }
                return "";
            }
            public override string WriteLogForEffect(NumericEffect effect, SukaLambdaEngine vm) => "";
        }
        public Island68(string databasePath, SukaLambdaEngine? vm = null) : base(databasePath, 1, 1, vm)
        {
            string generator = """
            仓仓草草口口口口口口森森
            仓仓草草口口口口口口口口
            草草森森森口口森口森森森
            口森森森森口口森口森森森
            口口口口口口口森口森森森

            森森森森口森森森口水水水
            森森口口口森森森口水森森
            森森口森森口口森口口口口
            森森口口口口口口口水森森
            水水森水口水水水水水森森
            
            水水水水口森森水森森森森
            森森口口口森森水水水水水
            森森口森森森口口口森森森
            森森口口口口口口井森森森
            森森森森森森森口口口口口
            """;
            string[] rows = generator.Replace("\r", "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
            this.height = (ushort)rows.Count();
            this.width = (ushort)rows[0].Count();

            for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
                for (int columnIndex = 0; columnIndex < rows[rowIndex].Length; columnIndex++)
                    switch (rows[rowIndex][columnIndex])
                    {
                        case '仓':  InsertMapBlock(new Warehouse((ushort)columnIndex, (ushort)rowIndex)); break;
                        case '森':  InsertMapBlock(new Forest((ushort)columnIndex, (ushort)rowIndex)); break;
                        case '草':  InsertMapBlock(new Lawn((ushort)columnIndex, (ushort)rowIndex)); break;
                        case '水':  InsertMapBlock(new Water((ushort)columnIndex, (ushort)rowIndex)); break;
                        // TODO: 井
                        case '口':  default: break;
                    }
            ;
        }
    }

    public class Warehouse : MapBlock
    {
        public new Dictionary<Altitude, ushort> mobilityCost = new() { { Altitude.Surface, 0 } };
        public Warehouse(ushort x, ushort y, SukaLambdaEngine? vm = null) : base(x, y, vm) { }
        public new string RenderAsText(Language lang) => "仓";
    }

    public class Forest : MapBlock
    {
        public new Dictionary<Altitude, ushort> mobilityCost = new() { {Altitude.Surface, 3} };
        public Forest(ushort x, ushort y, SukaLambdaEngine? vm = null) : base(x, y, vm) { }
        public new string RenderAsText(Language lang) => "森林"[(x+y+((y*3<vm?.map?.height) ? 1 : 0)) % 2].ToString();
    }
    public class Lawn : MapBlock
    {
        public Lawn(ushort x, ushort y, SukaLambdaEngine? vm = null) : base(x, y, vm) { }
        public new string RenderAsText(Language lang) => "草";//"🌼🌻"[(x+1) % 2].ToString();
    }
    public class Water : MapBlock
    {
        public new bool AllowEntrancy(Character character,
            Heading?[] movements, ushort movementIndexEnteringThisBlock) => false;
        public Water(ushort x, ushort y, SukaLambdaEngine? vm = null) : base(x, y, vm) { }
        public new string RenderAsText(Language lang) => "水";
    }
}
