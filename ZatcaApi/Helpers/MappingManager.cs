using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using Zatca.eInvoice.Helpers;
using Zatca.eInvoice.Models;
using ZatcaApi.Models;

namespace ZatcaApi.Helpers
{
    public class MappingManager
    {
        public static Invoice GenerateInvoiceObject(GatewayRequestApi gatewayRequest, BusinessInfo businessInfo, Int32 iCv = 0, int pIh = 0)
        {
            string decodedDataJson = DecodeInvoiceData(gatewayRequest.InvoiceData);

            ManagerInvoice mi = JsonConvert.DeserializeObject<ManagerInvoice>(decodedDataJson);
            string invoiceCurrencyCode = DetermineInvoiceCurrencyCode(mi); //Need more Invoice Sample for Foreign Curency

            double ExchangeRate = mi.Data.ExchangeRate;

            bool AmountsIncludeTax = mi.Data.AmountsIncludeTax;

            Invoice invoice = new Invoice
            {
                ProfileID = "reporting:1.0",
                ID = new ID(mi.Data.Reference),
                UUID = mi.Data.Id,
                IssueDate = mi.Data.IssueDate,
                IssueTime = "00:00:00",
                InvoiceTypeCode = new InvoiceTypeCode((InvoiceType)gatewayRequest.InvoiceType, (InvoiceSubType)gatewayRequest.InvoiceSubType),
                DocumentCurrencyCode = invoiceCurrencyCode,
                TaxCurrencyCode = mi.BaseCurrency?.Code ?? "SAR"
            };

            invoice.AdditionalDocumentReference = CreateAdditionalDocumentReferences(iCv, pIh).ToArray();
            invoice.AccountingSupplierParty = CreateAccountingSupplierParty(businessInfo);
            invoice.AccountingCustomerParty = CreateAccountingCustomerParty(gatewayRequest.CustomerInfo);

            invoice.Delivery = new Delivery()
            {
                ActualDeliveryDate = mi.Data.IssueDate,
                LatestDeliveryDate = mi.Data.IssueDate //?
            };

            invoice.PaymentMeans = new PaymentMeans("10");

            if (mi?.Data?.Lines != null)
            {
                invoice.AllowanceCharge = CreateAllowanceCharge(mi.Data.Lines, invoiceCurrencyCode);
                invoice.InvoiceLine = CreateInvoiceLines(mi.Data.Lines, invoiceCurrencyCode).ToArray();
                invoice.TaxTotal = CalculateTaxTotals(mi.Data.Lines, invoiceCurrencyCode, AmountsIncludeTax).ToArray();
                invoice.LegalMonetaryTotal = CalculateLegalMonetaryTotal(mi.Data.Lines, invoiceCurrencyCode, AmountsIncludeTax);
            }

            return invoice;
        }

        //need more info about PIH
        private static string GeneratePIH(int pIh)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(pIh.ToString());
                return Convert.ToBase64String(sha256.ComputeHash(inputBytes));
            }
        }

        private static string DecodeInvoiceData(string encodedData)
        {
            string decodedDataJson = Encoding.UTF8.GetString(Convert.FromBase64String(encodedData));
            if (decodedDataJson.StartsWith("\"") && decodedDataJson.EndsWith("\""))
            {
                decodedDataJson = decodedDataJson.Substring(1, decodedDataJson.Length - 2);
            }
            return decodedDataJson;
        }

        private static string DetermineInvoiceCurrencyCode(ManagerInvoice mi)
        {
            string invoiceCurrencyCode = mi.BaseCurrency?.Code ?? "SAR";
            if (!string.IsNullOrEmpty(mi.Data.Customer.Currency))
            {
                ForeignCurrency foreignCurrency = mi.ForeignCurrencies?.GetValueOrDefault(mi.Data.Customer.Currency);
                if (foreignCurrency != null)
                {
                    invoiceCurrencyCode = foreignCurrency.Code;
                }
            }

            return invoiceCurrencyCode;
        }

        private static List<AdditionalDocumentReference> CreateAdditionalDocumentReferences(Int32 iCv = 1, int pIh = 0)
        {
            List<AdditionalDocumentReference> references = new List<AdditionalDocumentReference>();

            AdditionalDocumentReference referenceICV = new AdditionalDocumentReference
            {
                ID = new ID("ICV"),
                UUID = iCv.ToString()
            };
            references.Add(referenceICV);

            AdditionalDocumentReference referencePIH = new AdditionalDocumentReference
            {
                ID = new ID("PIH"),
                Attachment = new Attachment
                {
                    EmbeddedDocumentBinaryObject = new EmbeddedDocumentBinaryObject(GeneratePIH(pIh))
                }
            };
            references.Add(referencePIH);

            return references;
        }

        private static AccountingSupplierParty CreateAccountingSupplierParty(BusinessInfo businessInfo)
        {
            return new AccountingSupplierParty
            {
                Party = new Party
                {
                    PartyIdentification = new PartyIdentification
                    {
                        ID = new ID
                        {
                            SchemeID = businessInfo.SchemeID,
                            Value = businessInfo.ID
                        }
                    },
                    PostalAddress = new PostalAddress
                    {
                        StreetName = businessInfo.StreetName,
                        BuildingNumber = businessInfo.BuildingNumber,
                        CitySubdivisionName = businessInfo.CitySubdivisionName,
                        CityName = businessInfo.CityName,
                        PostalZone = businessInfo.PostalZone,
                        Country = new Country
                        {
                            IdentificationCode = businessInfo.CountryIdentificationCode
                        }
                    },
                    PartyTaxScheme = new PartyTaxScheme
                    {
                        CompanyID = businessInfo.CompanyID,
                        TaxScheme = new TaxScheme
                        {
                            ID = new ID(businessInfo.TaxSchemeID)
                        }
                    },
                    PartyLegalEntity = new PartyLegalEntity
                    {
                        RegistrationName = businessInfo.RegistrationName
                    }
                }
            };
        }

        private static AccountingCustomerParty CreateAccountingCustomerParty(string customerInfoJson)
        {
            CustomerInfo customerInfo = CustomerPartyParser.ParseCustomerParty(customerInfoJson);

            return new AccountingCustomerParty
            {
                Party = new Party
                {
                    PostalAddress = new PostalAddress
                    {
                        StreetName = customerInfo.StreetName,
                        BuildingNumber = customerInfo.BuildingNumber,
                        CitySubdivisionName = customerInfo.CitySubdivisionName,
                        CityName = customerInfo.CityName,
                        PostalZone = customerInfo.PostalZone,
                        Country = new Country
                        {
                            IdentificationCode = customerInfo.CountryIdentificationCode
                        }
                    },
                    PartyTaxScheme = new PartyTaxScheme
                    {
                        CompanyID = customerInfo.CompanyID,
                        TaxScheme = new TaxScheme
                        {
                            ID = new ID(customerInfo.TaxSchemeID)
                        }
                    },
                    PartyLegalEntity = new PartyLegalEntity
                    {
                        RegistrationName = customerInfo.RegistrationName
                    }
                }
            };
        }

        private static AllowanceCharge CreateAllowanceCharge(List<Line> lines, string currencyCode)
        {
            AllowanceCharge allowanceCharge = new AllowanceCharge
            {
                ChargeIndicator = false,
                //AllowanceChargeReasonCode = null,
                AllowanceChargeReason = "discount",
                Amount = new Amount(currencyCode, 0)
            };

            List<TaxCategory> taxCategories = new List<TaxCategory>();

            foreach (var line in lines)
            {
                if (line.TaxCode != null)
                {
                    double rate = line.TaxCode?.Rate == null ? 0 : line.TaxCode.Rate;
                    string taxName = line.TaxCode.Name ?? "";
                    TaxCategory taxCategory = new TaxCategory
                    {
                        ID = new ID("UN/ECE 5305", "6", rate == 0 ? (taxName.Contains("Exempt") ? "E" : "Z") : "S"),
                        //TaxExemptionReason = "",  "E"
                        //TaxExemptionReasonCode = "", "E"
                        Percent = rate,
                        TaxScheme = new TaxScheme
                        {
                            ID = new ID("UN/ECE 5153", "6", "VAT")
                        }
                    };
                    taxCategories.Add(taxCategory);
                }
                else
                {
                    TaxCategory taxCategory = new TaxCategory
                    {
                        ID = new ID("UN/ECE 5305", "6", "O"), 
                        Percent = 0,
                        TaxScheme = new TaxScheme
                        {
                            ID = new ID("UN/ECE 5153", "6", "VAT")
                        }
                    };
                    taxCategories.Add(taxCategory);
                }
            }

            allowanceCharge.TaxCategory = taxCategories.ToArray();

            return allowanceCharge;
        }

        private static List<InvoiceLine> CreateInvoiceLines(List<Line> lines, string currencyCode)
        {
            List<InvoiceLine> invoiceLines = new List<InvoiceLine>();
            int i = 0;

            foreach (var line in lines)
            {
                InvoiceLine invoiceLine = new InvoiceLine
                {
                    ID = new ID((++i).ToString()),
                    InvoicedQuantity = new InvoicedQuantity(line.Item.UnitName, line.Qty),
                    Item = new Zatca.eInvoice.Models.Item
                    {
                        Name = line.Item.ItemName
                    },
                    LineExtensionAmount = new Amount(currencyCode, Math.Round((line.Qty * line.SalesUnitPrice) - line.DiscountAmount, 2)),
                    Price = new Price
                    {
                        PriceAmount = new Amount(currencyCode, line.SalesUnitPrice),
                        AllowanceCharge = new AllowanceCharge
                        {
                            ChargeIndicator = true,
                            AllowanceChargeReasonCode = null,
                            AllowanceChargeReason = "discount",
                            Amount = new Amount(currencyCode, line.DiscountAmount)
                        }
                    }
                };

                //if (line.TaxCode != null)
                //{

                double rate = line.TaxCode?.Rate == null ? 0 : line.TaxCode.Rate;
                string taxName = line.TaxCode.Name ?? "";

                invoiceLine.Item.ClassifiedTaxCategory = new ClassifiedTaxCategory
                {

                    Percent = rate,
                    ID = new ID(rate == 0 ? (taxName.Contains("Exempt") ? "E" : "Z") : "S"),
                    //TaxExemptionReason = "",  "E"
                    //TaxExemptionReasonCode = "", "E"
                    TaxScheme = new TaxScheme
                    {
                        ID = new ID("VAT")
                    }
                };
                invoiceLine.TaxTotal = new TaxTotal
                {
                    TaxAmount = new Amount(currencyCode, Math.Round((line.Qty * line.SalesUnitPrice - line.DiscountAmount) * (rate / 100), 2)),
                    RoundingAmount = new Amount(currencyCode, Math.Round(((line.Qty * line.SalesUnitPrice) - line.DiscountAmount) + ((line.Qty * line.SalesUnitPrice - line.DiscountAmount) * (rate / 100)), 2))
                };
                //}

                invoiceLines.Add(invoiceLine);
            }

            return invoiceLines;
        }

        private static List<TaxTotal> CalculateTaxTotals(List<Line> lines, string currencyCode, bool amountsIncludeTax)
        {
            List<TaxTotal> taxTotals = new List<TaxTotal>();
            double totalTaxAmount = 0;
            List<TaxSubtotal> taxSubtotals = new List<TaxSubtotal>();

            foreach (var line in lines)
            {
                if (line.TaxCode != null)
                {
                    double lineTaxAmount;
                    if (amountsIncludeTax)
                    {
                        lineTaxAmount = Math.Round((line.Qty * line.SalesUnitPrice - line.DiscountAmount) * (line.TaxCode.Rate / (100 + line.TaxCode.Rate)), 2);
                    }
                    else
                    {
                        lineTaxAmount = Math.Round((line.Qty * line.SalesUnitPrice - line.DiscountAmount) * (line.TaxCode.Rate / 100), 2);
                    }
                    totalTaxAmount += lineTaxAmount;

                    
                    double rate = line.TaxCode?.Rate == null ? 0 : line.TaxCode.Rate;
                    string taxName = line.TaxCode.Name ?? "";

                    TaxSubtotal taxSubtotal = new TaxSubtotal
                    {
                        TaxableAmount = new Amount(currencyCode, (line.Qty * line.SalesUnitPrice) - line.DiscountAmount),
                        TaxAmount = new Amount(currencyCode, lineTaxAmount),

                        TaxCategory = new TaxCategory
                        {
                            Percent = rate,
                            ID = new ID(rate == 0 ? (taxName.Contains("Exempt") ? "E" : "Z") : "S"),
                            //TaxExemptionReasonCode = rate == 0 ? "VATEX - SA - 35" : null, 
                            //TaxExemptionReason = rate == 0 ? "Medicines and medical equipment | الأدوية والمعدات الطبية" : null,
                            TaxScheme = new TaxScheme
                            {
                                ID = new ID("VAT")
                            }
                        }
                    };
                    taxSubtotals.Add(taxSubtotal);
                }
            }

            taxTotals.Add(new TaxTotal
            {
                TaxAmount = new Amount(currencyCode, totalTaxAmount),
            });

            TaxTotal taxTotal = new TaxTotal
            {
                TaxAmount = new Amount(currencyCode, totalTaxAmount),
                TaxSubtotal = taxSubtotals.ToArray()
            };

            taxTotals.Add(taxTotal);

            return taxTotals;
        }

        
        private static LegalMonetaryTotal CalculateLegalMonetaryTotal(List<Line> lines, string currencyCode, bool amountsIncludeTax)
        {
            double lineExtensionAmount = 0;
            double taxExclusiveAmount = 0;
            double taxInclusiveAmount = 0;
            double allowanceTotalAmount = 0;

            foreach (var line in lines)
            {
                if (line != null)
                {
                    double lineQty = line.Qty;
                    double lineSalesUnitPrice = line.SalesUnitPrice;
                    double lineDiscountAmount = line.DiscountAmount;
                    double lineTaxRate = line.TaxCode?.Rate ?? 0;

                    if (amountsIncludeTax)
                    {
                        // If AmountsIncludeTax is true, tax is already included in the line amounts
                        lineExtensionAmount += Math.Round((lineQty * lineSalesUnitPrice) - lineDiscountAmount, 2);
                        taxInclusiveAmount += Math.Round((lineQty * lineSalesUnitPrice) - lineDiscountAmount, 2);
                        taxExclusiveAmount += Math.Round(((lineQty * lineSalesUnitPrice) - lineDiscountAmount) / (1 + (lineTaxRate / 100)), 2);
                    }
                    else
                    {
                        // If AmountsIncludeTax is false, calculate tax based on the line amounts
                        lineExtensionAmount += Math.Round((lineQty * lineSalesUnitPrice) - lineDiscountAmount, 2);
                        taxExclusiveAmount += Math.Round((lineQty * lineSalesUnitPrice) - lineDiscountAmount, 2);
                        double taxAmount = line.TaxCode != null ? Math.Round(((lineQty * lineSalesUnitPrice - lineDiscountAmount) * (lineTaxRate / 100)), 2) : 0;
                        taxInclusiveAmount += Math.Round(((lineQty * lineSalesUnitPrice) - lineDiscountAmount) + taxAmount, 2);
                    }

                    allowanceTotalAmount += lineDiscountAmount;
                }
            }

            return new LegalMonetaryTotal
            {
                LineExtensionAmount = new Amount(currencyCode, lineExtensionAmount),
                TaxExclusiveAmount = new Amount(currencyCode, taxExclusiveAmount),
                TaxInclusiveAmount = new Amount(currencyCode, taxInclusiveAmount),
                AllowanceTotalAmount = new Amount(currencyCode, allowanceTotalAmount),
                PrepaidAmount = new Amount(currencyCode, 0),
                PayableAmount = new Amount(currencyCode, taxInclusiveAmount)
            };
        }
    }
}
