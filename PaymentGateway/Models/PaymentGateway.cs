using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PaymentGatewayAPI.Models
{
    public class PaymentGateway
    {
        public int Id {get; set;}
        public string HowTo {get; set;}
        public string Platform { get; set;}
        public string PaymentGate { get; set; }
    }

    public class Merchant
    {
        public int MerchantId { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string LastAuthenticationDate { get; set; }
    }

    public class Orders
    {
        public int OrdersId { get; set; }
        public string ExpiryDate { get; set; }
        public int Amount { get; set; }
        public string CardNumber { get; set; }
        public string Currency { get; set; }
        public string Type { get; set; }
        public string CVV { get; set; }
        public string BankName { get; set; }
        public string BillingAddress { get; set; }
        public int MerchantId { get; set; }
    }

    public class Bank
    {
        public int BankId { get; set; }
        public string BankName { get; set; }
    }

    public class Transactions
    {
        public int TransactionsId { get; set; }
        public string status { get; set; }
        public int OrdersId { get; set; }
        public int BankId { get; set; }
        public string ExecutionDate { get; set; }
    }

    public class Cards
    {
        public int CardsId { get; set; }
        public string CardNumber { get; set; }
        public int Funds { get; set; }
        public int BankId { get; set; }
    }
}