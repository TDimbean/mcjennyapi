using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using McJenny.WebAPI.Data.Models;

namespace McJenny.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierStocksController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;

        public SupplierStocksController(FoodChainsDbContext context) => _context = context;

        #region Gets

        #region Basic

        // GET: api/SupplierStocks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> GetSupplierStocks()
        {
            var stocks = await _context.SupplierStocks
                .Select(s=>new {s.SupplierStockId, s.SupplierId, s.SupplyCategoryId })
                .ToArrayAsync();

            var catIds = stocks.Select(s => s.SupplyCategoryId).Distinct().ToArray();
            var supIds = stocks.Select(s => s.SupplierId).Distinct().ToArray();

            var cats = await _context.SupplyCategories
                .Where(c => catIds.Contains(c.SupplyCategoryId))
                .Select(c => new { c.SupplyCategoryId, c.Name })
                .ToArrayAsync();

            var sups = await _context.Suppliers
                .Where(s => supIds.Contains(s.SupplierId))
                .Select(s => new { s.SupplierId, s.AbreviatedCountry,
                    s.AbreviatedState, s.City, s.Name })
                .ToArrayAsync();

            var result = new string[stocks.Length];
            for (int i = 0; i < stocks.Length; i++)
            {
                var cat = cats.SingleOrDefault(c => c.SupplyCategoryId == stocks[i].SupplyCategoryId).Name;
                var sup = sups.SingleOrDefault(s => s.SupplierId == stocks[i].SupplierId);

                result[i] = string.Format("Stock [{0}]: ({1}) {2}, {3}, {4}{5} stocks ({6}) {7}",
                    stocks[i].SupplierStockId,
                    stocks[i].SupplierId,
                    sup.Name, sup.AbreviatedCountry,
                    sup.AbreviatedState == "N/A" ? string.Empty :
                    sup.AbreviatedState + ", ",
                    sup.City,
                    stocks[i].SupplyCategoryId,
                    cat);
            }

            return result;
        }

        // GET: api/SupplierStocks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> GetSupplierStock(int id)
        {
            var stock = await _context.SupplierStocks
                .Select(s => new { s.SupplierStockId, s.SupplierId, s.SupplyCategoryId })
                .FirstOrDefaultAsync(s => s.SupplierStockId == id);

            if (stock == null) return NotFound();

            var cat = (await _context.SupplyCategories.FindAsync(stock.SupplyCategoryId)).Name;
            var sup = await _context.Suppliers
                .Select(s => new
                {
                    s.SupplierId,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.City,
                    s.Name
                })
                .FirstOrDefaultAsync(s => s.SupplierId == stock.SupplierId);

            if (cat == null || sup == null) return BadRequest();

            return string.Format("({0}) {1}, {2}, {3}{4} stocks ({5}) {6}",
                    sup.SupplierId,
                    sup.Name, sup.AbreviatedCountry,
                    sup.AbreviatedState == "N/A" ? string.Empty :
                    sup.AbreviatedState + ", ",
                    sup.City,
                    stock.SupplyCategoryId,
                    cat);
        }

        // GET: api/SupplierStocks/5/basic
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<SupplierStock>> GetSupplierStockBasic(int id)
        {
            var stock = await _context.SupplierStocks.FindAsync(id);
            if (stock == null) return NotFound();

            return stock;
        }

        #endregion

        #endregion

        // PUT: api/SupplierStocks/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupplierStock(int id, SupplierStock supplierStock)
        {
            if (id != supplierStock.SupplierStockId)
            {
                return BadRequest();
            }

            _context.Entry(supplierStock).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await SupplierStockExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/SupplierStocks
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<SupplierStock>> CreateSupplierStock(SupplierStock supplierStock)
        {
            //Validation

            if (supplierStock.SupplierStockId != 0 ||
                supplierStock.Supplier != null || supplierStock.SupplyCategory != null ||
                await _context.Suppliers.FindAsync(supplierStock.SupplierId) == null ||
                await _context.SupplyCategories.FindAsync(supplierStock.SupplyCategoryId) == null ||
                await _context.SupplierStocks.AnyAsync(s =>
                    s.SupplyCategoryId == supplierStock.SupplyCategoryId &&
                    s.SupplierId == supplierStock.SupplierId))
                return BadRequest();

            //Validation

            _context.SupplierStocks.Add(supplierStock);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSupplierStockBasic", new { id = supplierStock.SupplierStockId }, supplierStock);
        }

        // DELETE: api/SupplierStocks/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<SupplierStock>> DeleteSupplierStock(int id)
        {
            var supplierStock = await _context.SupplierStocks.FindAsync(id);
            if (supplierStock == null) return NotFound();

            var lastStockId = await _context.SupplierStocks.CountAsync();

            if (id == lastStockId)
            {
                _context.SupplierStocks.Remove(supplierStock);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[SupplierStocks]', RESEED, " + (lastStockId - 1) + ")");
                await _context.SaveChangesAsync();
                return Ok();
            }

            var lastStock = await _context.SupplierStocks.FindAsync(lastStockId);
            supplierStock.SupplierId = lastStock.SupplierId;
            supplierStock.SupplyCategoryId = lastStock.SupplyCategoryId;

            _context.SupplierStocks.Remove(lastStock);
            await _context.SaveChangesAsync();
            await _context.Database.ExecuteSqlRawAsync
                ("DBCC CHECKIDENT('[SupplierStocks]', RESEED, " + (lastStockId - 1) + ")");
            await _context.SaveChangesAsync();
            return Ok();
        }

        private async Task<bool> SupplierStockExists(int id)
            => await _context.SupplierStocks.AnyAsync(e => e.SupplierStockId == id);
    }
}
