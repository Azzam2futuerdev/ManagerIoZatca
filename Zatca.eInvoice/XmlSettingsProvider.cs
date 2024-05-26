using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Zatca.eInvoice
{
    public static class XmlSettingsProvider
    {
        // Methods
        public static XmlReaderSettings GetStandardXsdReaderSettings() =>
            new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Parse,
                XmlResolver = new CustomUrlResovler()
            };
    }

    public class CustomUrlResovler : XmlUrlResolver
    {
        // Fields
        private readonly HashSet<string> allowedUris;

        // Methods
        public override Uri ResolveUri(Uri baseUri, string relativeUri)
        {
            throw new InvalidOperationException("[Security Issue] A security vulnerability is detected.Operation aborted");
        }
    }




}
