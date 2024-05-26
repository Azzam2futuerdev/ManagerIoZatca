namespace ZatcaApi.Models
{
    public class GatewaySetting
    {
        public string EcSecp256k1Privkeypem { get; set; }
        public string CCSIDComplianceRequestId { get; set; }
        public string CCSIDBinaryToken { get; set; }
        public string CCSIDSecret { get; set; }
        public string ComplianceCheckUrl { get; set; }
        public string PCSIDBinaryToken { get; set; }
        public string PCSIDSecret { get; set; }
        public string ReportingUrl { get; set; }
        public string ClearanceUrl { get; set; }
    }

}

