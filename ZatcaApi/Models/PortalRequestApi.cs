using Newtonsoft.Json;

namespace ZatcaApi.Models
{
    public class PortalRequestApi
    {
        [JsonProperty("invoiceHash")] 
        public string InvoiceHash { get; set; }
        
        [JsonProperty("uuid")] 
        public string Uuid { get; set; }

        [JsonProperty("invoice")] 
        public string Invoice { get; set; }
    }

}

