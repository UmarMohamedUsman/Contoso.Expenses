using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Counter;

namespace Contoso.Expenses.Web.AppMetrics
{
    public class MetricsRegistry
    {
        public static CounterOptions CreatedExpenseCounter => new CounterOptions
        {
            Name = "Created Expenses",
            Context = "Contoso Expense Web App",
            MeasurementUnit = Unit.Calls
        };
    }
}
