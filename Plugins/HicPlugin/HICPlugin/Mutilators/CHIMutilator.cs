// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using Rdmp.Core.ReusableLibraryCode.Checks;
using FAnsi.Discovery;
using Rdmp.Core.ReusableLibraryCode.Progress;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataLoad.Engine.Mutilators;
using Rdmp.Core.Curation.Data.DataLoad;
using Rdmp.Core.DataLoad;
using Rdmp.Core.DataLoad.Engine.Job;
using System.Data.Common;

namespace HICPlugin.Mutilators;

public class CHIMutilator:IPluginMutilateDataTables
{
    private DiscoveredDatabase _dbInfo;
    private LoadStage _loadStage;

    [DemandsInitialization("The CHI column you want to mutilate based on")]
    public ColumnInfo ChiColumn { get; set; }

    [DemandsInitialization("If true, program will attempt to add zero to the front of 9 digit CHIs prior to running the CHI validity check", Mandatory = true, DefaultValue = true)]
    public bool TryAddingZeroToFront { get; set; }

    [DemandsInitialization("Timeout in seconds", DefaultValue = 30)]
    public int Timeout { get; set; } = 30;

    [DemandsInitialization("Columns failing validation will have this consequence applied to them", Mandatory=true, DefaultValue = MutilationAction.CrashDataLoad)]
    public MutilationAction FailedRows { get; set; }


    public void Check(ICheckNotifier notifier)
    {
            
    }

    public void LoadCompletedSoDispose(ExitCodeType exitCode, IDataLoadEventListener postLoadEventsListener)
    {
            
    }

    public bool DisposeImmediately { get; set; }

    public void Initialize(DiscoveredDatabase dbInfo, LoadStage loadStage)
    {
        _dbInfo = dbInfo;
        _loadStage = loadStage;
    }

    private static string DropCHIFunctionIfExists()
    {
        return @"IF OBJECT_ID('dbo.checkCHI') IS NOT NULL
  DROP FUNCTION checkCHI";
    }


    private static string CreateCHIFunction()
    {
        return @"
CREATE FUNCTION [dbo].[checkCHI](@CHI as varchar(255))
RETURNS bit AS
BEGIN

    DECLARE @SumTotal int
    DECLARE @CheckDigit int
    DECLARE @Result bit
    DECLARE @i int
    SET @i = 0
    SET @SumTotal = 0
    SET @Result = 0
    --return 0 if the CHI is non-numeric
    IF(ISNUMERIC(@CHI) <> 1)
        RETURN 0
        
    --return 0 if the day of birth is greater than 31
    IF(LEFT(@CHI, 2) > 31)
        RETURN 0    
    
    --return 0 if the month of birth is greater than 12
    IF(SUBSTRING(@CHI, 3, 2) > 12)
        RETURN 0    
        
    --return 0 if the CHI is not 10 digits long
    IF(LEN(@CHI) = 10)
    BEGIN
        --Calculate the sum
        WHILE @i < 9
        BEGIN
            SET @SumTotal = @SumTotal + (convert(int,substring(@CHI,@i+1,1)) * (10 - @i))
            SET @i = @i + 1
        END

        --Obtain Check Digit
        SET @CheckDigit = 11 - (@SumTotal % 11)

        IF @CheckDigit = 11
            SET @CheckDigit = 0

        --Compare Check Digit
        IF @CheckDigit = convert(int,substring(@CHI,10,1))
            SET @Result = 1

        RETURN @Result
    END
    
    RETURN 0
END
";
    }

    private string GetUpdateSQL(LoadStage loadStage)
    {
        var tableName = ChiColumn.TableInfo.GetRuntimeName(loadStage);
        var colName = ChiColumn.GetRuntimeName(loadStage);

        return FailedRows switch
        {
            MutilationAction.SetNull => $"UPDATE {tableName} SET {colName} = NULL WHERE dbo.checkCHI({colName}) = 0",
            MutilationAction.DeleteRows => $"DELETE FROM {tableName} WHERE dbo.checkCHI({colName}) = 0",
            MutilationAction.CrashDataLoad =>
                $"IF EXISTS (SELECT 1 FROM {tableName} WHERE dbo.checkCHI({colName}) = 0) raiserror('Found Dodgy CHIs', 16, 1);",
            _ => throw new InvalidOperationException($"Invalid {nameof(MutilationAction)} value {FailedRows}")
        };
    }

    public ExitCodeType Mutilate(IDataLoadJob job)
    {
        if (_loadStage != LoadStage.AdjustRaw && _loadStage != LoadStage.AdjustStaging)
            throw new NotSupportedException("This mutilator can only run in AdjustRaw or AdjustStaging");

        using var con = _dbInfo.Server.GetConnection();
        con.Open();

        AddTimeout(_dbInfo.Server.GetCommand(DropCHIFunctionIfExists(), con)).ExecuteNonQuery();
        AddTimeout(_dbInfo.Server.GetCommand(CreateCHIFunction(), con)).ExecuteNonQuery();

        if (TryAddingZeroToFront)
            AddTimeout(_dbInfo.Server.GetCommand(PrePendNineDigitCHIs(_loadStage), con)).ExecuteNonQuery();

        var affectedRows = AddTimeout(_dbInfo.Server.GetCommand(GetUpdateSQL(_loadStage), con)).ExecuteNonQuery();

        job.OnNotify(this, new NotifyEventArgs(ProgressEventType.Information,
            $"CHIMutilator affected {affectedRows} rows"));

        return ExitCodeType.Success;
    }

    private DbCommand AddTimeout(DbCommand dbCommand)
    {
        dbCommand.CommandTimeout = Timeout;
        return dbCommand;
    }

    private string PrePendNineDigitCHIs(LoadStage loadStage)
    {
        var tableName = ChiColumn.TableInfo.GetRuntimeName(loadStage);
        var colName = ChiColumn.GetRuntimeName(loadStage);

        return $"UPDATE {tableName} SET {colName} ='0' + {colName} WHERE LEN({colName}) = 9 ";
    }
}

public enum MutilationAction
{
    SetNull,
    DeleteRows,
    CrashDataLoad
}