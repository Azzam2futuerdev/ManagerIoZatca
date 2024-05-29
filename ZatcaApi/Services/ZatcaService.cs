using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Serialization;
using Zatca.eInvoice;
using Zatca.eInvoice.Models;
using ZatcaApi.Helpers;
using ZatcaApi.Models;
using ZatcaApi.Services;

public class ZatcaService : IZatcaService
{
    private readonly HttpClient _httpClient;
    private readonly GatewaySetting _settings;
    private readonly BusinessInfo _businessInfo;
    private readonly BusinessDataCustomField _businessDataCustomField;
    private readonly AppDbContext _dbContext;

    public ZatcaService(HttpClient httpClient, GatewaySetting settings, BusinessInfo businessInfo, BusinessDataCustomField businessDataCustomField, AppDbContext dbContext)
    {
        _httpClient = httpClient;
        _settings = settings;
        _businessInfo = businessInfo;
        _businessDataCustomField = businessDataCustomField;
        _dbContext = dbContext;

    }

    public async Task<(HttpStatusCode StatusCode, PortalResult ResultContent)> ReportInvoiceAsync(GatewayRequestApi request)
    {
        try
        {
            PortalResult result = await GetResultByInvoiceIdAsync(request.InvoiceId);
            if (result != null)
            {
                return (HttpStatusCode.OK, result);
            };

            (int ICV, string PIH) = await GetLastICVandPIHAsync();

            Invoice invoice = MappingManager.GenerateInvoiceObject(request, _businessInfo, _businessDataCustomField, ICV, PIH);
            InvoiceGenerator ig = new(
                    invoice,
                    Encoding.UTF8.GetString(Convert.FromBase64String(_settings.PCSIDBinaryToken)),
                    _settings.EcSecp256k1Privkeypem
                );

            ig.GetSignedInvoiceXML(out string base64SignedInvoice, out string base64QrCode, out string XmlFileName, out string requestApi);

            var payloadJson = requestApi;
            var requestContent = JsonConvert.DeserializeObject<PortalRequestApi>(requestApi);

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            _httpClient.DefaultRequestHeaders.Add("Clearance-Status", "0");
            _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.PCSIDBinaryToken}:{_settings.PCSIDSecret}")));

            var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_settings.ReportingUrl, content);
            
            var resultContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<PortalResult>(resultContent);

            apiResponse.RequestType = "Invoice Reporting";
            apiResponse.StatusCode = response.StatusCode.ToString();


            if (apiResponse.ReportingStatus == "REPORTED")
            {
                apiResponse.ICV = ICV;
                apiResponse.PIH = PIH;

                apiResponse.InvoiceHash = requestContent.InvoiceHash; //Current InvoiceHash
                apiResponse.Base64SignedInvoice = base64SignedInvoice;
                apiResponse.Base64QrCode = base64QrCode;
                apiResponse.XmlFileName = XmlFileName;

                if (!request.IsFromSwaggerClient) {
                    await LogApprovedInvoiceAsync(request, apiResponse);
                }
            }

            return ((HttpStatusCode)response.StatusCode, apiResponse);

        }
        catch (Exception ex)
        {
            PortalResult portalResult = new()
            {
                StatusCode = HttpStatusCode.OK.ToString(),
                Error = ex.Message
            };

            return (HttpStatusCode.InternalServerError, portalResult);
        }
    }

    public async Task<(HttpStatusCode StatusCode, PortalResult ResultContent)> ClearInvoiceAsync(GatewayRequestApi request)
    {
        try
        {
            PortalResult result = await GetResultByInvoiceIdAsync(request.InvoiceId);
            if (result != null)
            {
                return (HttpStatusCode.OK, result);
            };
           
            (int ICV, string PIH) = await GetLastICVandPIHAsync();

            Invoice invoice = MappingManager.GenerateInvoiceObject(request, _businessInfo, _businessDataCustomField, ICV , PIH);
            InvoiceGenerator ig = new(
                    invoice,
                    Encoding.UTF8.GetString(Convert.FromBase64String(_settings.PCSIDBinaryToken)),
                    _settings.EcSecp256k1Privkeypem
                );

            ig.GetSignedInvoiceXML(out string base64SignedInvoice, out string base64QrCode, out string XmlFileName, out string requestApi);


            var payloadJson = requestApi;
            var requestContent = JsonConvert.DeserializeObject<PortalRequestApi>(requestApi);

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            _httpClient.DefaultRequestHeaders.Add("Clearance-Status", "1");
            _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.PCSIDBinaryToken}:{_settings.PCSIDSecret}")));

            var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_settings.ClearanceUrl, content);

            var resultContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<PortalResult>(resultContent);

            if (apiResponse != null && apiResponse.ClearedInvoice != null)
            {
                var clearedInvoiceXml = Encoding.UTF8.GetString(Convert.FromBase64String(apiResponse.ClearedInvoice));

                XmlSerializer serializer = new(typeof(Invoice));
                using (StringReader reader = new(clearedInvoiceXml))
                {
                    var clearedInvoice = (Invoice)serializer.Deserialize(reader);

                    var qrCodeNode = clearedInvoice?.AdditionalDocumentReference?
                        .FirstOrDefault(docRef => docRef.ID.Value == "QR")?.Attachment?.EmbeddedDocumentBinaryObject;

                    if (qrCodeNode != null)
                    {
                        apiResponse.Base64QrCode = qrCodeNode.Value;
                    }
                }
            }

            apiResponse.RequestType = "Invoice Clearance";
            apiResponse.StatusCode = response.StatusCode.ToString();

            if (apiResponse.ClearanceStatus == "CLEARED")
            {
                apiResponse.ICV = ICV;
                apiResponse.PIH = PIH;
                apiResponse.Base64SignedInvoice = apiResponse.ClearedInvoice;
                apiResponse.ClearedInvoice = null;
                apiResponse.XmlFileName = XmlFileName;

                if (apiResponse.InvoiceHash == null)
                {
                    apiResponse.InvoiceHash = requestContent.InvoiceHash;
                }

                if (!request.IsFromSwaggerClient)
                {
                    await LogApprovedInvoiceAsync(request, apiResponse);
                }
            }

            return ((HttpStatusCode)response.StatusCode, apiResponse);
        }
        catch (Exception ex)
        {
            PortalResult portalResult = new()
            {
                StatusCode = HttpStatusCode.OK.ToString(),
                Error = ex.Message
            };

            return (HttpStatusCode.InternalServerError, portalResult);
        }
    }


    public async Task<(HttpStatusCode StatusCode, PortalResult ResultContent)> ComplianceInvoiceAsync(GatewayRequestApi request)
    {
        try
        {
            //PortalResult result = await GetResultByInvoiceIdAsync(request.InvoiceId);
            //if (result != null)
            //{
            //    return (HttpStatusCode.OK, result);
            //};

            (int ICV, string PIH) = await GetLastICVandPIHAsync();

            Invoice invoice = MappingManager.GenerateInvoiceObject(request, _businessInfo, _businessDataCustomField, ICV, PIH);
            InvoiceGenerator ig = new(
                    invoice,
                    Encoding.UTF8.GetString(Convert.FromBase64String(_settings.PCSIDBinaryToken)),
                    _settings.EcSecp256k1Privkeypem
                );

            ig.GetSignedInvoiceXML(out string base64SignedInvoice, out string base64QrCode, out string XmlFileName, out string requestApi);

            var payloadJson = requestApi;

            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));
            _httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.CCSIDBinaryToken}:{_settings.CCSIDSecret}")));

            var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_settings.ComplianceCheckUrl, content);

            var resultContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<PortalResult>(resultContent);

            apiResponse.RequestType = "Invoice Compliant Check";
            apiResponse.StatusCode = response.StatusCode.ToString();

            if (apiResponse.ReportingStatus == "REPORTED")
            {
                apiResponse.ICV = ICV;
                apiResponse.PIH = PIH;
                apiResponse.Base64SignedInvoice = base64SignedInvoice;
                apiResponse.Base64QrCode = base64QrCode;
                apiResponse.XmlFileName = XmlFileName;

                //await LogApprovedInvoiceAsync(request, apiResponse);
            }

            return ((HttpStatusCode)response.StatusCode, apiResponse);
        }
        catch (Exception ex)
        {
            PortalResult portalResult = new()
            {
                StatusCode = HttpStatusCode.OK.ToString(),
                Error = ex.Message
            };

            return (HttpStatusCode.InternalServerError, portalResult);

            throw;
        }
    }

    public async Task<(HttpStatusCode StatusCode, PortalResult ResultContent)> GetSignedInvoice(GatewayRequestApi request)
    {
        try
        {
            PortalResult result = await GetResultByInvoiceIdAsync(request.InvoiceId);
            if (result != null)
            {
                return (HttpStatusCode.OK, result);
            };

            (int ICV, string PIH) = await GetLastICVandPIHAsync();

            Invoice invoice = MappingManager.GenerateInvoiceObject(request, _businessInfo, _businessDataCustomField, ICV, PIH);

            InvoiceGenerator ig = new(
                    invoice,
                    Encoding.UTF8.GetString(Convert.FromBase64String(_settings.PCSIDBinaryToken)),
                    _settings.EcSecp256k1Privkeypem
                );

            ig.GetSignedInvoiceXML(out string base64SignedInvoice, out string base64QrCode, out string XmlFileName, out string requestApi);

            PortalRequestApi portalRequestApi = JsonConvert.DeserializeObject<PortalRequestApi>(requestApi);

            PortalResult portalResult = new()
            {
                RequestType = "Generate Signed Invoice",
                StatusCode = HttpStatusCode.OK.ToString(),
                UUID = portalRequestApi.Uuid,
                InvoiceHash = portalRequestApi.InvoiceHash,
                Base64QrCode = base64QrCode,
                Base64SignedInvoice = base64SignedInvoice,
                XmlFileName = XmlFileName
            };

            return await Task.FromResult((HttpStatusCode.OK, portalResult));
        }
        catch (Exception ex)
        {
            PortalResult portalResult = new()
            {
                StatusCode = HttpStatusCode.OK.ToString(),
                Error = ex.Message
            };
            return await Task.FromResult((HttpStatusCode.InternalServerError, portalResult));
        }
    }

    private async Task LogApprovedInvoiceAsync(GatewayRequestApi request, PortalResult response)
    {
        var approvedInvoice = new ApprovedInvoice
        {
            InvoiceId = request.InvoiceId,
            InvoiceType = request.InvoiceType,
            InvoiceSubType = request.InvoiceSubType,
            IssueDate = request.IssueDate,
            Reference = request.Reference,
            CustomerName = request.CustomerName,
            InvoiceData = request.InvoiceData,

            Base64QrCode = response.Base64QrCode,
            Base64SignedInvoice = response.Base64SignedInvoice,
            XmlFileName = response.XmlFileName,

            ICV = response.ICV,
            PIH = response.PIH,

            RequestType = response.RequestType,
            InvoiceHash = response.InvoiceHash,
            ClearanceStatus = response.ClearanceStatus,
            ReportingStatus = response.ReportingStatus,
            Timestamp = DateTime.Now
        };

        _dbContext.ApprovedInvoices.Add(approvedInvoice);
        await _dbContext.SaveChangesAsync();
    }

    private async Task<(int ICV, string PIH)> GetLastICVandPIHAsync()
    {
        var lastInvoice = await _dbContext.ApprovedInvoices
                                           .OrderByDescending(invoice => invoice.Timestamp)
                                           .FirstOrDefaultAsync();

        if (lastInvoice == null)
        {
            return (1, "NWZlY2ViNjZmZmM4NmYzOGQ5NTI3ODZjNmQ2OTZjNzljMmRiYzIzOWRkNGU5MWI0NjcyOWQ3M2EyN2ZiNTdlOQ==");
        }

        return (lastInvoice.ICV + 1, lastInvoice.InvoiceHash);
    }

    private async Task<PortalResult> GetResultByInvoiceIdAsync(string invoiceId)
    {
        var invoice = await _dbContext.ApprovedInvoices
                                      .FirstOrDefaultAsync(invoice => invoice.InvoiceId == invoiceId);

        if (invoice != null)
        {
            var portalResult = invoice.ToPortalResult();
            return portalResult;
        }

        return null;
    }

}
