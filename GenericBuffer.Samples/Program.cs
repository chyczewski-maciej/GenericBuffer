using GenericBuffer.Core;
using System;
using System.Threading.Tasks;

namespace GenericBuffer.Samples
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            int i = 0;
            var genericBuffer = new GenericBuffer<int>(
                factory: () => 
                {
                        Console.WriteLine("Generating new value...");
                        return i++;
                }, 
                bufferingPeriod: TimeSpan.FromMilliseconds(200));

            while(true)
            {
                Console.WriteLine($"Value: {genericBuffer.GetValue()}");
                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }

            // OUTPUT:
            //Generating new value...
            //Value: 0
            //Value: 0
            //Value: 0
            //Value: 0
            //Generating new value...
            //Value: 1
            //Value: 1
            //Value: 1
            //Value: 1
            //Generating new value...
            //Value: 2
            //Value: 2
            //Value: 2
            //Value: 2
            //Generating new value...
            //Value: 3
            //Value: 3
            //Value: 3
            //Value: 3
        }
    }
}
