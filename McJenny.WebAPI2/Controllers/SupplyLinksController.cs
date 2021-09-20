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
    public class SupplyLinksController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;

        public SupplyLinksController(FoodChainsDbContext context) => _context = context;

        #region Gets

        #region Basic

        // GET: api/SupplyLinks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> GetSupplyLinks()
        {
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

            return result;
        }

        // GET: api/SupplyLinks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> GetSupplyLink(int id)
        {
            var link = await _context.SupplyLinks
                .Select(l => new { l.SupplyLinkId, l.LocationId, l.SupplierId, l.SupplyCategoryId })
                .FirstOrDefaultAsync(l => l.SupplyLinkId == id);

            if (link == null) return NotFound();

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

        // GET: api/SupplyLinks/5/basic
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<SupplyLink>> GetSupplyLinkBasic(int id)
        {
            var supplyLink = await _context.SupplyLinks.FindAsync(id);

            if (supplyLink == null) return NotFound();

            return supplyLink;
        }

        #endregion

        #endregion

        // PUT: api/SupplyLinks/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupplyLink(int id, SupplyLink supplyLink)
        {
            if (id != supplyLink.SupplyLinkId)
            {
                return BadRequest();
            }

            _context.Entry(supplyLink).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await SupplyLinkExists(id))
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

        // POST: api/SupplyLinks
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<SupplyLink>> CreateSupplyLink(SupplyLink link)
        {
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
                return BadRequest();

            //Validation

            _context.SupplyLinks.Add(link);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSupplyLinkBasic", new { id = link.SupplyLinkId }, link);
        }

        // DELETE: api/SupplyLinks/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<SupplyLink>> DeleteSupplyLink(int id)
        {
            var supplyLink = await _context.SupplyLinks.FindAsync(id);
            if (supplyLink == null) return NotFound();

            var lastLinkId = await _context.SupplyLinks.CountAsync();

            if (id == lastLinkId)
            {
                _context.SupplyLinks.Remove(supplyLink);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[SupplyLinks]', RESEED, " + (lastLinkId - 1) + ")");
                await _context.SaveChangesAsync();
                return Ok();
            }

            var lastLink = await _context.SupplyLinks.FindAsync(lastLinkId);
            supplyLink.LocationId = lastLink.LocationId;
            supplyLink.SupplierId = lastLink.SupplierId;
            supplyLink.SupplyCategoryId = lastLink.SupplyCategoryId;

            _context.SupplyLinks.Remove(lastLink);
            await _context.SaveChangesAsync();
            await _context.Database.ExecuteSqlRawAsync
                ("DBCC CHECKIDENT('[SupplyLinks]', RESEED, " + (lastLinkId - 1) + ")");
            await _context.SaveChangesAsync();
            return Ok();
        }

        private async Task<bool> SupplyLinkExists(int id)
            => await _context.SupplyLinks.AnyAsync(e => e.SupplyLinkId == id);
    }
}
