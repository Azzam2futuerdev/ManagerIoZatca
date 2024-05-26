using System.Text.Json;
using System.Text;
using ZatcaApi.Models;
using Zatca.eInvoice.Models;
using Newtonsoft.Json;
using Zatca.eInvoice.Helpers;

public class ManagerMappingHelper()
{
    public static Invoice GenerateInvoice(GatewayRequestApi gatewayRequest, BusinessInfo businessInfo)
    {
        string decodedDataJson = Encoding.UTF8.GetString(Convert.FromBase64String(gatewayRequest.InvoiceData));
        if (decodedDataJson.StartsWith("\"") && decodedDataJson.EndsWith("\""))
        {
            decodedDataJson = decodedDataJson.Substring(1, decodedDataJson.Length - 2);
        }

        // Using Newtonsoft.Json for deserialization
        ManagerInvoice mi = JsonConvert.DeserializeObject<ManagerInvoice>(decodedDataJson);

        string InvoiceCurrencyCode = mi.BaseCurrency?.Code ?? "ASR";

        if (!string.IsNullOrEmpty(mi.Data.Customer.Currency))
        {
            ForeignCurrency foreignCurrency = mi.ForeignCurrencies?.GetValueOrDefault(mi.Data.Customer.Currency);
            if (foreignCurrency != null)
            {
                InvoiceCurrencyCode = foreignCurrency.Code;
            }
        }

        Invoice invoice = new Invoice
        {
            ProfileID = "reporting:1.0",
            ID = new ID(mi.Data.Reference),
            UUID = mi.Data.Id,
            IssueDate = mi.Data.IssueDate,
            IssueTime = "00:00:00",
            InvoiceTypeCode = new InvoiceTypeCode(InvoiceType.TaxInvoice, InvoiceSubType.Simplified),
            DocumentCurrencyCode = mi.Data.Customer.Currency,
            TaxCurrencyCode = mi.Data.Customer.Currency,
        };

        List<AdditionalDocumentReference> references = new List<AdditionalDocumentReference>
    {
        new AdditionalDocumentReference
        {
            ID = new ID("ICV"),
            UUID = "123"
        },
        new AdditionalDocumentReference
        {
            ID = new ID("PIH"),
            Attachment = new Attachment
            {
                EmbeddedDocumentBinaryObject = new EmbeddedDocumentBinaryObject("")
            }
        }
    };

        invoice.AdditionalDocumentReference = references.ToArray();

        AccountingSupplierParty accountingSupplierParty = new AccountingSupplierParty
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

        invoice.AccountingSupplierParty = accountingSupplierParty;

        CustomerInfo customerInfo = CustomerPartyParser.ParseCustomerParty(gatewayRequest.CustomerInfo);

        AccountingCustomerParty accountingCustomerParty = new AccountingCustomerParty
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

        invoice.AccountingCustomerParty = accountingCustomerParty;

        Delivery delivery = new Delivery
        {
            ActualDeliveryDate = null,
            LatestDeliveryDate = null
        };
        invoice.Delivery = delivery;

        PaymentMeans paymentMeans = new PaymentMeans
        {
            PaymentMeansCode = null,
            InstructionNote = null
        };
        invoice.PaymentMeans = paymentMeans;

        if (mi?.Data?.Lines != null)
        {
            AllowanceCharge allowanceCharge = new AllowanceCharge
            {
                ChargeIndicator = false,
                AllowanceChargeReasonCode = null,
                AllowanceChargeReason = "discount",
                Amount = new Amount(InvoiceCurrencyCode, 0),
            };

            List<TaxCategory> taxCategories = new List<TaxCategory>();
            double previousRate = -1;

            foreach (var line in mi.Data.Lines)
            {
                if (line.TaxCode != null && line.TaxCode.Rate != previousRate)
                {
                    double rate = line.TaxCode.Rate;
                    TaxCategory taxCategory = new TaxCategory
                    {
                        Percent = rate,
                        TaxScheme = new TaxScheme
                        {
                            ID = new ID("UN/ECE 5305", "6", "S")
                        }
                    };
                    taxCategories.Add(taxCategory);
                    previousRate = rate;
                }
            }

            allowanceCharge.TaxCategory = taxCategories.ToArray();

            invoice.AllowanceCharge = allowanceCharge;

            int i = 0;
            List<InvoiceLine> Lines = new List<InvoiceLine>();

            foreach (var line in mi.Data.Lines)
            {
                InvoiceLine invoiceLine = new InvoiceLine
                {
                    ID = new ID((++i).ToString()),
                    InvoicedQuantity = new InvoicedQuantity(line.Item.UnitName, line.Qty),
                    Item = new Zatca.eInvoice.Models.Item
                    {
                        Name = !string.IsNullOrEmpty(line.LineDescription) ? line.LineDescription : line.Item.ItemName
                    },
                    LineExtensionAmount = new Amount(InvoiceCurrencyCode, Math.Round((line.Qty * line.SalesUnitPrice) - line.DiscountAmount, 2)),
                    Price = new Price
                    {
                        PriceAmount = new Amount(InvoiceCurrencyCode, line.SalesUnitPrice),
                        AllowanceCharge = new AllowanceCharge
                        {
                            ChargeIndicator = false,
                            AllowanceChargeReasonCode = null,
                            AllowanceChargeReason = "discount",
                            Amount = new Amount(InvoiceCurrencyCode, line.DiscountAmount)
                        }
                    },
                    TaxTotal = new TaxTotal()
                };

                if (line.TaxCode != null)
                {
                    invoiceLine.Item.ClassifiedTaxCategory = new ClassifiedTaxCategory
                    {
                        ID = new ID("S"),
                        Percent = line.TaxCode.Rate,
                        TaxScheme = new TaxScheme
                        {
                            ID = new ID("VAT")
                        }
                    };

                    invoiceLine.TaxTotal.TaxAmount = new Amount(InvoiceCurrencyCode, Math.Round((line.Qty * line.SalesUnitPrice) * line.TaxCode.Rate, 2));
                    invoiceLine.TaxTotal.RoundingAmount = new Amount(InvoiceCurrencyCode, Math.Round((line.Qty * line.SalesUnitPrice) * line.TaxCode.Rate, 2));
                }

                Lines.Add(invoiceLine);
            }

            invoice.InvoiceLine = Lines.ToArray();

            // Invoice Tax Total
            List<TaxTotal> taxTotals = new List<TaxTotal>();

            double taxTotal = 0;
            foreach (var line in mi.Data.Lines)
            {
                if (line.TaxCode != null)
                {
                    taxTotal += Math.Round(((line.Qty * line.SalesUnitPrice) - line.DiscountAmount) * line.TaxCode.Rate, 2);
                }
            }

            TaxTotal taxTotal1 = new TaxTotal
            {
                TaxAmount = new Amount(InvoiceCurrencyCode, taxTotal)
            };
            taxTotals.Add(taxTotal1);

            taxTotal = 0;
            List<TaxSubtotal> taxSubtotals = new List<TaxSubtotal>();

            foreach (var line in mi.Data.Lines)
            {
                if (line.TaxCode != null)
                {
                    taxTotal += Math.Round(((line.Qty * line.SalesUnitPrice) - line.DiscountAmount) * line.TaxCode.Rate, 2);

                    TaxSubtotal taxSubtotal = new TaxSubtotal
                    {
                        TaxableAmount = new Amount(InvoiceCurrencyCode, Math.Round((line.Qty * line.SalesUnitPrice) - line.DiscountAmount, 2)),
                        TaxAmount = new Amount(InvoiceCurrencyCode, Math.Round(((line.Qty * line.SalesUnitPrice) - line.DiscountAmount) * line.TaxCode.Rate, 2)),
                        TaxCategory = new TaxCategory
                        {
                            ID = new ID("UN/ECE 5305", "6", "S"),
                            Percent = line.TaxCode.Rate,
                            TaxScheme = new TaxScheme
                            {
                                ID = new ID("UN/ECE 5153", "6", "VAT")
                            }
                        }
                    };
                    taxSubtotals.Add(taxSubtotal);
                }
            }

            TaxTotal taxTotal2 = new TaxTotal
            {
                TaxAmount = new Amount(InvoiceCurrencyCode, taxTotal),
                TaxSubtotal = taxSubtotals.ToArray()
            };
            taxTotals.Add(taxTotal2);

            invoice.TaxTotal = taxTotals.ToArray();

            // Legal Monetary Total
            double lineExtensionAmount = 0;
            double taxExclusiveAmount = 0;
            double taxInclusiveAmount = 0;
            double allowanceTotalAmount = 0;

            foreach (var line in mi.Data.Lines)
            {
                if (line != null)
                {
                    double lineQty = line.Qty;
                    double lineSalesUnitPrice = line.SalesUnitPrice;
                    double lineDiscountAmount = line.DiscountAmount;
                    double lineTaxRate = line.TaxCode?.Rate ?? 0;

                    lineExtensionAmount += Math.Round((lineQty * lineSalesUnitPrice) - lineDiscountAmount, 2);
                    taxExclusiveAmount += Math.Round((lineQty * lineSalesUnitPrice) - lineDiscountAmount, 2);

                    double taxAmount = line.TaxCode != null ? Math.Round(((lineQty * lineSalesUnitPrice) - lineDiscountAmount) * lineTaxRate, 2) : 0;

                    taxInclusiveAmount += Math.Round(((lineQty * lineSalesUnitPrice) - lineDiscountAmount) + taxAmount, 2);
                    allowanceTotalAmount += lineDiscountAmount;
                }
            }

            LegalMonetaryTotal legalMonetaryTotal = new LegalMonetaryTotal
            {
                LineExtensionAmount = new Amount(InvoiceCurrencyCode, lineExtensionAmount),
                TaxExclusiveAmount = new Amount(InvoiceCurrencyCode, taxExclusiveAmount),
                TaxInclusiveAmount = new Amount(InvoiceCurrencyCode, taxInclusiveAmount),
                AllowanceTotalAmount = new Amount(InvoiceCurrencyCode, allowanceTotalAmount),
                PayableAmount = new Amount(InvoiceCurrencyCode, taxInclusiveAmount)
            };

            invoice.LegalMonetaryTotal = legalMonetaryTotal;
        }
        else
        {
            Console.WriteLine("No lines found in the invoice data.");
        }

        return invoice;
    }

}

