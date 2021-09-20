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
    public class SuppliersController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;

        public SuppliersController(FoodChainsDbContext context) => _context = context;

        #region Gets

        #region Basic

        // GET: api/Suppliers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> GetSuppliers()
        => await _context.Suppliers
            .Take(20)
            .Select(s =>
            string.Format("({0}) {1}, {2}{3}, {4}",
                s.SupplierId, s.Name, s.AbreviatedCountry,
                s.AbreviatedState == "N/A" ? string.Empty : ", " + s.AbreviatedState,
                s.City
                ))
            .ToListAsync();

        // GET: api/Suppliers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<dynamic>> GetSupplier(int id)
        {
            if (!await SupplierExists(id)) return NotFound();

            return await _context.Suppliers
                .Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.City
                })
                .FirstOrDefaultAsync(s => s.SupplierId == id);
        }

        // GET: api/Suppliers/5/basic
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<Supplier>> GetSupplierBasic(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();

            return supplier;
        }

        // GET: api/Suppliers/5/stocks
        [HttpGet("{id}/stocks")]
        public async Task<ActionResult<dynamic>> GetSupplyCategories(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();

            return await _context.SupplyCategories
                .Where(c => GetSupplyCategoryIdsAsync(id).Result
                    .Contains(c.SupplyCategoryId))
                .Select(c => new { c.SupplyCategoryId, c.Name })
                .ToArrayAsync();
        }

        // GET: api/Suppliers/5/locations
        [HttpGet("{id}/locations")]
        public async Task<ActionResult<IEnumerable<string>>> GetLocations(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();

            return await _context.Locations
                .Where(l => GetSuppliedLocationIdsAsync(id).Result
                    .Contains(l.LocationId))
                .Select(l => string.Format("({0}), {1}, {2}{3}, {4}",
                    l.LocationId,
                    l.AbreviatedCountry,
                    l.AbreviatedState == "N/A" ? string.Empty :
                    l.AbreviatedState + ", ",
                    l.City,
                    l.Street))
                .ToArrayAsync();
        }

        //    {
        //    // GET: api/Suppliers/5/links
        //    [HttpGet("{id}/links")]
        //    public async Task<ActionResult<IEnumerable<string>>> GetSupplyLinks(int id)
        //    {
        //        var supplier = await _context.Suppliers.FindAsync(id);
        //        if (supplier == null) return NotFound();

        //        var dbLinks = await _context.SupplyLinks.ToArrayAsync();
        //        var locationIds = new List<int>();
        //        var cats = await _context.SupplyCategories.Select(c => new { c.SupplyCategoryId, c.Name }).ToArrayAsync();

        //        foreach (var dbLink in dbLinks)
        //            if (!locationIds.Contains(dbLink.LocationId)) locationIds.Add(dbLink.LocationId);

        //        var locations = await _context.Locations.Where(l => locationIds.Contains(l.LocationId)).ToArrayAsync();

        //        var statements = new string[dbLinks.Length];

        //        for (int i = 0; i < dbLinks.Length; i++)
        //        {
        //            var location = locations.SingleOrDefault(l => l.LocationId == dbLinks[i].LocationId);

        //            statements[i] = string.Format("{0} ({1}), {2}, {3}{4} supplies {5}, {6}{7}, {8} ({9}) with {10} ({11})",
        //            supplier.Name, id, supplier.AbreviatedCountry,
        //            supplier.AbreviatedState == "N/A" ? string.Empty : supplier.AbreviatedState + ", ",
        //            supplier.City, location.AbreviatedCountry,
        //            location.AbreviatedState == "N/A" ? string.Empty : location.AbreviatedState + ", ",
        //            location.City, location.Street, dbLinks[i].LocationId,
        //            cats.SingleOrDefault(c => c.SupplyCategoryId == dbLinks[0].SupplyCategoryId).Name,
        //            dbLinks[0].SupplyCategoryId);
        //        }
        //        return statements;

        //    }
        //}

        #endregion

        #region Advanced

        // GET: api/Suppliers/query
        [HttpGet("query:{query}")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetSuppliersQueried(string query)
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
                case (false, false, true): return await GetSuppliersFiltered(filter);
                case (false, true, false): return await GetSuppliersSorted(sortBy, desc);
                case (true, false, false): return await GetSuppliersPaged(pgInd, pgSz);
                case (false, true, true):
                    return await GetSuppliersSorted(sortBy, desc,
(await GetSuppliersFiltered(filter)).Value);
                case (true, false, true):
                    return await GetSuppliersPaged(pgInd, pgSz,
(await GetSuppliersFiltered(filter)).Value);
                case (true, true, false):
                    return await GetSuppliersPaged(pgInd, pgSz,
(await GetSuppliersSorted(sortBy, desc)).Value);
                case (true, true, true):
                    return await GetSuppliersPaged(pgInd, pgSz,
(await GetSuppliersSorted(sortBy, desc,
(await GetSuppliersFiltered(filter)).Value)).Value);
                default:
                    return await GetSuppliersPaged(0, 20);
            }
        }

        public async Task<ActionResult<IEnumerable<dynamic>>> GetSuppliersFiltered(string filter, IEnumerable<dynamic> col = null)
        => col == null ?
                    (dynamic)await _context.Suppliers
                     .Select(s => new
                     {
                         s.SupplierId,
                         s.Name,
                         s.AbreviatedCountry,
                         s.AbreviatedState,
                         s.Country,
                         s.State,
                         s.City
                     })
                     .Where(s => s.Name.ToUpper()
                         .Contains(filter.ToUpper()) ||
                        s.AbreviatedCountry.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.AbreviatedState.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.Country.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.State.ToUpper()
                            .Contains(filter.ToUpper()) ||
                        s.City.ToUpper()
                            .Contains(filter.ToUpper()))
                     .ToArrayAsync() :
                (dynamic)col
                 .Where(s => s.Name.ToUpper()
                     .Contains(filter.ToUpper()) ||
                    s.AbreviatedCountry.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    s.AbreviatedState.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    s.Country.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    s.State.ToUpper()
                        .Contains(filter.ToUpper()) ||
                    s.City.ToUpper()
                        .Contains(filter.ToUpper()))
                        .ToArray();

        public async Task<ActionResult<IEnumerable<dynamic>>> GetSuppliersPaged(int pgInd, int pgSz, IEnumerable<dynamic> col = null)
            => col == null ?
            (dynamic)await _context.Suppliers
                .Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.Country,
                    s.State,
                    s.City
                })
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync() :
            (dynamic)col
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();

        public async Task<ActionResult<IEnumerable<dynamic>>> GetSuppliersSorted(string sortBy, bool? desc, IEnumerable<dynamic> col = null)
            => col == null ?
                sortBy.ToUpper() switch
                {
                    "NAME" => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderByDescending(s => s.Name).ToArrayAsync() :
                        (dynamic)await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderBy(s => s.Name).ToArrayAsync(),
                    "COUNTRY" => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderByDescending(s => s.AbreviatedCountry).ToArrayAsync() :
                        (dynamic)await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderBy(s => s.AbreviatedCountry)
                            .ToArrayAsync(),
                    "STATE" => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderByDescending(s => s.AbreviatedState).ToArrayAsync() :
                        (dynamic)await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderBy(s => s.AbreviatedState)
                            .ToArrayAsync(),
                    "CITY" => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderByDescending(s => s.City)
                            .ToArrayAsync() :
                        (dynamic)await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderBy(s => s.City)
                            .ToArrayAsync(),
                    _ => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .OrderByDescending(s => s.SupplierId)
                            .ToArrayAsync() :
                        (dynamic)await _context.Suppliers
                            .Select(s => new
                            {
                                s.SupplierId,
                                s.Name,
                                s.AbreviatedCountry,
                                s.AbreviatedState,
                                s.Country,
                                s.State,
                                s.City
                            })
                            .ToArrayAsync()
                } :
                  sortBy.ToUpper() switch
                  {
                      "NAME" => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(s => s.Name)
                              .ToArray() :
                          (dynamic)col
                              .OrderBy(s => s.Name)
                                .ToArray(),
                      "COUNTRY" => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(s => s.AbreviatedCountry)
                              .ToArray() :
                          (dynamic)col
                              .OrderBy(s => s.AbreviatedCountry)
                                .ToArray(),
                      "STATE" => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(s => s.AbreviatedState)
                              .ToArray() :
                          (dynamic)col
                              .OrderBy(s => s.AbreviatedState)
                                .ToArray(),
                      "CITY" => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(s => s.City)
                              .ToArray() :
                          (dynamic)col
                              .OrderBy(s => s.City)
                                .ToArray(),
                      _ => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .OrderByDescending(s => s.SupplierId)
                      .ToArray() :
                          (dynamic)col
                              .ToArray()
                  };

        #endregion

        #endregion

        // PUT: api/Suppliers/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupplier(int id, Supplier supplier)
        {
            if (!await SupplierExists(id)) return NotFound();

            //Validation

            if (supplier.SupplierId != 0 ||
                (!string.IsNullOrEmpty(supplier.AbreviatedCountry) &&
                    (string.IsNullOrWhiteSpace(supplier.AbreviatedCountry) ||
                    supplier.AbreviatedCountry.Length > 4)) ||
                (!string.IsNullOrEmpty(supplier.AbreviatedState) &&
                    (string.IsNullOrWhiteSpace(supplier.AbreviatedState) ||
                    supplier.AbreviatedState.Length > 4)) ||
                (!string.IsNullOrEmpty(supplier.City) &&
                    (string.IsNullOrWhiteSpace(supplier.City) ||
                    supplier.City.Length > 50)) ||
                (!string.IsNullOrEmpty(supplier.Country) &&
                    (string.IsNullOrWhiteSpace(supplier.Country) ||
                    supplier.Country.Length > 50)) ||
                (!string.IsNullOrEmpty(supplier.State) &&
                    (string.IsNullOrWhiteSpace(supplier.State) ||
                    supplier.State.Length > 50)) ||
                (!string.IsNullOrEmpty(supplier.Name) &&
                    (string.IsNullOrWhiteSpace(supplier.Name) ||
                    supplier.Name.Length > 50)) ||
                (!string.IsNullOrEmpty(supplier.Name)&&
                await _context.Suppliers
                    .AnyAsync(s => s.Name == supplier.Name &&
                    s.SupplierId!=id) )||
                supplier.SupplierStocks == null || supplier.SupplierStocks.Count != 0 ||
                supplier.SupplyLinks == null || supplier.SupplyLinks.Count != 0)
                return BadRequest();

            //Validation

            var oldSupplier = await _context.Suppliers.FindAsync(id);

            if (!string.IsNullOrEmpty(supplier.AbreviatedCountry))
            oldSupplier.AbreviatedCountry = supplier.AbreviatedCountry;
            if (!string.IsNullOrEmpty(supplier.AbreviatedState))
            oldSupplier.AbreviatedState = supplier.AbreviatedState;
            if (!string.IsNullOrEmpty(supplier.City))
            oldSupplier.City = supplier.City;
            if (!string.IsNullOrEmpty(supplier.Name))
            oldSupplier.Name = supplier.Name;
            if (!string.IsNullOrEmpty(supplier.Country))
            oldSupplier.Country = supplier.Country;
            if (!string.IsNullOrEmpty(supplier.State))
            oldSupplier.State = supplier.State;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Suppliers
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<Supplier>> CreateSupplier(Supplier supplier)
        {
            //Validation

            if (supplier.SupplierId != 0 ||
                string.IsNullOrEmpty(supplier.AbreviatedCountry) ||
                string.IsNullOrWhiteSpace(supplier.AbreviatedCountry) ||
                supplier.AbreviatedCountry.Length > 4 ||
                string.IsNullOrEmpty(supplier.AbreviatedState) ||
                string.IsNullOrWhiteSpace(supplier.AbreviatedState) ||
                supplier.AbreviatedState.Length > 4 ||
                string.IsNullOrEmpty(supplier.City) ||
                string.IsNullOrWhiteSpace(supplier.City) ||
                supplier.City.Length > 50 ||
                string.IsNullOrEmpty(supplier.Country) ||
                string.IsNullOrWhiteSpace(supplier.Country) ||
                supplier.Country.Length > 50 ||
                string.IsNullOrEmpty(supplier.State) ||
                string.IsNullOrWhiteSpace(supplier.State) ||
                supplier.State.Length > 50 ||
                string.IsNullOrEmpty(supplier.Name) ||
                string.IsNullOrWhiteSpace(supplier.Name) ||
                supplier.Name.Length > 50 ||
                await _context.Suppliers.AnyAsync(s=>s.Name==supplier.Name)||
                supplier.SupplierStocks == null || supplier.SupplierStocks.Count != 0 ||
                supplier.SupplyLinks == null || supplier.SupplyLinks.Count != 0)
                return BadRequest();

            //Validation

            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSupplierBasic", new { id = supplier.SupplierId }, supplier);
        }

        // DELETE: api/Suppliers/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Supplier>> DeleteSupplier(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();

            // If it has SupplyStocks or SupplyLinks, turn it into an Empty Supplier
            if (await _context.SupplierStocks.AnyAsync(s => s.SupplierId == id) ||
                await _context.SupplyLinks.AnyAsync(l => l.SupplierId == id))
            {
                supplier.Name = string.Format("{0} : {1}",
                    "Empty", DateTime.Today.ToShortDateString());
                supplier.AbreviatedCountry = "XX";
                supplier.AbreviatedState = "XX";
                supplier.Country = "None";
                supplier.State = "None";
                supplier.City = "None";
                await _context.SaveChangesAsync();
                return Ok();
            }

            var lastSupId = await _context.Suppliers.CountAsync();

            if (id == lastSupId)/*If it's last, delete it*/
            {
                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[Suppliers]', RESEED, " + (lastSupId - 1) + ")");
                await _context.SaveChangesAsync();
                return Ok();
            }

            //If it's not last, swap it with last and del that

            /*Migrate Stocks and Links from Last Supplier to supId*/
            var stocks = await _context.SupplierStocks
                .Where(s => s.SupplierId == lastSupId)
                .ToArrayAsync();
            var links = await _context.SupplyLinks
                .Where(s => s.SupplierId == lastSupId)
                .ToArrayAsync();

            foreach (var stock in stocks)
                stock.SupplierId = id;

            foreach (var link in links)
                link.SupplierId = id;

            await _context.SaveChangesAsync();

            /*Swap and del last*/
            var lastSup = await _context.Suppliers.FindAsync(lastSupId);
            supplier.AbreviatedCountry = lastSup.AbreviatedCountry;
            supplier.AbreviatedState = lastSup.AbreviatedState;
            supplier.Country = lastSup.Country;
            supplier.State = lastSup.State;
            supplier.City = lastSup.City;
            supplier.Name = lastSup.Name;

            _context.Suppliers.Remove(lastSup);
            await _context.SaveChangesAsync();
            await _context.Database.ExecuteSqlRawAsync
                ("DBCC CHECKIDENT('[Suppliers]', RESEED, " + (lastSupId - 1) + ")");
            await _context.SaveChangesAsync();
            return Ok();
        }

        private async Task<bool> SupplierExists(int id)
            => await _context.Suppliers.FindAsync(id)!=null;

        //Helpers

        private async Task<int[]> GetSupplyCategoryIdsAsync(int id)
        => await _context.SupplierStocks
                .Where(ss => ss.SupplierId == id)
                .Select(ss => ss.SupplyCategoryId)
                .ToArrayAsync();

        private async Task<int[]> GetSuppliedLocationIdsAsync(int id)
        => await _context.SupplyLinks
                .Where(l => l.SupplierId == id)
                .Select(l => l.LocationId)
                .ToArrayAsync();
    }
}
