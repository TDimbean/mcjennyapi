using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using McJenny.WebAPI.Data.Models;
using McJenny.WebAPI.Helpers;

namespace McJenny.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplyCategoriesController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;

        public SupplyCategoriesController(FoodChainsDbContext context)
        => _context = context;

        #region Gets

        #region Basic

        // GET: api/SupplyCategories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetSupplyCategories()
        =>await _context.SupplyCategories
            .Select(c=>new {c.SupplyCategoryId, c.Name })
            .ToListAsync();

        // GET: api/SupplyCategories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> GetSupplyCategory(int id)
        {
            try
            {
                return (await _context.SupplyCategories.FindAsync(id)).Name;
            }
            catch (NullReferenceException ex)
            {
                //Log
                return NotFound();
            }
            catch (Exception ex)
            {
                //Log
                return BadRequest();
            }
        }

        // GET: api/SupplyCategories/5/basic
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<SupplyCategory>> GetSupplyCategoryBasic(int id)
        {
            var supplyCategory = await _context.SupplyCategories.FindAsync(id);
            if (supplyCategory == null) return NotFound();

            return supplyCategory;
        }

        #endregion

        #region Advanced

        // GET: api/SupplyCategories/query
        [HttpGet("query:{query}")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetSupplyCategoriesQueried(string query)
        {
            var quer = query.ToUpper().Replace("QUERY:", string.Empty);

            bool isPaged, isFiltered, isSorted, desc;

            var pgInd = 0;
            var pgSz = 0;
            var filter = string.Empty;
            var sortBy = string.Empty;

            isPaged = quer.Contains("PGIND=") && quer.Contains("PGSZ=");
            isFiltered = quer.Contains("FILTER=");
            isSorted = quer.Contains("SORTBY=");
            desc = quer.Contains("DESC=TRUE");

            if (isPaged)
            {
                var indStr = quer.QueryToParam("PGIND");
                var szStr = quer.QueryToParam("PGSZ");

                var indParses = int.TryParse(indStr, out pgInd);
                var szParses = int.TryParse(szStr, out pgSz);

                if (!indParses || !szParses) isPaged = false;
                else
                {
                    pgInd = Math.Abs(pgInd);
                    pgSz = pgSz == 0 ? 10 : Math.Abs(pgSz);
                }
            }

            if (isFiltered) filter = quer.QueryToParam("FILTER");
            if (isSorted) sortBy = quer.QueryToParam("SORTBY");

            switch (isPaged, isSorted, isFiltered)
            {
                case (false, false, true): return await GetSupplyCategoriesFiltered(filter);
                case (false, true, false): return await GetSupplyCategoriesSorted(sortBy, desc);
                case (true, false, false): return await GetSupplyCategoriesPaged(pgInd, pgSz);
                case (false, true, true):
                    return await GetSupplyCategoriesSorted(sortBy, desc,
(await GetSupplyCategoriesFiltered(filter)).Value);
                case (true, false, true):
                    return await GetSupplyCategoriesPaged(pgInd, pgSz,
(await GetSupplyCategoriesFiltered(filter)).Value);
                case (true, true, false):
                    return await GetSupplyCategoriesPaged(pgInd, pgSz,
(await GetSupplyCategoriesSorted(sortBy, desc)).Value);
                case (true, true, true):
                    return await GetSupplyCategoriesPaged(pgInd, pgSz,
(await GetSupplyCategoriesSorted(sortBy, desc,
(await GetSupplyCategoriesFiltered(filter)).Value)).Value);
                default:
                    return await GetSupplyCategories();
            }
        }

        public async Task<ActionResult<IEnumerable<dynamic>>> GetSupplyCategoriesFiltered(string filter, IEnumerable<dynamic> col = null)
        => col == null ?
                (dynamic)await _context.SupplyCategories
                 .Select(c => new { c.SupplyCategoryId, c.Name })
                 .Where(c => c.Name.ToUpper()
                     .Contains(filter.ToUpper()))
                 .ToArrayAsync() :
                (dynamic)col
                     .Select(c => new { c.SupplyCategoryId, c.Name })
                     .Where(c => c.Name.ToUpper()
                     .Contains(filter.ToUpper()))
                        .ToArray();

        public async Task<ActionResult<IEnumerable<dynamic>>> GetSupplyCategoriesPaged(int pgInd, int pgSz, IEnumerable<dynamic> col = null)
            => col == null ?
            (dynamic)await _context.SupplyCategories
                .Select(c => new { c.SupplyCategoryId, c.Name })
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync() :
            (dynamic)col
                .Select(c => new { c.SupplyCategoryId, c.Name })
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();

        public async Task<ActionResult<IEnumerable<dynamic>>> GetSupplyCategoriesSorted(string sortBy, bool? desc, IEnumerable<dynamic> col = null)
            => col == null ?
                sortBy.ToUpper() switch
                {
                    "NAME" => desc.GetValueOrDefault() ?
                        (dynamic)await _context.SupplyCategories
                            .Select(c => new { c.SupplyCategoryId, c.Name })
                            .OrderByDescending(c => c.Name).ToArrayAsync() :
                        (dynamic)await _context.SupplyCategories
                            .Select(c => new { c.SupplyCategoryId, c.Name })
                            .OrderBy(c => c.Name).ToArrayAsync(),
                    _ => desc.GetValueOrDefault() ?
                        (dynamic)await _context.SupplyCategories
                            .Select(c => new { c.SupplyCategoryId, c.Name })
                            .OrderByDescending(c => c.SupplyCategoryId).ToArrayAsync() :
                        (dynamic)await _context.SupplyCategories
                            .Select(c => new { c.SupplyCategoryId, c.Name })
                            .ToArrayAsync()
                } :
                  sortBy.ToUpper() switch
                  {
                      "NAME" => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .Select(c => new { c.SupplyCategoryId, c.Name })
                              .OrderByDescending(c => c.Name).ToArray() :
                          (dynamic)col
                              .Select(c => new { c.SupplyCategoryId, c.Name })
                              .OrderBy(c => c.Name).ToArray(),
                      _ => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .Select(c => new { c.SupplyCategoryId, c.Name })
                              .OrderByDescending(c => c.SupplyCategoryId).ToArray() :
                          (dynamic)col
                              .Select(c => new { c.SupplyCategoryId, c.Name })
                              .ToArray()
                  };

        #endregion

        #endregion

        // PUT: api/SupplyCategories/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupplyCategory(int id, SupplyCategory supplyCategory)
        {
            if (!await SupplyCategoryExists(id)) return NotFound();

            //Validation

            if (supplyCategory.SupplyCategoryId != 0 ||
                supplyCategory.SupplierStocks == null ||
                supplyCategory.SupplyLinks == null ||
                supplyCategory.DishRequirements == null ||
                supplyCategory.SupplierStocks.Count() != 0 ||
                supplyCategory.DishRequirements.Count() != 0 ||
                supplyCategory.SupplyLinks.Count() != 0 ||
                (!string.IsNullOrEmpty(supplyCategory.Name) &&
                    (string.IsNullOrWhiteSpace(supplyCategory.Name) ||
                    supplyCategory.Name.Length > 50)) ||
                await _context.SupplyCategories
                    .AnyAsync
                    (d => d.Name != null &&
                    d.Name.ToUpper() == supplyCategory.Name.ToUpper() &&
                    d.SupplyCategoryId != id))
                return BadRequest();

                //Validation

                var oldCat = await _context.SupplyCategories.FindAsync(id);

            oldCat.Name = supplyCategory.Name;

                await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/SupplyCategories
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<SupplyCategory>> CreateSupplyCategory(SupplyCategory supplyCategory)
        {
            //Validation

            if (supplyCategory.SupplyCategoryId != 0 ||
                string.IsNullOrEmpty(supplyCategory.Name) ||
                string.IsNullOrWhiteSpace(supplyCategory.Name) ||
                supplyCategory.Name.Length > 50 ||
                await _context.SupplyCategories.AnyAsync(c=>c.Name.ToUpper()==supplyCategory.Name.ToUpper())||
                supplyCategory.DishRequirements == null ||
                supplyCategory.DishRequirements.Count != 0 ||
                supplyCategory.SupplierStocks == null ||
                supplyCategory.SupplierStocks.Count != 0 ||
                supplyCategory.SupplyLinks == null ||
                supplyCategory.SupplyLinks.Count != 0)
                return BadRequest();
            //Validation

            _context.SupplyCategories.Add(supplyCategory);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSupplyCategoryBasic", new { id = supplyCategory.SupplyCategoryId }, supplyCategory);
        }

        // DELETE: api/SupplyCategories/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<SupplyCategory>> DeleteSupplyCategory(int id)
        {
            var supplyCategory = await _context.SupplyCategories.FindAsync(id);
            if (supplyCategory == null) return NotFound();

            // If it has Stocks, Links or Requirements, turn it into a blank category
            if (await _context.SupplyLinks.AnyAsync(l => l.SupplyCategoryId == id)||
                await _context.DishRequirements.AnyAsync(r=>r.SupplyCategoryId == id)||
                await _context.SupplierStocks.AnyAsync(s=>s.SupplyCategoryId==id))
            {
                supplyCategory.Name = string.Format("Blank {0}:{1}",
                    DateTime.Now.ToShortDateString(), id);
                await _context.SaveChangesAsync();
                return Ok();
            }

            var lastCatId = await _context.SupplyCategories.CountAsync();

            if (id == lastCatId)/*If it's last, delete it*/
            {
                _context.SupplyCategories.Remove(supplyCategory);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[SupplyCategories]', RESEED, " + (lastCatId - 1) + ")");
                await _context.SaveChangesAsync();
                return Ok();
            }

            //If it's not last, swap it with last and del that

            /*Migrate Stocks, Links and Requirements from Last Category to catId*/
            var requirements = await _context.DishRequirements
                    .Where(r => r.SupplyCategoryId == lastCatId)
                    .ToArrayAsync();
            var stocks = await _context.SupplierStocks
                    .Where(s=>s.SupplyCategoryId == lastCatId)
                    .ToArrayAsync();
            var links = await _context.SupplyLinks
                    .Where(l=>l.SupplyCategoryId == lastCatId)
                    .ToArrayAsync();

            foreach (var req in requirements)
                req.SupplyCategoryId = id;
            foreach (var link in links)
                link.SupplyCategoryId = id;
            foreach (var stock in stocks)
                stock.SupplyCategoryId = id;

            await _context.SaveChangesAsync();

            /*Swap and del last*/
            var lastCat = await _context.SupplyCategories.FindAsync(lastCatId);
            supplyCategory.Name = lastCat.Name;

            _context.SupplyCategories.Remove(lastCat);
            await _context.SaveChangesAsync();
            await _context.Database.ExecuteSqlRawAsync
                ("DBCC CHECKIDENT('[SupplyCategories]', RESEED, " + (lastCatId - 1) + ")");
            await _context.SaveChangesAsync();
            return Ok();
        }

        private async Task<bool> SupplyCategoryExists(int id) => await _context.SupplyCategories.FindAsync(id) != null;
    }
}
