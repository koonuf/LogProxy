using System.Collections.Generic;
using System.Text;
using LogProxy.Lib.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogProxy.Lib.Tests.Unit
{
    [TestClass]
    public class ChunkSearchBufferTests
    {
        [TestMethod]
        public void AddContentDataTest1()
        {
            var target = new ChunkSearchBuffer();
            var chunks = new List<string> 
            { 
                "5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n",
                "5\r\nabcdb\r\n6\r",
                "\nabcdba\r\n0",
                "\r\nDate:Sun, 06 Nov 1994 08:49:37 GMT\r\nContent-MD5:1B2M2Y8Asg",
                "TpgAmY7PhCfg==\r\n\r\n\r"
            };

            foreach (var chunk in chunks)
            {
                target.AddContentData(Encoding.ASCII.GetBytes(chunk));
            }

            Assert.IsTrue(target.FinishedLoading);
            Assert.AreEqual(1, target.ContentOffset);
        }

        [TestMethod]
        public void AddContentDataTest2()
        {
            var target = new ChunkSearchBuffer();
            var chunks = new List<string> 
            { 
                "8\r\nabcdb",
                "abc\r\n2\r\nab\r\n0\r\n\r\nab"
            };

            foreach (var chunk in chunks)
            {
                target.AddContentData(Encoding.ASCII.GetBytes(chunk));
            }

            Assert.IsTrue(target.FinishedLoading);
            Assert.AreEqual(2, target.ContentOffset);
        }

        [TestMethod]
        public void AddContentDataTest3()
        {
            var target = new ChunkSearchBuffer();
            var chunks = new List<string> 
            { 
                "8\r\nabcdbabc\r\n",
                "2\r\nab\r\n0\r\n\r\n"
            };

            foreach (var chunk in chunks)
            {
                target.AddContentData(Encoding.ASCII.GetBytes(chunk));
            }

            Assert.IsTrue(target.FinishedLoading);
            Assert.AreEqual(0, target.ContentOffset);
        }

        [TestMethod]
        public void AddContentDataTest4()
        {
            var target = new ChunkSearchBuffer();
            var chunks = new List<string> 
            { 
                "8\r\nabcdb",
                "abc\r\n14\r\nabxs",
                "abcsbsabcsbs",
                "abcs\r\n",
                "0\r\n\r\nbasdf"
            };

            foreach (var chunk in chunks)
            {
                target.AddContentData(Encoding.ASCII.GetBytes(chunk));
            }

            Assert.IsTrue(target.FinishedLoading);
            Assert.AreEqual(5, target.ContentOffset);
        }

        [TestMethod]
        public void AddContentDataTest5()
        {
            var target = new ChunkSearchBuffer();
            var chunks = new List<string> 
            { 
                "8\r\nabcdbabc\r",
                "\n14\r\nabxs",
                "abcsbsabcsbs",
                "abcs\r",
                "\n5\r\nabceb\r\n1\r\na\r\n",
                "0\r\nDate:Sun, 06 Nov 1994 08:49:37 GMT\r\nContent-MD5:1B2M2Y8AsgTpgAmY7PhCfg==\r\n\r\nabcde"
            };

            foreach (var chunk in chunks)
            {
                target.AddContentData(Encoding.ASCII.GetBytes(chunk));
            }

            Assert.IsTrue(target.FinishedLoading);
            Assert.AreEqual(5, target.ContentOffset);
        }

        [TestMethod]
        public void AddContentDataTest6()
        {
            var target = new ChunkSearchBuffer();
            var chunks = new List<string> 
            { 
                @"cc
<Extent xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <left>0.5</left>
  <bottom>-1.5</bottom>
  <right>-1.5</right>
  <top>0.5</top>
</Extent>
0

"
            };

            foreach (var chunk in chunks)
            {
                target.AddContentData(Encoding.ASCII.GetBytes(chunk));
            }

            Assert.IsTrue(target.FinishedLoading);
            Assert.AreEqual(0, target.ContentOffset);
        }

        [TestMethod]
        public void AddContentDataTest7()
        {
            var target = new ChunkSearchBuffer();
            var chunks = new List<string> 
            { 
                "cc\r\n",
                @"<Extent xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <left>0.5</left>
  <bottom>-1.5</bottom>
  <right>-1.5</right>
  <top>0.5</top>
</Extent>",
                "\r\n",
                "0\r\n",
                "\r\n12345"
            };

            foreach (var chunk in chunks)
            {
                target.AddContentData(Encoding.ASCII.GetBytes(chunk));
            }

            Assert.IsTrue(target.FinishedLoading);
            Assert.AreEqual(5, target.ContentOffset);
        }
    }
}
