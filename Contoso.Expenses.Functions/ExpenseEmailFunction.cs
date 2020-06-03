using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using Contoso.Expenses.Common.Models;

namespace Contoso.Expenses.Functions
{
    // see https://docs.microsoft.com/en-us/azure/azure-functions/functions-create-your-first-function-visual-studio#prerequisites
    // https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue
    // https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-sendgrid
    public static class ExpenseEmailFunction
    {
        [FunctionName("ExpenseEmailFunction")]
        public static void Run([QueueTrigger("contosoexpenses", Connection = "AzureWebJobsStorage")]string expenseItem, ILogger log,
                                [SendGrid(ApiKey = "SendGridKey")] out SendGridMessage message)
        {
            Expense expense = JsonConvert.DeserializeObject<Expense>(expenseItem);

            string emailFrom = "Expense@ContosoExpenses.com";
            string emailTo = expense.ApproverEmail;
            string emailSubject = $"New Expense for the amount of ${expense.Amount} submitted";
            string emailBody = $"Hello {expense.ApproverEmail}, <br/> New Expense report submitted for the purpose of: {expense.Purpose}. <br/> Please review as soon as possible. <br/> <br/> <br/> This is a auto generated email, please do not reply to this email";

            log.LogInformation($"Email Subject: {emailSubject}");
            log.LogInformation($"Email body: {emailBody}");

            message = new SendGridMessage();
            message.From = new EmailAddress(emailFrom, "Contoso Expenses");
            message.AddTo(emailTo, expense.ApproverEmail);
            message.Subject = emailSubject;
            message.AddContent(MimeType.Html, emailBody);

            log.LogInformation($"Email sent successfully to: {emailTo}");
        }
    }
 }
