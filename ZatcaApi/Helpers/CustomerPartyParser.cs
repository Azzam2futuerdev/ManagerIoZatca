﻿using ZatcaApi.Models;

namespace ZatcaApi.Helpers
{
    public static class CustomerPartyParser
    {
        private static readonly string[] separator = new[] { "\n" };
        private static readonly string[] separatorArray = new[] { "=" };

        public static CustomerInfo ParseCustomerParty(string customerParty)
        {
            var customerInfo = new CustomerInfo();
            var keyValuePairs = customerParty.Split(separator, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(pair => pair.Split(separatorArray, 2, StringSplitOptions.RemoveEmptyEntries))
                                             .Where(pair => pair.Length == 2)
                                             .ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim().Trim('"'));

            foreach (var pair in keyValuePairs)
            {
                switch (pair.Key)
                {
                    case "StreetName":
                        customerInfo.StreetName = pair.Value;
                        break;
                    case "BuildingNumber":
                        customerInfo.BuildingNumber = pair.Value;
                        break;
                    case "CitySubdivisionName":
                        customerInfo.CitySubdivisionName = pair.Value;
                        break;
                    case "CityName":
                        customerInfo.CityName = pair.Value;
                        break;
                    case "PostalZone":
                        customerInfo.PostalZone = pair.Value;
                        break;
                    case "CountryIdentificationCode":
                        customerInfo.CountryIdentificationCode = pair.Value;
                        break;
                    case "CompanyID":
                        customerInfo.CompanyID = pair.Value;
                        break;
                    case "TaxSchemeID":
                        customerInfo.TaxSchemeID = pair.Value;
                        break;
                    case "RegistrationName":
                        customerInfo.RegistrationName = pair.Value;
                        break;
                }
            }

            return customerInfo;
        }
    }
}