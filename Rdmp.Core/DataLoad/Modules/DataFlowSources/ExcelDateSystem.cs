// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Buffers.Binary;
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
    private const int BiffRecordHeaderSize = 4;

    /// <summary>
    /// Reads <paramref name="destination"/>.Length bytes starting <paramref name="offset"/> bytes into a stream within a
    /// compound file.  Returns false if the stream does not have them.
    /// </summary>
    private delegate bool ReadAt(long offset, Span<byte> destination);

    /// <summary>
    /// Returns true if the workbook uses the 1904 date system.  Returns false for the 1900 system, for formats it cannot
    /// inspect (e.g. .xlsb) and if the flag cannot be read.
    /// <para>The workbook is the whole of <paramref name="stream"/> (which must be seekable) wherever it is currently
    /// positioned and that position is restored before returning.  Only the parts of the file needed to find the flag
    /// are read.</para>
    /// </summary>
    public static bool IsDate1904(Stream stream)
    {
        var position = stream.Position;
        try
        {
            stream.Position = 0;
            var signature = new byte[CompoundFileSignature.Length];
            var read = stream.ReadAtLeast(signature, signature.Length, false);

            if (read >= ZipSignature.Length && signature.AsSpan(0, ZipSignature.Length).SequenceEqual(ZipSignature))
                return IsXlsxDate1904(stream);

            if (read == signature.Length && signature.AsSpan().SequenceEqual(CompoundFileSignature))
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
            stream.Position = position;
        }
    }

    private static bool IsXlsxDate1904(Stream stream)
    {
        // a zip archive is read from its end so this too is independent of where the stream is positioned
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, true);
        var entry = zip.GetEntry("xl/workbook.xml") ??
                    zip.Entries.FirstOrDefault(e =>
                        e.FullName.EndsWith("/workbook.xml", StringComparison.OrdinalIgnoreCase));
        if (entry == null)
            return false;

        using var entryStream = entry.Open();
        using var xml = XmlReader.Create(entryStream,
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
        var file = new CompoundFile(stream);

        // Excel 97 and later keep the workbook in a stream called Workbook, earlier versions in one called Book
        var workbook = file.OpenStream("Workbook", out var size) ?? file.OpenStream("Book", out size);

        return workbook != null && HasDate1904Record(workbook, size);
    }

    /// <summary>
    /// Looks for a DATEMODE record saying the workbook uses the 1904 date system.  A workbook stream is a sequence of
    /// records, each a 4 byte header (type then length of the body) followed by the body.  Only the headers are read, the
    /// bodies are skipped, and only as far as the first EOF record: that ends the records describing the workbook as a
    /// whole, which is where DATEMODE is, and the worksheets follow it.
    /// </summary>
    private static bool HasDate1904Record(ReadAt read, long size)
    {
        Span<byte> header = stackalloc byte[BiffRecordHeaderSize];

        for (long offset = 0; offset + BiffRecordHeaderSize <= size && read(offset, header);)
        {
            var type = BinaryPrimitives.ReadUInt16LittleEndian(header);
            var length = BinaryPrimitives.ReadUInt16LittleEndian(header[2..]);
            offset += BiffRecordHeaderSize;

            if (type == BiffEofRecord)
                return false;

            if (type == BiffDateModeRecord)
            {
                var body = header[..2];
                return length >= body.Length && offset + body.Length <= size && read(offset, body) &&
                       BinaryPrimitives.ReadUInt16LittleEndian(body) == 1;
            }

            offset += length;
        }

        return false;
    }

    /// <summary>
    /// Minimal reader for the compound file (OLE2) container used by .xls: a header then sectors of a fixed size which are
    /// linked into chains by the file allocation table (FAT), itself held in sectors.  A stream is a chain of sectors and a
    /// directory, also a chain, gives the name, first sector and size of each stream.
    /// </summary>
    private sealed class CompoundFile
    {
        private const uint EndOfChain = 0xFFFFFFFE;
        private const uint MaxRegularSector = 0xFFFFFFFA;
        private const int HeaderSize = 512;
        private const int HeaderFatSectors = 109;
        private const int DirectoryEntrySize = 128;
        private const byte StreamEntry = 2;
        private const byte RootEntry = 5;

        private readonly Stream _stream;
        private readonly int _sectorSize;
        private readonly int _miniSectorSize;
        private readonly uint _miniStreamCutoff;
        private readonly uint _firstDirectorySector;
        private readonly uint _firstMiniFatSector;

        /// <summary>Number of sectors in the file, no chain can have more than this</summary>
        private readonly long _sectorCount;

        /// <summary>The location of each sector of the FAT</summary>
        private readonly List<uint> _fatSectors = new();

        /// <summary>The sectors of the FAT read so far, by their position in the FAT</summary>
        private readonly Dictionary<int, uint[]> _fat = new();

        private byte[] _directory;

        public CompoundFile(Stream stream)
        {
            _stream = stream;

            var header = new byte[HeaderSize];
            stream.Position = 0;
            stream.ReadExactly(header);

            // 512 byte sectors (4096 in the rarely used version 4) and 64 byte mini sectors in every real file
            var sectorShift = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(0x1E));
            var miniSectorShift = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(0x20));
            if (sectorShift is < 9 or > 20 || miniSectorShift >= sectorShift)
                throw new InvalidDataException("Unsupported compound file sector size");

            _sectorSize = 1 << sectorShift;
            _miniSectorSize = 1 << miniSectorShift;
            _sectorCount = Math.Max(0, (stream.Length - 1) / _sectorSize); // the header takes the place of the first
            _firstDirectorySector = ReadUInt32(header, 0x30);
            _miniStreamCutoff = ReadUInt32(header, 0x38);
            _firstMiniFatSector = ReadUInt32(header, 0x3C);

            // The header lists the first sectors of the FAT.  A file of more than a few MB continues the list in a chain
            // of sectors, each ending with the location of the next.  The FAT cannot take up more sectors than the file
            // has, whatever the header claims, which bounds how long the list can grow for a corrupt file.
            var fatSectorCount = Math.Min(ReadUInt32(header, 0x2C), _sectorCount);
            for (var i = 0; i < HeaderFatSectors && _fatSectors.Count < fatSectorCount; i++)
                _fatSectors.Add(ReadUInt32(header, 0x4C + i * 4));

            var listSector = ReadUInt32(header, 0x44);
            var entriesPerSector = _sectorSize / 4 - 1;
            for (long guard = 0; _fatSectors.Count < fatSectorCount && IsSector(listSector) && guard < _sectorCount; guard++)
            {
                var list = ReadSector(listSector);
                for (var i = 0; i < entriesPerSector && _fatSectors.Count < fatSectorCount; i++)
                    _fatSectors.Add(ReadUInt32(list, i * 4));

                listSector = ReadUInt32(list, entriesPerSector * 4);
            }
        }

        /// <summary>
        /// Returns a reader for the first stream called <paramref name="name"/> (the one ExcelDataReader reads) and its
        /// <paramref name="size"/>, or null if there is no such stream.
        /// </summary>
        public ReadAt OpenStream(string name, out long size)
        {
            _directory ??= ReadChain(_firstDirectorySector);

            uint? miniStreamStart = null;
            long miniStreamSize = 0;
            size = 0;

            for (var offset = 0; offset + DirectoryEntrySize <= _directory.Length; offset += DirectoryEntrySize)
            {
                var type = _directory[offset + 0x42];
                var start = ReadUInt32(_directory, offset + 0x74);
                long entrySize = ReadUInt32(_directory, offset + 0x78);

                if (type == RootEntry) // always the first entry, its stream holds the mini stream
                {
                    miniStreamStart = start;
                    miniStreamSize = entrySize;
                    continue;
                }

                if (type != StreamEntry ||
                    !string.Equals(EntryName(_directory, offset), name, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (entrySize >= _miniStreamCutoff)
                {
                    size = entrySize;
                    return new SectorChain(this, start).TryRead;
                }

                if (miniStreamStart == null)
                    return null;

                // A stream of under 4096 bytes is stored in 64 byte mini sectors, which have their own FAT and are
                // themselves held in the root entry's stream.  All of that is a few KB at most so it is simply loaded.
                var miniFat = ToUInt32s(ReadChain(_firstMiniFatSector));
                var miniStream = Truncate(ReadChain(miniStreamStart.Value), miniStreamSize);
                var contents = Truncate(ReadMiniChain(miniStream, miniFat, start), entrySize);

                size = contents.Length;
                return (position, destination) =>
                {
                    if (position + destination.Length > contents.Length)
                        return false;

                    contents.AsSpan((int)position, destination.Length).CopyTo(destination);
                    return true;
                };
            }

            return null;
        }

        private static string EntryName(byte[] directory, int offset)
        {
            var nameLength = BinaryPrimitives.ReadUInt16LittleEndian(directory.AsSpan(offset + 0x40));
            return nameLength < 2 ? "" : Encoding.Unicode.GetString(directory, offset, Math.Min((int)nameLength, 64) - 2);
        }

        /// <summary>
        /// True if <paramref name="sector"/> is the number of a sector of the file rather than a marker such as the end of
        /// a chain
        /// </summary>
        private bool IsSector(uint sector) => sector <= MaxRegularSector && sector < _sectorCount;

        /// <summary>
        /// Returns the sector which follows <paramref name="sector"/> in its chain, reading the sector of the FAT which
        /// says so if it has not already been read.
        /// </summary>
        private uint Next(uint sector)
        {
            var entriesPerSector = _sectorSize / 4;
            var index = (int)(sector / entriesPerSector);
            if (index >= _fatSectors.Count || !IsSector(_fatSectors[index]))
                return EndOfChain;

            if (!_fat.TryGetValue(index, out var entries))
                _fat.Add(index, entries = ToUInt32s(ReadSector(_fatSectors[index])));

            return entries[sector % entriesPerSector];
        }

        /// <summary>
        /// Loads the whole of a chain of sectors (for the small structures: the directory and the mini stream and its FAT)
        /// </summary>
        private byte[] ReadChain(uint start)
        {
            using var result = new MemoryStream();
            var visited = new HashSet<uint>();
            for (var sector = start; IsSector(sector) && visited.Add(sector); sector = Next(sector))
                result.Write(ReadSector(sector));

            return result.ToArray();
        }

        private byte[] ReadMiniChain(byte[] miniStream, uint[] miniFat, uint start)
        {
            using var result = new MemoryStream();
            var visited = new HashSet<uint>();
            for (var sector = start; sector < miniFat.Length && visited.Add(sector); sector = miniFat[sector])
            {
                var offset = (long)sector * _miniSectorSize;
                if (offset + _miniSectorSize > miniStream.Length)
                    break;

                result.Write(miniStream, (int)offset, _miniSectorSize);
            }

            return result.ToArray();
        }

        private byte[] ReadSector(uint sector)
        {
            var buffer = new byte[_sectorSize];
            _stream.Position = (sector + 1L) * _sectorSize;
            _stream.ReadExactly(buffer);
            return buffer;
        }

        private static uint ReadUInt32(byte[] bytes, int offset) =>
            BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset));

        private static uint[] ToUInt32s(byte[] bytes)
        {
            var result = new uint[bytes.Length / 4];
            for (var i = 0; i < result.Length; i++)
                result[i] = ReadUInt32(bytes, i * 4);

            return result;
        }

        private static byte[] Truncate(byte[] bytes, long size) =>
            size >= bytes.Length ? bytes : bytes[..(int)size];

        /// <summary>
        /// A stream held as a chain of sectors, read without loading it: the chain is followed only as far as the offset
        /// asked for and only the bytes asked for are read from the file, so skipping over part of the stream costs
        /// nothing but looking up the sectors it occupies in the FAT.
        /// </summary>
        private sealed class SectorChain
        {
            private readonly CompoundFile _file;
            private readonly uint _start;

            /// <summary>The sector holding the part of the stream at offset <see cref="_index"/> x sector size</summary>
            private uint _sector;

            private long _index;
            private long _steps;

            public SectorChain(CompoundFile file, uint start)
            {
                _file = file;
                _start = _sector = start;
            }

            public bool TryRead(long offset, Span<byte> destination)
            {
                while (!destination.IsEmpty)
                {
                    // what is asked for can span sectors, which need not be next to each other in the file
                    if (!MoveTo(offset / _file._sectorSize))
                        return false;

                    var within = (int)(offset % _file._sectorSize);
                    var count = Math.Min(destination.Length, _file._sectorSize - within);

                    _file._stream.Position = (_sector + 1L) * _file._sectorSize + within;
                    _file._stream.ReadExactly(destination[..count]);

                    destination = destination[count..];
                    offset += count;
                }

                return true;
            }

            private bool MoveTo(long index)
            {
                if (index < _index)
                {
                    _sector = _start;
                    _index = 0;
                }

                for (; _index < index && _file.IsSector(_sector); _index++)
                {
                    // a chain longer than the file has sectors must loop back on itself
                    if (++_steps > _file._sectorCount)
                        return false;

                    _sector = _file.Next(_sector);
                }

                return _index == index && _file.IsSector(_sector);
            }
        }
    }
}
