using Newtonsoft.Json;

namespace ZatcaApi.Models
{
    public class BusinessInfo
    {
        //PartyIdentification
        public string ID { get; set; }

        [JsonProperty("schemeID")]
        public string SchemeID { get; set; }

        // Postal
        public string StreetName { get; set; }
        public string BuildingNumber { get; set; }
        public string CitySubdivisionName { get; set; }
        public string CityName { get; set; }
        public string PostalZone { get; set; }
        public string CountryIdentificationCode { get; set; }

        // PartyTaxScheme
        public string CompanyID { get; set; }
        public string TaxSchemeID { get; set; }


        public string RegistrationName { get; set; }
    }
}