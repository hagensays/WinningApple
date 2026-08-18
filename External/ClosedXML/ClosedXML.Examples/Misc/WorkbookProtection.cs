using ClosedXML.Excel;
using System;

namespace ClosedXML.Examples.Misc
{
    public class WorkbookProtection : IXLExample
    {
        public void Create(String filePath)
        {
            using var wb = new XLWorkbook();
            wb.Worksheets.Add("Workbook Protection");
            wb.Protect("Abc@123");
            wb.SaveAs(filePath);
        }
    }
}
