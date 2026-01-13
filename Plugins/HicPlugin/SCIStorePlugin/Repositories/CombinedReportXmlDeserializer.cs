// Copyright (c) The University of Dundee 2018-2025
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Serialization;
using SCIStorePlugin.Data;

namespace SCIStorePlugin.Repositories;

/// <summary>
/// Deserializes combined laboratory report XML with automatic handling of invalid PCL escape code characters found in legacy Fife Haematology data
/// </summary>
public class CombinedReportXmlDeserializer
{
    private static readonly XmlSerializer Serializer = new(typeof(CombinedReportData));

    public CombinedReportData DeserializeFromZipEntry(ZipArchiveEntry entry, string fileLocation)
    {
        using (var stream = entry.Open())
        {
            try
            {
                return Serializer.Deserialize(stream) as CombinedReportData;
            }
            catch (Exception)
            {
                // we have failed so will fall through to attempt below
            }
        }

        // Not putting this into the above catch as the Deflate stream can't be rewound, and would rather open a new stream for safety
        // Exception might be due to invalid characters, attempt to replace them then deserialize again
        using (var stream = entry.Open())
        {
            return RetryDeserializationAfterCharacterReplacement(stream, fileLocation);
        }
    }

    public static CombinedReportData DeserializeFromXmlString(string xml)
    {
        using var reader = new StringReader(xml);
        return Serializer.Deserialize(reader) as CombinedReportData;
    }

    // The first three were found in Fife Haematology data, they are PCL escape codes
    private static readonly ValueTuple<string, string>[] CharacterSubstitutions = new ValueTuple<string, string>[]
    {
        ("&#x1B;(s3B", "[b]"), // begin bold
        ("&#x1B;(s0B", "[/b]"), // end bold
        ("&#x1B;(s", "[unknown|x1B;(s]"), // looks like truncation, in original file it looked like a truncation of 'end bold',
        ("&#x1B;", "") // basic escape sequence, if this remains on its own then get rid of it
    };

    public static string RemoveInvalidCharactersFromStream(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var xmlString = reader.ReadToEnd();
        return CharacterSubstitutions.Aggregate(xmlString, (current, value) => current.Replace(value.Item1, value.Item2));
    }

    private static CombinedReportData RetryDeserializationAfterCharacterReplacement(Stream stream, string fileLocation)
    {
        try
        {
            var xmlString = RemoveInvalidCharactersFromStream(stream);

            using var reader = new StringReader(xmlString);
            return Serializer.Deserialize(reader) as CombinedReportData;
        }
        catch (Exception e)
        {
            throw new Exception($"Error deserializing report, even after replacing invalid characters:{fileLocation}", e);
        }
    }
}