namespace ZatcaApi.Models
{
    public class GatewayRequestApi
    {
        public string InvoiceId { get; set; }
        public string IssueDate { get; set; }
        public string Reference { get; set; }
        public string PartyName { get; set; }
        public int InvoiceType { get; set; }
        public string InvoiceSubType { get; set; }
        public string InvoiceData { get; set; }
        public bool IsFromSwaggerClient { get; set; } = false;
    }

}

