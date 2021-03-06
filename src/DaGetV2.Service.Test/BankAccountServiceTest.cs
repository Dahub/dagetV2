﻿namespace DaGetV2.Service.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using ApplicationCore.Domain;
    using ApplicationCore.DTO;
    using ApplicationCore.Exceptions;
    using ApplicationCore.Interfaces;
    using ApplicationCore.Services;
    using Moq;
    using Shared.TestTool;
    using Xunit;

    public class BankAccountServiceTest
    {
        [Fact]
        public void Delete_Bank_Account_Should_Delete_Bank_Account_With_All_Operations_And_All_OperationsTypes()
        {
            var dbName = DataBaseHelper.Instance.NewDataBase();
            var user = DataBaseHelper.Instance.UseNewUser(dbName);
            var bankAccountType = DataBaseHelper.Instance.UseNewBankAccountType(dbName);
            var bankAccount = DataBaseHelper.Instance.UseNewBankAccount(dbName, user.Id, bankAccountType.Id);
            var operationType1 = DataBaseHelper.Instance.UseNewOperationType(dbName, bankAccount.Id);
            var operationType2 = DataBaseHelper.Instance.UseNewOperationType(dbName, bankAccount.Id);
            DataBaseHelper.Instance.UseNewOperation(dbName, bankAccount.Id, operationType1.Id);
            DataBaseHelper.Instance.UseNewOperation(dbName, bankAccount.Id, operationType2.Id);

            var bankAccountService = new BankAccountService();

            using (var context = DataBaseHelper.Instance.CreateContext(dbName))
            {
                bankAccountService.DeleteBankAccountById(context, user.UserName, bankAccount.Id);

                Assert.Empty(context.OperationTypes.Where(ot => ot.BankAccount.Id.Equals(bankAccount.Id)));
                Assert.Empty(context.Operations.Where(o => o.BankAccount.Id.Equals(bankAccount.Id)));
                Assert.Empty(context.BankAccounts.Where(ba => ba.Id.Equals(bankAccount.Id)));
                Assert.Empty(context.UserBankAccounts.Where(uba => uba.BankAccount.Id.Equals(bankAccount.Id)));
            }
        }

        [Fact]
        public void Delete_Bank_Account_Where_User_Is_Not_Owner_Should_Throw_DaGet_Unauthorized_Exception()
        {
            var dbName = DataBaseHelper.Instance.NewDataBase();
            var user = DataBaseHelper.Instance.UseNewUser(dbName);
            var bankAccountType = DataBaseHelper.Instance.UseNewBankAccountType(dbName);
            var bankAccount = DataBaseHelper.Instance.UseNewBankAccount(dbName, user.Id, bankAccountType.Id);

            var bankAccountService = new BankAccountService();

            using (var context = DataBaseHelper.Instance.CreateContext(dbName))
            {
                var userBankAccount = context.UserBankAccounts.SingleOrDefault(uba =>
                    uba.BankAccountId.Equals(bankAccount.Id) && uba.UserId.Equals(user.Id));
                userBankAccount.IsOwner = false;
                userBankAccount.IsReadOnly = false;
                context.Update(userBankAccount);

                context.Commit();
            }

            using (var context = DataBaseHelper.Instance.CreateContext(dbName))
            {
                Assert.Throws<DaGetUnauthorizedException>(() =>
                    bankAccountService.DeleteBankAccountById(context, user.UserName, bankAccount.Id));
            }
        }


        [Fact]
        public void Delete_Bank_Account_Where_User_Is_Empty_Should_Throw_DaGet_Unauthorized_Exception()
        {
            var dbName = DataBaseHelper.Instance.NewDataBase();
            var bankAccountType = DataBaseHelper.Instance.UseNewBankAccountType(dbName);
            var bankAccount = DataBaseHelper.Instance.UseNewBankAccount(dbName, Guid.NewGuid(), bankAccountType.Id);

            var bankAccountService = new BankAccountService();

            using (var context = DataBaseHelper.Instance.CreateContext(dbName))
            {
                Assert.Throws<DaGetUnauthorizedException>(() =>
                    bankAccountService.DeleteBankAccountById(context, String.Empty, bankAccount.Id));
            }
        }

        [Fact]
        public void Add_With_Unknow_User_Should_Throw_DaGet_Unauthorized_Exception()
        {
            var dbName = DataBaseHelper.Instance.NewDataBase();
            var bankAccountType = DataBaseHelper.Instance.UseNewBankAccountType(dbName);
            var bankAccountService = new BankAccountService();

            using (var context = DataBaseHelper.Instance.CreateContext(dbName))
            {
                Assert.Throws<DaGetUnauthorizedException>(() => bankAccountService.Create(context, Guid.NewGuid().ToString(), new CreateBankAccountDto()
                {
                    BankAccountTypeId = bankAccountType.Id,
                    Wording = Guid.NewGuid().ToString(),
                    InitialBalance = 0m,
                    OperationsTypes = new List<string>()
                    {
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString()
                    }
                }));
            }
        }

        [Fact]
        public void Add_Should_Add_BankAccount()
        {
            var dbName = DataBaseHelper.Instance.NewDataBase();
            var user = DataBaseHelper.Instance.UseNewUser(dbName);
            var bankAccountType = DataBaseHelper.Instance.UseNewBankAccountType(dbName);
            var bankAccountService = new BankAccountService();

            var bankAccountWording = "test bank account";
            var initialBalance = 120.50m;

            using (var context = DataBaseHelper.Instance.CreateContext(dbName))
            {
                bankAccountService.Create(context, user.UserName, new CreateBankAccountDto()
                {
                    BankAccountTypeId = bankAccountType.Id,
                    Wording = bankAccountWording,
                    InitialBalance = initialBalance,
                    OperationsTypes = new List<string>()
                    {
                        "operation type est 1",
                        "operation type est 2",
                        "operation type est 3"
                    }
                });
            }

            using (var context = DataBaseHelper.Instance.CreateContext(dbName))
            {
                var bankAccountFromDb = context.BankAccounts.SingleOrDefault(ba => ba.Wording.Equals(bankAccountWording));

                Assert.NotNull(bankAccountFromDb);

                Assert.Equal(initialBalance, bankAccountFromDb.Balance);
                Assert.Equal(initialBalance, bankAccountFromDb.OpeningBalance);
                Assert.Equal(initialBalance, bankAccountFromDb.ActualBalance);
                Assert.Equal(bankAccountType.Id, bankAccountFromDb.BankAccountTypeId);

                var userBankAccountFromDb = context.UserBankAccounts.SingleOrDefault(uba => uba.BankAccountId.Equals(bankAccountFromDb.Id));

                Assert.NotNull(userBankAccountFromDb);
                Assert.Equal(user.Id, userBankAccountFromDb.UserId);
                Assert.True(userBankAccountFromDb.IsOwner);
                Assert.False(userBankAccountFromDb.IsReadOnly);                

                var operationsTypes = context.OperationTypes.Select(ot => ot.BankAccountId.Equals(bankAccountFromDb.Id));

                Assert.NotEmpty(operationsTypes);
                Assert.Equal(3, operationsTypes.Count());
            }
        }

        //[Fact]
        //public void GetAll_Should_Return_All_Bank_Account_For_User()
        //{
        //    var bankAccountType = new BankAccountType()
        //    {
        //        Id = Guid.NewGuid(),
        //        Wording = "bank account type",
        //        CreationDate = DateTime.Now,
        //        ModificationDate = DateTime.Now
        //    };

        //    var user = new User()
        //    {
        //        CreationDate = DateTime.Now,
        //        Id = Guid.NewGuid(),
        //        UserName = "Sammy",
        //        ModificationDate = DateTime.Now
        //    };

        //    var firstBankAccount = new BankAccount()
        //    {
        //        Balance = 50m,
        //        BankAccountTypeId = bankAccountType.Id,
        //        BankAccountType = bankAccountType,
        //        CreationDate = DateTime.Now,
        //        Id = Guid.NewGuid(),
        //        ModificationDate = DateTime.Now,
        //        OpeningBalance = 128.25m,
        //        Wording = "bank account 1"
        //    };

        //    var secondBankAccount = new BankAccount()
        //    {
        //        Balance = -90m,
        //        BankAccountTypeId = bankAccountType.Id,
        //        BankAccountType = bankAccountType,
        //        Id = Guid.NewGuid(),
        //        ModificationDate = DateTime.Now,
        //        OpeningBalance = 128.25m,
        //        Wording = "bank account 2"
        //    };

        //    bankAccountType.BanksAccounts = new List<BankAccount>()
        //    {
        //        firstBankAccount, secondBankAccount
        //    };

        //    var firstUserBankAccount = new UserBankAccount()
        //    {
        //        BankAccount = firstBankAccount,
        //        BankAccountId = firstBankAccount.Id,
        //        CreationDate = DateTime.Now,
        //        Id = Guid.NewGuid(),
        //        IsOwner = true,
        //        IsReadOnly = false,
        //        ModificationDate = DateTime.Now,
        //        User = user,
        //        UserId = user.Id
        //    };

        //    var secondUserBankAccount = new UserBankAccount()
        //    {
        //        BankAccount = secondBankAccount,
        //        BankAccountId = secondBankAccount.Id,
        //        CreationDate = DateTime.Now,
        //        Id = Guid.NewGuid(),
        //        IsOwner = false,
        //        IsReadOnly = false,
        //        ModificationDate = DateTime.Now,
        //        User = user,
        //        UserId = user.Id
        //    };

        //    user.UsersBanksAccounts = new List<UserBankAccount>()
        //    {
        //        firstUserBankAccount, secondUserBankAccount
        //    };

        //    firstBankAccount.UsersBanksAccounts = new List<UserBankAccount>()
        //    {
        //        firstUserBankAccount
        //    };

        //    secondBankAccount.UsersBanksAccounts = new List<UserBankAccount>()
        //    {
        //        secondUserBankAccount
        //    };

        //    var bankAccountRepositoryMock = new Mock<IBankAccountRepository>();
        //    bankAccountRepositoryMock.Setup(p => p.GetAllByUser("Sammy")).Returns(new List<BankAccount>()
        //    {
        //        firstBankAccount, secondBankAccount
        //    });

        //    var contextMock = new Mock<IContext>();
        //    contextMock.Setup(c => c.GetBankAccountRepository()).Returns(bankAccountRepositoryMock.Object);

        //    var bankAccountService = new BankAccountService();

        //    var bankAccounts = bankAccountService.GetAll(contextMock.Object, "Sammy");

        //    Assert.Equal(2, bankAccounts.Count());

        //    var myBankAccount = bankAccounts.SingleOrDefault(ba => ba.Id.Equals(secondBankAccount.Id));

        //    Assert.NotNull(myBankAccount);

        //    Assert.Equal(secondBankAccount.Balance, myBankAccount.Balance);
        //    Assert.Equal(bankAccountType.Wording, myBankAccount.BankAccountType);
        //    Assert.Equal(secondUserBankAccount.IsOwner, myBankAccount.IsOwner);

        //    Assert.Equal(secondUserBankAccount.IsReadOnly, myBankAccount.IsReadOnly);
        //    Assert.Equal(secondBankAccount.Wording, myBankAccount.Wording);
        //}

        [Fact]
        public void Update_With_Unknow_User_Should_Throw_DaGet_Unauthorized_Exception()
        {
            var dbName = DataBaseHelper.Instance.NewDataBase();
            var user = DataBaseHelper.Instance.UseNewUser(dbName);
            var bankAccountType = DataBaseHelper.Instance.UseNewBankAccountType(dbName);
            var bankAccount = DataBaseHelper.Instance.UseNewBankAccount(dbName, user.Id, bankAccountType.Id);
            var bankAccountService = new BankAccountService();

            using (var context = DataBaseHelper.Instance.CreateContext(dbName))
            {
                Assert.Throws<DaGetUnauthorizedException>(() => bankAccountService.Update(context, Guid.NewGuid().ToString(), new UpdateBankAccountDto()
                {
                    Id = bankAccount.Id,
                    BankAccountTypeId = bankAccountType.Id,
                    InitialBalance = 0m,
                    Wording = Guid.NewGuid().ToString()
                }));
            }
        }

        [Fact]
        public void Update_With_Unknow_Bank_Account_Should_Throw_DaGet_Not_Found_Exception()
        {
            var dbName = DataBaseHelper.Instance.NewDataBase();
            var user = DataBaseHelper.Instance.UseNewUser(dbName);
            var bankAccountType = DataBaseHelper.Instance.UseNewBankAccountType(dbName);
            var bankAccount = DataBaseHelper.Instance.UseNewBankAccount(dbName, user.Id, bankAccountType.Id);
            var bankAccountService = new BankAccountService();

            using (var context = DataBaseHelper.Instance.CreateContext(dbName))
            {
                Assert.Throws<DaGetNotFoundException>(() => bankAccountService.Update(context, Guid.NewGuid().ToString(), new UpdateBankAccountDto()
                {
                    Id = Guid.NewGuid(),
                    BankAccountTypeId = bankAccountType.Id,
                    InitialBalance = 0m,
                    Wording = Guid.NewGuid().ToString()
                }));
            }
        }

        [Fact]
        public void Update_With_Unknow_Bank_Account_Type_Should_Throw_DaGet_Not_Found_Exception()
        {
            var dbName = DataBaseHelper.Instance.NewDataBase();
            var user = DataBaseHelper.Instance.UseNewUser(dbName);
            var bankAccountType = DataBaseHelper.Instance.UseNewBankAccountType(dbName);
            var bankAccount = DataBaseHelper.Instance.UseNewBankAccount(dbName, user.Id, bankAccountType.Id);
            var bankAccountService = new BankAccountService();

            using (var context = DataBaseHelper.Instance.CreateContext(dbName))
            {
                Assert.Throws<DaGetNotFoundException>(() => bankAccountService.Update(context, user.UserName, new UpdateBankAccountDto()
                {
                    Id = bankAccount.Id,
                    BankAccountTypeId = Guid.NewGuid(),
                    InitialBalance = 0m,
                    Wording = Guid.NewGuid().ToString()
                }));
            }
        }

        [Fact]
        public void Update_With_Delete_Used_Operation_Type_Should_Throw_Daget_Service_Exception()
        {
            var dbName = DataBaseHelper.Instance.NewDataBase();
            var user = DataBaseHelper.Instance.UseNewUser(dbName);
            var bankAccountType = DataBaseHelper.Instance.UseNewBankAccountType(dbName);
            var bankAccount = DataBaseHelper.Instance.UseNewBankAccount(dbName, user.Id, bankAccountType.Id);
            var operationType = DataBaseHelper.Instance.UseNewOperationType(dbName, bankAccount.Id);

            DataBaseHelper.Instance.UseNewOperation(dbName, bankAccount.Id, operationType.Id);

            var bankAccountService = new BankAccountService();

            using (var context = DataBaseHelper.Instance.CreateContext(dbName))
            {
                Assert.Throws<DaGetServiceException>(() => bankAccountService.Update(context, user.UserName, new UpdateBankAccountDto()
                {
                    Id = bankAccount.Id,
                    BankAccountTypeId = bankAccountType.Id,
                    InitialBalance = 0m,
                    Wording = Guid.NewGuid().ToString(),
                    OperationsTypes = new List<KeyValuePair<Guid?, string>>()
                }));
            }
        }

        [Fact]
        public void Update_Should_Update_Bank_Account()
        {
            var dbName = DataBaseHelper.Instance.NewDataBase();
            var user = DataBaseHelper.Instance.UseNewUser(dbName);
            var bankAccountType = DataBaseHelper.Instance.UseNewBankAccountType(dbName);
            var bankAccount = DataBaseHelper.Instance.UseNewBankAccount(dbName, user.Id, bankAccountType.Id);
            var operationType = DataBaseHelper.Instance.UseNewOperationType(dbName, bankAccount.Id);

            var bankAccountService = new BankAccountService();

            var newBankAccountType = DataBaseHelper.Instance.UseNewBankAccountType(dbName);
            var newWording = Guid.NewGuid().ToString();
            var newExistingOperationTypeWording = Guid.NewGuid().ToString();
            var newOperationTypeWording = Guid.NewGuid().ToString();
            var expectingDeltaInBalance = 250.25m;

            Thread.Sleep(1); // ensure modification date will be greater than creation date

            using (var context = DataBaseHelper.Instance.CreateContext(dbName))
            {
                bankAccountService.Update(context, user.UserName, new UpdateBankAccountDto()
                {
                    Id = bankAccount.Id,
                    BankAccountTypeId = newBankAccountType.Id,
                    InitialBalance = bankAccount.OpeningBalance + expectingDeltaInBalance,
                    OperationsTypes = new List<KeyValuePair<Guid?, string>>()
                    {
                        new KeyValuePair<Guid?, string>(operationType.Id, newExistingOperationTypeWording),
                        new KeyValuePair<Guid?, string>(null, newOperationTypeWording)
                    },
                    Wording = newWording
                });
            }

            using (var context = DataBaseHelper.Instance.CreateContext(dbName))
            {
                var bankAccountFromDb = context.BankAccounts.SingleOrDefault(ba => ba.Id.Equals(bankAccount.Id));

                Assert.NotNull(bankAccountFromDb);
                Assert.Equal(bankAccount.OpeningBalance + expectingDeltaInBalance, bankAccountFromDb.OpeningBalance);
                Assert.Equal(bankAccount.Balance + expectingDeltaInBalance, bankAccountFromDb.Balance);
                Assert.Equal(bankAccount.ActualBalance + expectingDeltaInBalance, bankAccountFromDb.ActualBalance);
                Assert.Equal(newBankAccountType.Id, bankAccountFromDb.BankAccountTypeId);
                Assert.Equal(bankAccount.CreationDate, bankAccountFromDb.CreationDate);
                Assert.True(bankAccount.ModificationDate < bankAccountFromDb.ModificationDate);

                var operationsTypesFromDbs = context.OperationTypes.Where(ot => ot.BankAccountId.Equals(bankAccount.Id));

                Assert.NotEmpty(operationsTypesFromDbs);
                Assert.Equal(2, operationsTypesFromDbs.Count());
                Assert.True(operationsTypesFromDbs.Any(ot => ot.Wording.Equals(newOperationTypeWording)));
                Assert.True(operationsTypesFromDbs.Any(ot => ot.Wording.Equals(newExistingOperationTypeWording)));
            }
        }

        [Fact]
        public void GetById_Should_Return_Bank_Account()
        {
            var dbName = DataBaseHelper.Instance.NewDataBase();
            var user = DataBaseHelper.Instance.UseNewUser(dbName);
            var bankAccountType = DataBaseHelper.Instance.UseNewBankAccountType(dbName);
            var bankAccount = DataBaseHelper.Instance.UseNewBankAccount(dbName, user.Id, bankAccountType.Id);

            var bankAccountService = new BankAccountService();

            using (var context = DataBaseHelper.Instance.CreateContext(dbName))
            {
                var bankAccountFromDb = bankAccountService.GetById(context, user.UserName, bankAccount.Id);

                Assert.NotNull(bankAccountFromDb);
            }
        }

        [Fact]
        public void GetById_Should_Return_DaGet_Not_Found_Exception_Exception_When_User_Try_To_Get_Another_User_Bank_Account()
        {
            var dbName = DataBaseHelper.Instance.NewDataBase();
            var user = DataBaseHelper.Instance.UseNewUser(dbName);
            var user2 = DataBaseHelper.Instance.UseNewUser(dbName);
            var bankAccountType = DataBaseHelper.Instance.UseNewBankAccountType(dbName);
            var bankAccount = DataBaseHelper.Instance.UseNewBankAccount(dbName, user.Id, bankAccountType.Id);
            var bankAccount2 = DataBaseHelper.Instance.UseNewBankAccount(dbName, user2.Id, bankAccountType.Id);

            var bankAccountService = new BankAccountService();

            using (var context = DataBaseHelper.Instance.CreateContext(dbName))
            {
                Assert.Throws<DaGetNotFoundException>(() => bankAccountService.GetById(context, user2.UserName, bankAccount.Id));
            }
        }

        [Fact]
        public void GetById_Should_Return_DaGet_Not_Found_Exception_When_Bank_Account_Dont_Exists()
        {
            var dbName = DataBaseHelper.Instance.NewDataBase();
            var user = DataBaseHelper.Instance.UseNewUser(dbName);
            var bankAccountType = DataBaseHelper.Instance.UseNewBankAccountType(dbName);
            var bankAccount = DataBaseHelper.Instance.UseNewBankAccount(dbName, user.Id, bankAccountType.Id);

            var bankAccountService = new BankAccountService();

            using (var context = DataBaseHelper.Instance.CreateContext(dbName))
            {
                Assert.Throws<DaGetNotFoundException>(() => bankAccountService.GetById(context, user.UserName, Guid.NewGuid()));
            }
        }
    }
}
