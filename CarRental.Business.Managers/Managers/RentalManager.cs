﻿using CarRental.Business.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CarRental.Business.Entities;
using Core.Common.Core;
using Core.Common.Contracts;
using System.ComponentModel.Composition;
using System.ServiceModel;
using CarRental.Data.Contracts;
using Core.Common.Exceptions;
using CarRental.Common;
using System.Security.Permissions;
using CarRental.Business.Common;


namespace CarRental.Business.Managers
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall,
        ConcurrencyMode = ConcurrencyMode.Multiple,
        ReleaseServiceInstanceOnTransactionComplete = false)]
    public class RentalManager : ManagerBase, IRentalService
    {
        public RentalManager()
        {
            ObjectBase.Container.SatisfyImportsOnce(this);
        }

        public RentalManager(IDataRepositoryFactory dataRepositoryFactory)
        {
            _DataRepositoryFactory = dataRepositoryFactory;
        }

        public RentalManager(IBusinessEngineFactory businessEngineFactory)
        {
            _BusinessEngineFactory = businessEngineFactory;
        }

        public RentalManager(IDataRepositoryFactory dataRepositoryFactory, IBusinessEngineFactory businessEngineFactory)
        {
            _DataRepositoryFactory = dataRepositoryFactory;
            _BusinessEngineFactory = businessEngineFactory;
        }

        [Import]
        IDataRepositoryFactory _DataRepositoryFactory;

        [Import]
        IBusinessEngineFactory _BusinessEngineFactory;

        protected override Account LoadAuthorizationValidationAccount(string loginName)
        {
            IAccountRepository accountRepository = _DataRepositoryFactory.GetDataRepository<IAccountRepository>();
            Account authAcct = accountRepository.GetByLogin(loginName);
            if (authAcct == null)
            {
                NotFoundException ex = new NotFoundException(string.Format("Cannot find account for login name {0} to use for security trimming.", loginName));
                throw new FaultException<NotFoundException>(ex, ex.Message);
            }

            return authAcct;
        }

        [OperationBehavior(TransactionScopeRequired = true)]
        [PrincipalPermission(SecurityAction.Demand, Role = Security.CarRentalAdmin)]
        public Rental RentCarToCustomer(string loginEmail, int carId, DateTime dateDueBack)
        {
            return ExecuteFaultHandledOperation(() =>
            {
                ICarRentalEngine carRentalEngine = _BusinessEngineFactory.GetBusinessEngine<ICarRentalEngine>();

                try
                {
                    Rental rental = carRentalEngine.RentCarToCustomer(loginEmail, carId, DateTime.Now, dateDueBack);

                    return rental;
                }
                catch (UnableToRentForDateException ex)
                {
                    throw new FaultException<UnableToRentForDateException>(ex, ex.Message);
                }
                catch (CarCurrentlyRentedException ex)
                {
                    throw new FaultException<CarCurrentlyRentedException>(ex, ex.Message);
                }
                catch (NotFoundException ex)
                {
                    throw new FaultException<NotFoundException>(ex, ex.Message);
                }
            });
        }

        [OperationBehavior(TransactionScopeRequired = true)]
        [PrincipalPermission(SecurityAction.Demand, Role = Security.CarRentalAdmin)]
        public Rental RentCarToCustomer(string loginEmail, int carId, DateTime rentalDate, DateTime dateDueBack)
        {
            return ExecuteFaultHandledOperation(() =>
            {
                ICarRentalEngine carRentalEngine = _BusinessEngineFactory.GetBusinessEngine<ICarRentalEngine>();

                try
                {
                    Rental rental = carRentalEngine.RentCarToCustomer(loginEmail, carId, rentalDate, dateDueBack);

                    return rental;
                }
                catch (UnableToRentForDateException ex)
                {
                    throw new FaultException<UnableToRentForDateException>(ex, ex.Message);
                }
                catch (CarCurrentlyRentedException ex)
                {
                    throw new FaultException<CarCurrentlyRentedException>(ex, ex.Message);
                }
                catch (NotFoundException ex)
                {
                    throw new FaultException<NotFoundException>(ex, ex.Message);
                }
            });
        }

        [OperationBehavior(TransactionScopeRequired = true)]
        [PrincipalPermission(SecurityAction.Demand, Role = Security.CarRentalAdmin)]
        [PrincipalPermission(SecurityAction.Demand, Name = Security.CarRentalUser)]
        public Reservation MakeReservation(string loginEmail, int carId, DateTime rentalDate, DateTime returnDate)
        {
            return ExecuteFaultHandledOperation(() =>
            {
                IAccountRepository accountRepository = _DataRepositoryFactory.GetDataRepository<IAccountRepository>();
                IReservationRepository reservationRepository = _DataRepositoryFactory.GetDataRepository<IReservationRepository>();

                Account account = accountRepository.GetByLogin(loginEmail);
                if (account == null)
                {
                    NotFoundException ex = new NotFoundException(string.Format("No account found for login '{0}'.", loginEmail));
                    throw new FaultException<NotFoundException>(ex, ex.Message);
                }

                ValidateAuthorization(account);

                Reservation reservation = new Reservation()
                {
                    AccountId = account.AccountId,
                    CarId = carId,
                    RentalDate = rentalDate,
                    ReturnDate = returnDate
                };

                Reservation savedEntity = reservationRepository.Add(reservation);

                return savedEntity;
            });
        }


        [PrincipalPermission(SecurityAction.Demand, Role = Security.CarRentalAdmin)]
        [PrincipalPermission(SecurityAction.Demand, Name = Security.CarRentalUser)]
        public IEnumerable<Entities.Rental> GetRentalHistory(string loginEmail)
        {
            return ExecuteFaultHandledOperation(() =>
            {
                IAccountRepository accountRepository = _DataRepositoryFactory.GetDataRepository<IAccountRepository>();
                IRentalRepository rentalRepository = _DataRepositoryFactory.GetDataRepository<IRentalRepository>();

                Account account = accountRepository.GetByLogin(loginEmail);
                if (account == null)
                {
                    NotFoundException ex = new NotFoundException(string.Format("No account found for login '{0}'.", loginEmail));
                    throw new FaultException<NotFoundException>(ex, ex.Message);
                }

                ValidateAuthorization(account);

                IEnumerable<Rental> rentalHistory = rentalRepository.GetRentalHistoryByAccount(account.AccountId);

                return rentalHistory;
            });
        }

        [OperationBehavior(TransactionScopeRequired = true)]
        [PrincipalPermission(SecurityAction.Demand, Role = Security.CarRentalAdmin)]
        public void ExecuteRentalFromReservation(int reservationId)
        {
            ExecuteFaultHandledOperation (() =>
            {
                IAccountRepository accountRepository = _DataRepositoryFactory.GetDataRepository<IAccountRepository>();
                IReservationRepository reservationRepository = _DataRepositoryFactory.GetDataRepository<IReservationRepository>();
                ICarRentalEngine carRentalEngine = _BusinessEngineFactory.GetBusinessEngine<ICarRentalEngine>();

                Reservation reservation = reservationRepository.Get(reservationId);
                if (reservation == null)
                {
                    NotFoundException ex = new NotFoundException(string.Format("Reservation {0} is not found.", reservationId));
                    throw new FaultException<NotFoundException>(ex, ex.Message);
                }

                Account account = accountRepository.Get(reservation.AccountId);
                if (account == null)
                {
                    NotFoundException ex = new NotFoundException(string.Format("No account found for account ID '{0}'.", reservationId));
                    throw new FaultException<NotFoundException>(ex, ex.Message);
                }

                try
                {
                    Rental rental = carRentalEngine.RentCarToCustomer(account.LoginEmail, reservation.CarId, reservation.RentalDate, reservation.ReturnDate);
                }
                catch (UnableToRentForDateException ex)
                {
                    throw new FaultException<UnableToRentForDateException>(ex, ex.Message);
                }
                catch (CarCurrentlyRentedException ex)
                {
                    throw new FaultException<CarCurrentlyRentedException>(ex, ex.Message);
                }
                catch (NotFoundException ex)
                {
                    throw new FaultException<NotFoundException>(ex, ex.Message);
                }

                reservationRepository.Remove(reservationId);
            });
        }

        [OperationBehavior(TransactionScopeRequired = true)]
        [PrincipalPermission(SecurityAction.Demand, Role = Security.CarRentalAdmin)]
        [PrincipalPermission(SecurityAction.Demand, Name = Security.CarRentalUser)]
        public void CancelReservation(int reservationId)
        {
            ExecuteFaultHandledOperation(() =>
            {
                IReservationRepository reservationRepository = _DataRepositoryFactory.GetDataRepository<IReservationRepository>();

                Reservation reservation = reservationRepository.Get(reservationId);
                if (reservation == null)
                {
                    NotFoundException ex = new NotFoundException(string.Format("No reservation found for id '{0}", reservationId));
                    throw new FaultException<NotFoundException>(ex, ex.Message);
                }

                ValidateAuthorization(reservation);

                reservationRepository.Remove(reservationId);
            });
        }

        [PrincipalPermission(SecurityAction.Demand, Role = Security.CarRentalAdmin)]
        public CustomerReservationData[] GetCurrentReservations()
        {
            IReservationRepository reservationRepository = _DataRepositoryFactory.GetDataRepository<IReservationRepository>();

            List<CustomerReservationData> reservationData = new List<CustomerReservationData>();

            IEnumerable<CustomerReservationInfo> reservationInfoSet = reservationRepository.GetCurrentCustomerReservationInfo();
            foreach (CustomerReservationInfo reservationInfo in reservationInfoSet)
            {
                reservationData.Add(new CustomerReservationData
                {
                    ReservationId = reservationInfo.Reservation.ReservationId,
                    CustomerName = reservationInfo.Account.FirstName + " " + reservationInfo.Account.LastName,
                    Car = reservationInfo.Car.Color + " " + reservationInfo.Car.Year + " " + reservationInfo.Car.Description,
                    RentalDate = reservationInfo.Reservation.RentalDate,
                    ReturnDate = reservationInfo.Reservation.ReturnDate
                });
            }

            return reservationData.ToArray();
        }

        [PrincipalPermission(SecurityAction.Demand, Role = Security.CarRentalAdmin)]
        [PrincipalPermission(SecurityAction.Demand, Name = Security.CarRentalUser)]
        public CustomerReservationData[] GetCustomerReservations(string loginEmail)
        {
            return ExecuteFaultHandledOperation(() =>
            {
                IAccountRepository accountRepository = _DataRepositoryFactory.GetDataRepository<IAccountRepository>();
                IReservationRepository reservationRepository = _DataRepositoryFactory.GetDataRepository<IReservationRepository>();

                Account account = accountRepository.GetByLogin(loginEmail);
                if (account == null)
                {
                    NotFoundException ex = new NotFoundException(string.Format("No account found for login '{0}", loginEmail));
                    throw new FaultException<NotFoundException>(ex, ex.Message);
                }

                ValidateAuthorization(account);

                List<CustomerReservationData> reservationData = new List<CustomerReservationData>();

                IEnumerable<CustomerReservationInfo> reservationInfoSet = new List<CustomerReservationInfo>();
                foreach (CustomerReservationInfo reservationInfo in reservationInfoSet)
                {
                    reservationData.Add(new CustomerReservationData
                    {
                        ReservationId = reservationInfo.Reservation.ReservationId,
                        CustomerName = reservationInfo.Account.FirstName + " " + reservationInfo.Account.LastName,
                        Car = reservationInfo.Car.Color + " " + reservationInfo.Car.Year + " " + reservationInfo.Car.Description,
                        RentalDate = reservationInfo.Reservation.RentalDate,
                        ReturnDate = reservationInfo.Reservation.ReturnDate
                    });
                }

                return reservationData.ToArray();
            });
        }

        [PrincipalPermission(SecurityAction.Demand, Role = Security.CarRentalAdmin)]
        [PrincipalPermission(SecurityAction.Demand, Name = Security.CarRentalUser)]
        public Rental GetRental(int rentalId)
        {
            return ExecuteFaultHandledOperation(() =>
            {
                IRentalRepository rentalRepository = _DataRepositoryFactory.GetDataRepository<IRentalRepository>();

                Rental rental = rentalRepository.Get(rentalId);
                if (rental == null)
                {
                    NotFoundException ex = new NotFoundException(string.Format("No rental record found for id '{0}", rentalId));
                    throw new FaultException<NotFoundException>(ex, ex.Message);
                }

                ValidateAuthorization(rental);

                return rental;
            });
        }

        [PrincipalPermission(SecurityAction.Demand, Role = Security.CarRentalAdmin)]
        public CustomerRentalData[] GetCurrentRentals()
        {
            return ExecuteFaultHandledOperation(() =>
            {
                IRentalRepository rentalRepository = _DataRepositoryFactory.GetDataRepository<IRentalRepository>();

                List<CustomerRentalData> rentalData = new List<CustomerRentalData>();

                IEnumerable<CustomerRentalInfo> rentalInfoSet = rentalRepository.GetCurrentCustomerRentalInfo();
                foreach (CustomerRentalInfo rentalInfo in rentalInfoSet)
                {
                    rentalData.Add(new CustomerRentalData
                    {
                        RentalId = rentalInfo.Rental.RentalId,
                        CustomerName = rentalInfo.Customer.FirstName + " " + rentalInfo.Customer.LastName,
                        Car = rentalInfo.Car.Color + " " + rentalInfo.Car.Year + " " + rentalInfo.Car.Description,
                        DateRented = rentalInfo.Rental.DateRented,
                        ExpectedReturn = rentalInfo.Rental.DateDue
                    });
                }

                return rentalData.ToArray();
            });
        }

        [PrincipalPermission(SecurityAction.Demand, Role = Security.CarRentalAdmin)]
        public Reservation[] GetDeadReservations()
        {
            return ExecuteFaultHandledOperation(() =>
            {
                IReservationRepository reservationRepository = _DataRepositoryFactory.GetDataRepository<IReservationRepository>();

                IEnumerable<Reservation> reservations = reservationRepository.GetReservationsByPickupDate(DateTime.Now.AddDays(-1));

                return (reservations != null ? reservations.ToArray() : null);
            });
        }

        [PrincipalPermission(SecurityAction.Demand, Role = Security.CarRentalAdmin)]
        public bool IsCarCurrentlyRented(int carId)
        {
            return ExecuteFaultHandledOperation(() =>
            {
                ICarRentalEngine carRentalEngine = _BusinessEngineFactory.GetBusinessEngine<ICarRentalEngine>();

                return carRentalEngine.IsCarCurrentlyRented(carId);
            });
        }

        [OperationBehavior(TransactionScopeRequired = true)]
        [PrincipalPermission(SecurityAction.Demand, Role = Security.CarRentalAdmin)]
        public void AcceptCarReturn(int carId)
        {
            ExecuteFaultHandledOperation(() =>
            {
                IRentalRepository rentalRepository = _DataRepositoryFactory.GetDataRepository<IRentalRepository>();
                ICarRepository carRespository = _DataRepositoryFactory.GetDataRepository<ICarRepository>();

                Rental rental = rentalRepository.GetCurrentRentalByCar(carId);
                if (rental == null)
                {
                    NotFoundException ex = new NotFoundException(string.Format("Car '{0}' is not rented", carId));
                    throw new FaultException<NotFoundException>(ex, ex.Message);
                }

                rental.DateReturned = DateTime.Now;

                rentalRepository.Update(rental);
            });
        }

        [PrincipalPermission(SecurityAction.Demand, Role = Security.CarRentalAdmin)]
        [PrincipalPermission(SecurityAction.Demand, Name = Security.CarRentalUser)]
        public Reservation GetReservation(int reservationId)
        {
            return ExecuteFaultHandledOperation(() =>
            {
                IReservationRepository reservationRepository = _DataRepositoryFactory.GetDataRepository<IReservationRepository>();

                Reservation reservation = reservationRepository.Get(reservationId);
                if (reservation == null)
                {
                    NotFoundException ex = new NotFoundException(string.Format("No reservation record found for id '{0}", reservationId));
                    throw new FaultException<NotFoundException>(ex, ex.Message);
                }

                ValidateAuthorization(reservation);

                return reservation;
            });
        }
    }
}
