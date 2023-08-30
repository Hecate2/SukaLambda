namespace sukalambda
{
    public class Island68 : Map
    {
        [OutGameCommand("i68", "i68", "Start game on island 68")]
        public static bool Start(string account, string command, RootController controller)
        {
            if (controller.vm != null)  return false;
            SukaLambdaEngine vm = new(controller, map: new Island68($"file:{nameof(Island68)}?mode=memory&cache=shared"));
            vm.AddCharacter(new Lakhesh(account), 0, 0, new Heading(HeadingDirection.E));
            return true;
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
            string[] rows = generator.Split('\n', StringSplitOptions.RemoveEmptyEntries);
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
                        case '口':  default: break;
                    }
        }
    }

    public class Warehouse : MapBlock
    {
        public Warehouse(ushort x, ushort y, SukaLambdaEngine? vm = null) : base(x, y, vm) { }
        public new string RenderAsText(Language lang) => "🏠";
    }

    public class Forest : MapBlock
    {
        public Forest(ushort x, ushort y, SukaLambdaEngine? vm = null) : base(x, y, vm) { }
        public new string RenderAsText(Language lang) => "🌲🌳"[(x+y+((y*3<vm?.map?.height) ? 1 : 0)) % 2].ToString();
    }
    public class Lawn : MapBlock
    {
        public Lawn(ushort x, ushort y, SukaLambdaEngine? vm = null) : base(x, y, vm) { }
        public new string RenderAsText(Language lang) => "草";//"🌼🌻"[(x+1) % 2].ToString();
    }
    public class Water : MapBlock
    {
        public Water(ushort x, ushort y, SukaLambdaEngine? vm = null) : base(x, y, vm) { }
        public new string RenderAsText(Language lang) => "水";
    }
}
