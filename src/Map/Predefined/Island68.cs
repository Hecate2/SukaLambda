namespace sukalambda
{
    public class Island68 : Map
    {
        public Island68(string databasePath, ushort width, ushort height, SukaLambdaEngine? vm = null) : base(databasePath, width, height, vm)
        {
        }
    }

    public class Forest : MapBlock
    {
        public Forest(ushort x, ushort y, SukaLambdaEngine? vm) : base(x, y, vm) { }

        public new string RenderAsText(Language lang) => "森";
    }
    public class Lawn : MapBlock
    {
        public Lawn(ushort x, ushort y, SukaLambdaEngine? vm) : base(x, y, vm) { }

        public new string RenderAsText(Language lang) => "草";
    }
}
