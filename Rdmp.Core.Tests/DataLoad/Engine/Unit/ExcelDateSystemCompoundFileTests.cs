// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Linq;
using System.Text;
using ExcelDataReader;
using NUnit.Framework;
using Rdmp.Core.DataLoad.Modules.DataFlowSources;

namespace Rdmp.Core.Tests.DataLoad.Engine.Unit;

/// <summary>
/// Tests how <see cref="ExcelDateSystem"/> finds the date system of an .xls workbook, which is a compound file: a small
/// file system of 512 byte sectors chained together by a file allocation table (FAT).  The workbook is one stream in it,
/// made of records each with a 4 byte header (type and length of the body which follows).
/// </summary>
[TestFixture]
[Category("Unit")]
public class ExcelDateSystemCompoundFileTests
{
    public const string LargeDate1904XlsFile = "LargeDate1904.xls";

    private const int SectorSize = 512;
    private const uint EndOfChain = 0xFFFFFFFE;
    private const uint FatSector = 0xFFFFFFFD;
    private const uint Unused = 0xFFFFFFFF;

    private const ushort BofRecord = 0x0809;
    private const ushort DateModeRecord = 0x0022;
    private const ushort EofRecord = 0x000A;
    private const ushort UnknownRecord = 0x7FFF;

    [OneTimeSetUp]
    public void RegisterCodePages()
    {
        // ExcelDataReader needs the Windows code pages to open an .xls workbook
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// LargeDate1904.xls is a 1904 date system workbook of 4,000 rows written by Libre Office (converted from a
    /// <see cref="MinimalXlsxBuilder"/> workbook).  Its date system record is 226 bytes into a workbook stream of 313,770
    /// bytes, which is not stored contiguously, and its FAT has 5 sectors.
    /// </summary>
    [Test]
    public void LargeWorkbook_OnlyTheStartOfTheWorkbookIsRead()
    {
        using var file = new MemoryStream();
        using (var resource = typeof(ExcelTests).Assembly.GetManifestResourceStream(
                   $"{typeof(ExcelTests).Namespace}.TestFile.{LargeDate1904XlsFile}"))
            resource.CopyTo(file);

        file.Position = 0;
        var counter = new ReadCountingStream(file);

        Assert.Multiple(() =>
        {
            Assert.That(file.Length, Is.GreaterThan(300_000));
            Assert.That(ExcelDateSystem.IsDate1904(counter), Is.True);
            Assert.That(counter.BytesRead, Is.LessThan(file.Length / 50), "Expected under 2% of the file to be read");
        });

        // ExcelDataReader agrees: the first date is serial number 1000 which is 1906-09-27 only in the 1904 date system
        file.Position = 0;
        using var reader = ExcelReaderFactory.CreateReader(file);
        reader.Read();
        reader.Read();
        Assert.That(reader.GetValue(1), Is.EqualTo(new DateTime(1906, 9, 27)));
    }

    [Test]
    public void BuiltWorkbook_IsReadableByExcelDataReader()
    {
        using var reader = ExcelReaderFactory.CreateReader(new MemoryStream(CompoundFile(Workbook(600, 1))));

        Assert.That(reader.ResultsCount, Is.Zero, "The workbook should be found and have no worksheets");
    }

    /// <summary>
    /// The date system record's 4 byte header and 2 byte body can each lie in one sector or be split across two
    /// </summary>
    [Test]
    public void DateSystemRecord_IsReadWhereverItFallsAcrossSectors(
        [Values(505, 506, 507, 508, 509, 510, 511, 512, 513, 2554, 2555, 2556, 2557, 2558, 2559, 2560, 2561)]
        int offset, [Values] bool date1904)
    {
        var workbook = Workbook(offset, (ushort)(date1904 ? 1 : 0));
        var counter = new ReadCountingStream(new MemoryStream(CompoundFile(workbook)));

        Assert.Multiple(() =>
        {
            Assert.That(ExcelDateSystem.IsDate1904(counter), Is.EqualTo(date1904));

            // the file header, FAT and directory sectors and a few bytes of record headers but not the body of the
            // record before the date system record, which is most of a sector or (the later offsets) five of them
            Assert.That(counter.BytesRead, Is.LessThan(4 * SectorSize));
        });
    }

    [Test]
    public void SectorsOutOfOrder_AreReadInChainOrder()
    {
        var file = CompoundFile(Workbook(2558, 1), reverseSectors: true);

        Assert.That(ExcelDateSystem.IsDate1904(new MemoryStream(file)), Is.True);
    }

    [Test]
    public void Biff5Workbook_IsFoundInTheBookStream()
    {
        var file = CompoundFile(Workbook(600, 1), "Book");

        Assert.That(ExcelDateSystem.IsDate1904(new MemoryStream(file)), Is.True);
    }

    /// <summary>
    /// The first end of file record ends the part of the workbook which describes it as a whole, the worksheets follow
    /// </summary>
    [Test]
    public void DateSystemRecord_IsNotLookedForBeyondTheFirstEndOfFileRecord()
    {
        var workbook = Record(BofRecord, Bof).Concat(Record(EofRecord)).Concat(Record(DateModeRecord, 1, 0)).ToArray();

        Assert.That(ExcelDateSystem.IsDate1904(new MemoryStream(CompoundFile(workbook))), Is.False);
    }

    /// <summary>
    /// A corrupt FAT can make the chain of sectors loop.  Here the workbook claims to be 64MB of empty records in an 11
    /// sector file whose last sector chains back to its first.
    /// </summary>
    [Test]
    public void LoopingSectorChain_IsAbandoned()
    {
        const int dataSectors = 8;
        var workbook = Enumerable.Repeat(Record(UnknownRecord), dataSectors * SectorSize / 4).SelectMany(r => r).ToArray();
        var file = CompoundFile(workbook, declaredSize: 64 << 20, editFat: fat => fat[2 + dataSectors - 1] = 2);

        var counter = new ReadCountingStream(new MemoryStream(file));

        Assert.Multiple(() =>
        {
            Assert.That(ExcelDateSystem.IsDate1904(counter), Is.False);
            Assert.That(counter.BytesRead, Is.LessThan(10 * file.Length));
        });
    }

    #region Building workbooks

    /// <summary>The body of a beginning of file record for the workbook as a whole (BIFF8)</summary>
    private static readonly byte[] Bof = { 0x00, 0x06, 0x05, 0x00, 0xBB, 0x0D, 0xCC, 0x07, 0, 0, 0, 0, 0x06, 0, 0, 0 };

    private static byte[] Record(ushort type, params byte[] body) =>
        BitConverter.GetBytes(type).Concat(BitConverter.GetBytes((ushort)body.Length)).Concat(body).ToArray();

    /// <summary>
    /// Returns a workbook stream in which the date system record starts at <paramref name="dateModeOffset"/>.  The record
    /// before it fills the space, its body is made to look like a run of end of file records in case any of it is read.
    /// </summary>
    private static byte[] Workbook(int dateModeOffset, ushort dateMode)
    {
        var bof = Record(BofRecord, Bof);
        var filler = new byte[dateModeOffset - bof.Length - 4];
        for (var i = 0; i < filler.Length; i += 4)
            filler[i] = (byte)EofRecord;

        return bof.Concat(Record(UnknownRecord, filler)).Concat(Record(DateModeRecord, (byte)dateMode, 0))
            .Concat(Record(EofRecord)).ToArray();
    }

    /// <summary>
    /// Returns a compound file with one stream: sector 0 is the FAT, sector 1 the directory and the stream's sectors follow
    /// (it is padded to at least 4096 bytes, smaller streams are stored differently).
    /// </summary>
    /// <param name="workbook">Contents of the stream</param>
    /// <param name="streamName"></param>
    /// <param name="reverseSectors">True to store the last sector of the stream first and so on</param>
    /// <param name="declaredSize">Size of the stream according to the directory if not its real size</param>
    /// <param name="editFat">Changes to make to the FAT, to corrupt it</param>
    private static byte[] CompoundFile(byte[] workbook, string streamName = "Workbook", bool reverseSectors = false,
        int? declaredSize = null, Action<uint[]> editFat = null)
    {
        var dataSectors = Math.Max(8, (workbook.Length + SectorSize - 1) / SectorSize);
        var file = new byte[(3 + dataSectors) * SectorSize];
        var fat = Enumerable.Repeat(Unused, SectorSize / 4).ToArray();

        // where each sector is in the file: the header takes the place of a sector so sector 0 follows it
        int FileOffset(int sector) => (1 + sector) * SectorSize;
        int StreamSector(int index) => 2 + (reverseSectors ? dataSectors - 1 - index : index);

        fat[0] = FatSector;
        fat[1] = EndOfChain;
        for (var i = 0; i < dataSectors; i++)
        {
            fat[StreamSector(i)] = i == dataSectors - 1 ? EndOfChain : (uint)StreamSector(i + 1);

            var length = Math.Min(workbook.Length - i * SectorSize, SectorSize);
            if (length > 0)
                Array.Copy(workbook, i * SectorSize, file, FileOffset(StreamSector(i)), length);
        }

        editFat?.Invoke(fat);
        Buffer.BlockCopy(fat, 0, file, FileOffset(0), SectorSize);

        // header
        new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }.CopyTo(file, 0);
        Write(file, 0x18, (ushort)0x003E); // minor version
        Write(file, 0x1A, (ushort)3); // major version
        Write(file, 0x1C, (ushort)0xFFFE); // little endian
        Write(file, 0x1E, (ushort)9); // 512 byte sectors
        Write(file, 0x20, (ushort)6); // 64 byte mini sectors
        Write(file, 0x2C, 1u); // number of FAT sectors
        Write(file, 0x30, 1u); // first directory sector
        Write(file, 0x38, 4096u); // streams smaller than this are stored in the mini stream
        Write(file, 0x3C, EndOfChain); // no mini FAT
        Write(file, 0x44, EndOfChain); // no further list of FAT sectors
        Write(file, 0x4C, 0u); // the FAT is sector 0
        for (var i = 1; i < 109; i++)
            Write(file, 0x4C + i * 4, Unused);

        // directory: the root entry and the stream, the other two entries of the sector are unused
        var directory = FileOffset(1);
        WriteDirectoryEntry(file, directory, "Root Entry", 5, 1, EndOfChain, 0);
        WriteDirectoryEntry(file, directory + 128, streamName, 2, Unused, (uint)StreamSector(0),
            (uint)(declaredSize ?? dataSectors * SectorSize));
        WriteDirectoryEntry(file, directory + 256, "", 0, Unused, 0, 0);
        WriteDirectoryEntry(file, directory + 384, "", 0, Unused, 0, 0);

        return file;
    }

    private static void WriteDirectoryEntry(byte[] file, int offset, string name, byte type, uint child, uint start,
        uint size)
    {
        Encoding.Unicode.GetBytes(name).CopyTo(file, offset);
        Write(file, offset + 0x40, (ushort)(name.Length == 0 ? 0 : (name.Length + 1) * 2));
        file[offset + 0x42] = type;
        file[offset + 0x43] = 1; // black
        Write(file, offset + 0x44, Unused); // no left sibling
        Write(file, offset + 0x48, Unused); // no right sibling
        Write(file, offset + 0x4C, child);
        Write(file, offset + 0x74, start);
        Write(file, offset + 0x78, size);
    }

    private static void Write(byte[] file, int offset, ushort value) => BitConverter.GetBytes(value).CopyTo(file, offset);

    private static void Write(byte[] file, int offset, uint value) => BitConverter.GetBytes(value).CopyTo(file, offset);

    #endregion

    /// <summary>
    /// Counts the bytes read from the stream it wraps
    /// </summary>
    private sealed class ReadCountingStream : Stream
    {
        private readonly Stream _inner;

        public ReadCountingStream(Stream inner)
        {
            _inner = inner;
        }

        public long BytesRead { get; private set; }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;

        public override long Position
        {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _inner.Read(buffer, offset, count);
            BytesRead += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        public override void Flush()
        {
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
