using System.Net;
using ZatcaApi.Models;

namespace ZatcaApi.Services
{
    public interface IZatcaService
    {
        Task<(HttpStatusCode StatusCode, PortalResult ResultContent)> ReportInvoiceAsync(GatewayRequestApi request);
        Task<(HttpStatusCode StatusCode, PortalResult ResultContent)> ClearInvoiceAsync(GatewayRequestApi request);
        Task<(HttpStatusCode StatusCode, PortalResult ResultContent)> ComplianceInvoiceAsync(GatewayRequestApi request);
        Task<(HttpStatusCode StatusCode, PortalResult ResultContent)> GetSignedInvoice(GatewayRequestApi request);
        //Task<(HttpStatusCode StatusCode, byte[] ResultContent)> GenerateSignedInvoice(GatewayRequestApi request);
    }
}

