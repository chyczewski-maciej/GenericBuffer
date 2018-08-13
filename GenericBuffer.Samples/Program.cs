using GenericBuffer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenericBuffer.Samples
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var infiniteLoop = InfiniteLoop().GetEnumerator();
            var buffer = new AsyncGenericBuffer<int>(() =>
            {
                infiniteLoop.MoveNext();
                Console.WriteLine("New value: " + infiniteLoop.Current);
                return Task.FromResult(infiniteLoop.Current);
            }, bufferingPeriod: TimeSpan.FromSeconds(0.5));


            while (true)
            {
                Enumerable.Range(0, 10000).AsParallel().ForAll(x => buffer.ForceRefreshAsync().GetAwaiter().GetResult());
            }
        }

        public static IEnumerable<int> InfiniteLoop()
        {
            while (true)
                for (int i = 0; i < 10; i++)
                {
                    Console.WriteLine("Generated new values");
                    yield return i;
                }
        }
    }
}
