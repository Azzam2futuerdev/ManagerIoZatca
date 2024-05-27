# ManagerIO - Zatca Gateway

This project serves as a tool to convert ManagerIO Invoices to Zatca Invoices and send them.

## How This Application Works
1. ManagerIO sends invoice data to the Gateway API.
2. The Gateway processes the ManagerIO invoice into XML and sends it to the Zatca Portal.
3. The Gateway forwards the response from the Zatca server to ManagerIO.
4. ManagerIO receives the request and stores the QR code in the invoice.

Currently tested using their sandbox APIs.

## How to Try It
1. Import ManagerIO Data. 
   There are two custom fields added in the Business data:
   - `QrcodeCustomField`: for storing the QR code content.
   - `CustomerInfoCustomField`: for storing tax information of customers.
   
   A JavaScript extension is used to send invoice data and receive responses.

2. Run the ZatcaApi Project. A new database (`history.db`) and a `config.ini` file will be created automatically. 
   By default, `config.ini` contains settings about business info and the configuration for connecting to the Zatca server. Adjust the `config.ini` file according to your business data and server connection settings.

   To check and test the API endpoints, visit:
   - `https://localhost:7106/swagger/index.html`
   - `http://localhost:5000/swagger/index.html`

3. Open the extension in Manager Business and adjust the necessary parameters.

## Customer Info Format
When adding customer information to each customer in the business data, use the following format:

```
"Party.PostalAddress.StreetName": "صلاح الدين | Salah Al-Din",
"Party.PostalAddress.BuildingNumber": "1111",
"Party.PostalAddress.CitySubdivisionName": "المروج | Al-Murooj",
"Party.PostalAddress.CityName": "الرياض | Riyadh",
"Party.PostalAddress.PostalZone": "12222",
"Party.PostalAddress.Country.IdentificationCode": "SA",
"PartyTaxScheme.CompanyID": "399999999800003",
"PartyTaxScheme.TaxScheme.ID": "VAT",
"PartyLegalEntity.RegistrationName": "شركة نماذج فاتورة المحدودة | Fatoora Samples LTD"
```

## Ongoing Work
Please note that there is still much to be done on this project, especially in the conversion of ManagerIO Invoices to Zatca Invoices. Many parts are still being improved. Your feedback and contributions are welcome to enhance the functionality and efficiency of this tool.

## Additional Notes
Ensure that your environment is properly configured and that you have the necessary permissions to run and modify the configuration files. The project is intended to streamline the invoice processing workflow between ManagerIO and the Zatca portal, making tax compliance more efficient.

.: Terima Kasih :.
