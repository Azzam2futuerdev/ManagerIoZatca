using Newtonsoft.Json;
using Zatca.eInvoice.Models;

namespace ZatcaApi.Models
{
    public class ManagerInvoice
    {
        public BaseCurrency BaseCurrency { get; set; }
        public string PartyName { get; set; }
        public string PartyTaxInfo { get; set; }
        public string PartyCurrency { get; set; }
        public int InvoiceType { get; set; }
        public string InvoiceSubType { get; set; }
        public Data Data { get; set; }
        public string DecimalSeparator { get; set; }
        public Dictionary<string, ForeignCurrency> ForeignCurrencies { get; set; }
    }

    public class CurrencyInfo
    {
        public string Code { get; set; }
        public int DecimalPlaces { get; set; }
    }

    public class BaseCurrency
    {
        public string Code { get; set; } = "ASR";
        public int DecimalPlaces { get; set; } = 2;
    }
    public class ForeignCurrency
    {
        public string Code { get; set; }
        public int DecimalPlaces { get; set; }
    }
    public class Data
    {
        public bool CanBeRealizedCurrencyTransaction { get; set; }
        public string Description { get; set; }
        public bool Discount { get; set; } = false;
        public bool AmountsIncludeTax { get; set; } = false;

        private string _DueDateDate;
        public string DueDateDate
        {
            get => _DueDateDate;
            set
            {
                if (DateTime.TryParse(value, out DateTime parsedDate))
                {
                    _DueDateDate = parsedDate.ToString("yyyy-MM-dd");
                }
                else
                {
                    throw new ArgumentException("Invalid date format for DueDateDate.");
                }
            }
        }
        public double EarlyPaymentDiscountAmount { get; set; }
        public double ExchangeRate { get; set; } = 1;

        private string _issueDate;

        public string IssueDate
        {
            get => _issueDate;
            set
            {
                if (DateTime.TryParse(value, out DateTime parsedDate))
                {
                    _issueDate = parsedDate.ToString("yyyy-MM-dd");
                }
                else
                {
                    throw new ArgumentException("Invalid date format for IssueDate.");
                }
            }
        }
        public List<Line> Lines { get; set; }
        public string Reference { get; set; }
        public string Text { get; set; }
        public double WithholdingTaxAmount { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
        public CustomFields2 CustomFields2 { get; set; }
        public SalesInvoice SalesInvoice { get; set; }
        public PurchaseInvoice PurchaseInvoice { get; set; }


    }
    public class PurchaseInvoice
    {
        public string Reference { get; set; }
    }
    public class SalesInvoice
    {
        public string Reference { get; set; }
    }

    public class CustomFields2
    {
        public Dictionary<string, string> Strings { get; set; }
    }

    public class Line
    {
        public double DiscountAmount { get; set; } = 0;
        public Item Item { get; set; }
        public string LineDescription { get; set; }
        public double Qty { get; set; } = 0;
        public double SalesUnitPrice { get; set; } = 0;
        public double PurchaseUnitPrice { get; set; } = 0;

        [JsonIgnore]
        public double UnitPrice => SalesUnitPrice > PurchaseUnitPrice ? SalesUnitPrice : PurchaseUnitPrice;

        public TaxCode TaxCode { get; set; }
    }

    public class Item
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string UnitName { get; set; }
        public bool HasDefaultTaxCode { get; set; }
        public CustomFields2 CustomFields2 { get; set; }
    }

    public class TaxCode
    {
        public string Name { get; set; }
        public string Label { get; set; } = "";
        public double Rate { get; set; } = 0;
    }

}