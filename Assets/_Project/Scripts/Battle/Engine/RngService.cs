namespace MurimRunaway.Battle.Engine
{
    public sealed class RngService : IRngService
    {
        private System.Random _random = new(0);
        public void Reseed(int seed) => _random = new System.Random(seed);
        public float NextFloat01() => (float)_random.NextDouble();
    }
}
