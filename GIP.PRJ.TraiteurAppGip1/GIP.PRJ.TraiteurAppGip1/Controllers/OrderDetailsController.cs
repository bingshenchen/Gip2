using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GIP.PRJ.TraiteurAppGip1.Models;

namespace GIP.PRJ.TraiteurAppGip1.Controllers
{
    public class OrderDetailsController : Controller
    {
        private readonly TraiteurAppDbContext _context;

        public OrderDetailsController(TraiteurAppDbContext context)
        {
            _context = context;
        }

        // GET: OrderDetails
        public async Task<IActionResult> Index(int id)
        {
            /// show details of orderid (= id)
            /// 
            ViewBag.OrderId = id;
            Order order = _context.Orders.Find(id);
            if (order != null)
            {
                if (order.IsPaid)
                {
                    return RedirectToAction("Invoice", "Orders", new { id = id });
                }
            }
            var traiteurAppDbContext = _context.OrderDetails.Where(od => od.OrderId == id).Include(o => o.MenuItem);
            return View(await traiteurAppDbContext.ToListAsync());
        }

        // GET: OrderDetails/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.OrderDetails == null)
            {
                return NotFound();
            }

            var orderDetail = await _context.OrderDetails
                .Include(o => o.MenuItem)
                .Include(o => o.Order)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (orderDetail == null)
            {
                return NotFound();
            }

            return View(orderDetail);
        }

        // GET: OrderDetails/Create
        /// <summary>
        /// Creates a new orderdetail for the specified order (= id parameter)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IActionResult Create(int id)
        {
            ViewBag.Message = TempData["Message"];
            ViewData["MenuItemId"] = new SelectList(_context.MenuItems, "Id", "Name");
            ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id");

            /// set OrderId (to link OrderDetail to the Order) + set default value for Quantity to 1
            return View(new OrderDetail { OrderId = id, Quantity = 1});
        }

        // POST: OrderDetails/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OrderId,MenuItemId,Quantity,Price")] OrderDetail orderDetail)
        {
            if (ModelState.IsValid)
            {
                /// get item price
                /// 
                MenuItem menuItem = _context.MenuItems.FirstOrDefault(mi => mi.Id == orderDetail.MenuItemId);
                orderDetail.Price = menuItem?.Price ?? 0;

                _context.Add(orderDetail);
                await _context.SaveChangesAsync();
                TempData["Message"] = orderDetail.Quantity + " x " + menuItem?.Name + " werd aan de bestelling toegevoegd.";
                return RedirectToAction("Create", "OrderDetails", new { id = orderDetail.OrderId});
            }

            ViewData["MenuItemId"] = new SelectList(_context.MenuItems, "Id", "Name", orderDetail.MenuItemId);
            ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderDetail.OrderId);
            return View(orderDetail);
        }

        // GET: OrderDetails/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.OrderDetails == null)
            {
                return NotFound();
            }

            var orderDetail = await _context.OrderDetails.FindAsync(id);
            if (orderDetail == null)
            {
                return NotFound();
            }
            ViewData["MenuItemId"] = new SelectList(_context.MenuItems, "Id", "Id", orderDetail.MenuItemId);
            ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderDetail.OrderId);
            return View(orderDetail);
        }

        // POST: OrderDetails/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,OrderId,MenuItemId,Quantity,Price")] OrderDetail orderDetail)
        {
            if (id != orderDetail.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(orderDetail);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderDetailExists(orderDetail.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index), new { id = orderDetail.OrderId});
            }
            ViewData["MenuItemId"] = new SelectList(_context.MenuItems, "Id", "Id", orderDetail.MenuItemId);
            ViewData["OrderId"] = new SelectList(_context.Orders, "Id", "Id", orderDetail.OrderId);
            return View(orderDetail);
        }

        // GET: OrderDetails/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.OrderDetails == null)
            {
                return NotFound();
            }

            var orderDetail = await _context.OrderDetails
                .Include(o => o.MenuItem)
                .Include(o => o.Order)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (orderDetail == null)
            {
                return NotFound();
            }

            return View(orderDetail);
        }

        // POST: OrderDetails/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.OrderDetails == null)
            {
                return Problem("Entity set 'TraiteurAppDbContext.OrderDetails'  is null.");
            }
            var orderDetail = await _context.OrderDetails.FindAsync(id);
            if (orderDetail != null)
            {
                _context.OrderDetails.Remove(orderDetail);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { id = orderDetail.OrderId});
        }

        private bool OrderDetailExists(int id)
        {
          return (_context.OrderDetails?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
