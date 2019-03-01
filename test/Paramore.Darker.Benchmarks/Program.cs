using System;
using BenchmarkDotNet.Running;

namespace Paramore.Darker.Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Benchmark>();
            Console.WriteLine(summary);
        }
    }
}