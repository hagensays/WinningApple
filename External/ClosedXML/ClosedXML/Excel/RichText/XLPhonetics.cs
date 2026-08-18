using System;
using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel.Formatting;

namespace ClosedXML.Excel
{
    internal class XLPhonetics : IXLPhonetics
    {
        private readonly List<IXLPhonetic> _phonetics = new();
        private readonly XLWorkbookStyles _styles;
        private readonly XLFontFormatValue _defaultFont;
        private readonly Action _onChange;
        private XLFontFormatValue _font;
        private XLPhoneticAlignment _alignment;
        private XLPhoneticType _type;

        public XLPhonetics(XLFontFormatValue font, XLFontFormatValue defaultFont, XLWorkbookStyles styles, Action onChange)
        {
            _styles = styles;
            _defaultFont = defaultFont;
            _font = font;
            _type = XLPhoneticType.FullWidthKatakana;
            _alignment = XLPhoneticAlignment.Left;
            _onChange = onChange;
        }

        public Int32 Count => _phonetics.Count;

        public Boolean Bold
        {
            get => _font.Bold;
            set => ChangeFont(f => f with { Bold = value});
        }

        public Boolean Italic
        {
            get => _font.Italic;
            set => ChangeFont(f => f with { Italic = value });
        }

        public XLFontUnderlineValues Underline
        {
            get => _font.Underline;
            set => ChangeFont(f => f with { Underline = value });
        }

        public Boolean Strikethrough
        {
            get => _font.Strikethrough;
            set => ChangeFont(f => f with { Strikethrough = value });
        }

        public XLFontVerticalTextAlignmentValues VerticalAlignment
        {
            get => _font.VerticalAlignment;
            set => ChangeFont(f => f with { VerticalAlignment = value });
        }

        public Boolean Shadow
        {
            get => _font.Shadow;
            set => ChangeFont(f => f with { Shadow = value });
        }

        public Double FontSize
        {
            get => _font.Size.Points;
            set => ChangeFont(f => f with { Size = XLFontSize.FromPoints(value) });
        }

        public XLColor FontColor
        {
            get => _font.Color;
            set => ChangeFont(f => f with { Color = value });
        }

        public String FontName
        {
            get => _font.Name.Text;
            set => ChangeFont(f => f with { Name = value });
        }

        public XLFontFamilyNumberingValues FontFamilyNumbering
        {
            get => _font.Family;
            set => ChangeFont(f => f with { Family = value });
        }

        public XLFontCharSet FontCharSet
        {
            get => _font.Charset;
            set => ChangeFont(f => f with { Charset = value });
        }

        public XLFontScheme FontScheme
        {
            get => _font.Scheme;
            set => ChangeFont(f => f with { Scheme = value });
        }

        public XLPhoneticAlignment Alignment
        {
            get => _alignment;
            set
            {
                _alignment = value;
                _onChange();
            }
        }

        public XLPhoneticType Type
        {
            get => _type;
            set
            {
                _type = value;
                _onChange();
            }
        }

        internal XLFontFormatValue Font => _font;

        public IXLPhonetics SetBold() { Bold = true; return this; }

        public IXLPhonetics SetBold(Boolean value) { Bold = value; return this; }

        public IXLPhonetics SetItalic() { Italic = true; return this; }

        public IXLPhonetics SetItalic(Boolean value) { Italic = value; return this; }

        public IXLPhonetics SetUnderline() { Underline = XLFontUnderlineValues.Single; return this; }

        public IXLPhonetics SetUnderline(XLFontUnderlineValues value) { Underline = value; return this; }

        public IXLPhonetics SetStrikethrough() { Strikethrough = true; return this; }

        public IXLPhonetics SetStrikethrough(Boolean value) { Strikethrough = value; return this; }

        public IXLPhonetics SetVerticalAlignment(XLFontVerticalTextAlignmentValues value) { VerticalAlignment = value; return this; }

        public IXLPhonetics SetShadow() { Shadow = true; return this; }

        public IXLPhonetics SetShadow(Boolean value) { Shadow = value; return this; }

        public IXLPhonetics SetFontSize(Double value) { FontSize = value; return this; }

        public IXLPhonetics SetFontColor(XLColor value) { FontColor = value; return this; }

        public IXLPhonetics SetFontName(String value) { FontName = value; return this; }

        public IXLPhonetics SetFontFamilyNumbering(XLFontFamilyNumberingValues value) { FontFamilyNumbering = value; return this; }

        public IXLPhonetics SetFontCharSet(XLFontCharSet value) { FontCharSet = value; return this; }

        public IXLPhonetics SetFontScheme(XLFontScheme value) { FontScheme = value; return this; }

        public IXLPhonetics SetAlignment(XLPhoneticAlignment phoneticAlignment) { Alignment = phoneticAlignment; return this; }

        public IXLPhonetics SetType(XLPhoneticType phoneticType) { Type = phoneticType; return this; }

        public IXLPhonetics Add(String text, Int32 start, Int32 end)
        {
            _phonetics.Add(new XLPhonetic(text, start, end));
            _onChange();
            return this;
        }

        public IXLPhonetics ClearText()
        {
            _phonetics.Clear();
            _onChange();
            return this;
        }

        public IXLPhonetics ClearFont()
        {
            _font = _defaultFont;
            _onChange();
            return this;
        }

        public IEnumerator<IXLPhonetic> GetEnumerator()
        {
            return _phonetics.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Equals(IXLPhonetics? other) => Equals(other as XLPhonetics);

        public bool Equals(XLPhonetics? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (!_phonetics.SequenceEqual(other._phonetics))
                return false;

            return
                _font.Equals(other._font) &&
                Type == other.Type &&
                Alignment == other.Alignment;
        }

        private void ChangeFont(Func<XLFontFormatValue, XLFontFormatValue> modifyFont)
        {
            _font = _styles.GetRegisteredFontFormat(_font, modifyFont);
            _onChange();
        }
    }
}
