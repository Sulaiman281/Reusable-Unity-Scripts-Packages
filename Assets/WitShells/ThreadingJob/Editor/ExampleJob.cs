namespace WitShells.ThreadingJob
{
    public class ExampleJob : ThreadJob<int>
    {
        private int _input;
        public ExampleJob(int input)
        {
            _input = input;
        }
        public override int Execute()
        {
            // Simulate work
            System.Threading.Thread.Sleep(1000);
            return _input * 2;
        }
    }
}
