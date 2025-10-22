﻿using System;

namespace SCIStorePlugin.Data;

public class SciStoreDodgyXmlException : Exception
{
    public string LabNumber { get; set; }

    public SciStoreDodgyXmlException(string message): base(message)
    {
    }
    public SciStoreDodgyXmlException(string message, string labNumber): base(message)
    {
        LabNumber = labNumber;
    }

    public SciStoreDodgyXmlException( string message,string labNumber, Exception e) : base(message,e)
    {
        LabNumber = labNumber;
    }

    public override string ToString()
    {
        return $"{Message}(LabNumber:{(LabNumber ?? "Unknown")}){Environment.NewLine}{base.ToString()}";
    }
}