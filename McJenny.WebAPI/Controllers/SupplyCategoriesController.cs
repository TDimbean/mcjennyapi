using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using McJenny.WebAPI.Data.Models;
using McJenny.WebAPI.Helpers;
using Microsoft.Extensions.Logging;
using System.Text;

namespace McJenny.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplyCategoriesController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;
        private readonly ILogger<SupplyCategoriesController> _logger;

        public SupplyCategoriesController(FoodChainsDbContext context,
            ILogger<SupplyCategoriesController> logger)
        {
            _context = context;
            _logger = logger;
            _logger.LogDebug("Logger injected into Supply Categories Controller.");
        }

        #region Gets

        #region Basic

        /// <summary>
        /// Get Supply Categories
        /// </summary>
        /// <remarks>
        /// Returns a collection of all Supply Categories, formated to
        /// include each Supply Category's Id and Name.
        /// </remarks>
        // GET: api/SupplyCategories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetSupplyCategories()
        {
            _logger.LogDebug("Supply Categories Controller fetching all " +
                "Supply Categories...");
            return await _context.SupplyCategories
            .Select(c => new { c.SupplyCategoryId, c.Name })
            .ToListAsync();
        }

        /// <summary>
        /// Get Supply Category
        /// </summary>
        /// <param name="id">Id of desired Supply Category</param>
        /// <remarks>
        /// Returns the Name of the Supply Category whose Id matches
        /// the given 'id' parameter.
        /// If no such Supply Category exists,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/SupplyCategories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> GetSupplyCategory(int id)
        {
            _logger.LogDebug("Supply Categories Controller fetching " +
                "Supply Category with ID: " + id);
            try
            {
                _logger.LogDebug("Supply Categories Controller returning " +
                    "Supply Category with ID: " + id);
                return (await _context.SupplyCategories.FindAsync(id)).Name;
            }
            catch (NullReferenceException ex)
            {
                _logger.LogDebug("Supply Categories Controller found no " +
                    "Supply Category with ID: " + id);
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Supply Categories Controller encountered " +
                    "an unexpected Exception. Details: " + ex.Message);
                return BadRequest();
            }
        }

        /// <summary>
        /// Get Supply Category Links
        /// </summary>
        /// <param name="id">Id of desired Supply Category</param>
        /// <remarks>
        /// Returns a collection of strings explaining the Supply Link
        /// relationship between Suppliers, the Location they supply and the provided
        /// Supply Category whose Id matches the given 'id' parameter, formated to include
        /// each Supplier's Id, Name, Abreviated Country, Abreviated State and City as well
        /// as each Location's Id, Abreviated Country, Abreviated State, City and Street
        /// Address.
        /// If no Supply Category whose Id matches the given 'id' parameter exists,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/SupplyCategories/5/links
        [HttpGet("{id}/links")]
        public async Task<ActionResult<IEnumerable<string>>> GetSupplyCategoryLinks(int id)
        {
            _logger.LogDebug("Supply Categories Controller fetching Supply " +
                "Links for Supply Category with ID: " + id);
            var supplyCategory = await _context.SupplyCategories.FindAsync(id);
            if (supplyCategory == null)
            {
                _logger.LogDebug("Supply Categories Controller found no " +
                    "Supply Category with ID: " + id);
                return NotFound();
            }

            var supplyLinks = await _context.SupplyLinks
                .Where(l => l.SupplyCategoryId == id)
                .ToArrayAsync();

            var supIds = new List<int>();
            var locIds = new List<int>();

            foreach (var link in supplyLinks)
            {
                supIds.Add(link.SupplierId);
                locIds.Add(link.LocationId);
            };

            supIds = supIds.Distinct().ToList();
            locIds = locIds.Distinct().ToList();

            var suppliers = await _context.Suppliers
                .Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.City
                })
                .Where(s => supIds.Contains(s.SupplierId))
                .ToArrayAsync();

            var locations = await _context.Locations
                .Select(l => new
                {
                    l.LocationId,
                    l.AbreviatedCountry,
                    l.AbreviatedState,
                    l.City,
                    l.Street
                })
                .Where(l => locIds.Contains(l.LocationId))
                .ToArrayAsync();

            var links = new string[supplyLinks.Length];

            for (int i = 0; i < links.Length; i++)
            {
                var sup = suppliers.FirstOrDefault(s => s.SupplierId == supplyLinks[i].SupplierId);
                var loc = locations.FirstOrDefault(l => l.LocationId == supplyLinks[i].LocationId);

                links[i] = string.Format("(Supply Link: {0}): ({1}) {2}, {3}, {4}{5} "+
                    "supplies ({6}) {7}, {8}{9}, {10}.",
                    i,
                    sup.SupplierId,
                    sup.Name,
                    sup.AbreviatedCountry,
                    sup.AbreviatedState=="N/A"?string.Empty:
                    sup.AbreviatedState,
                    sup.City,
                    loc.LocationId,
                    loc.AbreviatedCountry,
                    loc.AbreviatedState,
                    loc.City,
                    loc.Street);
            }

            _logger.LogDebug("Supply Categories Controller returning Supply " +
                "Links for Supply Category with ID: " + id);
            return links;
        }

        /// <summary>
        /// Get Supply Category Dishes
        /// </summary>
        /// <param name="id">Id of desired Supply Category</param>
        /// <remarks>
        /// Returns a collection of the Dishes that require the Supply Category
        /// whose Id matches the given 'id' parameter based on the DishRequirement
        /// relationship they share, formated to include each Dish's Id and Name
        /// If no Supply Category whose Id matches the given 'id' parameter exists,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/SupplyCategories/5/dishes
        [HttpGet("{id}/dishes")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetDishes(int id)
        {
            _logger.LogDebug("Supply Categories Controller fetching Dishes " +
                "that need Supply Category with ID: " + id);
            var supplyCategory = await _context.SupplyCategories.FindAsync(id);
            if (supplyCategory == null)
            {
                _logger.LogDebug("Supply Categories Controller found no Supply " +
                    "Category with ID: " + id);
                return NotFound();
            }

            var dishRequirements = await _context.DishRequirements
                .Where(r => r.SupplyCategoryId == id)
                .ToArrayAsync();

            var dishIds = new List<int>();

            foreach (var req in dishRequirements)
                dishIds.Add(req.DishId);

            dishIds = dishIds.Distinct().ToList();

            _logger.LogDebug("Supply Categories Controller returning Dishes " +
                "that need Supply Category with ID: " + id);
            return await _context.Dishes
                .Select(d => new
                {
                    d.DishId,
                    d.Name
                })
                .Where(d => dishIds.Contains(d.DishId))
                .ToArrayAsync();
        }

        /// <summary>
        /// Get Supply Category Suppliers
        /// </summary>
        /// <param name="id">Id of desired Supply Category</param>
        /// <remarks>
        /// Returns a collection of the Suppliers that deal in the Supply Category
        /// whose Id matches the given 'id' parameter based on the Supplier Stock
        /// relationship they share, formated to include each Supplier's Id,
        /// Abreviated Country, Abreviated State, City and Name.
        /// If no Supply Category whose Id matches the given 'id' parameter exists,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/SupplyCategories/5/suppliers
        [HttpGet("{id}/suppliers")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetSuppliers(int id)
        {
            _logger.LogDebug("Supply Categories Controller fetching Suppliers " +
                "that stock Supply Category with ID: " + id);
            var supplyCategory = await _context.SupplyCategories.FindAsync(id);
            if (supplyCategory == null)
            {
                _logger.LogDebug("Supply Categories Controller found no Supply" +
                    " Category with ID: " + id);
                return NotFound();
            }

            var supplierStocks = await _context.SupplierStocks
                .Where(r => r.SupplyCategoryId == id)
                .ToArrayAsync();

            var supIds = new List<int>();

            foreach (var stock in supplierStocks)
                supIds.Add(stock.SupplierId);

            supIds = supIds.Distinct().ToList();

            _logger.LogDebug("Supply Categories Controller returning Suppliers " +
                "that stock Supply Category with ID: " + id);
            return await _context.Suppliers
                .Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.AbreviatedCountry,
                    s.AbreviatedState,
                    s.City
                })
                .Where(s => supIds.Contains(s.SupplierId))
                .ToArrayAsync();
        }

        /// <summary>
        /// Get Supply Category Basic
        /// </summary>
        /// <param name="id">Id of desired Supply Category</param>
        /// <remarks>
        /// Returns a Supply Category object whose Id matches the given 'id'
        /// parameter, containing the Supply Category Id and Name, as well as empty 
        /// collections of Dish Requirements, Supplier Stocks and Supply Links.
        /// If no Supply Category whose Id matches the 'id' parameter exists,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/SupplyCategories/5/basic
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<SupplyCategory>> GetSupplyCategoryBasic(int id)
        {
            _logger.LogDebug("Supply Categories Controller fetching Supply Category " +
                "with ID: " + id);
            var supCat = await _context.SupplyCategories.FindAsync(id);
            if (supCat == null)
            {
                _logger.LogDebug("Supply Categories Controller found no Supply " +
                    "Category with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Supply Categories Controller returning basic Supply " +
                "Category with ID: " + id);
            return supCat;
        }

        #endregion

        #region Advanced

        /// <summary>
        /// Get Supply Categories Queries
        /// </summary>
        /// <remarks>
        /// Returns a list of Supply Categories according to the specified query,
        /// formated to include each Supply Category's Id and Name.
        /// Query Keywords: Filter, SortBy(Optional: Desc), PgInd and PgSz.
        /// Keywords are followed by '=', a string parameter, and, if further keywords
        /// are to be added afterwards, the Ampersand Separator
        /// Filter: Only return Supply Categories where the Name
        /// contains the given string.
        /// SortBy: Supply Categories will be ordered in ascending order by 
        /// either their Name, if the string parameter is 'name',
        /// or by their ID, if any other string parameter is given.
        /// Desc: If SortBy is used, and the string parameter is 'true',
        /// Supply Categories wil be returned in Descending Order.
        /// PgInd: If the following int is greater than 0 and the PgSz 
        /// keyword is also present, Supply Categories will be paged and
        /// page 'PgInd' of size 'PgSz' will be returned.
        /// PgSz: If the following int is greater than 0 and the PgInd 
        /// keyword is also present, Supply Categories will be paged and
        /// page 'PgInd' of size 'PgSz' will be returned.
        /// Any of the Keywords can be used or ommited.
        /// Using a keyword more than once will result in only the 
        /// first occurence being recognized.
        /// If a keyword is followed by an incorrectly formatted parameter, the keyword will be ignored.
        /// Order of Query Operations: Filter > Sort > Pagination
        /// Any improperly formatted text will be ignored.
        /// </remarks>
        // GET: api/SupplyCategories/query
        [HttpGet("query:{query}")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetSupplyCategoriesQueried(string query)
        {
            _logger.LogDebug("Supply Categories Controller looking up query...");
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

            var returnString = new StringBuilder(!isPaged ? string.Empty :
                    "Paginated: Page " + pgInd + " of Size " + pgSz);
            if (isSorted)
            {
                if (returnString.Length > 0) returnString.Insert(0, " and ");
                returnString.Insert(0, "Sorted: By " + sortBy + " In " +
                    (desc ? "Descending" : "Ascending") + " Order");
            }
            if (isFiltered)
            {
                if (isSorted && isPaged) returnString.Insert(0, ", ");
                else if (isFiltered || isPaged) returnString.Insert(0, " and ");
                returnString.Insert(0, "Filtered By: " + filter);
            }
            _logger.LogDebug("Supply Categories Controller returning " +
                (!isPaged && !isSorted && !isFiltered ? "all " : string.Empty) +
                "Supply Categories" +
                (returnString.Length == 0 ? string.Empty :
                " " + returnString.ToString()) + ".");

            return (isPaged, isSorted, isFiltered)
            switch
            {
                (false, false, true) => await GetSupplyCategoriesFiltered(filter),
                (false, true, false) => await GetSupplyCategoriesSorted(sortBy, desc),
                (true, false, false) => await GetSupplyCategoriesPaged(pgInd, pgSz),
                (false, true, true) => await GetSupplyCategoriesSorted(sortBy, desc,
                    (await GetSupplyCategoriesFiltered(filter)).Value),
                (true, false, true) => await GetSupplyCategoriesPaged(pgInd, pgSz,
                    (await GetSupplyCategoriesFiltered(filter)).Value),
                (true, true, false) => await GetSupplyCategoriesPaged(pgInd, pgSz,
                    (await GetSupplyCategoriesSorted(sortBy, desc)).Value),
                (true, true, true) => await GetSupplyCategoriesPaged(pgInd, pgSz,
                    (await GetSupplyCategoriesSorted(sortBy, desc,
                    (await GetSupplyCategoriesFiltered(filter)).Value)).Value),
                _ => await GetSupplyCategories(),
            };
        }

        [NonAction]
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

        [NonAction]
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

        [NonAction]
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

        /// <summary>
        /// Update Supply Category
        /// </summary>
        /// <param name="id">Id of target Supply Category</param>
        /// <param name="supplyCategory">A Supply Category object whose
        /// properties are to be transfered to the existing Supply Category.</param>
        /// <remarks>
        /// Updates the values of an existing Supply Category, whose Id 
        /// corresponds to the given 'id' parameter, using the values
        /// from the 'supplyCategory' parameter.
        /// Only the Name of the Supply Category may be updated through this API.
        /// For its Supply Link, Supplier Stock and Dish Requirement relationships
        /// their respective API controllers must be used.
        /// The Supply Category Id may not be updated.
        /// The given parameter of type Supply Category must contain a Name.
        /// Additionally it may contain empty collections for Supply Links,
        /// Supplier Stocks and Dish Requirements, as well as a
        /// Supply Category Id of 0
        /// The 'id' parameter must correspond to an existing Supply Category,
        /// otherwise Not Found is returned.
        /// If the 'supplyCategory parameter is incorrect, Bad Request is returned.
        /// Supply Category parameter errors:
        /// (1) The Supply Category Id is not 0, 
        /// (2) The Name string is empty,
        /// (3) The Name string is longer than 50 characters,
        /// (4) The Name string is the same as another Supply Category's Name,
        /// (5) Dish Requirements, Supply Links or Supplier Stocks are null,
        /// (6) Dish Requirements, Supply Links or Supplier Stocks
        /// are not empty collections.
        /// </remarks>
        // PUT: api/SupplyCategories/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupplyCategory(int id, SupplyCategory supplyCategory)
        {
            _logger.LogDebug("Supply Categories Controller trying to update " +
                "Supply Category with ID: " + id);
            if (!await SupplyCategoryExists(id))
            {
                _logger.LogDebug("Supply Categories Controller found no " +
                    "Supply Category with ID: " + id);
                return NotFound();
            }

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
            {
                _logger.LogDebug("Supply Categories Controller found " +
                    "'supplyCategory' parameter to violate update constraints");
                return BadRequest();
            }

                //Validation

                var oldCat = await _context.SupplyCategories.FindAsync(id);

            oldCat.Name = supplyCategory.Name;

                await _context.SaveChangesAsync();

            _logger.LogDebug("Supply Categories Controller successfully updated " +
                "Supply Category with ID: " + id);
            return NoContent();
        }

        /// <summary>
        /// Create Supply Category
        /// </summary>
        /// <param name="supplyCategory">A Supply Category object
        /// to be added to the Database</param>
        /// <remarks>
        /// Creates a new Supply Category and inserts it into the Database,
        /// using the values from the 'supplyCategory' parameter.
        /// Only the Name of the Supply Category may be set.
        /// For its Dish Requirements, Supply Links and Supplie Stocks relationships,
        /// their respective API controllers must be used.
        /// The Supply Category Id may not be set. The Databse will automatically
        /// assign it a new Id, equivalent to the last Supply Category Id 
        /// in the Database + 1.
        /// The given parameter of type Supply Category may contain a Name.
        /// Additionaly, it may also hold empty collections for
        /// Dish Requirements, Supply Links and Supplier Stocks, as well as a
        /// Supply Category Id of 0.
        /// If the Supply Category parameter is incorrect, Bad Request is returned.
        /// Supply Category parameter errors:
        /// (1) The Supply Category Id is not 0, 
        /// (2) The Name string is empty,
        /// (3) The Name string is longer than 50 characters,
        /// (4) The Name string is the same as another Supply Category's Name,
        /// (5) Dish Requirements, Supply Links or Supplier Stocks are null,
        /// (6) Dish Requirements, Supply Links or Supplier Stocks are not empty collections.
        /// </remarks>
        // POST: api/SupplyCategories
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<SupplyCategory>> CreateSupplyCategory(SupplyCategory supplyCategory)
        {
            _logger.LogDebug("Supply Categories Controller trying to " +
                "create new Supply Category.");

            //Validation

            if (supplyCategory.SupplyCategoryId != 0 ||
                string.IsNullOrEmpty(supplyCategory.Name) ||
                string.IsNullOrWhiteSpace(supplyCategory.Name) ||
                supplyCategory.Name.Length > 50 ||
                await _context.SupplyCategories
                    .AnyAsync(c => c.Name.ToUpper() == supplyCategory.Name.ToUpper()) ||
                supplyCategory.DishRequirements == null ||
                supplyCategory.DishRequirements.Count != 0 ||
                supplyCategory.SupplierStocks == null ||
                supplyCategory.SupplierStocks.Count != 0 ||
                supplyCategory.SupplyLinks == null ||
                supplyCategory.SupplyLinks.Count != 0)
            {
                _logger.LogDebug("Supply Categories Controller found 'supplyCategory' " +
                    "parameter to violate creation constraints.");
                return BadRequest();
            }

            //Validation

            _context.SupplyCategories.Add(supplyCategory);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Supply Categories Controller successfully created new " +
                "Supply Category at ID: " + supplyCategory.SupplyCategoryId);
            return CreatedAtAction("GetSupplyCategoryBasic", new { id = supplyCategory.SupplyCategoryId }, supplyCategory);
        }

        /// <summary>
        /// Delete Supply Category
        /// </summary>
        /// <param name="id">Id of target Supply Category</param>
        /// <remarks>
        /// Removes a Supply Category from the Database.
        /// If there is no Supply Category, whose Id matches the given 'id' parameter, 
        /// Not Found is returned.
        /// If the desired Supply Category is the last in the Database, and it has no
        /// Supply Link, Supplier Stock or Dish Requirement relationships, it is removed
        /// from the Database and its Supply Category Id will be used by the next Supply Category
        /// to be inserted.
        /// If the desired Supply Category is not the last in the Database, and it has no
        /// Supply Link, Supplier Stock or Dish Requirement relationships, it is swapped with the last
        /// Supply Category in the Database, taking on its relationships and Name, before the last
        /// Supply Category is removed, freeing up the last Supply Category's Id for
        /// the next Supply Category to be inserted.
        /// If the desired Supply Category has any Supply Link, Suplier Stocks or
        /// Dish Requirement relationships, it is turned into a blank Supply Category, so as not to
        /// violate its relationships.
        /// Its Name will be changed to reflect that fact.
        /// </remarks>
        // DELETE: api/SupplyCategories/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<SupplyCategory>> DeleteSupplyCategory(int id)
        {
            _logger.LogDebug("Supply Categories Controller trying to delete " +
                "Supply Category with ID: " + id);
            var supplyCategory = await _context.SupplyCategories.FindAsync(id);
            if (supplyCategory == null)
            {
                _logger.LogDebug("Supply Categories Controller found no " +
                    "Supply Category with ID: " + id);
                return NotFound();
            }

            // If it has Stocks, Links or Requirements, turn it into a blank category
            if (await _context.SupplyLinks.AnyAsync(l => l.SupplyCategoryId == id)||
                await _context.DishRequirements.AnyAsync(r=>r.SupplyCategoryId == id)||
                await _context.SupplierStocks.AnyAsync(s=>s.SupplyCategoryId==id))
            {
                _logger.LogDebug("Supply Categories Controller found Supply Category " +
                    "with ID: " + id + " to have Supply Link, Supplier Stock or Dish " +
                    "Requirement relationships. As such, it will be turned into a blank " +
                    "Supply Category instead of being deleted.");

                supplyCategory.Name = string.Format("Blank {0}:{1}",
                    DateTime.Now.ToShortDateString(), id);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Supply Categories Controller successfully turned " +
                    "Supply Category with ID: " + id + " into a blank Supply Category.");
                return Ok();
            }

            var lastCatId = await _context.SupplyCategories.CountAsync();

            if (id == lastCatId)/*If it's last, delete it*/
            {
                _logger.LogDebug("Supply Categories Controller found Supply Category " +
                    "with ID: " + id + " to have no Supply Link, Supplier Stock or Dish " +
                    "Requirement relationships and to be last in the Database. As such, " +
                    "it will be deleted.");

                _context.SupplyCategories.Remove(supplyCategory);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[SupplyCategories]', RESEED, " + (lastCatId - 1) + ")");
                await _context.SaveChangesAsync();

                _logger.LogDebug("Supply Categories Controller successfully deleted " +
                    "Supply Category with ID: " + id);
                return Ok();
            }

            //If it's not last, swap it with last and del that

            _logger.LogDebug("Supply Categories Controller found Supply Category " +
                "with ID: " + id + " to have no Supply Link, Supplier Stock or Dish " +
                "Requirement relationships and to appear before the last Database entry. " +
                "As such, it will be swapped with the last Supply Category before that one " +
                "is deleted.");

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

            _logger.LogDebug("Supply Categories Controller successfully turned " +
                "Supply Category with ID: " + id + " into the last Supply Category, then " +
                "deleted that one.");
            return Ok();
        }

        [NonAction]
        private async Task<bool> SupplyCategoryExists(int id) => await _context.SupplyCategories.FindAsync(id) != null;
    }
}
