using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dinmore.Api.Helpers
{
    public static partial class Helpers
    {
        /// <summary>
        /// Converts a Stream into a byte array
        /// </summary>
        /// <param name="input">A Stream</param>
        /// <returns>A byte array from the input stream</returns>
        public static byte[] ReadFileStream(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
}
