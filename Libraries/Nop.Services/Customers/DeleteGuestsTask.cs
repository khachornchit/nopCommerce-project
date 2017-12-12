﻿using System;
using Nop.Core.Domain.Customers;
using Nop.Services.Tasks;

namespace Nop.Services.Customers
{
    /// <summary>
    /// Represents a task for deleting guest customers
    /// </summary>
    public partial class DeleteGuestsTask : IScheduleTask
    {
        private readonly ICustomerService _customerService;
        private readonly CustomerSettings _customerSettings;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="customerService">Customer service</param>
        /// <param name="customerSettings">Customer settings</param>
        public DeleteGuestsTask(ICustomerService customerService,
            CustomerSettings customerSettings)
        {
            this._customerService = customerService;
            this._customerSettings = customerSettings;
        }

        /// <summary>
        /// Executes a task
        /// </summary>
        public void Execute()
        {
            var olderThanMinutes = _customerSettings.DeleteGuestTaskOlderThanMinutes;
            // Default value in case 0 is returned.  0 would effectively disable this service and harm performance.
            olderThanMinutes = olderThanMinutes == 0 ? 1440 : olderThanMinutes;
    
            _customerService.DeleteGuestCustomers(null, DateTime.UtcNow.AddMinutes(-olderThanMinutes), true);
        }
    }
}
