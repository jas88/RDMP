using System.Text;
using System.Text.RegularExpressions;
using FAnsi.Discovery.QuerySyntax;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataExport.Data;
using Rdmp.Core.DataExport.DataExtraction.Pipeline.Sources;
using Rdmp.Core.QueryBuilding;
using Rdmp.Core.ReusableLibraryCode.Progress;

namespace HICPlugin.DataFlowComponents;

/// <summary>
/// Special explicit source for Chris Hall's specific data extraction needs
/// </summary>
public class ChrisHallSpecialExplicitSource :DataExtractionSpecialExplicitSource { }