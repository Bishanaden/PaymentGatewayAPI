using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace PaymentGatewayAPI.Models
{
    public class PaymentGatewayContext: DbContext
    {
        public PaymentGatewayContext(DbContextOptions<PaymentGatewayContext> options) : base(options)
        { 
        }

        public DbSet<PaymentGateway> PaymentGatewayItems { get; set; }
        public DbSet<Bank> Bank { get; set; }
        public DbSet<Cards> Cards { get; set; }
        public DbSet<Merchant> Merchant { get; set; }
        public DbSet<Orders> Orders { get; set; }
        public DbSet<Transactions> Transactions { get; set; }
    }
}
