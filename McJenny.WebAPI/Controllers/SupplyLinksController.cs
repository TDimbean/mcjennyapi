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
    public class SupplyLinksController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;
        private readonly ILogger<SupplyLinksController> _logger;

        public SupplyLinksController(FoodChainsDbContext context,
            ILogger<SupplyLinksController> logger)
        {
            _context = context;
            _logger = logger;
            _logger.LogDebug("Logger injected into Supply Categories Controller.");
        }

        #region Gets

        /// <summary>
        /// Get Supply Links
        /// </summary>
        /// <remarks>
        /// Returns a collection of strings explaining all Supply Link relationships
        /// between the Suppliers, Locations and the Supply Categories the one provides
        /// to the other. Each string contains the Supply Link's Id; the Location's
        /// Id, Abreviated Country, Abreviated State, City and Street Address; the Supplier's
        /// Id, Name, Abreviated Country, Abreviated State and City and the Supply Category's
        /// Id and Name.
        /// </remarks>
        // GET: api/SupplyLinks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> GetSupplyLinks()
        {
            _logger.LogDebug("Supply Links Controller fetching all Supply Links.");
            var links = await _context.SupplyLinks.
                Select(l=>new {l.SupplyLinkId, l.LocationId, l.SupplierId, l.SupplyCategoryId })
                .ToArrayAsync();

            var locIds = links.Select(l => l.LocationId).ToArray();
            var supIds = links.Select(l => l.SupplierId).ToArray();
            var catIds = links.Select(l => l.SupplyCategoryId).ToArray();

            var locs = await _context.Locations
                .Where(l => locIds.Contains(l.LocationId))
                .Select(l => new
                {
                    l.LocationId,
                    l.AbreviatedCountry,
                    l.AbreviatedState,
                    l.City,
                    l.Street
                })
                .ToArrayAsync();

            var sups = await _context.Suppliers
                .Where(s => supIds.Contains(s.SupplierId))
                .Select(s => new
                {
                    s.SupplierId,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.City,
                    s.Name
                })
                .ToArrayAsync();

            var cats = await _context.SupplyCategories
                .Where(c => catIds.Contains(c.SupplyCategoryId))
                .Select(c => new { c.SupplyCategoryId, c.Name })
                .ToArrayAsync();

            var result = new string[links.Length];
            for (int i = 0; i < links.Length; i++)
            {
                var loc = locs.SingleOrDefault(l => l.LocationId == links[i].LocationId);
                var sup = sups.SingleOrDefault(s => s.SupplierId == links[i].SupplierId);
                var cat = cats.SingleOrDefault(c => c.SupplyCategoryId == links[i].SupplyCategoryId).Name;

                result[i] = string.Format("Link [{0}]: ({1}) {2}, {3}{4}, {5} supplies" +
                    "({6}) {7}, {8}{9}, {10} with ({11}) {12}",
                    links[i].SupplyLinkId,
                    links[i].SupplierId,
                    sup.AbreviatedCountry,
                    sup.AbreviatedState == "N/A" ? string.Empty :
                    sup.AbreviatedState + ", ",
                    sup.City,
                    sup.Name,
                    links[i].LocationId,
                    loc.AbreviatedCountry,
                    loc.AbreviatedState == "N/A" ? string.Empty :
                    loc.AbreviatedState + ", ",
                    loc.City,
                    loc.Street,
                    links[i].SupplyCategoryId, cat);
            }

            _logger.LogDebug("Supply Links Controller returning all Supply Links.");
            return result;
        }

        /// <summary>
        /// Get Supply Link
        /// </summary>
        /// <param name="id">Id of desired Supply Link</param>
        /// <remarks>
        /// Returns a string explaining the Supply Link relationship
        /// whose Id matches the given 'id' parameter.
        /// The string will contain the Supplier's Id, Name, Abreviated Country, Abreviated
        /// State and City; the Location's Id, Abreviated Country, Abreviate State, City and 
        /// Street Address and the Supply Category's Id and Name.
        /// If no Supply Link whose Id matches the given 'id' parameter exists, 
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/SupplyLinks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> GetSupplyLink(int id)
        {
            _logger.LogDebug("Supply Links Controller fetching Supply Link " +
                "with ID: " + id);

            var link = await _context.SupplyLinks
                .Select(l => new { l.SupplyLinkId, l.LocationId, l.SupplierId, l.SupplyCategoryId })
                .FirstOrDefaultAsync(l => l.SupplyLinkId == id);

            if (link == null)
            {
                _logger.LogDebug("Supply Links Controller found no Supply Link " +
                    "with ID: " + id);
                return NotFound();
            }

            var loc = await _context.Locations
                .Select(l => new
                {
                    l.LocationId,
                    l.AbreviatedCountry,
                    l.AbreviatedState,
                    l.City,
                    l.Street
                })
                .SingleOrDefaultAsync(l => l.LocationId == link.LocationId);

            var sup = await _context.Suppliers
                .Select(s => new
                {
                    s.SupplierId,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.City,
                    s.Name
                })
                .SingleOrDefaultAsync(s => s.SupplierId == link.SupplierId);

            var cat = (await _context.SupplyCategories
                .FindAsync(link.SupplyCategoryId)).Name;

            _logger.LogDebug("Supply Links Controller returning formted Supply " +
                "Link with ID: " + id);
            return string.Format("({0}) {1}, {2}{3}, {4} supplies" +
                    "({5}) {6}, {7}{8}, {9} with ({10}) {11}",
                    sup.SupplierId,
                    sup.AbreviatedCountry,
                    sup.AbreviatedState == "N/A" ? string.Empty :
                    sup.AbreviatedState + ", ",
                    sup.City,
                    sup.Name,
                    loc.LocationId,
                    loc.AbreviatedCountry,
                    loc.AbreviatedState == "N/A" ? string.Empty :
                    loc.AbreviatedState + ", ",
                    loc.City,
                    loc.Street,
                    link.SupplyCategoryId, cat);
        }

        /// <summary>
        /// Get Supply Link Basic
        /// </summary>
        /// <param name="id">Id of desired Supply Link</param>
        /// <remarks>
        /// Returns a Supply Link object whose Id matches the given
        /// 'id' parameter. It will contain the Supply Link Id, Supplier Id,
        /// Supply Category Id and Location Id, as well as null Supplier,
        /// Supply Category and Location objects.
        /// If no Supply Link whose Id matches the given 'id' parameter exists,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/SupplyLinks/5/basic
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<SupplyLink>> GetSupplyLinkBasic(int id)
        {
            _logger.LogDebug("Supply Links Controller fetching Supply Link " +
                " with ID: " + id);
            var supplyLink = await _context.SupplyLinks.FindAsync(id);

            if (supplyLink == null)
            {
                _logger.LogDebug("Supply Links Controller found no Supply " +
                    "Link with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Supply Links Controller returning basic Supply " +
                "Link with ID: " + id);
            return supplyLink;
        }

        #endregion

        /// <summary>
        /// Create Supply Link
        /// </summary>
        /// <param name="link">Supply Link object to be 
        /// added to the Database</param>
        /// <remarks>
        /// Creates a new Supply Link and inserts it into the Database
        /// using the values from the provided 'link' parameter.
        /// Only the Supplier Id, Location Id and Supply Category Id of the
        /// Supply Link may be set.
        /// Its Supplier, Location and Supply Category will be automatically 
        /// set based on the Supplier Id, Location Id and Supply Category Id.
        /// The Supply Link Id may not be set. The Databse will automatically
        /// assign it a new Id, equivalent to the last Supply Link Id in the 
        /// Database + 1.
        /// The given parameter of type Supply must contain a Supplier Id,
        /// Location Id and Supply Category Id, belonging to a valid Supplier,
        /// Location and Supply Category. Additionally it may contain empty
        /// objects for the Supplier, Location and Supply Category, as well as
        /// a Supply Link Id of 0.
        /// If the 'link' parameter is incorrect, Bad Request is returned.
        /// Supply Link parameter errors:
        /// (1) The Supply Link Id is not 0, 
        /// (2) The Supplier Id does not match any existing Supplier,
        /// (3) The Location Id does not match any existing Location,
        /// (4) The Supply Category Id does not match any existing 
        /// Supply Category,
        /// (5) Another Supply Link with the same Supplier Id, Location Id
        /// and Supply Category Id values already exists,
        /// (5) Supplier, Location or Supply Category is not null.
        /// </remarks>
        // POST: api/SupplyLinks
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<SupplyLink>> CreateSupplyLink(SupplyLink link)
        {
            _logger.LogDebug("Supply Links Controller trying to create new " +
                "Supply Link.");

            //Validation

            if (link.SupplyLinkId != 0 ||
                link.Location != null || link.Supplier != null || link.SupplyCategory != null ||
                await _context.Locations.FindAsync(link.LocationId) == null ||
                await _context.Suppliers.FindAsync(link.SupplierId) == null ||
                await _context.SupplyCategories.FindAsync(link.SupplyCategoryId) == null ||
                await _context.SupplyLinks.AnyAsync(l =>
                    l.LocationId == link.LocationId &&
                    l.SupplierId == link.SupplierId &&
                    l.SupplyCategoryId == link.SupplyCategoryId) ||
                !await _context.SupplierStocks.AnyAsync(ss =>
                    ss.SupplierId == link.SupplierId &&
                    ss.SupplyCategoryId == link.SupplyCategoryId))
            {
                _logger.LogDebug("Supply Links Controller found provided " +
                    "'supplyLink' parameter to violate creation constraints.");
                return BadRequest();
            }

            //Validation

            _context.SupplyLinks.Add(link);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Supply Links Controller successfully created new " +
                "Supply Link at ID: " + link.SupplyLinkId);
            return CreatedAtAction("GetSupplyLinkBasic", new { id = link.SupplyLinkId }, link);
        }

        /// <summary>
        /// Delete Supply Link
        /// </summary>
        /// <param name="id">Id of target Supply Link</param>
        /// <remarks>
        /// Removes a Supply Link from the Database.
        /// If there is no Supply Link, whose Id matches the given 'id' parameter, 
        /// Not Found is returned.
        /// If the desired Supply Link is the last in the Database it is removed from the Database
        /// and its Supply Link Id will be used by the next Supply Link to be inserted.
        /// If the desired Supply Link is not the last in the Database, it is swapped with the last
        /// Supply Link, taking on the last Supply Link's Supplier Id, Location Id
        /// and Supply Category Id before removing the last Supply Link and freeing up its 
        /// Supply Link's Id for the next Supply Link to be inserted.
        /// </remarks>
        // DELETE: api/SupplyLinks/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<SupplyLink>> DeleteSupplyLink(int id)
        {
            _logger.LogDebug("Supply Links Controller trying to delete " +
                "Supply Link with ID: " + id);

            var supplyLink = await _context.SupplyLinks.FindAsync(id);
            if (supplyLink == null)
            {
                _logger.LogDebug("Supply Links Controller found no Supply " +
                    "Link with ID: " + id);
                return NotFound();
            }

            var lastLinkId = await _context.SupplyLinks.CountAsync();

            if (id == lastLinkId)
            {
                _logger.LogDebug("Supply Links Controller found Supply Link " +
                    "with ID: " + id + " to be the last in the Database and will " +
                    "delete it.");
                _context.SupplyLinks.Remove(supplyLink);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[SupplyLinks]', RESEED, " + (lastLinkId - 1) + ")");
                await _context.SaveChangesAsync();

                _logger.LogDebug("Supply Links Controller successfully deleted Supply " +
                    "Link with ID: " + id);
                return Ok();
            }

            _logger.LogDebug("Supply Links Controller found Supply Link with ID: " +
                id + " not to be the last in the Database. As such, it will swap it with " +
                "the last Supply Link before deleting that one.");

            var lastLink = await _context.SupplyLinks.FindAsync(lastLinkId);
            supplyLink.LocationId = lastLink.LocationId;
            supplyLink.SupplierId = lastLink.SupplierId;
            supplyLink.SupplyCategoryId = lastLink.SupplyCategoryId;

            _context.SupplyLinks.Remove(lastLink);
            await _context.SaveChangesAsync();
            await _context.Database.ExecuteSqlRawAsync
                ("DBCC CHECKIDENT('[SupplyLinks]', RESEED, " + (lastLinkId - 1) + ")");
            await _context.SaveChangesAsync();

            _logger.LogDebug("Supply Links Controller successfully turned Supply Link " +
                "with ID: " + id + " into the last Supply Link, then deleted that one.");
            return Ok();
        }

        [NonAction]
        private async Task<bool> SupplyLinkExists(int id)
            => await _context.SupplyLinks.AnyAsync(e => e.SupplyLinkId == id);
    }
}
