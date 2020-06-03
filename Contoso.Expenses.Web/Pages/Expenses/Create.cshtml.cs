using App.Metrics;
using Contoso.Expenses.Common.Models;
using Contoso.Expenses.Web.AppMetrics;
using Contoso.Expenses.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Contoso.Expenses.Web.Pages.Expenses
{
    public class CreateModel : PageModel
    {
        private readonly ContosoExpensesWebContext _context;
        private readonly QueueInfo _queueInfo;
        private string costCenterAPIUrl;
        private readonly IMetrics _metrics;

        public CreateModel(ContosoExpensesWebContext context, IOptions<ConfigValues> config, QueueInfo queueInfo, IMetrics metrics)
        {
            _metrics = metrics;
            _context = context;
            _queueInfo = queueInfo;
            costCenterAPIUrl = config.Value.CostCenterAPIUrl;
        }


        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Expense Expense { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            CostCenter costCenter = await GetCostCenterAsync(costCenterAPIUrl, Expense.SubmitterEmail);
            if (costCenter != null)
            {
                Expense.CostCenter = costCenter.CostCenterName;
                Expense.ApproverEmail = costCenter.ApproverEmail;
            }
            else
            {
                Expense.CostCenter = "Unkown";
                Expense.ApproverEmail = "Unknown";
            }

            // Write to DB, but don't wait right now
            _context.Expense.Add(Expense);
            Task t = _context.SaveChangesAsync();

            // Serialize the expense and write it to the Azure Storage Queue
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_queueInfo.ConnectionString);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference(_queueInfo.QueueName);
            await queue.CreateIfNotExistsAsync();
            CloudQueueMessage queueMessage = new CloudQueueMessage(JsonConvert.SerializeObject(Expense));
            await queue.AddMessageAsync(queueMessage);

            // Ensure the DB write is complete
            t.Wait();

            _metrics.Measure.Counter.Increment(MetricsRegistry.CreatedExpenseCounter);

            return RedirectToPage("./Index");
        }

        private static async Task<CostCenter> GetCostCenterAsync(string apiBaseURL, string email)
        {
            string requestUri = "api/costcenter" + "/" + email;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(apiBaseURL);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                HttpResponseMessage httpResponse = await client.GetAsync(requestUri);

                if (httpResponse.IsSuccessStatusCode)
                {
                    //CostCenter costCenter = await httpResponse.Content.ReadAsAsync<CostCenter>();
                    var response = await httpResponse.Content.ReadAsStringAsync();
                    var costCenter = JsonConvert.DeserializeObject<CostCenter>(response);

                    if (costCenter != null)
                        Console.WriteLine("SubmitterEmail: {0} \r\n ApproverEmail: {1} \r\n CostCenterName: {2}", 
                            costCenter.SubmitterEmail, costCenter.ApproverEmail, costCenter.CostCenterName);
                    return costCenter;
                }
                else
                {
                    Console.WriteLine("Internal server error: " + httpResponse.StatusCode);
                    return null;
                }
            }
        }
    }
}