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

            string invoiceCurrencyCode = DetermineInvoiceCurrencyCode(mi);
            string TaxCurrencyCode = "SAR";

            Invoice invoice = new()
            {
                ProfileID = "reporting:1.0",
                ID = new ID(mi.Data.Reference),
                UUID = mi.Data.Id,
                IssueDate = mi.Data.IssueDate,
                IssueTime = "00:00:00",
                // need improvement for InvoiceSubType 
                InvoiceTypeCode = new InvoiceTypeCode((InvoiceType)mi.InvoiceType, mi.InvoiceSubType),
                DocumentCurrencyCode = invoiceCurrencyCode,
                TaxCurrencyCode = TaxCurrencyCode
            };

            invoice.BillingReference = CreateBillingReferences(mi);
            invoice.AdditionalDocumentReference = CreateAdditionalDocumentReferences(iCv, pIh).ToArray();
            invoice.AccountingSupplierParty = CreateAccountingSupplierParty(businessInfo);
            invoice.AccountingCustomerParty = CreateAccountingCustomerParty(mi.PartyTaxInfo);

            invoice.Delivery = new Delivery()
            {
                ActualDeliveryDate = mi.Data.IssueDate,
                LatestDeliveryDate = mi.Data.IssueDate //?
            };

            invoice.PaymentMeans = CreatePaymentMeans(mi, businessDataCustomField);

            if (mi?.Data != null)
            {
                //AllowanceCharge on Document Level
                //invoice.AllowanceCharge = CreateAllowanceCharge(mi, invoiceCurrencyCode, businessDataCustomField);
                invoice.InvoiceLine = CreateInvoiceLines(mi, invoiceCurrencyCode, businessDataCustomField).ToArray();
                invoice.TaxTotal = CalculateTaxTotals(mi, invoiceCurrencyCode, TaxCurrencyCode, businessDataCustomField).ToArray();
                invoice.LegalMonetaryTotal = CalculateLegalMonetaryTotal(mi, invoiceCurrencyCode);
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
            if (!string.IsNullOrEmpty(mi.PartyCurrency))
            {
                ForeignCurrency foreignCurrency = mi.ForeignCurrencies?.GetValueOrDefault(mi.PartyCurrency);
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
            List<AdditionalDocumentReference> references = new();

            AdditionalDocumentReference referenceICV = new()
            {
                ID = new ID("ICV"),
                UUID = iCv.ToString()
            };
            references.Add(referenceICV);

            AdditionalDocumentReference referencePIH = new()
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
            var paymentMeansCode = 30;
            string paymentMeans = null;
            string instructionNote = null;

            if (mi.Data.CustomFields2?.Strings != null)
            {
                mi.Data.CustomFields2.Strings.TryGetValue(businessDataCustomField.PaymentMeansCodeGuid, out paymentMeans);
                mi.Data.CustomFields2.Strings.TryGetValue(businessDataCustomField.InstructionNoteGuid, out instructionNote);
            }

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
                InstructionNote = instructionNote,
            };
        }

        private static AllowanceCharge CreateAllowanceCharge(ManagerInvoice mi, string currencyCode, BusinessDataCustomField businessDataCustomField)
        {
            List<Line> lines = mi.Data.Lines;
            bool hasDiscount = mi.Data.Discount;

            double totalDiscount = 0;

            if (hasDiscount)
            {
                List<TaxCategory> taxCategories = new();

                foreach (var line in lines)
                {
                    totalDiscount += line.DiscountAmount;

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

                AllowanceCharge allowanceCharge = new()
                {
                    ChargeIndicator = false,
                    AllowanceChargeReason = "discount",
                    Amount = new Amount(currencyCode, totalDiscount)
                };

                allowanceCharge.TaxCategory = taxCategories.ToArray();

                return allowanceCharge;

            }

            return null;
        }

        
        private static List<InvoiceLine> CreateInvoiceLines(ManagerInvoice mi, string currencyCode,  BusinessDataCustomField businessDataCustomField)
        {
            List<Line> lines = mi.Data.Lines;
            bool amountsIncludeTax = mi.Data.AmountsIncludeTax;
            bool hasDiscount = mi.Data.Discount;

            List<InvoiceLine> invoiceLines = new();
            int i = 0;

            foreach (var line in lines)
            {
                string itemTaxCategoryID = line.Item.CustomFields2.Strings[businessDataCustomField.ItemTaxCategoryGuid];
                VATInfo vatInfo = GetVATInfo(itemTaxCategoryID);

                double percent = (line.TaxCode?.Rate ?? 0) / 100;
                double invoicedQuantity = Math.Round(line.Qty,4);

                //There is no example of how to calculate discounts at Invoice-line level.
                //Temporarily skip the discount and use price - discount as the price at the Invoice-line level.

                //double priceAmount = Math.Round(amountsIncludeTax ? line.UnitPrice / (1 + percent) : line.UnitPrice,4);
                //double discount = Math.Round(amountsIncludeTax ? line.DiscountAmount / (1 + percent) : line.DiscountAmount,4);
                //double lineExtensionAmount = Math.Round((invoicedQuantity * priceAmount) - discount, 2);

                double discount = line.DiscountAmount;
                double priceAmount = Math.Round(amountsIncludeTax ? (line.UnitPrice - discount) / (1 + percent) : (line.UnitPrice - discount), 4);
                double lineExtensionAmount = Math.Round((invoicedQuantity * priceAmount), 2);

                double taxAmount = Math.Round(lineExtensionAmount * percent, 2);

                InvoiceLine invoiceLine = new()
                {
                    ID = new ID((++i).ToString()),
                    InvoicedQuantity = new InvoicedQuantity(line.Item.UnitName, invoicedQuantity),
                    Item = new Zatca.eInvoice.Models.Item
                    {
                        Name = line.Item.ItemName
                    },
                    LineExtensionAmount = new Amount(currencyCode, lineExtensionAmount),
                    Price = new Price
                    {
                        PriceAmount = new Amount(currencyCode, priceAmount),
                        //AllowanceCharge = hasDiscount ? new AllowanceCharge
                        //{
                        //    ChargeIndicator = false, // Set to false for a discount
                        //    AllowanceChargeReasonCode = null,
                        //    AllowanceChargeReason = "discount",
                        //    Amount = new Amount(currencyCode, discount)
                        //} : null
                    }
                };

                double rate = line.TaxCode?.Rate ?? 0;

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
                    TaxAmount = new Amount(currencyCode, taxAmount),
                    RoundingAmount = new Amount(currencyCode, lineExtensionAmount + taxAmount)
                };

                invoiceLines.Add(invoiceLine);
            }

            return invoiceLines;
        }

        
        private static List<TaxTotal> CalculateTaxTotals(ManagerInvoice mi, string currencyCode, string taxCurrencyCode, BusinessDataCustomField businessDataCustomField)
        {
            List<Line> lines = mi.Data.Lines;
            bool amountsIncludeTax = mi.Data.AmountsIncludeTax;
            double exchangeRate = mi.Data.ExchangeRate == 0 ? 1 : mi.Data.ExchangeRate;


            List<TaxTotal> taxTotals = new();
            double totalTaxAmount = 0;
            List<TaxSubtotal> taxSubtotals = new();

            foreach (var line in lines)
            {
                if (line.TaxCode != null)
                {
                    string itemTaxCategoryID = line.Item.CustomFields2.Strings[businessDataCustomField.ItemTaxCategoryGuid];
                    VATInfo vatInfo = GetVATInfo(itemTaxCategoryID);

                    double percent = (line.TaxCode?.Rate ?? 0) / 100;
                    double invoicedQuantity = Math.Round(line.Qty, 4);

                    //There is no example of how to calculate discounts at Invoice-line level.
                    //Temporarily skip the discount and use price - discount as the price at the Invoice-line level.
                    //double priceAmount = Math.Round(amountsIncludeTax ? line.UnitPrice / (1 + percent) : line.UnitPrice,4);
                    //double discount = Math.Round(amountsIncludeTax ? line.DiscountAmount / (1 + percent) : line.DiscountAmount,4);
                    //double lineExtensionAmount = Math.Round((invoicedQuantity * priceAmount) - discount, 2);

                    double discount = line.DiscountAmount;
                    double priceAmount = Math.Round(amountsIncludeTax ? (line.UnitPrice - discount) / (1 + percent) : (line.UnitPrice - discount), 4);
                    double lineExtensionAmount = Math.Round((invoicedQuantity * priceAmount), 2);

                    double taxAmount = Math.Round(lineExtensionAmount * percent, 2);

                    totalTaxAmount += taxAmount;

                    double rate = line.TaxCode?.Rate ?? 0;

                    TaxSubtotal taxSubtotal = new()
                    {
                        TaxableAmount = new Amount(currencyCode, lineExtensionAmount),
                        TaxAmount = new Amount(currencyCode, taxAmount),
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

            TaxTotal taxTotalWithSubtotals = new()
            {
                TaxAmount = new Amount(currencyCode, totalTaxAmount),
                TaxSubtotal = taxSubtotals.ToArray()
            };
            taxTotals.Add(taxTotalWithSubtotals);

            TaxTotal taxTotalWithoutSubtotals = new()
            {
                TaxAmount = new Amount(taxCurrencyCode, totalTaxAmount * exchangeRate)
            };
            taxTotals.Add(taxTotalWithoutSubtotals);

            return taxTotals;
        }

        private static LegalMonetaryTotal CalculateLegalMonetaryTotal(ManagerInvoice mi, string currencyCode)
        {
            List<Line> lines = mi.Data.Lines;
            bool amountsIncludeTax = mi.Data.AmountsIncludeTax;

            double sumLineExtensionAmount = 0;
            double sumTaxExclusiveAmount = 0;
            double sumTaxInclusiveAmount = 0;
            double sumAllowanceTotalAmount = 0;

            foreach (var line in lines)
            {
                double percent = (line.TaxCode?.Rate ?? 0) / 100;
                double invoicedQuantity = Math.Round(line.Qty, 4);

                //There is no example of how to calculate discounts at Invoice-line level.
                //Temporarily skip the discount and use price - discount as the price at the Invoice-line level.
                //double priceAmount = Math.Round(amountsIncludeTax ? line.UnitPrice / (1 + percent) : line.UnitPrice,4);
                //double discount = Math.Round(amountsIncludeTax ? line.DiscountAmount / (1 + percent) : line.DiscountAmount,4);
                //double lineExtensionAmount = Math.Round((invoicedQuantity * priceAmount) - discount, 2);

                double discount = line.DiscountAmount;
                double priceAmount = Math.Round(amountsIncludeTax ? (line.UnitPrice - discount) / (1 + percent) : (line.UnitPrice - discount), 4);
                double lineExtensionAmount = Math.Round((invoicedQuantity * priceAmount), 2);

                double taxAmount = Math.Round(lineExtensionAmount * percent, 2);


                sumLineExtensionAmount += lineExtensionAmount;
                sumTaxExclusiveAmount += lineExtensionAmount;
                sumTaxInclusiveAmount += lineExtensionAmount + taxAmount;
                //sumAllowanceTotalAmount += discount;
            }

            return new LegalMonetaryTotal
            {
                LineExtensionAmount = new Amount(currencyCode, sumLineExtensionAmount),
                TaxExclusiveAmount = new Amount(currencyCode, sumTaxExclusiveAmount),
                TaxInclusiveAmount = new Amount(currencyCode, sumTaxInclusiveAmount),
                AllowanceTotalAmount = new Amount(currencyCode, sumAllowanceTotalAmount),
                PrepaidAmount = new Amount(currencyCode, 0),
                PayableAmount = new Amount(currencyCode, sumTaxInclusiveAmount)
            };
        }

    }
}
