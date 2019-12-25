using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using PaymentGatewayAPI.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace PaymentGatewayAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentGatewayController : ControllerBase
    {
        private readonly PaymentGatewayContext _context;

        public PaymentGatewayController(PaymentGatewayContext context) => _context = context;

        //GET: api/paymentgateway/n
        [HttpGet("{id}")]
        [Produces("application/json")]
        public ActionResult<Orders> GetPayment(int id)
        {
            var message = new JObject();
            JArray array = new JArray();
            message["Transaction Details"] = array;

            string cardNumber = null, expiryDate = null, type = null, cvv = null, billingAddress = null, status = null, executionDate = null;

            var orderItem = from orders in _context.Orders
                            join trans in _context.Transactions on orders.OrdersId equals trans.OrdersId
                            where (orders.OrdersId == id)
                            select new { CardNumber = orders.CardNumber, ExpiryDate = orders.ExpiryDate, Type = orders.Type, CVV = orders.CVV, BillingAddress = orders.BillingAddress, Status = trans.status, ExecutionDate = trans.ExecutionDate};

            foreach (var item in orderItem)
            {
                cardNumber = item.CardNumber;
                expiryDate = item.ExpiryDate;
                type = item.Type;
                cvv = item.CVV;
                billingAddress = item.BillingAddress;
                status = item.Status;
                executionDate = item.ExecutionDate;
            }

            if (cardNumber == null && expiryDate == null && type == null && cvv == null && billingAddress == null && status == null && executionDate == null)
            {
                array.Add("Status Code: 400 Bad Request");
                array.Add("Error Message: The orderId supplied does not exist.");
                return new ContentResult()
                {
                    StatusCode = 400,
                    Content = message.ToString(),
                    ContentType = "application/json"
                };
            }
            else
            {
                string decryptedCardNumber = Decrypt(cardNumber, "sblw-3hn8-sqoy19");
                array.Add("Status Code: 200 Success OK");
                array.Add("CardNumber: " + decryptedCardNumber.Substring(decryptedCardNumber.Length - 4).PadLeft(decryptedCardNumber.Length, '*'));
                array.Add("ExpiryDate: " + expiryDate);
                array.Add("Type: " + type);
                array.Add("CVV: " + cvv);
                array.Add("Billing Address: " + billingAddress);
                array.Add("Transaction Status: " + status);
                array.Add("Execution Date: " + executionDate);
                return new ContentResult()
                {
                    StatusCode = 200,
                    Content = message.ToString(),
                    ContentType = "application/json"
                };
            }
        }

        //POST: api/paymentgateway
        [HttpPost]
        [Produces("application/json")]
        public ActionResult<Orders> PostPayment(Orders orders)
        {
            var message = new JObject();
            JArray array = new JArray();
            message["Status Message"] = array;

            if (!String.IsNullOrEmpty(validateMerchantId(orders.MerchantId)))
            {
                array.Add("Status Code: 400 Bad Request");
                array.Add(validateMerchantId(orders.MerchantId));
                array.Add("Date: " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
                return new ContentResult()
                {
                    StatusCode = 400,
                    Content = message.ToString(),
                    ContentType = "application/json"
                };
            }
            
            if (String.IsNullOrEmpty(validateRequestValues(orders))) {
                orders.CardNumber.Trim();
                orders.CVV.Trim();
                orders.CardNumber = Encrypt(orders.CardNumber, "sblw-3hn8-sqoy19");
                _context.Orders.Add(orders);
                _context.SaveChanges();
            }
            else {
                array.Add("Status Code: 400 Bad Request");
                array.Add(validateRequestValues(orders));
                array.Add("Date: " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
                return new ContentResult()
                {
                    StatusCode = 400,
                    Content = message.ToString(),
                    ContentType = "application/json"
                };
            }

            string processPaymentResponse = processPayment(orders);

            if (processPaymentResponse == "Success")
            {
                array.Add("Status Code: 200 Success OK");
                array.Add("Response Message: Payment has been accepted and processed.");
                array.Add("Date: " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
                return new ContentResult()
                {
                    StatusCode = 200,
                    Content = message.ToString(),
                    ContentType = "application/json"
                };
            }
            else {
                array.Add("Status Code: 400 Bad Request");
                array.Add(processPaymentResponse);
                array.Add("Date: " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
                return new ContentResult()
                {
                    StatusCode = 400,
                    Content = message.ToString(),
                    ContentType = "application/json"
                };
            }
        }

        public string validateRequestValues(Orders orders) {

            string result = "";
            DateTime Date;

            if (String.IsNullOrEmpty(orders.CardNumber) || String.IsNullOrEmpty(orders.ExpiryDate) || String.IsNullOrEmpty(orders.Amount.ToString()) || String.IsNullOrEmpty(orders.Currency) || String.IsNullOrEmpty(orders.Type) || String.IsNullOrEmpty(orders.CVV) || String.IsNullOrEmpty(orders.BankName) || String.IsNullOrEmpty(orders.BillingAddress))
                result = "Error Message: All fields are required";

            if (!orders.Amount.ToString().All(char.IsNumber) || !orders.CardNumber.All(char.IsNumber) || !orders.CVV.All(char.IsNumber))
                result = "Error Message: The fields 'Amount, CardNumber, CVV' should be a numeric value.";

            if (orders.CardNumber.Length != 16)
                result = "Error Message: The field 'CardNumber' should be only 16 digits.";

            if (orders.CVV.Length != 3)
                result = "Error Message: The field 'CVV' should be only 3 digits.";

            if (!DateTime.TryParseExact(orders.ExpiryDate, "dd/yy", DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out Date))
                result = "Error Message: The field 'Expiry Date' should be in format dd/yy.";

            return result;
        }

        public string validateMerchantId (int merchantId)
        {
            string result = "";

            var queryResult = _context.Merchant.Where(a => a.MerchantId == merchantId).SingleOrDefault();

            if (queryResult == null)
                return result = "Error Message: The merchant identifier supplied in the 'MerchantId' field does not exist.";

            return result;
        }

        public string processPayment(Orders orders)
        {
            var transactions = new Transactions { };
            string bankResponse = "";
            var bank = _context.Bank.Where(a => a.BankName == orders.BankName).SingleOrDefault();

            if (bank == null)
                return bankResponse = "Error Message: The bank name supplied in the 'BankName' field does not exist.";
            else {
                transactions = new Transactions { status = "Pending", OrdersId = orders.OrdersId, BankId = bank.BankId, ExecutionDate = DateTime.Today.ToString("dd-MM-yyyy HH:mm:ss") };
                _context.Transactions.Add(transactions);
                _context.SaveChanges();
            }

            bankResponse = bankSimulator(orders, bank.BankId);

            if (bankResponse == "Success") {
                transactions.status = "Accepted";
                transactions.ExecutionDate = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
                _context.Transactions.Update(transactions);
                _context.SaveChanges();
            } else {
                transactions.status = "Declined";
                transactions.ExecutionDate = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");
                _context.Transactions.Update(transactions);
                _context.SaveChanges();
            }

            return bankResponse;
        }

        public string bankSimulator(Orders orders, int bankId)
        {
            var card = _context.Cards.Where(c => c.CardNumber == Decrypt(orders.CardNumber, "sblw-3hn8-sqoy19")).SingleOrDefault();
            string response = "";
            if(card == null)
                return "Error Message: The card supplied in the 'CardNumber' field does not exist.";

            var bankQuery = from cards in _context.Cards
                          join bank in _context.Bank on cards.BankId equals bank.BankId
                          where (cards.CardNumber == card.CardNumber)
                          select new { BankId = bank.BankId, CardNumber = cards.CardNumber, Funds = cards.Funds };

            foreach (var bank in bankQuery)
            {
                if (Decrypt(orders.CardNumber, "sblw-3hn8-sqoy19") == bank.CardNumber) {
                    if (bank.Funds > orders.Amount) {
                        response = "Success";
                        break;
                    }
                    else { 
                        response = "Error Message: Not enough funds on the user's card account.";
                        break;
                    }
                }
                else {
                    response = "Error Message: The card supplied in the 'CardNumber' field does not exist.";
                    break;
                }
            }
            return response;
        }

        public static string Encrypt(string input, string key)
        {
            byte[] inputArray = System.Text.UTF8Encoding.UTF8.GetBytes(input);
            TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider();
            tripleDES.Key = UTF8Encoding.UTF8.GetBytes(key);
            tripleDES.Mode = CipherMode.ECB;
            tripleDES.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = tripleDES.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);
            tripleDES.Clear();
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }
        public static string Decrypt(string input, string key)
        {
            byte[] inputArray = Convert.FromBase64String(input);
            TripleDESCryptoServiceProvider tripleDES = new TripleDESCryptoServiceProvider();
            tripleDES.Key = UTF8Encoding.UTF8.GetBytes(key);
            tripleDES.Mode = CipherMode.ECB;
            tripleDES.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = tripleDES.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);
            tripleDES.Clear();
            return UTF8Encoding.UTF8.GetString(resultArray);
        }
    }
}
