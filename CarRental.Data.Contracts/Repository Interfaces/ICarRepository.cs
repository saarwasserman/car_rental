﻿using CarRental.Business.Entities;
using Core.Common.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarRental.Data.Contracts
{
    public interface ICarRepository : IDataRepository<Car>
    {
    }
}
