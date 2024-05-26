namespace Zatca.eInvoice.Models
{
    public class UblElement
    {
        public string InvoiceHash { get; set; } = "";
        public string SignatureValue { get; set; } = "";
        public string X509Certificate { get; set; } = "";
        public string SigningTime { get; set; } = "";
        public string CertDigestValue { get; set; } = "";
        public string X509IssuerName { get; set; } = "";
        public string X509SerialNumber { get; set; } = "";
        public string CertificateHash { get; set; } = "";

        public string ToStringXML()
        {
            string UBLString = SharedUtilities.ReadResource("ZatcaDataUbl.xml");
            return UBLString.Replace("INVOICE_HASH", InvoiceHash).
                Replace("SIGNATURE_VALUE", SignatureValue).
                Replace("X509_CERTIFICATE", X509Certificate).
                Replace("SIGNING_TIME", SigningTime).
                Replace("CERT_DIGEST_VALUE", CertDigestValue).
                Replace("X509_ISSUERNAME", X509IssuerName).
                Replace("X509_SERIALNUMBER", X509SerialNumber);
        }

    }
}
