# ManagerIO - Zatca Gateway

This project serves as a tool to convert ManagerIO Invoices to Zatca Invoices and send them.

## How This Application Works
1. ManagerIO sends invoice data to the Gateway API.
2. The Gateway processes the ManagerIO invoice into XML and sends it to the Zatca Portal.
3. The Gateway forwards the response from the Zatca server to ManagerIO.
4. ManagerIO receives the request and stores the QR code in the invoice.

Currently tested using their sandbox APIs.

# ZatcaApi Gateway

## How to Try It

### ZatcaApi Gateway Installation

1. **Download the Release from GitHub**
   - Visit the GitHub repository and download the latest release zip file.
   
2. **Download and Install .NET SDK**
   - Download the .NET SDK from the following link: [Download .NET SDK](https://download.visualstudio.microsoft.com/download/pr/2e3d0e1d-ad81-4ca7-b186-49f2313547e7/ee8546e4148b87c6e14878b5055406e9/dotnet-sdk-8.0.301-win-x64.exe)
   - Install the .NET SDK on your machine.

3. **Extract the Zip File**
   - Once the download is complete, locate the zip file and extract its contents to a folder of your choice.

4. **Navigate to the Extracted Folder**
   - Open the folder where you extracted the zip file.

5. **Run ZatcaApi.exe or Install as a Service**
   - To run the application directly, execute `ZatcaApi.exe`.
   - Alternatively, you can install it as a Windows service:
     - Right-click on `CreateWindowsServices.bat` and select "Run as administrator".

6. **Open the Browser**
   - Open your preferred web browser and navigate to `http://localhost:4454/swagger`.
   - If the Swagger UI page opens, the gateway is ready to execute commands.

### ManagerIO Setup

1. **Open the Manager Application**
   - Launch the Manager application on your computer.

2. **Import Business**
   - In the Manager application, navigate to the import function.
   - Select the business file named `Zatca eInvoice.manager` located in the `ManagerIO-Files` folder and import it.

3. **Gateway is Ready**
   - Once the business is imported, the gateway is ready for testing and use.

Please let me know if any part of this application needs correction or if you have any additional requirements. Thank you!


.: Terima Kasih :.

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/Y8Y1YRH26)
