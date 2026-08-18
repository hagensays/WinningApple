using System.Collections.Generic;
using System.Linq;

namespace ClosedXML.Excel;

/// <summary>
/// A base class for pivot styling API. It has takes a selected <see cref="XLPivotArea"/>
/// and applies the style using <c>.Style*</c> API. The derived classes are responsible for
/// exposing API so user can define an area and then create the desired area (from what user
/// specified) through <see cref="GetCurrentArea"/> method.
/// </summary>
internal abstract class XLPivotStyleFormatBase : IXLPivotStyleFormat
{
    protected readonly XLPivotTable PivotTable;

    protected XLPivotStyleFormatBase(XLPivotTable pivotTable)
    {
        PivotTable = pivotTable;
    }

    #region IXLPivotStyleFormat members

    public XLPivotStyleFormatElement AppliesTo { get; init; } = XLPivotStyleFormatElement.Data;

    public IXLStyle Style
    {
        get => Format;
        set => Format.SetStyle(value);
    }

    #endregion IXLPivotStyleFormat members

    // TODO Styles: Ensure that each pivot area is there only once in a pivot table. Ensure it on load and during modifications.
    internal XLDxFormat Format => new XLDxFormat(PivotTable.Worksheet.Workbook.Styles, GetFormats().First());

    internal abstract XLPivotArea GetCurrentArea();

    internal abstract bool Filter(XLPivotArea area);

    private IEnumerable<XLPivotFormat> GetFormats()
    {
        var exists = false;
        foreach (var format in PivotTable.Formats)
        {
            if (format.Action == XLPivotFormatAction.Formatting && Filter(format.PivotArea))
            {
                exists = true;
                yield return format;
            }
        }

        if (!exists)
        {
            var format = new XLPivotFormat(GetCurrentArea())
            {
                FormatValue = null
            };
            PivotTable.AddFormat(format);
            yield return format;
        }
    }
}
