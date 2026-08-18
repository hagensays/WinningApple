using System;
using ClosedXML.Excel;

namespace ClosedXML.Examples.Misc
{
    public class HideSheets : IXLExample
    {
        public void Create(String filePath)
        {
            var wb = new XLWorkbook();
            
            wb.Worksheets.Add("First Hidden").Hide();
            wb.Worksheets.Add("Visible");
            wb.Worksheets.Add("Unhidden").Hide().Unhide();
            wb.Worksheets.Add("VeryHidden").Visibility = XLWorksheetVisibility.VeryHidden;
            wb.Worksheets.Add("Last Hidden").Hide();

            wb.SaveAs(filePath);
        }
    }
}
