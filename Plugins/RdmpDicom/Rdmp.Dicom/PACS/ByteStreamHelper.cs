// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System.IO;

namespace Rdmp.Dicom.PACS;

public static class ByteStreamHelper
{
    /// <summary>
    /// Reads data from a stream until the end is reached. The
    /// data is returned as a byte array. An IOException is
    /// thrown if any of the underlying IO calls fail.
    /// </summary>
    /// <param name="stream">The stream to read data from</param>
    /// <exception cref="IOException"></exception>
    public static byte[] ReadFully(Stream stream)
    {
        var buffer = new byte[32768];
        long length;
        try
        {
            length = stream.Length;
        }
        catch
        {
            // Use default size of 32k
            length = 32768;
        }
        using var ms = new MemoryStream((int)length);
        while (true)
        {
            var read = stream.Read(buffer, 0, buffer.Length);
            if (read <= 0)
                return ms.ToArray();
            ms.Write(buffer, 0, read);
        }
    }
}