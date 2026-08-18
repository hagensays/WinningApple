#nullable enable
using ClosedXML.IO;

namespace ClosedXML.Tests.IO;

internal interface IExpectedXmlNode
{
    void AssertMatches(IXmlReader reader);
}
