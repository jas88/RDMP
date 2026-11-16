// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;

namespace Rdmp.Core.DataLoad.Modules.DataFlowSources;

/// <summary>
/// Determines whether an Excel workbook uses the 1904 date system (serial 0 is 1904-01-01) rather than the default
/// 1900 system.  ExcelDataReader applies this itself to cells it recognises as dates but does not expose it, so it is read
/// directly: the workbookPr date1904 attribute for .xlsx and the DATEMODE record for .xls.
/// </summary>
public static class ExcelDateSystem
{
    private static readonly byte[] ZipSignature = { 0x50, 0x4B, 0x03, 0x04 };
    private static readonly byte[] CompoundFileSignature = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

    private const ushort BiffDateModeRecord = 0x0022;
    private const ushort BiffEofRecord = 0x000A;

    /// <summary>
    /// Returns true if the workbook uses the 1904 date system.  Returns false for the 1900 system, for formats it cannot
    /// inspect (e.g. .xlsb) and if the flag cannot be read.
    /// </summary>
    public static bool IsDate1904(Stream stream)
    {
        var start = stream.Position;
        try
        {
            var header = new byte[CompoundFileSignature.Length];
            var read = stream.Read(header, 0, header.Length);
            stream.Position = start;

            if (read >= ZipSignature.Length && header.Take(ZipSignature.Length).SequenceEqual(ZipSignature))
                return IsXlsxDate1904(stream);

            if (read == header.Length && header.SequenceEqual(CompoundFileSignature))
                return IsXlsDate1904(stream);

            return false;
        }
        catch (Exception e) when (e is InvalidDataException or IOException or XmlException or
                                      ArgumentOutOfRangeException or IndexOutOfRangeException)
        {
            // Not something we can inspect; the reader will report genuinely corrupt files itself
            return false;
        }
        finally
        {
            stream.Position = start;
        }
    }

    private static bool IsXlsxDate1904(Stream stream)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, true);
        var entry = zip.GetEntry("xl/workbook.xml") ??
                    zip.Entries.FirstOrDefault(e =>
                        e.FullName.EndsWith("/workbook.xml", StringComparison.OrdinalIgnoreCase));
        if (entry == null)
            return false;

        using var xml = XmlReader.Create(entry.Open(),
            new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        while (xml.Read())
        {
            if (xml.NodeType != XmlNodeType.Element || xml.LocalName != "workbookPr")
                continue;

            var value = xml.GetAttribute("date1904");
            return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool IsXlsDate1904(Stream stream)
    {
        var workbook = CompoundFile.ReadStream(stream, "Workbook") ?? CompoundFile.ReadStream(stream, "Book");
        if (workbook == null)
            return false;

        // Scan the workbook globals substream (ends at the first EOF record) for DATEMODE
        var offset = 0;
        while (offset + 4 <= workbook.Length)
        {
            var type = BitConverter.ToUInt16(workbook, offset);
            var length = BitConverter.ToUInt16(workbook, offset + 2);
            offset += 4;

            if (type == BiffEofRecord)
                return false;

            if (type == BiffDateModeRecord && length >= 2 && offset + 2 <= workbook.Length)
                return BitConverter.ToUInt16(workbook, offset) == 1;

            offset += length;
        }

        return false;
    }

    /// <summary>
    /// Minimal reader for the OLE2 compound file container used by .xls, sufficient to extract a named top level stream.
    /// </summary>
    private static class CompoundFile
    {
        private const uint EndOfChain = 0xFFFFFFFE;
        private const uint FreeSector = 0xFFFFFFFF;
        private const int DirectoryEntrySize = 128;

        public static byte[] ReadStream(Stream stream, string name)
        {
            stream.Position = 0;
            var header = ReadExactly(stream, 512);
            var sectorSize = 1 << BitConverter.ToUInt16(header, 0x1E);
            var miniSectorSize = 1 << BitConverter.ToUInt16(header, 0x20);
            var fatSectorCount = BitConverter.ToUInt32(header, 0x2C);
            var firstDirectorySector = BitConverter.ToUInt32(header, 0x30);
            var miniStreamCutoff = BitConverter.ToUInt32(header, 0x38);
            var firstMiniFatSector = BitConverter.ToUInt32(header, 0x3C);
            var firstDifatSector = BitConverter.ToUInt32(header, 0x44);

            var fat = ReadFat(stream, header, sectorSize, fatSectorCount, firstDifatSector);
            var directory = ReadChain(stream, fat, firstDirectorySector, sectorSize);

            uint? rootStart = null;
            long rootSize = 0;
            for (var offset = 0; offset + DirectoryEntrySize <= directory.Length; offset += DirectoryEntrySize)
            {
                var type = directory[offset + 0x42];
                var start = BitConverter.ToUInt32(directory, offset + 0x74);
                var size = (long)BitConverter.ToUInt32(directory, offset + 0x78);

                if (type == 5) // root entry, owns the mini stream
                {
                    rootStart = start;
                    rootSize = size;
                    continue;
                }

                if (type != 2 || !string.Equals(EntryName(directory, offset), name, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (size >= miniStreamCutoff)
                    return Truncate(ReadChain(stream, fat, start, sectorSize), size);

                if (rootStart == null)
                    return null;

                var miniFat = ToUInt32s(ReadChain(stream, fat, firstMiniFatSector, sectorSize));
                var miniStream = Truncate(ReadChain(stream, fat, rootStart.Value, sectorSize), rootSize);
                return Truncate(ReadMiniChain(miniStream, miniFat, start, miniSectorSize), size);
            }

            return null;
        }

        private static string EntryName(byte[] directory, int offset)
        {
            var nameLength = BitConverter.ToUInt16(directory, offset + 0x40);
            return nameLength < 2 ? "" : Encoding.Unicode.GetString(directory, offset, Math.Min((int)nameLength, 64) - 2);
        }

        private static uint[] ReadFat(Stream stream, byte[] header, int sectorSize, uint fatSectorCount,
            uint firstDifatSector)
        {
            var fatSectors = new List<uint>();
            for (var i = 0; i < 109 && fatSectors.Count < fatSectorCount; i++)
                fatSectors.Add(BitConverter.ToUInt32(header, 0x4C + i * 4));

            var difatSector = firstDifatSector;
            var guard = 0;
            while (fatSectors.Count < fatSectorCount && difatSector != EndOfChain && difatSector != FreeSector &&
                   guard++ < 1_000_000)
            {
                var difat = ReadSector(stream, difatSector, sectorSize);
                var entries = sectorSize / 4 - 1;
                for (var i = 0; i < entries && fatSectors.Count < fatSectorCount; i++)
                    fatSectors.Add(BitConverter.ToUInt32(difat, i * 4));
                difatSector = BitConverter.ToUInt32(difat, entries * 4);
            }

            using var fat = new MemoryStream();
            foreach (var sector in fatSectors)
                fat.Write(ReadSector(stream, sector, sectorSize));

            return ToUInt32s(fat.ToArray());
        }

        private static byte[] ReadChain(Stream stream, uint[] fat, uint start, int sectorSize)
        {
            using var result = new MemoryStream();
            var visited = new HashSet<uint>();
            for (var sector = start; sector < fat.Length && visited.Add(sector); sector = fat[sector])
                result.Write(ReadSector(stream, sector, sectorSize));

            return result.ToArray();
        }

        private static byte[] ReadMiniChain(byte[] miniStream, uint[] miniFat, uint start, int miniSectorSize)
        {
            using var result = new MemoryStream();
            var visited = new HashSet<uint>();
            for (var sector = start; sector < miniFat.Length && visited.Add(sector); sector = miniFat[sector])
            {
                var offset = (long)sector * miniSectorSize;
                if (offset + miniSectorSize > miniStream.Length)
                    break;
                result.Write(miniStream, (int)offset, miniSectorSize);
            }

            return result.ToArray();
        }

        private static byte[] ReadSector(Stream stream, uint sector, int sectorSize)
        {
            stream.Position = (sector + 1L) * sectorSize;
            return ReadExactly(stream, sectorSize);
        }

        private static byte[] ReadExactly(Stream stream, int count)
        {
            var buffer = new byte[count];
            stream.ReadExactly(buffer);
            return buffer;
        }

        private static uint[] ToUInt32s(byte[] bytes)
        {
            var result = new uint[bytes.Length / 4];
            Buffer.BlockCopy(bytes, 0, result, 0, result.Length * 4);
            return result;
        }

        private static byte[] Truncate(byte[] bytes, long size) =>
            size >= bytes.Length ? bytes : bytes[..(int)size];
    }
}
