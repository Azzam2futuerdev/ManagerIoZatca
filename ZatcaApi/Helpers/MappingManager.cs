using Newtonsoft.Json;
using System.Text;
using Zatca.eInvoice.Helpers;
using Zatca.eInvoice.Models;
using ZatcaApi.Models;
using static ZatcaApi.Helpers.VATInfoHelper;

namespace ZatcaApi.Helpers
{
    public class MappingManager
    {

        public static Invoice GenerateInvoiceObject(GatewayRequestApi gatewayRequest, BusinessInfo businessInfo, BusinessDataCustomField businessDataCustomField, Int32 iCv, string pIh)
        {
            string decodedDataJson = DecodeInvoiceData(gatewayRequest.InvoiceData);

            ManagerInvoice mi = JsonConvert.DeserializeObject<ManagerInvoice>(decodedDataJson);

            string invoiceCurrencyCode = DetermineInvoiceCurrencyCode(mi); //Need more Invoice Sample for Foreign Curency
            double ExchangeRate = mi.Data.ExchangeRate;
            string TaxCurrencyCode = mi.BaseCurrency?.Code ?? "SAR";

            bool AmountsIncludeTax = mi.Data.AmountsIncludeTax;

            Invoice invoice = new()
            {
                ProfileID = "reporting:1.0",
                ID = new ID(mi.Data.Reference),
                UUID = mi.Data.Id,
                IssueDate = mi.Data.IssueDate,
                IssueTime = "00:00:00",
                // need improvement for InvoiceSubType
                InvoiceTypeCode = new InvoiceTypeCode((InvoiceType)gatewayRequest.InvoiceType, gatewayRequest.InvoiceSubType),
                DocumentCurrencyCode = invoiceCurrencyCode,
                TaxCurrencyCode = TaxCurrencyCode
            };

            invoice.BillingReference = CreateBillingReferences(mi);
            invoice.AdditionalDocumentReference = CreateAdditionalDocumentReferences(iCv, pIh).ToArray();
            invoice.AccountingSupplierParty = CreateAccountingSupplierParty(businessInfo);
            invoice.AccountingCustomerParty = CreateAccountingCustomerParty(gatewayRequest.CustomerInfo);

            invoice.Delivery = new Delivery()
            {
                ActualDeliveryDate = mi.Data.IssueDate,
                LatestDeliveryDate = mi.Data.IssueDate //?
            };

            invoice.PaymentMeans = CreatePaymentMeans(mi, businessDataCustomField);

            if (mi?.Data?.Lines != null)
            {
                invoice.AllowanceCharge = CreateAllowanceCharge(mi.Data.Lines, invoiceCurrencyCode, businessDataCustomField);
                invoice.InvoiceLine = CreateInvoiceLines(mi.Data.Lines, invoiceCurrencyCode, businessDataCustomField).ToArray();
                invoice.TaxTotal = CalculateTaxTotals(mi.Data.Lines, invoiceCurrencyCode, AmountsIncludeTax, businessDataCustomField).ToArray();
                invoice.LegalMonetaryTotal = CalculateLegalMonetaryTotal(mi.Data.Lines, invoiceCurrencyCode, AmountsIncludeTax);
            }

            return invoice;
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

        private static BillingReference CreateBillingReferences(ManagerInvoice mi)
        {
            InvoiceDocumentReference invoiceDocumentReference = null;

            if (mi.Data.SalesInvoice?.Reference != null)
            {
                invoiceDocumentReference = new InvoiceDocumentReference
                {
                    ID = new ID(mi.Data.SalesInvoice.Reference)
                };
            }
            else
            {
                if (mi.Data.PurchaseInvoice?.Reference != null)
                {
                    invoiceDocumentReference = new InvoiceDocumentReference
                    {
                        ID = new ID(mi.Data.PurchaseInvoice.Reference)
                    };
                }
            }

            return invoiceDocumentReference != null ? new BillingReference { InvoiceDocumentReference = invoiceDocumentReference } : null;

        }

        private static List<AdditionalDocumentReference> CreateAdditionalDocumentReferences(Int32 iCv, string pIh)
        {
            List<AdditionalDocumentReference> references = new ();

            AdditionalDocumentReference referenceICV = new ()
            {
                ID = new ID("ICV"),
                UUID = iCv.ToString()
            };
            references.Add(referenceICV);

            AdditionalDocumentReference referencePIH = new ()
            {
                ID = new ID("PIH"),
                Attachment = new Attachment
                {
                    EmbeddedDocumentBinaryObject = new EmbeddedDocumentBinaryObject(pIh)
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
        private static PaymentMeans CreatePaymentMeans(ManagerInvoice mi, BusinessDataCustomField businessDataCustomField)
        {
            var paymentMeansCode = 30; // Default value Credit
            var paymentMeans = mi.Data.CustomFields2?.Strings[businessDataCustomField.PaymentMeansCodeGuid];
            if (paymentMeans != null)
            {
                var parts = paymentMeans.Split('|');
                if (parts.Length >= 1 && int.TryParse(parts[0].Trim(), out int paymentCode))
                {
                    paymentMeansCode = paymentCode;
                }
            }

            return new PaymentMeans()
            {
                PaymentMeansCode = paymentMeansCode.ToString(),
                InstructionNote = mi.Data.Description
            };
        }

        private static AllowanceCharge CreateAllowanceCharge(List<Line> lines, string currencyCode, BusinessDataCustomField businessDataCustomField)
        {
            AllowanceCharge allowanceCharge = new ()
            {
                ChargeIndicator = false,
                //AllowanceChargeReasonCode = null,
                AllowanceChargeReason = "discount",
                Amount = new Amount(currencyCode, 0)
            };


            List<TaxCategory> taxCategories = new();

            foreach (var line in lines)
            {

                string itemTaxCategoryID = line.Item.CustomFields2?.Strings[businessDataCustomField.ItemTaxCategoryGuid];
                VATInfo vatInfo = GetVATInfo(itemTaxCategoryID);

                if (line.TaxCode != null)
                {
                    double rate = line.TaxCode?.Rate == null ? 0 : line.TaxCode.Rate;

                    TaxCategory taxCategory = new()
                    {
                        ID = new ID(vatInfo.CategoryID),
                        TaxExemptionReasonCode = rate == 0 ? vatInfo.ExemptReasonCode : null,
                        TaxExemptionReason = rate == 0 ? vatInfo.ExemptReason : null,
                        Percent = rate,
                        TaxScheme = new TaxScheme
                        {
                            ID = new ID("VAT")
                        }
                    };
                    taxCategories.Add(taxCategory);
                }
                else
                {
                    TaxCategory taxCategory = new()
                    {
                        ID = new ID(itemTaxCategoryID),
                        Percent = 0,
                        TaxScheme = new TaxScheme
                        {
                            ID = new ID("VAT")
                        }
                    };
                    taxCategories.Add(taxCategory);
                }
            }

            allowanceCharge.TaxCategory = taxCategories.ToArray();

            return allowanceCharge;
        }

        private static List<InvoiceLine> CreateInvoiceLines(List<Line> lines, string currencyCode, BusinessDataCustomField businessDataCustomField)
        {
            List<InvoiceLine> invoiceLines = new ();
            int i = 0;

            foreach (var line in lines)
            {
                string itemTaxCategoryID = line.Item.CustomFields2.Strings[businessDataCustomField.ItemTaxCategoryGuid];
                VATInfo vatInfo = GetVATInfo(itemTaxCategoryID);

                InvoiceLine invoiceLine = new()
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

                invoiceLine.Item.ClassifiedTaxCategory = new ClassifiedTaxCategory
                {

                    Percent = rate,
                    ID = new ID(vatInfo.CategoryID),
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

        private static List<TaxTotal> CalculateTaxTotals(List<Line> lines, string currencyCode, bool amountsIncludeTax, BusinessDataCustomField businessDataCustomField)
        {
            List<TaxTotal> taxTotals = new ();
            double totalTaxAmount = 0;
            List<TaxSubtotal> taxSubtotals = new ();

            foreach (var line in lines)
            {
                if (line.TaxCode != null)
                {
                    string itemTaxCategoryID = line.Item.CustomFields2.Strings[businessDataCustomField.ItemTaxCategoryGuid];
                    VATInfo vatInfo = GetVATInfo(itemTaxCategoryID);

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

                    TaxSubtotal taxSubtotal = new()
                    {
                        TaxableAmount = new Amount(currencyCode, (line.Qty * line.SalesUnitPrice) - line.DiscountAmount),
                        TaxAmount = new Amount(currencyCode, lineTaxAmount),

                        TaxCategory = new TaxCategory
                        {
                            Percent = rate,
                            ID = new ID(vatInfo.CategoryID),
                            TaxExemptionReasonCode = rate == 0 ? vatInfo.ExemptReasonCode : null,
                            TaxExemptionReason = rate == 0 ? vatInfo.ExemptReason : null,
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

            TaxTotal taxTotal = new()
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
                //if (line != null)
                //{
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
                //}
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
