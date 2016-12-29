﻿using CarRental.Business.Entities;
using Core.Common.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarRental.Data.Contracts.Repository_Interfaces
{
    public interface IReservationRepository : IDataRepository<Reservation>
    {
        //IEnumerable<CustomerReservationInfo> GetCurrentCustomerReservationInfo();
        //IEnumerable<CustomerReservationInfo> GetCustomerOpenReservationInfo();
        IEnumerable<Reservation> GetReservationsByPickupDate(DateTime pickupDate);
    }
}
