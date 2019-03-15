using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryTest
{
    class Program
    {
        private static Stream data;
        private static int cacheSize;

        public static async Task Main(string[] args)
        {
            data = new UnbufferedMemoryStream.UnbufferedMemoryStream();
            var cts = new CancellationTokenSource();
            var writer = Writer(cts.Token);
            var reader = Reader(cts.Token);

            Console.CancelKeyPress += (sender, eventArgs) => cts.Cancel();
            while (!cts.IsCancellationRequested)
            {
                await Task.Delay(1000, cts.Token);
                Console.WriteLine(cacheSize);
            }

            await writer;
            await reader;
        }

        private static async Task Writer(CancellationToken token)
        {
            var buffer = new byte[2048];

            while (!token.IsCancellationRequested)
            {
                await data.WriteAsync(buffer, 0, 2048, token);
                if(Interlocked.Add(ref cacheSize, 2048) > 1024L * 1024L)
                    await Task.Delay(1000, CancellationToken.None);
            }
        }

        private static async Task Reader(CancellationToken token)
        {
            var buffer = new byte[2048];

            while (!token.IsCancellationRequested)
            {
                var read = await data.ReadAsync(buffer, 0, 2048, token);
                Interlocked.Add(ref cacheSize, -read);
                await Task.Delay(100, token);
            }
        }
    }
}
