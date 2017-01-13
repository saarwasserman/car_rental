﻿using CarRental.Business.Common;
using CarRental.Business.Contracts;
using CarRental.Business.Entities;
using CarRental.Common;
using CarRental.Data.Contracts;
using Core.Common.Contracts;
using Core.Common.Core;
using Core.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Security.Permissions;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace CarRental.Business.Managers
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall,
        ConcurrencyMode = ConcurrencyMode.Multiple,
        ReleaseServiceInstanceOnTransactionComplete = false)]
    public class InventoryManager : ManagerBase, IInventoryService
    {
        public InventoryManager()
        {
            ObjectBase.Container.SatisfyImportsOnce(this);
        }

        public InventoryManager(IDataRepositoryFactory dataRepositoryFactory)
        {
            _DataRepositoryFactory = dataRepositoryFactory;
        }

        public InventoryManager(IBusinessEngineFactory businessEngineFactory)
        {
            _BusinessEngineFactory = businessEngineFactory;
        }

        public InventoryManager(IDataRepositoryFactory dataRepositoryFactory, IBusinessEngineFactory businessEngineFactory)
        {
            _DataRepositoryFactory = dataRepositoryFactory;
            _BusinessEngineFactory = businessEngineFactory;
        }

        [Import]
        IDataRepositoryFactory _DataRepositoryFactory;

        [Import]
        IBusinessEngineFactory _BusinessEngineFactory;

        [PrincipalPermission(SecurityAction.Demand, Role = Security.CarRentalAdmin)]
        [PrincipalPermission(SecurityAction.Demand, Name = Security.CarRentalUser)]
        public Entities.Car GetCar(int carId)
        {
            return ExecuteFaultHandledOperation(() =>
            {
                ICarRepository carRepository = _DataRepositoryFactory.GetDataRepository<ICarRepository>();

                Car carEntity = carRepository.Get(carId);
                if (carEntity == null)
                {
                    NotFoundException ex = new NotFoundException(string.Format("Car with ID of {0} is not in the database", carId));
                    throw new FaultException<NotFoundException>(ex, ex.Message);
                }

                return carEntity;
            });
        }

        [PrincipalPermission(SecurityAction.Demand, Role = Security.CarRentalAdmin)]
        [PrincipalPermission(SecurityAction.Demand, Name = Security.CarRentalUser)]
        public Entities.Car[] GetAllCars()
        {
            return ExecuteFaultHandledOperation(() =>
            {
                ICarRepository carRepository = _DataRepositoryFactory.GetDataRepository<ICarRepository>();
                IRentalRepository rentalRepository = _DataRepositoryFactory.GetDataRepository<IRentalRepository>();

                IEnumerable<Car> cars = carRepository.Get();
                IEnumerable<Rental> rentedCars = rentalRepository.GetCurrentlyRentedCars();

                foreach (Car car in cars)
                {
                    Rental rentedCar = rentedCars.Where(item => item.CarId == car.CarId).FirstOrDefault();
                    car.CurrentlyRented = (rentedCar != null);
                }

                return cars.ToArray();
            });
        }

        [OperationBehavior(TransactionScopeRequired = true)]
        [PrincipalPermission(SecurityAction.Demand, Role = Security.CarRentalAdmin)]
        public Car UpdateCar(Car car)
        {
            return ExecuteFaultHandledOperation(() =>
            {
                ICarRepository carRepository = _DataRepositoryFactory.GetDataRepository<ICarRepository>();

                Car updateEntity = null;

                if (car.CarId == 0)
                    updateEntity = carRepository.Add(car);
                else
                    updateEntity = carRepository.Update(car);

                return updateEntity;
            });
        }

        [OperationBehavior(TransactionScopeRequired = true)]
        [PrincipalPermission(SecurityAction.Demand, Role = Security.CarRentalAdmin)]
        public void DeleteCar(int carId)
        {
            ExecuteFaultHandledOperation(() =>
            {
                ICarRepository carRepository = _DataRepositoryFactory.GetDataRepository<ICarRepository>();

                carRepository.Remove(carId);
            });
        }

        [PrincipalPermission(SecurityAction.Demand, Role = Security.CarRentalAdmin)]
        [PrincipalPermission(SecurityAction.Demand, Name = Security.CarRentalUser)]
        public Car[] GetAvailableCars(DateTime pickupDate, DateTime returnDate)
        {
            return ExecuteFaultHandledOperation(() =>
            {
                ICarRepository carRepository = _DataRepositoryFactory.GetDataRepository<ICarRepository>();
                IRentalRepository rentalRepository = _DataRepositoryFactory.GetDataRepository<IRentalRepository>();
                IReservationRepository reservationRepository = _DataRepositoryFactory.GetDataRepository<IReservationRepository>();

                ICarRentalEngine carRentalEngine = _BusinessEngineFactory.GetBusinessEngine<ICarRentalEngine>();

                IEnumerable<Car> allCars = carRepository.Get();
                IEnumerable<Rental> rentedCars = rentalRepository.GetCurrentlyRentedCars();
                IEnumerable<Reservation> reservedCars = reservationRepository.Get();

                List<Car> avaliableCars = new List<Car>();

                foreach (Car car in allCars)
                {
                    if (carRentalEngine.IsCarAvailable(car.CarId, pickupDate, returnDate, rentedCars, reservedCars))
                        avaliableCars.Add(car);
                }

                return avaliableCars.ToArray();
            });
        }
    }
}
