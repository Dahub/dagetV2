﻿namespace DaGetV2.ApplicationCore.Domain
{
    using System;
    using DaGetV2.ApplicationCore.Interfaces;

    public class UserBankAccount : IDomainObject
    {
        public Guid Id { get; set; }

        public DateTime CreationDate { get; set; }

        public DateTime ModificationDate { get; set; }

        public Guid UserId { get; set; }

        public User User { get; set; }

        public Guid BankAccountId { get; set; }

        public BankAccount BankAccount { get; set; }

        public bool IsOwner { get; set; }

        public bool IsReadOnly { get; set; }
    }
}
