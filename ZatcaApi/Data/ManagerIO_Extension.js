let extractedData = null;
const CustomerInfoFieldGuid = "93f79973-5346-4c6b-b912-90ea9bbf69c2";
const eInvoiceStatusCustomFieldGuid = "3c4c1fb3-3b0e-4f2e-9eeb-2d466c496e2f"

const API_PATHS = {
    generateXml: 'https://localhost:7106/signed-invoice',
    compliance: 'https://localhost:7106/compliance-invoice',
    clearance: 'https://localhost:7106/invoice-clearance',
    reporting: 'https://localhost:7106/invoice-reporting'
};

function encodeToBase64(data) {
    try {
        const jsonString = JSON.stringify(data);
        const utf8Bytes = new TextEncoder().encode(jsonString);
        const base64String = btoa(String.fromCharCode.apply(null, utf8Bytes));
        return base64String;
    } catch (error) {
        throw error;
    }
}

async function sendToGateway(apiPath, extractedData) {
    const apiResponseTextarea = document.getElementById('json-response');
    const qrCodeDiv = document.getElementById('qrcode-content');

    if (!extractedData) {
        apiResponseTextarea.value = `extractedData is null or undefined.`;
        return;
    }

    apiResponseTextarea.value = '';
    qrCodeDiv.value = '';

    const encodedData = encodeToBase64(extractedData);

    const requestData = {
        InvoiceId: extractedData.InvoiceId,
        InvoiceType: extractedData.InvoiceType,
        InvoiceSubType: extractedData.InvoiceSubType,
        IssueDate: extractedData.IssueDate,
        Reference: extractedData.Reference,
        CustomerName: extractedData.CustomerName,
        CustomerInfo: extractedData.CustomerInfo,
        InvoiceData: encodedData
    };

    try {
        const response = await fetch(apiPath, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestData)
        });

        if (!response.ok) {
            console.error('Response Error:', response.statusText);
            apiResponseTextarea.value = `Status Code: ${response.status}\n\nResponse Content:\n\n${response.statusText}`;
            return;
        }

        const responseBody = await response.json();

        apiResponseTextarea.value = `Status Code: ${response.status}\n\nResponse Content:\n\n${JSON.stringify(responseBody, null, 2)}`;

        if (responseBody.base64QrCode) {
            qrCodeDiv.innerHTML = '';
            new QRCode(qrCodeDiv, {
                text: responseBody.base64QrCode,
                width: 160,
                height: 160,
                colorDark: "#000000",
                colorLight: "#ffffff",
                correctLevel: QRCode.CorrectLevel.L
            });
        } else {
            qrCodeDiv.innerHTML = '';
        }

        if (responseBody.base64SignedInvoice && responseBody.xmlFileName) {
            const decodedInvoice = atob(responseBody.base64SignedInvoice);
            downloadFile(decodedInvoice, responseBody.xmlFileName);
        }
    } catch (error) {
        console.error('Network Error:', error);
        apiResponseTextarea.value = `Network error: ${error}`;
    }
}

function downloadFile(data, filename) {
    const blob = new Blob([data], { type: 'application/xml' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.style.display = 'none';
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
}


document.addEventListener('DOMContentLoaded', (event) => {
    const parentDiv = document.querySelector('.lg\\:mb-4');
    if (parentDiv) {
        const innerHTML = parentDiv.innerHTML;
        const containsSalesInvoice = innerHTML.includes("Sales Invoice");
        const containsDash = innerHTML.includes(" — ");

        if (containsSalesInvoice && containsDash) {
            const vModelFormDiv = document.getElementById('v-model-form');
            const button = document.createElement('button');
            button.innerHTML = '<i class="fas fa-edit" style="color:green; font-size: 14px;"></i> Zatca eInvoice';
            button.classList.add('bg-white', 'font-bold', 'border', 'border-neutral-300', 'hover:border-neutral-400', 'text-neutral-700', 'hover:text-neutral-800', 'rounded', 'py-2', 'px-4', 'hover:no-underline', 'hover:bg-neutral-100', 'hover:shadow-inner', 'dark:focus:ring-gray-700', 'dark:bg-gray-800', 'dark:text-gray-400', 'dark:border-gray-600', 'dark:hover:text-white', 'dark:hover:bg-gray-700');
            button.style.fontSize = '12px';

            button.onclick = function () {
                openPopupForm(extractedData);
            };

            const headerDiv = vModelFormDiv.querySelector('.flex');
            headerDiv.appendChild(button);

            const scriptElements = document.querySelectorAll('#nonBatchView script');

            scriptElements.forEach(scriptElement => {
                const scriptContent = scriptElement.textContent.trim();
                if (scriptContent.includes('app = new Vue')) {
                    extractedData = extractDataFromScript(scriptContent);
                }
            });
        }
    }
});


function removeUnnecessaryProperties(obj) {
    const propertiesToRemove = ['SalesInvoiceCustomTheme', 'SalesInvoiceFooters', 'SaleItemAccount'];
    for (const key in obj) {
        if (obj.hasOwnProperty(key)) {
            if (propertiesToRemove.includes(key) || obj[key] === null || obj[key] === false) {
                delete obj[key];
            } else if (typeof obj[key] === 'object') {
                removeUnnecessaryProperties(obj[key]);
                if (Object.keys(obj[key]).length === 0) {
                    delete obj[key];
                }
            }
        }
    }
}

function extractDataFromScript(scriptContent) {
    const baseCurrencyMatch = scriptContent.match(/const baseCurrency = (\{.*?\});/s);
    const foreignCurrenciesMatch = scriptContent.match(/const foreignCurrencies = (\{.*?\});/s);
    const decimalSeparatorMatch = scriptContent.match(/const decimalSeparator = "([^"]+)";/);
    const dataMatch = scriptContent.match(/data:\s*(\{[\s\S]*?\})\s*,\s*methods:/);

    const InvoiceId = app.id;
    let InvoiceType = 388;
    const InvoiceSubType = 1;
    const IssueDate = app.IssueDate;
    const Reference = app.Reference;
    const CustomerName = app.Customer.Name;
    const CustomerInfo = app.Customer.CustomFields2.Strings[CustomerInfoFieldGuid];
    const BaseCurrency = baseCurrencyMatch ? JSON.parse(baseCurrencyMatch[1]) : null;
    const ForeignCurrencies = foreignCurrenciesMatch ? JSON.parse(foreignCurrenciesMatch[1]) : null;
    const DecimalSeparator = decimalSeparatorMatch ? decimalSeparatorMatch[1] : null;
    let Data = dataMatch ? JSON.parse(dataMatch[1]) : null;

    const url = window.location.href;
    if (url.includes('/sales-invoice-form?')) {
        InvoiceType = 388;
    } else if (url.includes('/credit-note-form?')) {
        InvoiceType = 381;
    } else if (url.includes('/debit-note-form?')) {
        InvoiceType = 383;
    }

    Data = JSON.parse(JSON.stringify(Data));

    removeUnnecessaryProperties(Data);

    return {
        InvoiceId,
        InvoiceType,
        InvoiceSubType,
        IssueDate,
        Reference,
        CustomerName,
        CustomerInfo,
        BaseCurrency,
        ForeignCurrencies,
        DecimalSeparator,
        Data
    };
}

function openPopupForm(extractedData) {
    if (extractedData && extractedData.InvoiceId) {

        const invoiceSubTypeOptions = {
            1: "Standard",
            2: "Simplified"
        };

        let optionsHtml = '';
        for (const key in invoiceSubTypeOptions) {
            const selected = extractedData.InvoiceSubType == key ? 'selected' : '';
            optionsHtml += `<option value="${key}" ${selected}>${invoiceSubTypeOptions[key]}</option>`;
        }

        const modalHtml = `
    <div id="popup-modal" class="modal" style="display: block;">
        <div class="modal-dialog" style="width: 800px;">
            <div class="modal-content" style="border-radius: 8px;">
                <div class="modal-header" style="background-color: #007bff; color: white; border-top-left-radius: 8px; border-top-right-radius: 8px;">
                    <button type="button" class="close" onclick="document.getElementById('popup-modal').remove();" style="color: white;">×</button>
                    <div class="header">Zatca eInvoice</div>
                </div>
                
                <div class="modal-body" style="background-color: #f9f9f9; padding: 20px;">
                    <div class="row mb-3">
                        <div class="col-md-6">
                            <p><strong>Invoice Id:</strong> ${extractedData.InvoiceId}</p>
                            <p><strong>Invoice Type:</strong> ${extractedData.InvoiceType}</p>
                            <p><strong>Invoice SubType:</strong></p>
                            <select id="invoice-subtype-select" class="form-control">
                                ${optionsHtml}
                            </select>
                        </div>
                        <div class="col-md-6">
                            <p><strong>Issue Date:</strong> ${extractedData.IssueDate}</p>
                            <p><strong>Reference:</strong> ${extractedData.Reference}</p>
                            <p><strong>Customer:</strong> ${extractedData.CustomerName}</p>
                        </div>
                    </div>
                    
                    <div class="row mb-3">
                        <div class="col-md-12" style="display: flex; justify-content: space-between;">
                            <div style="width: 100%;">
                                <textarea id="json-request" class="form-control input-sm language-json" style="width: 100%; min-height: 180px; height: auto; background-color: white; color: #000; margin-bottom: 10px; margin-right: 5px;" readonly>${JSON.stringify(extractedData.Data, null, 2)}</textarea>
                            </div>
                            <div style="width: 15px;"> &nbsp; </div>
                            <div style="width: 30%;">
                                <div id="qrcode-content" style="background-color: white; border: 1px solid #bcbcbc; width: 180px; height: 180px; padding: 10px;">
                                    <!-- QR Code div -->
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="row mb-3">
                        <div class="col-md-12">
                            <textarea id="json-response" class="form-control input-sm language-json" style="width: 100%; min-height: 200px; height: auto; background-color: white; color: #000; padding: 10px;" readonly></textarea>
                        </div>
                    </div>
                    
                    <div class="modal-footer" style="background-color: #f9f9f9; padding: 10px 0px; display: flex; justify-content: space-between; align-items: center;">
                        <div style="margin-right: auto;">
                            <button class="btn btn-primary" style="background-color: #007bff; border-color: #007bff;" onclick="sendToGateway(API_PATHS.generateXml, extractedData)">Generate XML</button>
                            <button class="btn btn-primary" style="background-color: #007bff; border-color: #007bff;" onclick="sendToGateway(API_PATHS.compliance, extractedData)">Compliance Check</button>
                            <button class="btn btn-primary" style="background-color: #007bff; border-color: #007bff;" onclick="sendToGateway(API_PATHS.clearance, extractedData)">Clearance</button>
                            <button class="btn btn-primary" style="background-color: #007bff; border-color: #007bff;" onclick="sendToGateway(API_PATHS.reporting, extractedData)">Reporting</button>
                            <img src="resources/ajax-loader.gif" style="display: none; margin-left: 10px; margin-right: 10px" id="api-indicator">
                        </div>
                        <div>
                            <button class="btn btn-primary" style="background-color: #28a745; border-color: #28a745;" onclick="updateInvoice()">Update</button>
                            <button class="btn btn-default" style="background-color: #6c757d; border-color: #6c757d; color: white;" onclick="document.getElementById('popup-modal').remove();">Close</button>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
    `;

        const modalDiv = document.createElement('div');
        modalDiv.innerHTML = modalHtml;
        document.body.appendChild(modalDiv);
    }

    document.getElementById('invoice-subtype-select').addEventListener('change', function () {
        extractedData.InvoiceSubType = this.value;
    });
}

function updateInvoice() {
    const base64QrCode = document.getElementById('qrcode-content').title;

    if (app && app.CustomFields2 && app.CustomFields2.Strings) {
        app.CustomFields2.Strings[eInvoiceStatusCustomFieldGuid] = base64QrCode;
    }
    document.getElementById('popup-modal').remove();
}