using Newtonsoft.Json;

public class ManagerInvoice
{
    public BaseCurrency BaseCurrency { get; set; }
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
    public Customer Customer { get; set; }
    public string Description { get; set; }
    
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
    public double ExchangeRate { get; set; } = 0;
    
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
    public SalesOrder SalesOrder { get; set; }
    public string Text { get; set; }
    public double WithholdingTaxAmount { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }  
    public bool AmountsIncludeTax { get; set; } = false;

}

public class Customer
{
    public string Currency { get; set; }
    public CustomFields2 CustomFields2 { get; set; }
    public string Name { get; set; }
}

public class CustomFields2
{
    public Strings Strings { get; set; }
}

public class Strings
{
    public string _93f79973_5346_4c6b_b912_90ea9bbf69c2 { get; set; }
}

public class Line
{
    public double DiscountAmount { get; set; } = 0;
    public Item Item { get; set; }
    public string LineDescription { get; set; }
    public double Qty { get; set; } = 0;
    public double SalesUnitPrice { get; set; } = 0;
    public TaxCode TaxCode { get; set; }
}

public class Item
{
    public string ItemName { get; set; }
    public string UnitName { get; set; }
}

public class TaxCode
{
    public string Name { get; set; }
    public string Label { get; set; } = "";
    public double Rate { get; set; } = 0;
}

public class SalesOrder
{
    private string _Date;

    public string Date
    {
        get => _Date;
        set
        {
            if (DateTime.TryParse(value, out DateTime parsedDate))
            {
                _Date = parsedDate.ToString("yyyy-MM-dd");
            }
            else
            {
                throw new ArgumentException("Invalid date format for Date.");
            }
        }
    }
    public string Reference { get; set; }
}


