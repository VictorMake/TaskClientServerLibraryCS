using System;
using System.Threading;

namespace TaskClientServerLibrary
{
    public sealed class RandomProvider
    {
        private static int seed = Environment.TickCount;
        private static readonly ThreadLocal<Random> randomWrapper = new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));

        private RandomProvider()
        {
        }

        public static Random GetThreadRandom()
        {
            return randomWrapper.Value;
        }
    }
}
//Dim rnd As Random = RandomProvider.GetThreadRandom()
//Dim value As Integer = rnd.Next(0, 100)
