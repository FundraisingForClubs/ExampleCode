// Check Payments for Upcoming Draw
// Run daily at 7am 
// Check Direct Debit Payments - 2 days before the draw
// Email Club Admin if there is an issue

// Link to GoCardless to check for payment

// Link to BC Database to check and update ClubDraw


using FFCBCPaymentCheckmySQLv1.Functions;
using FFCBCPaymentCheckmySQLv1.Models;
using MySqlConnector;
using System.Data;
using System.Globalization;
using System.Text;

CultureInfo culture = new CultureInfo("en-GB");
string connString = "Server=xxxxxxxxxxxxx.mysql.database.azure.com;UserID=xxxxxx;Password=xxxxxxxxx;Database=xxxxxxxx;SslMode=Required";

using var connection = new MySqlConnection(connString);
await connection.OpenAsync();

int clubId = 1;
int numbersPurchased = 0;
int memberId = 0;
int rowsUpdated = 0;

List<int> clubDrawTicketList;
List<CustomerNotPaidOut> customerNotPaidOutList = new List<CustomerNotPaidOut>();

StringBuilder errorList = new StringBuilder();

DBFunctions dBFunctions = new DBFunctions(connection);

DateTime dateOfNextDraw = await dBFunctions.GetDateOfNextDraw(clubId);

Console.WriteLine("Date Of Next Draw: " + dateOfNextDraw.ToString("dd/MM/yyyy"));

//Check if been run already
//Check for ClubDrawDateId in ClubDraw Table
//TODO: If instant pay is available then there is the possibility that these have been entered into the ClubDraw Table already
//Option not available to Bishop's Castle at the moment
//Update - Already checks if numbers have been entered

ClubDrawDate clubDrawDate = await dBFunctions.GetClubDrawDate(clubId, dateOfNextDraw);

if (clubDrawDate != null)
{
    //Check ClubDraw
    Console.WriteLine("ClubDrawDateId: " + clubDrawDate.ClubDrawDateId);

    clubDrawTicketList = await dBFunctions.GetClubTicketEnteredInDraw(clubDrawDate.ClubDrawDateId);

    int pricePerNumber = await dBFunctions.GetPricePerNumber(clubId);

    if (pricePerNumber > 0)
    {
        List<SubscriptionPayment> paymentList = new List<SubscriptionPayment>();
        GoCardlessFunctions goCardlessFunctions = new GoCardlessFunctions();

        GoCardless.Services.CustomerListResponse customerList = new GoCardless.Services.CustomerListResponse();
        customerList = await goCardlessFunctions.GetGoCardlessCustomers();

        foreach (var customer in customerList.Customers)
        {
            Console.WriteLine("Customer: " + customer.GivenName + " " + customer.FamilyName);

            // Has Cusomer got a 100 Club Subscription?
            List<GoCardless.Resources.Subscription> subscriptions = new List<GoCardless.Resources.Subscription>();
            subscriptions = goCardlessFunctions.ListCustomerSubscription(customer.Id);

            if (subscriptions.Count > 0)
            {
                Console.WriteLine("Subriptions Count: " + subscriptions.Count.ToString());

                foreach (var subscription in subscriptions)
                {
                    // Get Payment for Subscription and Customer
                    paymentList = goCardlessFunctions.GetPaymentsFromCustomerSubscription(subscription.Id, dateOfNextDraw, culture);

                    foreach (var payment in paymentList)
                    {
                        Console.WriteLine("Charge Date: " + payment.ChargeDate.ToString("dd/MM/yyyy"));
                        Console.WriteLine("Amount: " + payment.Amount.ToString());
                        Console.WriteLine("Status: " + payment.Status);

                        if (payment.Status != "PaidOut")
                        {
                            // Add to Naughty List
                            CustomerNotPaidOut customerNotPaidOut = new CustomerNotPaidOut();
                            customerNotPaidOut.Customer = customer.GivenName + " " + customer.FamilyName;
                            customerNotPaidOut.Email = customer.Email;
                            customerNotPaidOut.PaymentChargeDate = payment.ChargeDate;
                            customerNotPaidOut.Status = payment.Status;
                            customerNotPaidOut.Amount = payment.Amount;
                            customerNotPaidOutList.Add(customerNotPaidOut);
                        }
                        else
                        {
                            numbersPurchased = 0;
                            // Calculate how many Numbers purchased
                            if (payment.Amount > 0 && pricePerNumber > 0)
                            {
                                numbersPurchased = payment.Amount / pricePerNumber;
                            }

                            if (numbersPurchased > 0)
                            {
                                Console.WriteLine("Numbers Purchased: " + numbersPurchased.ToString());

                                // Get MemberId from email
                                memberId = await dBFunctions.GetMemberIdFromEmail(customer.Email);

                                if (memberId > 0)
                                {
                                    // Get all numbers from ClubTicket 
                                    List<int> memberTicketList = await dBFunctions.GetClubTicketByMemberId(clubId, memberId);

                                    // Check that customer has the same or more numbers than purchased
                                    Console.WriteLine("Member Numbers Count: " + memberTicketList.Count.ToString());

                                    if (memberTicketList.Count != numbersPurchased)
                                    {
                                        errorList.AppendLine("Customer (" + customer.Email + ") has purchased " + numbersPurchased.ToString() + " Numbers - Numbers in DB: " + memberTicketList.Count.ToString());
                                    }

                                    for (int i = 0; i < numbersPurchased; i++)
                                    {
                                        // Check if not entered 
                                        if (!clubDrawTicketList.Contains(memberTicketList[i]))
                                        {
                                            // Add To Club Draw
                                            ClubDraw clubDraw = new ClubDraw(); 
                                            clubDraw.ClubTicketId = memberTicketList[i];
                                            clubDraw.DatePaid = payment.ChargeDate;
                                            clubDraw.AmountPencePaidIn = pricePerNumber;
                                            clubDraw.Prize = 0;
                                            clubDraw.DatePaidOut = DateTime.MinValue;
                                            clubDraw.ClubDrawDateId = clubDrawDate.ClubDrawDateId;

                                            Console.WriteLine("Add To Club Draw");
                                            Console.WriteLine("ClubTicketId: " + clubDraw.ClubTicketId.ToString());
                                            Console.WriteLine("DatePaid: " + clubDraw.DatePaid.ToString("dd/MM/yyyy"));
                                            Console.WriteLine("DatePaidOut: " + clubDraw.DatePaidOut.ToString("dd/MM/yyyy"));
                                            Console.WriteLine("ClubDrawDateId: " + clubDraw.ClubDrawDateId.ToString());

                                            // Update Database
                                            rowsUpdated = await dBFunctions.UpdateClubDraw(clubDraw);
                                            if (rowsUpdated == 0)
                                            {
                                                errorList.AppendLine("ClubDraw Not Updated - ClubTicketId: " + clubDraw.ClubTicketId.ToString());
                                            }

                                            Thread.Sleep(1000);

                                            // Add to ClubDrawTicketList
                                            clubDrawTicketList.Add(memberTicketList[i]);
                                        }
                                    }
                                }
                                else
                                {
                                    // Cannot find member 
                                    // Email issue to FFC Admin
                                    errorList.AppendLine("Customer cannot be found: " + customer.Email);
                                }
                            }
                        }
                    }

                }

            }
        }
    }
    else
    {
        // Stop Process!
        // Email FFC Admin
        errorList.AppendLine("Price Per Number is £0");
        Console.WriteLine("*** Price Per Number is £0 ***");
    }
}
else
{

    // ** STOP HERE **
    // Email FFC Admin
    errorList.AppendLine("ClubDrawDate Not Found");
    Console.WriteLine("*** ClubDrawDate Not Found ***");
}

// Send Email to FFC Admin to say Process has run and any errors
// errorList

if (customerNotPaidOutList.Count > 0)
{
    // Send email to FFC Admin and Club Admins 

}    
