// Models/InvoiceLog.cs
using Newtonsoft.Json;
using System;

namespace ZatcaApi.Models
{
    public class ApprovedInvoice
    {
        //Requested
        public int Id { get; set; }
        public string InvoiceId { get; set; }
        public int InvoiceType { get; set; }
        public string InvoiceSubType { get; set; }
        public string IssueDate { get; set; }
        public string Reference { get; set; }
        public string CustomerName { get; set; }
        

        //Counter
        public int ICV { get; set; }
        public string PIH { get; set; }


        //Portal Result

        public string RequestType { get; set; }

        [JsonProperty("invoiceHash")]
        public string InvoiceHash { get; set; }

        [JsonProperty("clearanceStatus")]
        public string ClearanceStatus { get; set; }

        [JsonProperty("reportingStatus")]
        public string ReportingStatus { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }


        public string InvoiceData { get; set; }

        [JsonProperty("base64QrCode")]
        public string Base64QrCode { get; set; }

        [JsonProperty("base64SignedInvoice")]
        public string Base64SignedInvoice { get; set; }

        [JsonProperty("xmlFileName")]
        public string XmlFileName { get; set; }

        

        public PortalResult ToPortalResult()
        {
            var portalResult = new PortalResult();

            portalResult.RequestType = RequestType + " * FROM APPROVED INVOICE LOG * ";
            portalResult.UUID = InvoiceId;
            portalResult.InvoiceHash = InvoiceHash;
            portalResult.ClearanceStatus = ClearanceStatus;
            portalResult.ReportingStatus = ReportingStatus;
            portalResult.Timestamp = Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            portalResult.ICV = ICV;
            portalResult.PIH = PIH;
            portalResult.XmlFileName = XmlFileName;
            // New fields added
            portalResult.Base64QrCode = Base64QrCode;
            portalResult.Base64SignedInvoice = Base64SignedInvoice;

            return portalResult;
        }

    }

}
