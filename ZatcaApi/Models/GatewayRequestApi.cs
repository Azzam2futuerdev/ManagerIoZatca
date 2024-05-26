namespace ZatcaApi.Models
{
    public class GatewayRequestApi
    {
        public string InvoiceId { get; set; } 
        public string IssueDate { get; set; } 
        public string Reference { get; set; } 
        public string CustomerName { get; set; }
        public string CustomerInfo { get; set; }
        public int InvoiceType { get; set; }
        public int InvoiceSubType { get; set; }
        public string InvoiceData { get; set; }
    }

}

