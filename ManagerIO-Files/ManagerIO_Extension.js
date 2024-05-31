<script src="resources/qrcode/qrcode.js"></script>
<script>
let extractedData = null;
const PartyInfoFieldGuid = '93f79973-5346-4c6b-b912-90ea9bbf69c2';
const eInvoiceStatusCustomFieldGuid = '3c4c1fb3-3b0e-4f2e-9eeb-2d466c496e2f'

const API_PATHS = {
    generateXml: 'http://localhost:4454/signed-invoice',
    compliance: 'http://localhost:4454/compliance-invoice',
    clearance: 'http://localhost:4454/invoice-clearance',
    reporting: 'http://localhost:4454/invoice-reporting'
};

function encodeToBase64(data) {
    try {
        const jsonString = JSON.stringify(data);
        const utf8Bytes = new TextEncoder().encode(jsonString);
        const base64String = btoa(String.fromCharCode.apply(null, utf8Bytes));
        return base64String;
    } catch (error) {
        console.error('Error encoding to Base64:', error);
        throw error;
    }
}

async function sendToGateway(apiPath) {
    const apiResponseTextarea = document.getElementById('json-response');
    const qrCodeDiv = document.getElementById('qrcode-content');
    const downloadLink = document.getElementById('download-link');

    if (!extractedData) {
        apiResponseTextarea.value = `extractedData is null or undefined.`;
        return;
    }

    apiResponseTextarea.value = '';
    qrCodeDiv.innerHTML = '';
    downloadLink.style.display = 'none';
    downloadLink.href = '#';

    const encodedData = encodeToBase64(extractedData);

    const requestData = {
        InvoiceId: extractedData.InvoiceId,
        InvoiceType: extractedData.InvoiceType,
        InvoiceSubType: extractedData.InvoiceSubType,
        IssueDate: extractedData.IssueDate,
        Reference: extractedData.Reference,
        PartyName: extractedData.PartyName,
        InvoiceData: encodedData
    };

    try {
        const response = await fetch(apiPath, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json; charset=UTF-8'
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
            new QRCode(qrCodeDiv, {
                text: responseBody.base64QrCode,
                width: 160,
                height: 160,
                colorDark: '#000000',
                colorLight: '#ffffff',
                correctLevel: QRCode.CorrectLevel.L
            });
            if (apiPath != API_PATHS.compliance){
                app.CustomFields2.Strings[eInvoiceStatusCustomFieldGuid] = responseBody.base64QrCode;
            }
        }

        if (responseBody.base64SignedInvoice && responseBody.xmlFileName) {
            const decodedInvoice = decodeBase64(responseBody.base64SignedInvoice);
            const blob = new Blob([decodedInvoice], { type: 'application/xml;charset=UTF-8' });
            const url = window.URL.createObjectURL(blob);

            downloadLink.href = url;
            downloadLink.download = responseBody.xmlFileName;
            downloadLink.style.display = 'block';
        }
    } catch (error) {
        console.error('Network Error:', error);
        apiResponseTextarea.value = `Network error: ${error}`;
    }
}

function decodeBase64(base64String) {
    const binaryString = atob(base64String);
    const bytes = new Uint8Array(binaryString.length);
    for (let i = 0; i < binaryString.length; i++) {
        bytes[i] = binaryString.charCodeAt(i);
    }
    return new TextDecoder().decode(bytes);
}

function removeUnnecessaryProperties(obj) {
    const propertiesToRemove = ['SalesInvoiceCustomTheme','CreditNoteCustomTheme', 'DebitNoteCustomTheme', 'SalesInvoiceFooters', 'DebitNoteFooters', 'CreditNoteFooters', 'SaleItemAccount', 'PurchaseItemAccount', 'Customer', 'Supplier'];
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
    const decimalSeparatorMatch = scriptContent.match(/const decimalSeparator = '([^']+)';/);
    const dataMatch = scriptContent.match(/data:\s*(\{[\s\S]*?\})\s*,\s*methods:/);

    const InvoiceId = app.id;
    let InvoiceType=388;
    const InvoiceSubType='0100000';
    const IssueDate = app.IssueDate;
    const Reference = app.Reference;
    
    let PartyName ='';
    let PartyTaxInfo ='';
    let PartyCurrency ='';
    
    const url = window.location.href;
    
    if (url.includes('/sales-invoice-form?')) {
        InvoiceType = 388;
    } else if (url.includes('/credit-note-form?')) {
        InvoiceType = 381;
    } else if (url.includes('/debit-note-form?')) {
        InvoiceType = 383;
    }
    
    if (url.includes('/debit-note-form?')) {
        PartyName = app.Supplier && app.Supplier.Name ? app.Supplier.Name : '';
        PartyTaxInfo = app.Supplier && app.Supplier.CustomFields2 && app.Supplier.CustomFields2.Strings[PartyInfoFieldGuid] ? app.Supplier.CustomFields2.Strings[PartyInfoFieldGuid] : '';
        PartyCurrency = app.Supplier && app.Supplier.Currency ? app.Supplier.Currency : '';
    } else {
        PartyName = app.Customer && app.Customer.Name ? app.Customer.Name : '';
        PartyTaxInfo = app.Customer && app.Customer.CustomFields2 && app.Customer.CustomFields2.Strings[PartyInfoFieldGuid] ? app.Customer.CustomFields2.Strings[PartyInfoFieldGuid] : '';
        PartyCurrency = app.Customer && app.Customer.Currency ? app.Customer.Currency : '';
    }
    
    const BaseCurrency = baseCurrencyMatch ? JSON.parse(baseCurrencyMatch[1]) : null;
    const ForeignCurrencies = foreignCurrenciesMatch ? JSON.parse(foreignCurrenciesMatch[1]) : null;
    const DecimalSeparator = decimalSeparatorMatch ? decimalSeparatorMatch[1] : null;
    let Data = dataMatch ? JSON.parse(dataMatch[1]) : null;

    Data = JSON.parse(JSON.stringify(Data));

    removeUnnecessaryProperties(Data);

    return {
        InvoiceId,
        InvoiceType,
        InvoiceSubType,
        IssueDate,
        Reference,
        PartyName,
        PartyTaxInfo,
        PartyCurrency,
        BaseCurrency,
        ForeignCurrencies,
        DecimalSeparator,
        Data
    };
}

document.addEventListener('DOMContentLoaded', (event) => {
	const url = window.location.href;
	const isInvoicePage = url.includes('/sales-invoice-form?') || url.includes('/credit-note-form?') || url.includes('/debit-note-form?');
    if (isInvoicePage) 
	{
		const updateButton = document.querySelector(`button.btn.btn-success[onclick='ajaxPost(true)']`);
		if (updateButton) 
		{
			const parentDiv = document.querySelector('.lg\\:mb-4');
			if (parentDiv) {
				const innerHTML = parentDiv.innerHTML;
				const containsDash = innerHTML.includes(' — ');
				if (containsDash) {
					const vModelFormDiv = document.getElementById('v-model-form');
					const button = document.createElement('button');
					button.innerHTML = `<i class='fas fa-edit' style='color:green; font-size: 14px;'></i> Zatca eInvoice`;
					button.classList.add('bg-white', 'font-bold', 'border', 'border-neutral-300', 'hover:border-neutral-400', 'text-neutral-700', 'hover:text-neutral-800', 'rounded', 'py-2', 'px-4', 'hover:no-underline', 'hover:bg-neutral-100', 'hover:shadow-inner', 'dark:focus:ring-gray-700', 'dark:bg-gray-800', 'dark:text-gray-400', 'dark:border-gray-600', 'dark:hover:text-white', 'dark:hover:bg-gray-700');
					button.style.fontSize = '12px';
		
					button.onclick = function() {
						openPopupForm();
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
		}
	}
});

function openPopupForm() {
    const modalHtml = `
    <div id='popup-modal' class='modal' style='display: block;'>
        <div class='modal-dialog' style='width: 800px;'>
            <div class='modal-content' style='border-radius: 8px;'>
                <div class='modal-header' style='background-color: #6c757d; color: white; border-top-left-radius: 8px; border-top-right-radius: 8px;'>
                    <button type='button' class='close' onclick="document.getElementById('popup-modal').remove();" style='color: white;'><i class="fa fa-times" aria-hidden="true"></i></button>
                    <div class='header'><i class='fa fa-at fa-spin fa-lg'></i>&nbsp;Zatca eInvoice</div>
                </div>
                
                <div class='modal-body' style='background-color: #f9f9f9; padding: 20px;'>
                
                    <div class='row mb-3'>
                        <div class='col-md-12'>
                            <table class='table table-bordered'>
                                <tbody>
                                    <tr>
                                        <td><strong>Invoice Id:</strong></td>
                                        <td>${extractedData.InvoiceId}</td>
                                        <td><strong>Reference:</strong></td>
                                        <td>${extractedData.Reference}</td>
                                    </tr>
                                    <tr>
                                        <td><strong>Customer:</strong></td>
                                        <td>${extractedData.PartyName}</td>
                                        <td><strong>Issue Date:</strong></td>
                                        <td>${extractedData.IssueDate}</td>
                                    </tr>
                                    <tr>
                                        <td><strong>Invoice Type:</strong></td>
                                        <td>${extractedData.InvoiceType}</td>
                                        <td><strong>Invoice SubType:</strong></td>
                                        <td><input type='text' id='invoice-subtype-inputbox' placeholder='0000000' class='form-control input-sm tt-input' value='0100000' style='background-color: lightyellow;'></td>
                                    </tr>
                                </tbody>
                            </table>
                        </div>
                    </div>
                    
                    <div class='row mb-3'>
                        <div class='col-md-12' style='display: flex; justify-content: space-between;'>
                            <div style='width: 100%;'>
                                <textarea id='json-request' class='form-control input-sm language-json' style='width: 100%; min-height: 180px; height: auto; background-color: rgb(247, 247, 247); color: #000; margin-bottom: 10px; margin-right: 5px; white-space: pre-wrap; font-family: monospace; padding: 10px;' readonly>${JSON.stringify(extractedData.Data, null, 2)}</textarea>
                            </div>
                            <div style='width: 15px;'> &nbsp; </div>
                            <div style='width: 30%;'>
                                <div id='qrcode-content' style='background-color : rgb(247, 247, 247); border: 1px solid #bcbcbc; width: 180px; height: 180px; padding: 10px;'>
                                    <!-- QR Code div -->
                                </div>
                                <a id='download-link' href='#' style='display: none; margin-top: 10px; text-align: center;'>Download XML File</a>
                            </div>
                        </div>
                    </div>
                    
                    <div class='row mb-3'>
                        <div class='col-md-12'>
                            <textarea id='json-response' class='form-control input-sm language-json' style='width: 100%; min-height: 200px; height: auto; color: #000; padding: 10px; white-space: pre-wrap; font-family: monospace; background-color: rgb(247, 247, 247);' readonly></textarea>
                        </div>
                    </div>
                    
                    <div class='modal-footer' style='background-color: #f9f9f9; padding: 10px 0px; display: flex; justify-content: space-between; align-items: center;'>
                        <div style='margin-right: auto;'>
                            <button class='btn btn-primary' style='background-color: #28a745; border-color: #28a745;' onclick='sendToGateway(API_PATHS.compliance)'>Compliance Check</button>
                        </div>
                        <div>
                            <button class='btn btn-primary' style='background-color: #007bff; border-color: #007bff;' onclick='confirmAction(API_PATHS.clearance)'>Clearance</button>
                            <button class='btn btn-primary' style='background-color: #007bff; border-color: #007bff;' onclick='confirmAction(API_PATHS.reporting)'>Reporting</button>
                            <button class='btn btn-default' style='background-color: #6c757d; border-color: #6c757d; color: white;' onclick="document.getElementById('popup-modal').remove();">Close</button>
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
    
    document.getElementById('invoice-subtype-inputbox').addEventListener('change', function () {
        extractedData.InvoiceSubType = this.value;
    });
}

function confirmAction(apiPath) {
    let actionTitle='Invoice Reporting';
    if (apiPath == API_PATHS.clearance){
        actionTitle = 'Invoice Clearance';
    }
    
    Swal.fire({
        title: `<p>Are you sure about</p><p><span style='color: blue;'>${actionTitle}</span>?</p>`,
        html: `<p><span style='font-size: larger;'>Do you want to send this request to the server?</span></p> <p><span style='color: red; font-size: larger;'>This action cannot be undone.</span></p>`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Yes, send it!',
        cancelButtonText: 'Cancel',
        didOpen: (popup) => {
            const confirmButton = popup.querySelector('.swal2-confirm');
            const cancelButton = popup.querySelector('.swal2-cancel');
            confirmButton.style.backgroundColor = '#007bff';
            confirmButton.style.borderColor = '#007bff';
            confirmButton.style.color = '#fff';
            confirmButton.style.padding = '8px 20px';
            confirmButton.style.fontSize = '14px';
            confirmButton.style.borderRadius = '5px';
            confirmButton.style.cursor = 'pointer';
            cancelButton.style.backgroundColor = '#6c757d';
            cancelButton.style.borderColor = '#6c757d';
            cancelButton.style.color = '#fff';
            cancelButton.style.padding = '8px 20px';
            cancelButton.style.fontSize = '14px';
            cancelButton.style.borderRadius = '5px';
            cancelButton.style.cursor = 'pointer';
        }
    }).then((result) => {
        if (result.isConfirmed) {
            sendToGateway(apiPath, extractedData);
        }
    });
}
</script>