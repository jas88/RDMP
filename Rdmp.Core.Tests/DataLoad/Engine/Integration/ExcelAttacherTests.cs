// Copyright (c) The University of Dundee 2018-2026
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using FAnsi;
using FAnsi.Discovery;
using NUnit.Framework;
using Rdmp.Core.Curation;
using Rdmp.Core.DataFlowPipeline;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.Job;
using Rdmp.Core.DataLoad.Modules.Attachers;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Rdmp.Core.Tests.DataLoad.Engine.Unit;
using System;
using System.IO;
using System.Linq;
using Tests.Common;

namespace Rdmp.Core.Tests.DataLoad.Engine.Integration;

public class ExcelAttacherTests : DatabaseTests
{
    private LoadDirectory _loadDirectory;
    private DirectoryInfo _parentDir;
    private DiscoveredDatabase _database;
    private DiscoveredTable _table;
    private string _filename;


    [OneTimeSetUp]
    protected void Setup()
    {
        base.SetUp();

        var workingDir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        _parentDir = workingDir.CreateSubdirectory("ExcelAttacherTests");
        _database = GetCleanedServer(DatabaseType.MicrosoftSQLServer);

        var toCleanup = _parentDir.GetDirectories().SingleOrDefault(d => d.Name.Equals("EXCEL_ATTACHER"));
        toCleanup?.Delete(true);

        _loadDirectory = LoadDirectory.CreateDirectoryStructure(_parentDir, "EXCEL_ATTACHER");

        using var con = _database.Server.GetConnection();
        con.Open();

        var cmdCreateTable = _database.Server.GetCommand(
            $"CREATE Table {_database.GetRuntimeName()}..ExcelAttacher([chi] [varchar](500),[value] [varchar](500))", con);
        cmdCreateTable.ExecuteNonQuery();


        _table = _database.ExpectTable("ExcelAttacher");
    }

    [TearDown]
    public void CleanUp()
    {
        File.Delete(_filename);
    }

    [Test]
    public void Test_BasicExcelAttacher()
    {
        var builder = new MinimalXlsxBuilder().Sheet("Sheet1")
            .Text("A1", "chi")
            .Text("B1", "value")
            .Text("A2", "1111111111")
            .Text("B2", "some value")
            .Text("A3", "2222222222")
            .Text("B3", "some other value");
        _filename = Path.Join(_loadDirectory.ForLoading.FullName, "ExcelAttacher.xlsx");
        builder.Save(_filename);

        var attacher = new ExcelAttacher();
        attacher.Initialize(_loadDirectory, _database);
        attacher.FilePattern = "ExcelAttacher*";
        attacher.TableName = "ExcelAttacher";
        Assert.DoesNotThrow(() => attacher.Attach(new ThrowImmediatelyDataLoadJob(), new GracefulCancellationToken()));
        var table = _database.ExpectTable("ExcelAttacher");
        Assert.That(table.Exists());
        using var con = _database.Server.GetConnection();
        con.Open();
        var reader = _database.Server.GetCommand("Select * from ExcelAttacher", con).ExecuteReader();
        Assert.Multiple(() =>
        {
            Assert.That(reader.Read());
            Assert.That(reader["chi"], Is.EqualTo("1111111111"));
            Assert.That(reader["value"], Is.EqualTo("some value"));
        });
        Assert.Multiple(() =>
        {
            Assert.That(reader.Read());
            Assert.That(reader["chi"], Is.EqualTo("2222222222"));
            Assert.That(reader["value"], Is.EqualTo("some other value"));
        });
        attacher.LoadCompletedSoDispose(ExitCodeType.Success, ThrowImmediatelyDataLoadEventListener.Quiet);
    }

    [Test]
    public void Test_BasicExcelAttacherWithBlankFirstColumn()
    {
        var builder = new MinimalXlsxBuilder().Sheet("Sheet1")
            .Text("B1", "chi")
            .Text("C1", "value")
            .Text("B2", "1111111111")
            .Text("C2", "some value")
            .Text("B3", "2222222222")
            .Text("C3", "some other value");
        _filename = Path.Join(_loadDirectory.ForLoading.FullName, "ExcelAttacher.xlsx");
        builder.Save(_filename);

        var attacher = new ExcelAttacher();
        attacher.Initialize(_loadDirectory, _database);
        attacher.FilePattern = "ExcelAttacher*";
        attacher.TableName = "ExcelAttacher";
        Assert.DoesNotThrow(() => attacher.Attach(new ThrowImmediatelyDataLoadJob(), new GracefulCancellationToken()));
        var table = _database.ExpectTable("ExcelAttacher");
        Assert.That(table.Exists());
        using var con = _database.Server.GetConnection();
        con.Open();
        var reader = _database.Server.GetCommand("Select * from ExcelAttacher", con).ExecuteReader();
        Assert.Multiple(() =>
        {
            Assert.That(reader.Read());
            Assert.That(reader["chi"], Is.EqualTo("1111111111"));
            Assert.That(reader["value"], Is.EqualTo("some value"));
        });
        Assert.Multiple(() =>
        {
            Assert.That(reader.Read());
            Assert.That(reader["chi"], Is.EqualTo("2222222222"));
            Assert.That(reader["value"], Is.EqualTo("some other value"));
        });
        attacher.LoadCompletedSoDispose(ExitCodeType.Success, ThrowImmediatelyDataLoadEventListener.Quiet);
    }

    [Test]
    public void Test_BasicExcelAttacherWithBlankFirstColumnAndRow()
    {
        var builder = new MinimalXlsxBuilder().Sheet("Sheet1")
            .Text("B2", "chi")
            .Text("C2", "value")
            .Text("B3", "1111111111")
            .Text("C3", "some value")
            .Text("B4", "2222222222")
            .Text("C4", "some other value");
        _filename = Path.Join(_loadDirectory.ForLoading.FullName, "ExcelAttacher.xlsx");
        builder.Save(_filename);

        var attacher = new ExcelAttacher();
        attacher.Initialize(_loadDirectory, _database);
        attacher.FilePattern = "ExcelAttacher*";
        attacher.TableName = "ExcelAttacher";
        Assert.DoesNotThrow(() => attacher.Attach(new ThrowImmediatelyDataLoadJob(), new GracefulCancellationToken()));
        var table = _database.ExpectTable("ExcelAttacher");
        Assert.That(table.Exists());
        using var con = _database.Server.GetConnection();
        con.Open();
        var reader = _database.Server.GetCommand("Select * from ExcelAttacher", con).ExecuteReader();
        Assert.Multiple(() =>
        {
            Assert.That(reader.Read());
            Assert.That(reader["chi"], Is.EqualTo("1111111111"));
            Assert.That(reader["value"], Is.EqualTo("some value"));
        });
        Assert.Multiple(() =>
        {
            Assert.That(reader.Read());
            Assert.That(reader["chi"], Is.EqualTo("2222222222"));
            Assert.That(reader["value"], Is.EqualTo("some other value"));
        });
        attacher.LoadCompletedSoDispose(ExitCodeType.Success, ThrowImmediatelyDataLoadEventListener.Quiet);
    }

    [Test]
    public void Test_BasicExcelAttacherWithBlankFirstColumnAndRowAndUnnecessaryOverwrite_ColumnOnly_Number()
    {
        var builder = new MinimalXlsxBuilder().Sheet("Sheet1")
            .Text("B2", "chi")
            .Text("C2", "value")
            .Text("B3", "1111111111")
            .Text("C3", "some value")
            .Text("B4", "2222222222")
            .Text("C4", "some other value");
        _filename = Path.Join(_loadDirectory.ForLoading.FullName, "ExcelAttacher.xlsx");
        builder.Save(_filename);

        var attacher = new ExcelAttacher();
        attacher.Initialize(_loadDirectory, _database);
        attacher.FilePattern = "ExcelAttacher*";
        attacher.TableName = "ExcelAttacher";
        attacher.ColumnOffset = "1";
        Assert.DoesNotThrow(() => attacher.Attach(new ThrowImmediatelyDataLoadJob(), new GracefulCancellationToken()));
        var table = _database.ExpectTable("ExcelAttacher");
        Assert.That(table.Exists());
        using var con = _database.Server.GetConnection();
        con.Open();
        var reader = _database.Server.GetCommand("Select * from ExcelAttacher", con).ExecuteReader();
        Assert.Multiple(() =>
        {
            Assert.That(reader.Read());
            Assert.That(reader["chi"], Is.EqualTo("1111111111"));
            Assert.That(reader["value"], Is.EqualTo("some value"));
        });
        Assert.Multiple(() =>
        {
            Assert.That(reader.Read());
            Assert.That(reader["chi"], Is.EqualTo("2222222222"));
            Assert.That(reader["value"], Is.EqualTo("some other value"));
        });
        attacher.LoadCompletedSoDispose(ExitCodeType.Success, ThrowImmediatelyDataLoadEventListener.Quiet);
    }


    [Test]
    public void Test_BasicExcelAttacherWithBlankFirstColumnAndRowAndUnnecessaryOverwrite_ColumnOnly_Letter()
    {
        var builder = new MinimalXlsxBuilder().Sheet("Sheet1")
            .Text("B2", "chi")
            .Text("C2", "value")
            .Text("B3", "1111111111")
            .Text("C3", "some value")
            .Text("B4", "2222222222")
            .Text("C4", "some other value");
        _filename = Path.Join(_loadDirectory.ForLoading.FullName, "ExcelAttacher.xlsx");
        builder.Save(_filename);

        var attacher = new ExcelAttacher();
        attacher.Initialize(_loadDirectory, _database);
        attacher.FilePattern = "ExcelAttacher*";
        attacher.TableName = "ExcelAttacher";
        attacher.ColumnOffset = "B";
        Assert.DoesNotThrow(() => attacher.Attach(new ThrowImmediatelyDataLoadJob(), new GracefulCancellationToken()));
        var table = _database.ExpectTable("ExcelAttacher");
        Assert.That(table.Exists());
        using var con = _database.Server.GetConnection();
        con.Open();
        var reader = _database.Server.GetCommand("Select * from ExcelAttacher", con).ExecuteReader();
        Assert.Multiple(() =>
        {
            Assert.That(reader.Read());
            Assert.That(reader["chi"], Is.EqualTo("1111111111"));
            Assert.That(reader["value"], Is.EqualTo("some value"));
        });
        Assert.Multiple(() =>
        {
            Assert.That(reader.Read());
            Assert.That(reader["chi"], Is.EqualTo("2222222222"));
            Assert.That(reader["value"], Is.EqualTo("some other value"));
        });
        attacher.LoadCompletedSoDispose(ExitCodeType.Success, ThrowImmediatelyDataLoadEventListener.Quiet);
    }

    [Test]
    public void Test_ExcelAttacherSkipFirstColumn_Letter()
    {
        var builder = new MinimalXlsxBuilder().Sheet("Sheet1")
            .Text("B1", "junk_data")
            .Text("C1", "chi")
            .Text("D1", "value")
            .Text("B2", "junk_data")
            .Text("C2", "1111111111")
            .Text("D2", "some value")
            .Text("B3", "junk_data")
            .Text("C3", "2222222222")
            .Text("D3", "some other value");
        _filename = Path.Join(_loadDirectory.ForLoading.FullName, "ExcelAttacher.xlsx");
        builder.Save(_filename);

        var attacher = new ExcelAttacher();
        attacher.Initialize(_loadDirectory, _database);
        attacher.FilePattern = "ExcelAttacher*";
        attacher.TableName = "ExcelAttacher";
        attacher.ColumnOffset = "C";
        Assert.DoesNotThrow(() => attacher.Attach(new ThrowImmediatelyDataLoadJob(), new GracefulCancellationToken()));
        var table = _database.ExpectTable("ExcelAttacher");
        Assert.That(table.Exists());
        using var con = _database.Server.GetConnection();
        con.Open();
        var reader = _database.Server.GetCommand("Select * from ExcelAttacher", con).ExecuteReader();
        Assert.Multiple(() =>
        {
            Assert.That(reader.Read());
            Assert.That(reader["chi"], Is.EqualTo("1111111111"));
            Assert.That(reader["value"], Is.EqualTo("some value"));
        });
        Assert.Multiple(() =>
        {
            Assert.That(reader.Read());
            Assert.That(reader["chi"], Is.EqualTo("2222222222"));
            Assert.That(reader["value"], Is.EqualTo("some other value"));
        });
        attacher.LoadCompletedSoDispose(ExitCodeType.Success, ThrowImmediatelyDataLoadEventListener.Quiet);
    }


    [Test]
    public void Test_ExcelAttacherSkipFirstColumn_Number()
    {
        var builder = new MinimalXlsxBuilder().Sheet("Sheet1")
            .Text("B1", "junk_data")
            .Text("C1", "chi")
            .Text("D1", "value")
            .Text("B2", "junk_data")
            .Text("C2", "1111111111")
            .Text("D2", "some value")
            .Text("B3", "junk_data")
            .Text("C3", "2222222222")
            .Text("D3", "some other value");
        _filename = Path.Join(_loadDirectory.ForLoading.FullName, "ExcelAttacher.xlsx");
        builder.Save(_filename);

        var attacher = new ExcelAttacher();
        attacher.Initialize(_loadDirectory, _database);
        attacher.FilePattern = "ExcelAttacher*";
        attacher.TableName = "ExcelAttacher";
        attacher.ColumnOffset = "2";
        Assert.DoesNotThrow(() => attacher.Attach(new ThrowImmediatelyDataLoadJob(), new GracefulCancellationToken()));
        var table = _database.ExpectTable("ExcelAttacher");
        Assert.That(table.Exists());
        using var con = _database.Server.GetConnection();
        con.Open();
        var reader = _database.Server.GetCommand("Select * from ExcelAttacher", con).ExecuteReader();
        Assert.Multiple(() =>
        {
            Assert.That(reader.Read());
            Assert.That(reader["chi"], Is.EqualTo("1111111111"));
            Assert.That(reader["value"], Is.EqualTo("some value"));
        });
        Assert.Multiple(() =>
        {
            Assert.That(reader.Read());
            Assert.That(reader["chi"], Is.EqualTo("2222222222"));
            Assert.That(reader["value"], Is.EqualTo("some other value"));
        });
        attacher.LoadCompletedSoDispose(ExitCodeType.Success, ThrowImmediatelyDataLoadEventListener.Quiet);
    }


    [Test]
    public void Test_ExcelAttacherSkipFirstColumnAndRow()
    {
        var builder = new MinimalXlsxBuilder().Sheet("Sheet1")
            .Text("B1", "junk_data")
            .Text("C1", "junk_data")
            .Text("D1", "junk_data")
            .Text("B2", "junk_data")
            .Text("C2", "chi")
            .Text("D2", "value")
            .Text("B3", "junk_data")
            .Text("C3", "1111111111")
            .Text("D3", "some value")
            .Text("B4", "junk_data")
            .Text("C4", "2222222222")
            .Text("D4", "some other value");
        _filename = Path.Join(_loadDirectory.ForLoading.FullName, "ExcelAttacher.xlsx");
        builder.Save(_filename);

        var attacher = new ExcelAttacher();
        attacher.Initialize(_loadDirectory, _database);
        attacher.FilePattern = "ExcelAttacher*";
        attacher.TableName = "ExcelAttacher";
        attacher.ColumnOffset = "2";
        attacher.RowOffset = 2;
        Assert.DoesNotThrow(() => attacher.Attach(new ThrowImmediatelyDataLoadJob(), new GracefulCancellationToken()));
        var table = _database.ExpectTable("ExcelAttacher");
        Assert.That(table.Exists());
        using var con = _database.Server.GetConnection();
        con.Open();
        var reader = _database.Server.GetCommand("Select * from ExcelAttacher", con).ExecuteReader();
        Assert.Multiple(() =>
        {
            Assert.That(reader.Read());
            Assert.That(reader["chi"], Is.EqualTo("1111111111"));
            Assert.That(reader["value"], Is.EqualTo("some value"));
        });
        Assert.Multiple(() =>
        {
            Assert.That(reader.Read());
            Assert.That(reader["chi"], Is.EqualTo("2222222222"));
            Assert.That(reader["value"], Is.EqualTo("some other value"));
        });
        attacher.LoadCompletedSoDispose(ExitCodeType.Success, ThrowImmediatelyDataLoadEventListener.Quiet);
    }

    [Test]
    public void Test_ExcelAttacherBadColumnOffset()
    {
        var builder = new MinimalXlsxBuilder().Sheet("Sheet1")
            .Text("B1", "junk_data")
            .Text("C1", "junk_data")
            .Text("D1", "junk_data")
            .Text("B2", "junk_data")
            .Text("C2", "chi")
            .Text("D2", "value")
            .Text("B3", "junk_data")
            .Text("C3", "1111111111")
            .Text("D3", "some value")
            .Text("B4", "junk_data")
            .Text("C4", "2222222222")
            .Text("D4", "some other value");
        _filename = Path.Join(_loadDirectory.ForLoading.FullName, "ExcelAttacher.xlsx");
        builder.Save(_filename);

        var attacher = new ExcelAttacher();
        attacher.Initialize(_loadDirectory, _database);
        attacher.FilePattern = "ExcelAttacher*";
        attacher.TableName = "ExcelAttacher";
        attacher.ColumnOffset = "2.987";
        attacher.RowOffset = 2;
        Assert.Throws<Exception>(() => attacher.Attach(new ThrowImmediatelyDataLoadJob(), new GracefulCancellationToken()),"test");
    }


}
