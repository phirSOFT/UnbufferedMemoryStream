using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NUnit.Tests1
{
    [TestFixture]

    public class UnbufferedMemoryStreamTest

    {
        [Test]
        [Timeout(1000)]
        public void TestSequentialReadWritre([Random(1, 1024, 10)] int length, [Random(10)] int seed)
        {
            var rnd = new Random(seed);
            var buffer = new byte[length];

            for (var i = 0; i < length; i++)
            {
                buffer[i] = (byte)rnd.Next(0, 255);
            }

            var output = new byte[length];

            using (var stream = new UnbufferedMemoryStream.UnbufferedMemoryStream())
            {
                stream.Write(buffer, 0, length);
                stream.Read(output, 0, length);
            }

            CollectionAssert.AreEquivalent(buffer, output);
        }

        [Test]
        [Timeout(10000)]
        public void TestSequentialReadWritre([Random(1024, 1024 * 1024, 100)] int length)
        {
            byte input = 0;
            byte output = 0;

            int generatedBytes = 0;
            int readBytesTotal = 0;

            var rnd = new Random(length);



            using (var stream = new UnbufferedMemoryStream.UnbufferedMemoryStream())
            {
                while (generatedBytes < length)
                {
                    var bufferSize = rnd.Next(1, 1024);
                    var buffer = new byte[bufferSize];
                    for (var i = 0; i < bufferSize; i++)
                    {
                        buffer[i] = input++;
                    }

                    generatedBytes += bufferSize;

                    stream.Write(buffer, 0, bufferSize);

                    bufferSize = rnd.Next(1, 1024);
                    buffer = new byte[bufferSize];
                    var read = stream.Read(buffer, 0, bufferSize);

                    for (int i = 0; i < read; i++)
                    {
                        Assert.AreEqual(output++, buffer[i]);
                    }

                    readBytesTotal += read;
                }

                while (generatedBytes > readBytesTotal)
                {
                    var bufferSize = rnd.Next(1, Math.Min(1024, generatedBytes - readBytesTotal));
                    var buffer = new byte[bufferSize];
                    var read = stream.Read(buffer, 0, bufferSize);

                    for (int i = 0; i < read; i++)
                    {
                        Assert.AreEqual(output++, buffer[i]);
                    }

                    readBytesTotal += read;
                }

            }

        }

        [Test]
        [Timeout(100)]
        public async Task TestConcurrent()
        {
            var output = new byte[10];
            Task<int> readTask;
            using (var s = new UnbufferedMemoryStream.UnbufferedMemoryStream())
            {
                readTask = s.ReadAsync(output, 0, 10);

                s.WriteByte(10);
            }

            var read = await readTask;

            Assert.AreEqual(1, read);
            Assert.AreEqual(10, output[0]);
        }

        [Test]
        public void TestBlocking()
        {
            int rc = 0;
            var s = new UnbufferedMemoryStream.UnbufferedMemoryStream();


            var t1 = Task.Run(() =>
            {
                s.ReadByte();
                if (Interlocked.Increment(ref rc) != 2)
                    Assert.Fail();
            });
            
            var t2 = Task.Run(async () =>
            {
                await Task.Delay(100);
                s.WriteByte(10);
                if (Interlocked.Increment(ref rc) != 1)
                    Assert.Fail();
            });


            Task.WaitAll(t1, t2);
            s.Dispose();


        }
    }
}
