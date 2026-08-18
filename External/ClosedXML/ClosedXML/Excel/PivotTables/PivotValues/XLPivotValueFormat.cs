using System;

namespace ClosedXML.Excel
{
    internal class XLPivotValueFormat : IXLPivotValueFormat
    {
        private readonly XLPivotDataField _pivotValue;

        public XLPivotValueFormat(XLPivotDataField pivotValue)
        {
            _pivotValue = pivotValue;
        }

        public Int32 NumberFormatId
        {
            get
            {
                if (_pivotValue.NumberFormatValue is null)
                    return -1;

                if (!XLPredefinedFormat.NumberFormatIds.TryGetValue(_pivotValue.NumberFormatValue.Value, out var numFmtId))
                    return -1;

                return numFmtId;
            }

            set
            {
                if (!XLPredefinedFormat.FormatCodes.TryGetValue(value, out var format))
                    throw new ArgumentOutOfRangeException($"Only predefined format is permitted. Use nested enums/members of {nameof(XLPredefinedFormat)}.");

                _pivotValue.NumberFormatValue = format;
            }
        }

        public String Format
        {
            get => _pivotValue.NumberFormatValue ?? string.Empty;
            set
            {
                _pivotValue.NumberFormatValue = XLNumberFormat.Parse(value);
            }
        }

        public IXLPivotValue SetNumberFormatId(Int32 value)
        {
            NumberFormatId = value;
            return _pivotValue;
        }

        public IXLPivotValue SetFormat(String value)
        {
            Format = value;
            return _pivotValue;
        }
    }
}
