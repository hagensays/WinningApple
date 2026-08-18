namespace ClosedXML.Excel;

public partial class XLColor
{
    internal XLColorKey Key { get; }

    private XLColor() : this(new XLColorKey() { ColorType = XLColorType.Automatic })
    {
        HasValue = false;
    }

    internal XLColor(XLColorKey key)
    {
        Key = key;
        HasValue = true;
    }

    /// <summary>
    /// Lower case color type for exception messages.
    /// </summary>
    private string LcColorType => ColorType.ToString().ToLowerInvariant();
}
