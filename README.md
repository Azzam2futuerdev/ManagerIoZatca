# ManagerIO - Zatca Gateway

This project serves as a tool to convert ManagerIO Invoices to Zatca Invoices and send them.

## How This Application Works
1. ManagerIO sends invoice data to the Gateway API.
2. The Gateway processes the ManagerIO invoice into XML and sends it to the Zatca Portal.
3. The Gateway forwards the response from the Zatca server to ManagerIO.
4. ManagerIO receives the request and stores the QR code in the invoice.

Currently tested using their sandbox APIs.

## How to Try It

### ZatcaApi Gateway Installation

1. **Download the Release from GitHub**
   - Visit the GitHub repository and download the latest release zip file.

2. **Extract the Zip File**
   - Once the download is complete, locate the zip file and extract its contents to a folder of your choice.

3. **Navigate to the Extracted Folder**
   - Open the folder where you extracted the zip file.

4. **Run ZatcaApi.exe or Install as a Service**
   - To run the application directly, execute `ZatcaApi.exe`.
   - Alternatively, you can install it as a Windows service:
     - Right-click on `CreateWindowsServices.bat` and select "Run as administrator".

5. **Open the Browser**
   - Open your preferred web browser and navigate to `http://localhost:4454/swagger`.
   - If the Swagger UI page opens, the gateway is ready to execute commands.

### ManagerIO Setup

1. **Open the Manager Application**
   - Launch the Manager application on your computer.

2. **Import Business**
   - In the Manager application, navigate to the import function.
   - Select the business file named `fileZatca eInvoice.manager` located in the `ManagerIO-Files` folder and import it.

3. **Gateway is Ready**
   - Once the business is imported, the gateway is ready for testing and use.

Please let me know if any part of this application needs correction or if you have any additional requirements. Thank you!


.: Terima Kasih :.
