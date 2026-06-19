using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout.Properties;
using SageIntegration.SageRepository.SalesOrder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using SageIntegration.Models;
using WooCommerceNET;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SageIntegration.SageService.Invoice
{
    public class UploadPDF
    {
        private readonly IConfiguration _configuration;

        public UploadPDF(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<string> GenerateOrderPDF(SalesOrderInfo orderInfo, string outputFilePath, WooCommerceNET.WooCommerce.v3.Customer customer, WooCommerceNET.WooCommerce.v3.Order? order)
        {
            // Create the PDF writer and document
            PdfWriter writer = new PdfWriter(outputFilePath);
            PdfDocument pdf = new PdfDocument(writer);
            Document document = new Document(pdf);

            // Header
            document.Add(new Paragraph("Austrian Glass UK Ltd T/A KBRH Catering Equipment")
                .SimulateBold()
                .SetFontSize(14));
            document.Add(new Paragraph("12 Jenner Avenue, London, W3 6EQ").SetFontSize(10));
            document.Add(new Paragraph("Tel: 03331122000 | Fax: 020 7377 9511 | Email: info@kbrhcatering.co.uk").SetFontSize(10));
            document.Add(new Paragraph("\nInvoice").SetFontSize(16).SimulateBold());

            // Invoice Info
            document.Add(new Paragraph($"Invoice No: {orderInfo.InvRef}"));
            document.Add(new Paragraph($"Date: {orderInfo.OrderDate?.ToShortDateString() ?? DateTime.Now.ToShortDateString()}"));
            document.Add(new Paragraph($"Customer Order: {customer.id.ToString()}"));
            document.Add(new Paragraph("\n"));

            // Billing and Delivery Info
            Table infoTable = new Table(new float[] { 1, 1 }).SetWidth(UnitValue.CreatePercentValue(100));
            infoTable.AddCell(new Cell().Add(new Paragraph("Invoice to:\n " + customer.first_name +" "+ customer.last_name+" \n " +customer.billing.address_1 +"\n " +customer.billing.city).SetFontSize(10)));
            infoTable.AddCell(new Cell().Add(new Paragraph("Deliver to:\n" + customer.first_name + " " + customer.last_name + "\n" + customer.billing.address_1 + "\n " + customer.billing.city).SetFontSize(10)));
            document.Add(infoTable);
            document.Add(new Paragraph("\n"));

            // Line Items
            Table lineItemsTable = new Table(new float[] { 2, 6, 2, 2 }).SetWidth(UnitValue.CreatePercentValue(100));
            lineItemsTable.AddHeaderCell(new Cell().Add(new Paragraph("Code").SimulateBold()));
            lineItemsTable.AddHeaderCell(new Cell().Add(new Paragraph("Description").SimulateBold()));
            lineItemsTable.AddHeaderCell(new Cell().Add(new Paragraph("Quantity").SimulateBold()));
            lineItemsTable.AddHeaderCell(new Cell().Add(new Paragraph("Unit Price").SimulateBold()));

            foreach (var item in orderInfo.Items)
            {
                lineItemsTable.AddCell(new Cell().Add(new Paragraph(item.ProductCode)));
                lineItemsTable.AddCell(new Cell().Add(new Paragraph(item.Description)));
                lineItemsTable.AddCell(new Cell().Add(new Paragraph(item.Quantity.ToString())));
                lineItemsTable.AddCell(new Cell().Add(new Paragraph($"£{item.PricePerUnit:F2}")));
            }
            document.Add(lineItemsTable);

            // Totals
            //document.Add(new Paragraph("\n"));
            //Table totalsTable = new Table(new float[] { 1, 1 }).SetWidth(UnitValue.CreatePercentValue(50)).SetHorizontalAlignment(HorizontalAlignment.RIGHT);
            // Totals Section
            decimal totalNetAmount = orderInfo?.TotalGBP ?? 0m;
            decimal carriageNet = orderInfo?.CarriageGBP ?? 0m;
            decimal carriageVAT = orderInfo?.CARR_TAX ?? 0m;
            decimal totalVATAmount = orderInfo?.TotalVAT ?? 0m;
            totalVATAmount = totalVATAmount + carriageVAT;

            document.Add(new Paragraph("\n"));
            Table totalTable = new Table(new float[] { 1, 1 }).SetWidth(UnitValue.CreatePercentValue(50)).SetHorizontalAlignment(HorizontalAlignment.RIGHT);
            totalTable.AddCell(new Cell().Add(new Paragraph("Total Net Amount").SimulateBold()));
            totalTable.AddCell(new Cell().Add(new Paragraph($"£{orderInfo.TotalGBP:F2}")));
            totalTable.AddCell(new Cell().Add(new Paragraph("Carriage Net").SimulateBold()));
            totalTable.AddCell(new Cell().Add(new Paragraph($"£{orderInfo.CarriageGBP:F2}")));
            totalTable.AddCell(new Cell().Add(new Paragraph("Total VAT Amount").SimulateBold()));
            totalTable.AddCell(new Cell().Add(new Paragraph($"£{orderInfo.TotalVAT:F2}")));
            totalTable.AddCell(new Cell().Add(new Paragraph("Invoice Total").SimulateBold()));
            // Safely handle null values


            // Calculate the invoice total safely
            decimal invoiceTotal = totalNetAmount + carriageNet + totalVATAmount;

            // Add the calculated value to the table
            totalTable.AddCell(new Cell().Add(new Paragraph($"£{invoiceTotal:F2}")));

            //totalsTable.AddCell(new Cell().Add(new Paragraph("Total GBP").SimulateBold()));
            //totalsTable.AddCell(new Cell().Add(new Paragraph($"£{orderInfo.TotalGBP:F2}")));
            //totalsTable.AddCell(new Cell().Add(new Paragraph("Total VAT Amount").SimulateBold()));
            //totalsTable.AddCell(new Cell().Add(new Paragraph($"£{orderInfo.TotalVAT:F2}")));
            //totalsTable.AddCell(new Cell().Add(new Paragraph("Carriage GBP").SimulateBold()));
            //totalsTable.AddCell(new Cell().Add(new Paragraph($"£{orderInfo.CarriageGBP:F2}")));
            //totalsTable.AddCell(new Cell().Add(new Paragraph("Gross GBP").SimulateBold()));
            //totalsTable.AddCell(new Cell().Add(new Paragraph($"£{orderInfo.GrossGBP:F2}")));
            document.Add(totalTable);

            // Footer
            document.Add(new Paragraph("\nThe goods remain the property of Austrian Glass UK Ltd until paid for in full.\n\n").SetFontSize(10));
            document.Add(new Paragraph("Our terms of business are 30 days from invoice date.").SetFontSize(10));
            document.Add(new Paragraph("Bank Details:\nAustrian Glass UK Ltd\nHSBC Bank PLC\nSort Code: 40-06-21\nAccount No: 01554204")
                .SetFontSize(10));
            document.Close();
            Console.WriteLine($"PDF generated at {Path.GetFullPath(outputFilePath)}");
            return outputFilePath;
        }
        public async Task UploadPDFToWooCommerce(string pdfFilePath, string filename)
        {
            // WordPress API details
            string siteUrl = _configuration["WooCommerceMedia:SiteUrl"];
            string username = _configuration["WooCommerceMedia:Username"];
            string applicationPassword = _configuration["WooCommerceMedia:ApplicationPassword"];

            try
            {
                using (var httpClient = new HttpClient())
                {
                    // Prepare authentication header
                    string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{applicationPassword}"));
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                    // Check for existing file in the media library
                    string queryUrl = $"{siteUrl}?search={Path.GetFileNameWithoutExtension(filename)}"; // Assumes filename is unique
                    HttpResponseMessage queryResponse = await httpClient.GetAsync(queryUrl);

                    if (queryResponse.IsSuccessStatusCode)
                    {
                        string queryResult = await queryResponse.Content.ReadAsStringAsync();
                        var mediaItems = JsonSerializer.Deserialize<List<MediaItem>>(queryResult);

                        if (mediaItems != null && mediaItems.Any())
                        {
                            foreach (var media in mediaItems)
                            {
                                string originalFilename = Path.GetFileNameWithoutExtension(filename);

                                // Optionally, clean the filename by removing suffix
                                string cleanedFilename = originalFilename.Split('-')[0];
                                if (media.title.rendered == cleanedFilename || originalFilename.Contains(media.title.rendered))
                                {
                                    // Delete the existing file
                                    string deleteUrl = $"{siteUrl}/{media.id}?force=true";
                                    HttpResponseMessage deleteResponse = await httpClient.DeleteAsync(deleteUrl);

                                    if (deleteResponse.IsSuccessStatusCode)
                                    {
                                        Console.WriteLine($"Deleted existing file with ID: {media.id}");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Failed to delete file. Status Code: {deleteResponse.StatusCode}");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Failed to query existing files. Status Code: {queryResponse.StatusCode}");
                    }

                    // Read the file to upload
                    byte[] fileData = await File.ReadAllBytesAsync(pdfFilePath);

                    // Prepare the content for upload
                    using (var content = new MultipartFormDataContent())
                    {
                        var fileContent = new ByteArrayContent(fileData);
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                        content.Add(fileContent, "file", filename);

                        // Send POST request to upload the file
                        HttpResponseMessage response = await httpClient.PostAsync(siteUrl, content);

                        if (response.IsSuccessStatusCode)
                        {
                            string responseBody = await response.Content.ReadAsStringAsync();
                            File.Delete(pdfFilePath);
                            Console.WriteLine("File uploaded successfully!");
                            Console.WriteLine("Response: " + responseBody);
                        }
                        else
                        {
                            Console.WriteLine($"Failed to upload file. Status Code: {response.StatusCode}");
                            string errorBody = await response.Content.ReadAsStringAsync();
                            Console.WriteLine("Error: " + errorBody);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        public class MediaItem
        {
            public int id { get; set; }
            public Title title { get; set; }
        }

        public class Title
        {
            public string rendered { get; set; }
        }

    }
}
