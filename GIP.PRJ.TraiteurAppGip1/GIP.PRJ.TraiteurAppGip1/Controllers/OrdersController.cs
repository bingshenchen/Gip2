using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GIP.PRJ.TraiteurAppGip1.Models;
using NuGet.Common;
using Microsoft.Identity.Client;
using System.Net.Mail;
using System.Net;

namespace GIP.PRJ.TraiteurAppGip1.Controllers
{
    public class OrdersController : Controller
    {
        private readonly TraiteurAppDbContext _context;

        public OrdersController(TraiteurAppDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var traiteurAppDbContext = _context.Orders.Include(o => o.Customer);
            return View(await traiteurAppDbContext.ToListAsync());
        }

        public async Task<IActionResult> CustomerIndex(int id)
        {
            var traiteurAppDbContext = _context.Orders.Include(o => o.Customer).Where(o => o.CustomerId == id);
            /// show the customer name in the view by using a ViewBag 
            /// 
            Customer customer = _context.Customers.FirstOrDefault(c => c.Id == id);
            ViewBag.CustomerName = customer?.Name ?? string.Empty;
            ViewBag.CustomerId = customer?.Id ?? 0;

            return View(await traiteurAppDbContext.ToListAsync());
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Orders == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            ViewBag.CanBeChanged = CheckTimeSlot(order.TimeSlot, 1) && (order.OrderedOn > DateTime.Today);
            return View(order);
        }

        // GET: Orders/Create
        public IActionResult Create(int? id)
        {
            Order order = new Order{ OrderedOn = DateTime.Now };
            if (id != null)
            {
                order.CustomerId = id.Value;
                ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name", order.CustomerId);
            }
            else
            {
                ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name");
            }
            
            ViewData["TimeSlots"] = new SelectList(GetTimeSlotDictionary(), "Key", "Value");

            /// set the OrderedOn date = DateTime.Now
            return View(order);
        }

        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderedOn,TimeSlot,CustomerId")] Order order)
        {
            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction("Create", "OrderDetails", new { id = order.Id });
            }

            ViewData["TimeSlots"] = new SelectList(GetTimeSlotDictionary(), "Key", "Value");
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name", order.CustomerId);
            return View(order);
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Orders == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            if (!CheckTimeSlot(order.TimeSlot, 1) || (order.OrderedOn < DateTime.Today))
            {
                return RedirectToAction(nameof(Details), order);
            }

            ViewData["TimeSlots"] = new SelectList(GetTimeSlotDictionary(), "Key", "Value");
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name", order.CustomerId);

            return View(order);
        }

        /// <summary>
        /// Checks a timeslot to the requested margin (in hours). True if still ok, false otherwise
        /// </summary>
        /// <param name="reservedTime">timeslot in string format HH:mm</param>
        /// <param name="margin">requested margin (time left) in hours</param>
        /// <returns></returns>
        private bool CheckTimeSlot(string reservedTime, int margin)
        {
            // get hour en minutes to create TimeOnly object
            int reservedHour = 0;
            int.TryParse(reservedTime.Substring(0, 2), out reservedHour);
            int reservedMinutes = 0;
            int.TryParse(reservedTime.Substring(3, 2), out reservedMinutes);

            // compare 
            return !(new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute).AddHours(margin) > new TimeOnly(reservedHour, reservedMinutes));
        }

        private Dictionary<string, string> GetTimeSlotDictionary()
        {
            /// get list of today's orders, group by the selected timeslots and select only those 
            /// timeslots that are already selected 2 times
            /// Key property after GroupBy = property that represents the grouped by value.
            var reservedSlots = _context.Orders.Where(o => o.OrderedOn >= DateTime.Today)
                .GroupBy(o => o.TimeSlot).Select(o => new { o.Key, count = o.Count() })
                .Where(o => o.count >= 2).Select(o => o.Key);

            /// Timeslots only possible from 11 o'clock
            int startHour = DateTime.Now.Hour + 1;
            if (startHour < 11)
            {
                startHour = 11;
            }

            /// create and add timeslots until 23 o'clock (last possible slot = 22:45)
            Dictionary<string, string> timeslots = new Dictionary<string, string>();

            for (int i = startHour; i < 23; i++)
            {
                for (int j = 0; j <= 45; j += 15)
                {
                    string timeslot = i.ToString() + ":" + j.ToString("0#");
                    if (!reservedSlots.Contains(timeslot))
                    {
                        timeslots.Add(timeslot, timeslot);
                    }
                }
            }
            return timeslots;
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,OrderedOn,TimeSlot,CustomerId")] Order order)
        {
            if (id != order.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["TimeSlots"] = new SelectList(GetTimeSlotDictionary(), "Key", "Value");
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name", order.CustomerId);
            return View(order);
        }

        public async Task<IActionResult> CheckOut(int? id)
        {
            if (id == null || _context.Orders == null)
            {
                return NotFound();
            }

            /// include Customer => to check customer rating (see below)
            var order = await _context.Orders.Include(o => o.Customer).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            // calculate total
            decimal total = _context.OrderDetails.Where(od => od.OrderId == id).Sum(od => od.Price * od.Quantity);
            order.Total = total;

            // get customer rating to set reduction (if A => 10% reduction)
            if (order.Customer.Rating == CustomerRating.A)
            {
                order.Reduction = 10;
            }

            ViewData["TimeSlots"] = new SelectList(GetTimeSlotDictionary(), "Key", "Value");

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int id, [Bind("Id,OrderedOn,TimeSlot,CustomerId,Total,Reduction")] Order order)
        {
            if (id != order.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);

                    /// set customer rating
                    /// 
                    int customerCount = _context.Orders.Count(o => o.CustomerId == order.CustomerId && o.IsPaid == true);
                    if (customerCount >= 3)
                    {
                        Customer customer = _context.Customers.Find(order.CustomerId);
                        customer.Rating = CustomerRating.A;
                    }

                    /// set order paid
                    order.IsPaid = true;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Invoice), new { id = order.Id });
            }

            ViewData["TimeSlots"] = new SelectList(GetTimeSlotDictionary(), "Key", "Value");
            ViewData["CustomerId"] = new SelectList(_context.Customers, "Id", "Name", order.CustomerId);
            return View(order);
        }

        public async Task<IActionResult> Invoice(int? id)
        {
            if (id == null || _context.Orders == null)
            {
                return NotFound();
            }


            var order = await _context.Orders.Include(o => o.Customer).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            ViewBag.TimeSlot = order.TimeSlot;
            ViewBag.OrderId = order.Id;
            ViewBag.CustomerInfo = order.Customer.Name + (string.IsNullOrEmpty(order.Customer.EmailAddress) ? string.Empty : " (" + order.Customer.EmailAddress + ")");
            ViewBag.PaymentInfo = (order.Total - (order.Total * order.Reduction / 100)) + (order.Reduction > 0 ? "(Korting: " + order.Reduction + "%)" : string.Empty);

            // include menuitem for name of item
            var orderDetails = await _context.OrderDetails.Where(od => od.OrderId == id).Include(o => o.MenuItem).ToListAsync();

            return View(orderDetails);
        }

        public async Task<IActionResult> SendMail(int? id)
        {
            if (id == null || _context.Orders == null)
            {
                return NotFound();
            }


            var order = await _context.Orders.Include(o => o.Customer).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            ViewBag.TimeSlot = order.TimeSlot;
            ViewBag.OrderId = order.Id;
            string customerInfo = order.Customer.Name + (string.IsNullOrEmpty(order.Customer.EmailAddress) ? string.Empty : " (" + order.Customer.EmailAddress + ")");
            string paymentInfo = (order.Total - (order.Total * order.Reduction / 100)) + (order.Reduction > 0 ? "(Korting: " + order.Reduction + "%)" : string.Empty);

            // include menuitem for name of item
            var orderDetails = await _context.OrderDetails.Where(od => od.OrderId == id).Include(o => o.MenuItem).ToListAsync();

            string detailInfo = string.Empty;
            foreach (var orderDetail in orderDetails)
            {
                detailInfo += "<tr>" +
                                    "<td>" + orderDetail.MenuItem.Name + "</td>" +
                                    "<td>" + orderDetail.Quantity + "</td>" +
                                    "<td>" + orderDetail.Price + "</td>" +
                                "</tr>";
            }

            string mailContent =
                "<html><body><h2>Factuur</h2>" +
                "<div><h4>Bestelling - " + order.Id + "</h4>" +
                "<hr>" +
                "<dl>" +
                "<dt>Timeslot</dt>" +
                "<dd>" + order.TimeSlot + "</dd>" +
                "<dt>Klant naam</dt>" +
                "<dd>" + customerInfo + "</dd>" +
                "<dt>Totaal</dt>" +
                "<dd>" + paymentInfo + "</dd>" +
                "</dl>" +
                "<table border=0>" +
                "<thead><tr> <th>MenuItem</th><th>Quantity</th><th>Price</th><th></th></tr></thead>" +
                "<tbody>" + detailInfo +
                "</tbody>" +
                "</table>" +
                "</div>" +
                "</body></html>";

            /// sending mail using the build in smtp client 
            /// Replace correct values for smtpUrl, from@domain and password
            /// Example:
            /// smtpUrl: relay.proximus.be
            /// from@domain: gip.teamx@proximus.be
            /// password: UcllGip2023,,
            SmtpClient smtpClient = new SmtpClient("smtpUrl", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("from@domain", "password")
            };
            MailMessage mailMessage = new MailMessage("from@domain", 
                order.Customer.EmailAddress, "Lekkerbek - Factuur (bestelling " + order.Id + ")", mailContent);
            mailMessage.IsBodyHtml = true;
            try
            {
                smtpClient.Send(mailMessage);
                ViewBag.InfoMessage = "De mail werd correct verstuurd";
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Fout tijdens versturen mail";
            }

            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Orders == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!CheckTimeSlot(order.TimeSlot, 2) || (order.OrderedOn < DateTime.Today))
            {
                return View("NoDelete", order);
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Orders == null)
            {
                return Problem("Entity set 'TraiteurAppDbContext.Orders'  is null.");
            }
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        private bool OrderExists(int id)
        {
            return (_context.Orders?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
