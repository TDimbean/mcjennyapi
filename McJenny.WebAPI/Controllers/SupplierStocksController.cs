using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using McJenny.WebAPI.Data.Models;
using Microsoft.Extensions.Logging;

namespace McJenny.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierStocksController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;
        private readonly ILogger<SupplierStocksController> _logger;

        public SupplierStocksController(FoodChainsDbContext context,
            ILogger<SupplierStocksController> logger)
        {
            _context = context;
            _logger = logger;
            _logger.LogDebug("Logger injected into Supplier Stocks Controller.");
        }

        #region Gets

        /// <summary>
        /// Get Supplier Stocks
        /// </summary>
        /// <remarks>
        /// Returns a collection of strings explaining the Supplier Stock
        /// relationships between Suppliers and the Supply Categories they 
        /// keep. Each string includes the Supplier Stock's Id; the Supplier's
        /// Id, Name , Abreviated Country, Abreviated State and City; as well as
        /// the Supply Category's Id and Name.
        /// </remarks>
        // GET: api/SupplierStocks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> GetSupplierStocks()
        {
            _logger.LogDebug("Supplier Stocks Controller fetching all Supplier Stocks.");
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

            _logger.LogDebug("Supplier Stocks Controller returning all Supplier Stocks.");
            return result;
        }

        /// <summary>
        /// Get Supplier Stock
        /// </summary>
        /// <param name="id">Id of desired Supplier Stock</param>
        /// <remarks>
        /// Returns a string explaining the Supplier Stock relationship
        /// upheld by the Supplier Stock whose Id matches the given 'id'
        /// parameter, formated to include the Supplier's Id, Name,
        /// Abreviated Country, Abreviated State and city, as well as the
        /// Supply Category's Id and Name.
        /// If no Supplier Stock whose Id matches the given 'id'
        /// parameter exists, Not Found is returned instead.
        /// </remarks>
        // GET: api/SupplierStocks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> GetSupplierStock(int id)
        {
            _logger.LogDebug("Supplier Stocks Controller fetching Supplier Stock " +
                "with ID: " + id);
            var stock = await _context.SupplierStocks
                .Select(s => new { s.SupplierStockId, s.SupplierId, s.SupplyCategoryId })
                .FirstOrDefaultAsync(s => s.SupplierStockId == id);

            if (stock == null)
            {
                _logger.LogDebug("Supplier Stocks Controller found no Supplier Stock" +
                    " with ID: " + id);
                return NotFound();
            }

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

            _logger.LogDebug("Supplier Stocks Controller returning formated " +
                "Supplier Stock with ID: " + id);
            return string.Format("({0}) {1}, {2}, {3}{4} stocks ({5}) {6}",
                    sup.SupplierId,
                    sup.Name, sup.AbreviatedCountry,
                    sup.AbreviatedState == "N/A" ? string.Empty :
                    sup.AbreviatedState + ", ",
                    sup.City,
                    stock.SupplyCategoryId,
                    cat);
        }

        /// <summary>
        /// Get Supplier Stock Basic
        /// </summary>
        /// <param name="id">Id of desired Supplier Stock</param>
        /// <remarks>
        /// Returns a Supplier Stock object from the Supplier Stock 
        /// whose Id matches the given 'id' parameter. 
        /// The Supplier Stock includes its own Id, the Id of the Supplier,
        /// that of the Supply Category, as well as empty Supplier and 
        /// Supply Category objects.
        /// If no Supplier Stock whose Id matches the given 'id' parameter
        /// exists, Not Found is returned instead.
        /// </remarks>
        // GET: api/SupplierStocks/5/basic
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<SupplierStock>> GetSupplierStockBasic(int id)
        {
            _logger.LogDebug("Supplier Stocks Controller fetching Supplier " +
                "Stock with ID: " + id);
            var stock = await _context.SupplierStocks.FindAsync(id);
            if (stock == null)
            {
                _logger.LogDebug("Supplier Stocks found no Supplier Stock with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Supplier Stocks returning basic Supplier Stock " +
                "with ID: " + id);
            return stock;
        }

        #endregion

        /// <summary>
        /// Create Supplier Stock
        /// </summary>
        /// <param name="supplierStock">Supplier Stock object to be added 
        /// to the Database</param>
        /// <remarks>
        /// Creates a new Supplier Stock and inserts it into the Database
        /// using the values from the provided 'supplierStock' parameter.
        /// Only the Supplier Id and Supply Category Id of the Suppleir Stock
        /// may be set.
        /// Its Suppleir and Supply Category will be automatically set based on the
        /// Supplier Id and Supply Category Id.
        /// The Supplier Stock Id may not be set. The Databse will automatically
        /// assign it a new Id, equivalent to the last Supplier Stock Id 
        /// in the Database + 1.
        /// The given parameter of type Supplier Stock must contain a 
        /// Supplier Id and Supply Category Id, belonging to a valid Suplier
        /// and Supply Category.
        /// If the 'supplierStock' parameter is incorrect, Bad Request is returned.
        /// Supplier Stock parameter errors:
        /// (1) The Supplier Stock Id is not 0, 
        /// (2) The Supplier Id does not match any Supplier's Ids,
        /// (3) The Supply Category Id does not match any Supply Category's Ids,
        /// (4) Another Supplier Stock with the same Supplier Id and Supply Category Id values
        /// already exists,
        /// (5) Supplier or Supply Category are not null.
        /// </remarks>
        // POST: api/SupplierStocks
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<SupplierStock>> CreateSupplierStock(SupplierStock supplierStock)
        {
            _logger.LogDebug("Supplier Stocks Controller trying to create " +
                "new Supplier Stock.");

            //Validation

            if (supplierStock.SupplierStockId != 0 ||
                supplierStock.Supplier != null || supplierStock.SupplyCategory != null ||
                await _context.Suppliers.FindAsync(supplierStock.SupplierId) == null ||
                await _context.SupplyCategories.FindAsync(supplierStock.SupplyCategoryId) == null ||
                await _context.SupplierStocks.AnyAsync(s =>
                    s.SupplyCategoryId == supplierStock.SupplyCategoryId &&
                    s.SupplierId == supplierStock.SupplierId))
            {
                _logger.LogDebug("Supplier Stocks Controller found provided " +
                    "'supplierStock' parameter to violate creation constraints.");
                return BadRequest();
            }

            //Validation

            _context.SupplierStocks.Add(supplierStock);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Supplier Stocks Controller successfully created new " +
                "Supplier Stock at ID: " + supplierStock.SupplierStockId);
            return CreatedAtAction("GetSupplierStockBasic", new { id = supplierStock.SupplierStockId }, supplierStock);
        }

        /// <summary>
        /// Delete Supplier Stock
        /// </summary>
        /// <param name="id">Id of target Supplier Stock</param>
        /// <remarks>
        /// Removes a Supplier Stock from the Database.
        /// If there is no Supplier Stock, whose Id matches the given 'id' parameter, 
        /// Not Found is returned.
        /// If the desired Supplier is the last in the Database, it is removed from the Database
        /// and its Supplier Stock Id will be used by the next Supplier Stock to be inserted.
        /// If the desired Supplier Stock is not the last in the Database, it is swapped with the last
        /// Supplier Stock in the Database before it is removed, taking on the last Supplier Stock's
        /// Supplier Id and Supply Category Id before removing it and freeing up the last 
        /// Supplier Stock's Id for the next Supplier Stock to be inserted.
        /// </remarks>
        // DELETE: api/SupplierStocks/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<SupplierStock>> DeleteSupplierStock(int id)
        {
            _logger.LogDebug("Supplier Stocks Controller trying to delete " +
                "Supplier Stock with ID: " + id);

            var supplierStock = await _context.SupplierStocks.FindAsync(id);
            if (supplierStock == null)
            {
                _logger.LogDebug("Supplier Stocks Controller found no Supplier Stock " +
                    "with ID: " + id);
                return NotFound();
            }

            var lastStockId = await _context.SupplierStocks.CountAsync();

            if (id == lastStockId)
            {
                _logger.LogDebug("Supplier Stocks Controller found Supplier Stock " +
                    "with ID: " + id + " to be the last in the Database and will delete it.");

                _context.SupplierStocks.Remove(supplierStock);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[SupplierStocks]', RESEED, " + (lastStockId - 1) + ")");
                await _context.SaveChangesAsync();

                _logger.LogDebug("Supplier Stock Controller successfully deleted Supplier " +
                    "Stock with ID: " + id);
                return Ok();
            }

            _logger.LogDebug("Supplier Stocks Controller found Supplier Stock with ID: " +
                id + " to not be the last in the Database. As such, it will swap places with the " +
                "last Supplier Stock before deleting that one.");

            var lastStock = await _context.SupplierStocks.FindAsync(lastStockId);
            supplierStock.SupplierId = lastStock.SupplierId;
            supplierStock.SupplyCategoryId = lastStock.SupplyCategoryId;

            _context.SupplierStocks.Remove(lastStock);
            await _context.SaveChangesAsync();
            await _context.Database.ExecuteSqlRawAsync
                ("DBCC CHECKIDENT('[SupplierStocks]', RESEED, " + (lastStockId - 1) + ")");
            await _context.SaveChangesAsync();

            _logger.LogDebug("Supplier Stock successfully turned Supplier Stock with ID: " +
                id + " into last Supplier Stock, then deleted that one.");
            return Ok();
        }

        [NonAction]
        private async Task<bool> SupplierStockExists(int id)
            => await _context.SupplierStocks.AnyAsync(e => e.SupplierStockId == id);
    }
}
