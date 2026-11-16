// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Tests.Common;

namespace Rdmp.Core.Tests.DesignPatternTests.ClassFileEvaluation;

internal class DocumentationCrossExaminationTest
{
    private readonly DirectoryInfo _slndir;

    private static readonly Regex MatchComments =
        new(@"///[^;\r\n]*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private string[] _mdFiles;

    private static readonly Regex MatchMdReferences =
        new(@"`(.*)`", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Set this environment variable to 1 (or true) to have the test rewrite Markdown files in place, adding links to
    /// Glossary terms which should be links.  Off by default so that running the tests never modifies the repository.
    /// </summary>
    public const string RewriteMarkdownEnvironmentVariable = "RDMP_REWRITE_MARKDOWN_GLOSSARY_LINKS";

    private static bool ReWriteMarkdownToReferenceGlossary =>
        Environment.GetEnvironmentVariable(RewriteMarkdownEnvironmentVariable) is { } v &&
        (v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase));

    private const string MarkdownCodeBlocksFile = "Rdmp.UI.Tests/DesignPatternTests/MarkdownCodeBlockTests.cs";

    private string _markdownCodeBlocksContent;

    //words that are in Pascal case and you can use in comments despite not being in the codebase... this is an ironic variable to be honest
    //since the very fact that you add something to Ignorelist means that it is in the codebase after all!

    #region IgnoreList Terms

    private static readonly HashSet<string> IgnoreList = new()
    {
        "AppData",
        "ExecuteAggregateGraph",
        "ExtractMetadata",
        "DateRange",
        "AlterTableMakeDistinct",
        "MSSQLLocalDB",
        "NormalCohorts",
        "FreakyCohorts",
        "PublicKeyToken",
        "DatabaseEntities",
        "MyDateCol",
        "NumberOfResults",
        "ANOGPCode",
        "ANOPatientIdentifier",
        "PracticeGP",
        "UNIONing",
        "PatientId",
        "PatientIds",
        "MotherId",
        "BabyId",
        "MyColumn",
        "DrugCode",
        "DrugCode_Desc",
        "DrugName",
        "LabNumber",
        "DrugName",
        "SendingLocation",
        "DischargeLocation",
        "SendingLocation_Desc",
        "DischargeLocation_Desc",
        "MyDb",
        "NewBiochemistry",
        "AdmissionDateTime",
        "DataAge",
        "PatientCareNumber",
        "MyDb",
        "MyTransform",
        "PatCHI",
        "PatientCHI",
        "MotherCHI",
        "FatherChiNo",
        "LabNumber",
        "ANOchi",
        "INFOs",
        "PluginDatabase",
        "PatientDateOfBirth",
        "PatientDateOfBirthApprox",
        "ANOLocation",
        "ConditionList",
        "DataAnalyst",
        "HumanReadableDrugName",
        "DrugPrescribed",
        "DrugAbuse",
        "RoutineLoaderAccount",
        "ReadonlyUserAccount",
        "HBA1c",
        "PrescribedDate",
        "NodaTime",
        "MotherCHI",
        "BabyCHI",
        "ANOIdentifier",
        "ANOLabNumber",
        "LabNumber",
        "EmailAddressOfAuthorizor",
        "GrandParent",
        "TestLabCode",
        "DataAge",
        "CatalogueItem1",
        "CatalogueItem2",
        "CatalogueItem1",
        "CatalogueItem2",
        "UPDATEd",
        "GUIDs",
        "MyColRenamed",
        "DateConsentedToStudy",
        "PrescribingGP",
        "CasesForProject123",
        "ControlsForProject123",
        "GUIDs",
        "UPPERd",
        "StackOverflow",
        "LabNumbers",
        "LabNumber",
        "ANOLabNumber",
        "PatientName",
        "PatientDob",
        "DataAge",
        "DemographyLoading",
        "MyDb",
        "MyTbl",
        "CapsName",
        "SummaryComment",
        "MySoftwareSuite",
        "MyApplication",
        "MyResources",
        "MyClass1",
        "FamFamFam",
        "FishFishFish",
        "CASCADEing",
        "MyAssembly",
        "HBA1c",
        "EnumNameFavourite",
        "EnumName",
        "CatalogueFavourite",
        "PatientDeleted",
        "MyDoc",
        "MyDataFiles",
        "MyServer",
        "MyData",
        "RemoteDataFetcher",
        "EndpointDefinition",
        "LoadID_Data_STAGING",
        "AuditObject",
        "EmailAddressOfAuthorizor",
        "FixedSource",
        "AverageResult",
        "MaxResult",
        "FormatFile",
        "IndexedExtractionIdentifierList_AggregateConfiguration5",
        "YAXLib",
        "DependsOn",
        "DependingOnThis",
        "ProposedFixes",
        "PropertyX",
        "FamilyMembers",
        "LookupConfiguration",

        //CreatingANewCollectionTreeNode.md
        "FolderOfX",

        //PluginWriting.md
        "MyPlugin",
        "MyPluginUserInterface",
        "ExecuteCommandRenameCatalogueToBunnies",
        "BasicDataTableAnonymiser1",
        "BasicDataTableAnonymiser3",
        "CodeExamples",
        "MyExamplePluginTests",
        "TEST_Catalogue",
        "BasicDataTableAnonymiser4",
        "GetCommonNamesTable",
        "TestBasicDataTableAnonymiser3",
        "BasicDataTableAnonymiser5",
        "TestAnonymisationPlugins",
        "LoggerTestCase",
        "ToConsole",
        "ToMemory",
        "ToDatabase",
        "TEST_Logging",
        "DatePrescribed",

        //This is a legit verb
        "ANDed",
        "FetchBytesExpensive",

        "RecordLoadedDate",
        "TblPatIndx",
        "DataLoadRunId",

        "DataStructures", //class diagram
        "MyYetToExistTable",

        "SexCode",
        "SexCode_Desc",
        "SendingLocationCode",
        "LocationTable",
        "AddressLine1",
        "AddressLine2",
        "PatientSexCode",
        "SexDescription",
        "SexDescriptionLong",
        "MyTransform",

        "MyObject",
        "MyObjectMenu",
        "AllServersNodeMenu",
        "SomeClass",
        "ProposeExecutionWhenCommandIs",
        "Log4Net",
        "ReleaseLocation",
        "MyCoolDb",
        "OperationType",
        "DrugList",
        "DrugCodeFormat",
        "GenderCodeDescription",
        "MyParam",

        "GPCode",
        "RepoType",
        "LoadingBiochem",

        //Stuff now in FAnsi
        "MySqlAggregateHelper",
        "MicrosoftSQLAggregateHelper",
        "DbCommandBuilder",
        "EnvironmentPotential",
        "MySqlConnection",
        "DbCommandBuilder",

        // Proposed refactoring terms (docs/refactoring/memory-repository-per-type-dictionaries.md)
        "ObjectsByType",
        "GetTypeDictionary",
        "GetObjectsOfType",
        "PerTypeDictionaryTests",
        "MemoryRepositoryPerformanceTests",

        // Directory/plugin names used in documentation
        "RdmpExtensions",
        "DbCommandBuilder",
        "IDecideTypesForStrings",
        "TypeCompatibilityGroup",
        "DatatypeComputerTests",
        "DataTypeRequest",
        "CrossPlatformTests",
        "IDecideTypesForStrings",
        "TypeCompatibilityGroup",
        "TypeTranslaterTests",
        "DatatypeComputerTests",
        "LoadRunID",
        "HelpDocs",
        "HicServices",
        "FAnsiSql",
        "ProposeExecutionWhenTargetIsX",
        "InternalsVisibleTo",
        "AxisDimension",
        "MyDataset1",
        "HicHash",
        "UserManual",
        "ObjectType",
        "UserInterfaceOverview",
        "MyPipelinePlugin",
        "TestAnonymisationPluginsDatabaseTests",
        "PDFs",
        "MyPatIndexTable",
        "MSBuild15CMD",
        "SetupLazy",
        "TestCaseSourceAttribute",
        "MySqlConnector",
        "DoSomething",
        "MyTest",
        "UserControl1",
        "MyPluginUI",
        "SelectIMapsDirectlyToDatabaseTableDialog",
        "NuGet",
        "MyPluginClass",
        "SubContainer",
        "DescribeCommand", // this class has now been removed from RDMP codebase, don't complain if you see it in docs (e.g. CHANGELOG.md).
        "GetDate",
        "StudyInstanceUID",
        "RDMP_ExampleData",
        "MakeAnonymous",
        "ReleaseIdentifierAllocation",
        "SocialSecurityNumber",
        "BuildInParallel",

        //Quickstart.md
        "ResearchDataManagmentPlatform",

        // CSVHandling
        "TypeTranslation",

        // MEF optimization migration guide - example names
        "MyNamespace",
        "AotObjectFactoryExample",

        // External protocols, APIs, and system classes
        "WebDAV",
        "CommunicationException",
        "EventWaitHandle",
        "TrySend",
        "DecimalString",

        // Domain-specific terms and product names
        "MetIDQ",
        "CUIs",

        // Database/table examples in documentation
        "CT_SeriesTable",
        "CT_ImageTable",
        "ImageTableSchema",
        "MassiveImageArchive",
        "Test_P_00202_RIGHT_CC",
        "Test_P_00077_LEFT_CC",
        "Training_P_01983_LEFT_MLO_1",

        // Documentation file names
        "NoteForNewDevelopers",
        "NlpPlugin",

        // Deprecated plugins that were removed during consolidation
        "HicPlugin",
        "RdmpDicom",
        "SCIStorePlugin",
        "DrsPlugin",
        "GoDartsPlugin",
        "InterfaceToJira",
        "JiraPlugin",

        // Deprecated test classes that were removed
        "ScriptExecutionTests",
        "SlowRollerTest",
        "SlowRollerAsync",

        // Deprecated classes that were removed
        "FilenameColumnName",
        "CsvContextAdapter",

        // Generic terms used in documentation
        "BaseClass",
        "ConcreteTypes",

        // .NET BCL types referenced in comments
        "TypeInfoResolverChain"
    };

    #endregion

    public DocumentationCrossExaminationTest(DirectoryInfo slndir)
    {
        _slndir = slndir;

        // Skip vendored third party content (e.g. the Apache-2.0 NPOI submodule under externals/), build output and
        // nested git checkouts (e.g. worktrees under .claude/worktrees); RDMP documentation rules such as Glossary
        // linking do not apply there, and links computed for files outside this checkout would be wrong
        _mdFiles = RepositoryLayout.EnumerateFiles(slndir, "*.md", true)
            .Select(static f => f.FullName)
            .ToArray();
    }

    public void FindProblems(List<string> csFilesFound)
    {
        //find all non comment code and extract all unique tokens

        //find all .md files and extract all `` code blocks

        //for each commend and `` code block

        //identify Pascal case words

        //are they in the codebase tokens?

        var problems = new List<string>();

        var codeTokens = new HashSet<string>();
        var fileCommentTokens = new Dictionary<string, HashSet<string>>();

        //find all comments in class files
        foreach (var file in csFilesFound)
        {
            var isDesignerFile = file.Contains(".Designer.cs");
            var relativePath = RepositoryLayout.GetRelativePath(_slndir, file);

            if (relativePath.Contains("CodeTutorials"))
                continue;

            //don't look in the packages dir!
            if (relativePath.Contains("packages"))
                continue;

            foreach (var line in File.ReadLines(file))
                //if it is a comment
                if (MatchComments.IsMatch(line))
                {
                    if (isDesignerFile)
                        continue;

                    if (!fileCommentTokens.ContainsKey(file))
                        fileCommentTokens.Add(file, new HashSet<string>());

                    //its a comment extract all pascal case words
                    foreach (Match word in Regex.Matches(line, @"\b([A-Z]\w+){2,}"))
                        fileCommentTokens[file].Add(word.Value);
                }
                else
                {
                    //else it is a code line, extract all tokens
                    foreach (Match word in Regex.Matches(line, @"\w+"))
                        codeTokens.Add(word.Value);
                }
        }

        //find all comments in .md tutorials
        foreach (var mdFile in _mdFiles)
        {
            //don't look in the packages dir!
            if (RepositoryLayout.GetRelativePath(_slndir, mdFile).Contains("packages"))
                continue;

            fileCommentTokens.Add(mdFile, new HashSet<string>());
            var fileContents = File.ReadAllText(mdFile);

            foreach (Match m in MatchMdReferences.Matches(fileContents))
                foreach (Match word in Regex.Matches(m.Groups[1].Value, @"([A-Z]\w+){2,}"))
                    fileCommentTokens[mdFile].Add(word.Value);

            EnsureMaximumGlossaryUse(mdFile, problems);

            EnsureCodeBlocksCompile(mdFile);
        }


        foreach (var (filename, tokens) in fileCommentTokens)
            problems.AddRange(tokens
                .Where(token => !codeTokens.Contains(token) && !codeTokens.Contains($"ExecuteCommand{token}"))
                .Where(token => !IgnoreList.Contains(token))
                .Where(token => token.ToUpper() != token)
                .Where(token => token.Length <= 2 || !token.EndsWith("s") || !codeTokens.Contains(token[..^1]))
                .Select(token =>
                    $"FATAL PROBLEM: File '{filename}' talks about something which isn't in the codebase, called a:{Environment.NewLine}{token}"));

        if (problems.Any())
        {
            var suggestedTerms = string.Join(", ", problems
                .Where(l => l.Contains('\n'))
                .Select(p => $"\"{p.Split('\n')[1]}\""));

            var fullProblems = string.Join(Environment.NewLine, problems);

            Assert.Fail($"Found {problems.Count} terms in comments not in codebase.{Environment.NewLine}{Environment.NewLine}" +
                       $"Add to DocumentationCrossExaminationTest.IgnoreList: {suggestedTerms}{Environment.NewLine}{Environment.NewLine}" +
                       $"Full details:{Environment.NewLine}{fullProblems}");
        }
    }

    private void EnsureCodeBlocksCompile(string mdFile)
    {
        var codeBlocks = Path.Combine(_slndir.FullName, MarkdownCodeBlocksFile);
        var codeBlocksContent = _markdownCodeBlocksContent ??= File.ReadAllText(codeBlocks);

        var rGuidComment = new Regex("<!--- (.{32}) --->");
        var rStartCodeBlock = new Regex("```csharp");
        var rEndCodeBlock = new Regex("```");

        var markdownCodeBlocks = new Dictionary<string, string>();

        var lines = File.ReadAllLines(mdFile);

        for (var i = 0; i < lines.Length; i++)
        {
            var match = rGuidComment.Match(lines[i]);

            //match a line like <!--- df7d2bb4cd6145719f933f6f15218b1a --->
            if (match.Success)
            {
                var guid = match.Groups[1].Value;
                var sb = new StringBuilder();

                markdownCodeBlocks.Add(guid, null);

                //consume the line and look for ```csharp on the next line
                if (!rStartCodeBlock.IsMatch(lines[++i]))
                    throw new Exception(
                        $"Expected code block in markdown for GUID {guid} to be followed by a line {rStartCodeBlock}");

                //skip the ```csharp line
                i++;

                //consume until the closing ``` line
                while (!rEndCodeBlock.IsMatch(lines[i]))
                    sb.AppendLine(lines[i++]);

                markdownCodeBlocks[guid] = sb.ToString();
            }
        }

        foreach (var kvp in markdownCodeBlocks)
        {
            var rBlock = new Regex($"#region {kvp.Key}([^#]*)#endregion", RegexOptions.Singleline);
            var m = rBlock.Match(codeBlocksContent);

            if (!m.Success)
                throw new Exception(
                    $"No code block found in {codeBlocks} for guid {kvp.Key}.  Try adding a #region section for the guid");

            var code = Regex.Replace(m.Groups[1].Value, "\\s+", " ");
            var docs = Regex.Replace(kvp.Value, "\\s+", " ");

            Assert.That(docs.Trim(), Is.EqualTo(code.Trim()),
                $"Code in the documentation markdown (actual) did not match the corresponding compiled code (expected) for code guid {kvp.Key} markdown file was {mdFile} and code file was {codeBlocks}");
        }
    }

    private void EnsureMaximumGlossaryUse(string mdFile, List<string> problems)
    {
        const string glossaryRelativePath = "Documentation/CodeTutorials/Glossary.md";

        var rGlossary = new Regex("##([A-z ]*)");
        var rWords = new Regex(@"\[?\w*\]?");
        var rGlossaryLink = new Regex(@"^\[\w*\]:");

        var glossaryPath = Path.Combine(_slndir.FullName, glossaryRelativePath);

        //don't evaluate the glossary!
        if (Path.GetFileName(mdFile) == "Glossary.md")
            return;

        if (Path.GetFileName(mdFile) == "template.md")
            return;

        var glossaryHeaders =
            new HashSet<string>(
                File.ReadAllLines(glossaryPath)
                    .Where(l => rGlossary.IsMatch(l))
                    .Select(l => rGlossary.Match(l).Groups[1].Value.Trim()));

        var inCodeBlock = false;
        var lineNumber = 0;

        var allLines = File.ReadAllLines(mdFile);
        var allLinesRevised = (string[])allLines.Clone();

        var suggestedLinks = new Dictionary<string, string>();

        foreach (var line in allLines)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            //don't complain about the glossary links at the bottom of the file.
            if (rGlossaryLink.IsMatch(line))
                continue;

            //don't complain about keywords in code blocks
            if (line.TrimStart().StartsWith("```") || line.TrimStart().StartsWith("> ```"))
                inCodeBlock = !inCodeBlock;

            if (!inCodeBlock)
                foreach (Match match in rWords.Matches(line))
                    if (glossaryHeaders.Contains(match.Value))
                    {
                        //It's already got a link on it e.g. [DBMS] or it's "UNION - sometext"
                        if (match.Index - 1 > 0
                            &&
                            (line[match.Index - 1] == '[' || line[match.Index - 1] == '"'))
                            continue;


                        var relPath = Path.GetRelativePath(Path.GetDirectoryName(mdFile), glossaryPath)
                            .Replace('\\', '/');

                        if (!relPath.StartsWith("."))
                            relPath = $"./{relPath}";

                        var suggestedLine = $"[{match.Value}]: {relPath}#{match.Value}";
                        var markdownLink = $"[{match.Value}]({relPath}#{match.Value})";

                        //if it has spaces on either side
                        if (line[Math.Max(0, match.Index - 1)] == ' ' && line[
                                                                          Math.Min(line.Length - 1,
                                                                              match.Index + match.Length)] == ' '
                                                                      //don't mess with lines that contain an image
                                                                      && !line.Contains("!["))
                            allLinesRevised[lineNumber - 1] =
                                allLinesRevised[lineNumber - 1].Replace($" {match.Value} ", $" [{match.Value}] ");

                        //also if we have a name like `Catalogue` it should probably be [Catalogue] instead so it works as a link
                        allLinesRevised[lineNumber - 1] =
                            allLinesRevised[lineNumber - 1].Replace($"`{match.Value}`", $"[{match.Value}]");

                        //if it is a novel occurrence
                        if (!allLines.Contains(suggestedLine) && !suggestedLinks.ContainsValue(suggestedLine) && !allLines.Contains(markdownLink) && !suggestedLinks.ContainsValue(markdownLink))
                        {
                            suggestedLinks.Add(match.Value, suggestedLine);
                            problems.Add(
                                $"Glossary term should be link in {mdFile} line number {lineNumber}.  Term is {match.Value}.  Suggested link line is:\"{suggestedLine}\"");
                        }
                    }
        }

        if (suggestedLinks.Any() && ReWriteMarkdownToReferenceGlossary)
        {
            File.WriteAllLines(mdFile, allLinesRevised);

            File.AppendAllText(mdFile, Environment.NewLine);
            File.AppendAllLines(mdFile, suggestedLinks.Values.Distinct());
        }
    }
}
