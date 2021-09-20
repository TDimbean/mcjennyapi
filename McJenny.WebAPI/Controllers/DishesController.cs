using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using McJenny.WebAPI.Data.Models;
using System.Text;
using McJenny.WebAPI.Helpers;
using McJenny.WebAPI.Data;
using Microsoft.Extensions.Logging;

namespace McJenny.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DishesController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;
        private readonly ILogger<DishesController> _logger;

        public DishesController(FoodChainsDbContext context, ILogger<DishesController> logger)
        {
            _context = context;
            _logger = logger;
            _logger.LogDebug("Logger injected into Dishes Controller");
        }

        #region Gets

        #region Basic

        /// <summary>
        /// Get All Dishes
        /// </summary>
        /// <remarks>
        /// Get A list of all the Dishes.
        /// A Dish has a Name and a DishId.
        /// It has Dish Requirements, Many to Many relationships between itself
        /// and Supply Categories
        /// It has Menu Items, Many to Many relationships between itself 
        /// and Menus
        /// </remarks>
        /// <returns></returns>
        // GET: api/Dishes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetDishes()
        {
            _logger.LogDebug("Dishes Controller fetching all Dishes");
            return await _context.Dishes
                .Select(d => new { ID = d.DishId, d.Name })
                .ToArrayAsync();
        }

        /// <summary>
        /// Get Dish
        /// </summary>
        /// <remarks>
        /// Gets the Dish whose DishId matches the given Id 
        /// Contains the Dishe's name and the Supply Categories it requires.
        /// If the given Id does not belong to any Dish
        /// Not Found is returned instead
        /// </remarks>
        /// <returns></returns>
        // GET: api/Dishes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<dynamic>> GetDish(int id)
        {
            _logger.LogDebug("Dishes Controller getting Dish with ID: "+id);

            var dish = string.Empty;
            try
            {
                dish = (await _context.Dishes.FindAsync(id)).Name;
            }
            catch (NullReferenceException ex)
            {
                //LOG
            _logger.LogDebug("Dishes Controller found no Dish with ID: "+id);
                return NotFound();
            }
            catch (Exception ex)
            {
                //LOG
            _logger.LogDebug("Dishes Controller encountered unexpected error."+
                " Details: "+ex.Message);
                return BadRequest();
            }

            var supCatIds = await _context.DishRequirements
                .Where(r => r.DishId == id)
                .Select(r => r.SupplyCategoryId)
                .ToArrayAsync();

            var supplyCategories = await _context.SupplyCategories
                .Where(c => supCatIds.Contains(c.SupplyCategoryId))
                .Select(c => new { ID = c.SupplyCategoryId, c.Name })
                .ToArrayAsync();

            _logger.LogDebug("Dishes Controller returning Dish with ID: "+id);
            return new { dish, supplyCategories };
        }

        /// <summary>
        /// Get Dish Basic
        /// </summary>
        /// <remarks>
        /// Returns the Dish corresponding to the given Id 
        /// Contains the DishId, Name and two empty collections, the Dish Requirements and Menu Items
        /// If the specified Id doesn't match any existing Dish,
        /// Not Found will be returned instead
        /// </remarks>
        /// <returns></returns>
        // GET: api/Dishes/5/basic
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<Dish>> GetDishBasic(int id)
        {
            var dish = await _context.Dishes.FindAsync(id);
            _logger.LogDebug("Dishes Controller fetching basic Dish with ID: "+id);
            if (dish == null)
            {
                _logger.LogDebug("Dishes Controller found no Dish with ID: "+id);
                return NotFound();
            }

            _logger.LogDebug("Dishes Controller returning basic Dish with ID: "+id);
            return dish;
        }

        /// <summary>
        /// Get Dish Locations
        /// </summary>
        /// <remarks>
        /// Returns the Locations where the Dish corresponding to the given Id is available
        /// as a list of strings.
        /// If no Dish with the specified Id exists,
        /// Not Found is returned instead
        /// </remarks>
        /// <returns></returns>
        // GET: api/Dishes/5/locations
        [HttpGet("{id}/locations")]
        public async Task<ActionResult<IEnumerable<string>>> GetLocations(int id)
        {
            _logger.LogDebug("Dishes Controller fetching Locations for Dish with ID: " + id);
            if (!await DishExists(id))
            {
                _logger.LogDebug("Dishes Controller found no Dish with ID: " + id);
                return NotFound();
            }

            var menuIds = (await GetMenus(id)).Value;

            _logger.LogDebug("Dishes Controller returning Locations for Dish with ID: " + id);
            return (await _context.Locations
                .Where(l => menuIds
                    .Contains(l.MenuId))
                .ToArrayAsync())
                .Select(l => string.Format(
                  "({0}) {1}, {2}{3} {4}",
                  l.LocationId,
                  l.AbreviatedCountry,
                  l.AbreviatedState == "N/A" ? string.Empty : l.AbreviatedState + ", ",
                  l.City, l.Street)).ToArray();
        }

        /// <summary>
        /// Get Dish Requirements
        /// </summary>
        /// <remarks>
        /// Returns the Supply Categories required to prepare the
        /// Dish corresponding to the given Id, based on the Dish's
        /// Dish Requirement relationships.
        /// If no Dish with the specified Id exists,
        /// Not Found is returned instead.
        /// </remarks>
        /// <returns></returns>
        // GET: api/Dishes/5/requirements
        [HttpGet("{id}/requirements")]
        public async Task<ActionResult<IEnumerable<string>>> GetRequirements(int id)
        {
            _logger.LogDebug("Dishes Controller fetching Requirements for Dish with ID: " + id);
            if (!await DishExists(id))
            {
                _logger.LogDebug("Dishes Controller found no Dish with ID: " + id);
                return NotFound();
            }

            var catIds = await _context.DishRequirements
                .Where(r => r.DishId == id)
                .Select(r => r.SupplyCategoryId)
                .ToArrayAsync();


            _logger.LogDebug("Dishes Controller returning Supply Categories"+
                " required for Dish with ID: " + id);
            return await _context.SupplyCategories
                .Where(c => catIds.Contains(c.SupplyCategoryId))
                .Select(c => string.Format("({0}) {1}", c.SupplyCategoryId, c.Name))
                .ToArrayAsync();
        }

        /// <summary>
        /// Get Dish Menus
        /// </summary>
        /// <remarks>
        /// Returns the Menus which contain the Dish
        /// corresponding to the given Id, based on the Dish's
        /// Menu Item relationships.
        /// If no Dish with the specified Id exists,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/Dishes/5/menus
        [HttpGet("{id}/menus")]
        public async Task<ActionResult<IEnumerable<int>>> GetMenus(int id)
        {
            _logger.LogDebug("Dishes Controller fetching Menus containing Dish with ID: " + id);
            if (!await DishExists(id))
            {
                _logger.LogDebug("Dishes Controller found no Dish with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Dishes Controller returning Menus containing Dish with ID: " + id);
            return await _context.MenuItems
                .Where(i => i.DishId == id)
                .Select(i => i.MenuId)
                .Distinct()
                .ToArrayAsync();
        }

        #endregion

        #region Advanced

        /// <summary>
        /// Get Dishes Queried
        /// </summary>
        /// <remarks>
        /// Returns a list of Dishes according to the specified query
        /// Query Keywords: Filter, SortBy(Optional: Desc), PgInd and PgSz.
        /// Keywords are followed by '=', a string parameter, and, if further keywords
        /// are to be added afterwards, the Ampersand Separator
        /// Filter: Only return Dishes where the Name
        /// contains the given string.
        /// SortBy: Dishes will be ordered in ascending order by either their Name,
        /// if the string parameter is 'name', or by their ID, if
        /// any other string parameter is given.
        /// Desc: If SortBy is used, and the string parameter is 'true',
        /// Dishes wil be returned in Descending Order.
        /// PgInd: If the following int is greater than 0 and the PgSz 
        /// keyword is also present, Dishes will be paged and
        /// page 'PgInd' of size 'PgSz' will be returned.
        /// PgSz: If the following int is greater than 0 and the PgInd 
        /// keyword is also present, Dishes will be paged and
        /// page 'PgInd' of size 'PgSz' will be returned.
        /// Any of the Keywords can be used or ommited.
        /// Using a keyword more than once will result in only the first keyword being recognized.
        /// If a keyword is followed by an incorrectly formatted parameter, the keyword will be ignored.
        /// Order of Query Operations: Filter > Sort > Pagination
        /// Any improperly formatted text will be ignored.
        /// </remarks>
        // GET: api/Dishes/query
        [HttpGet("query:{query}")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetDishesQueried(string query)
        {
            _logger.LogDebug("Dishes Controller looking up query...");
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

            var returnString = new StringBuilder(!isPaged?string.Empty:
                "Paginated: Page "+pgInd+" of Size "+pgSz);
            if(isSorted)
            {
                if (returnString.Length > 0) returnString.Insert(0, " and ");
                returnString.Insert(0, "Sorted: By " + sortBy + " In " +
                    (desc ? "Descending" : "Ascending") + " Order");
            }
            if(isFiltered)
            {
                if (isSorted && isPaged) returnString.Insert(0, ", ");
                else if (isSorted|| isPaged) returnString.Insert(0," and ");
                returnString.Insert(0, "Filtered By: " + filter);
            }
            _logger.LogDebug("Dishes Controller returning "+
                (!isPaged&&!isSorted&&!isFiltered?"all ":string.Empty)+
                "Dishes"+
                (returnString.Length==0?string.Empty:
                " "+returnString.ToString())+".");
            return (isPaged, isSorted, isFiltered)
            switch
            {
                (false, false, true) => await GetDishesFiltered(filter),
                (false, true, false) => await GetDishesSorted(sortBy, desc),
                (true, false, false) => await GetDishesPaged(pgInd, pgSz),
                (false, true, true) => await GetDishesSorted(sortBy, desc,
                                    (await GetDishesFiltered(filter)).Value),
                (true, false, true) => await GetDishesPaged(pgInd, pgSz,
                                    (await GetDishesFiltered(filter)).Value),
                (true, true, false) => await GetDishesPaged(pgInd, pgSz,
                                    (await GetDishesSorted(sortBy, desc)).Value),
                (true, true, true) => await GetDishesPaged(pgInd, pgSz,
                                    (await GetDishesSorted(sortBy, desc,
                                    (await GetDishesFiltered(filter)).Value)).Value),
                _ => await GetDishes(),
            };
        }

        [NonAction]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetDishesFiltered(string filter, IEnumerable<dynamic> col = null)
        => col == null ?
                (dynamic)await _context.Dishes
                 .Select(d => new { d.DishId, d.Name })
                 .Where(d => d.Name.ToUpper()
                     .Contains(filter.ToUpper()))
                 .ToArrayAsync() :
                (dynamic)col
                     .Where(d => d.Name.ToUpper()
                     .Contains(filter.ToUpper()))
                        .ToArray();

        [NonAction]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetDishesPaged(int pgInd, int pgSz, IEnumerable<dynamic> col = null)
            => col == null ?
            (dynamic)await _context.Dishes
                .Select(d => new { d.DishId, d.Name })
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync() :
            (dynamic)col
                .Select(d => new { d.DishId, d.Name })
                .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArray();

        [NonAction]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetDishesSorted(string sortBy, bool? desc, IEnumerable<dynamic> col = null)
            => col == null ?
                sortBy.ToUpper() switch
                {
                    "NAME" => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Dishes
                            .Select(d => new { d.DishId, d.Name })
                            .OrderByDescending(d => d.Name).ToArrayAsync() :
                        (dynamic)await _context.Dishes
                            .Select(d => new { d.DishId, d.Name })
                            .OrderBy(d => d.Name).ToArrayAsync(),
                    _ => desc.GetValueOrDefault() ?
                        (dynamic)await _context.Dishes
                            .Select(d => new { d.DishId, d.Name })
                            .OrderByDescending(d => d.DishId).ToArrayAsync() :
                        (dynamic)await _context.Dishes
                            .Select(d => new { d.DishId, d.Name })
                            .ToArrayAsync()
                } :
                  sortBy.ToUpper() switch
                  {
                      "NAME" => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .Select(d => new { d.DishId, d.Name })
                              .OrderByDescending(d => d.Name).ToArray() :
                          (dynamic)col
                              .Select(d => new { d.DishId, d.Name })
                              .OrderBy(d => d.Name).ToArray(),
                      _ => desc.GetValueOrDefault() ?
                          (dynamic)col
                              .Select(d => new { d.DishId, d.Name })
                              .OrderByDescending(d => d.DishId).ToArray() :
                          (dynamic)col
                              .Select(d => new { d.DishId, d.Name })
                              .ToArray()
                  };

        #endregion

        #endregion

        /// <summary>
        /// Update Dish
        /// </summary>
        /// <param name="id">The Id of the Dish that must be updated</param>
        /// <param name="dish">A Dish object whose properties are to be transfered to the existing Dish</param>
        /// <remarks>
        /// Updates the values of an existing Dish, whose Id corresponds to the given parameter,
        /// using the values from a dish parameter.
        /// Only the Name of the Dish may be updated through the API.
        /// For its relationships(Dish Requirements and Menu Items) their respective APIs
        /// must be used.
        /// The Dish Id may not be updated.
        /// The given parameter of type Dish must contain a Name, an empty collection
        /// of Dish Requirements and one of Menu Items and a Dish Id of 0
        /// The Dish Id Parameter must correspond to an existing Dish, otherwise
        /// Not Found is returned
        /// If the Dish parameter is incorrect, Bad Request is returned
        /// Dish parameter errors:
        /// (1) The Dish Id is not 0, 
        /// (2) The Name string is empty,
        /// (3) The Name string is longer than 50 characters,
        /// (4) The Name string is the same as another Dish's Name,
        /// (5) Dish Requirements or Menu Items are null
        /// (6) Dish Requirements or Menu Items are not empty collections
        /// </remarks>
        // PUT: api/Dishes/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDish(int id, Dish dish)
        {
            _logger.LogDebug("Dishes Controller trying to Update Dish with ID: " + id);
            var oldDish = await _context.Dishes.FindAsync(id);
            if (oldDish == null)
            {
                _logger.LogDebug("Dishes Controller found no Dish with ID: " + id);
                return NotFound();
            }

            // Validation

            if (dish.DishId != 0 ||
                (!string.IsNullOrEmpty(dish.Name) &&
                    (string.IsNullOrWhiteSpace(dish.Name) ||
                    dish.Name.Length > 50)) ||
                await _context.Dishes
                    .AnyAsync
                    (d => d.Name != null &&
                    d.Name.ToUpper() == dish.Name.ToUpper() &&
                    d.DishId != id) ||
                dish.DishRequirements == null || dish.DishRequirements.Count != 0 ||
                dish.MenuItems == null || dish.MenuItems.Count != 0)
            {
                _logger.LogDebug("Dishes Controller failed to Update Dish with ID: " + id + ". " +
                    "The given 'dish' parameter violates the update constraints.");
                return BadRequest();
            }

            // Validation


            oldDish.Name = dish.Name;
            await _context.SaveChangesAsync();

            _logger.LogDebug("Dishes Controller succesfully updated Dish with ID: " + id);
            return NoContent();
        }

        /// <summary>
        /// Create Dish
        /// </summary>
        /// <param name="dish">A Dish object to be added to the Database</param>
        /// <remarks>
        /// Creates a new Dish and inserts it into the Database
        /// using the values from a dish parameter.
        /// Only the Name of the Dish may be set.
        /// For its relationships(Dish Requirements and Menu Items) their respective APIs
        /// must be used.
        /// The Dish Id may not be set. The Databse will automatically
        /// assign it a new Id, equivalent to the last Dish Id in the Database + 1.
        /// The given parameter of type Dish must contain a Name, an empty collection
        /// of Dish Requirements and one of Menu Items and a Dish Id of 0
        /// If the Dish parameter is incorrect, Bad Request is returned
        /// Dish parameter errors:
        /// (1) The Dish Id is not 0, 
        /// (2) The Name string is empty,
        /// (3) The Name string is longer than 50 characters,
        /// (4) The Name string is the same as another Dish's Name,
        /// (5) Dish Requirements or Menu Items are null,
        /// (6) Dish Requirements or Menu Items are not empty collections.
        /// </remarks>
        // POST: api/Dishes
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<Dish>> CreateDish(Dish dish)
        {
            _logger.LogDebug("Dishes Controller trying to add new Dish to Database.");

            // Validation

            if (dish.DishId != 0 ||
                string.IsNullOrEmpty(dish.Name) ||
                string.IsNullOrWhiteSpace(dish.Name) ||
                dish.Name.Length > 50 ||
                await _context.Dishes.AnyAsync(d => d.Name.ToUpper() == dish.Name.ToUpper()) ||
                dish.MenuItems == null ||
                dish.MenuItems.Count != 0 ||
                dish.DishRequirements == null ||
                dish.DishRequirements.Count != 0)
            {
                _logger.LogDebug("Dishes Controller failed to Create new Dish. The given 'dish' " +
                    "parameter violates some of the creation constraints.");
                return BadRequest();
            }
            
            // Validation

            _context.Dishes.Add(dish);
            await _context.SaveChangesAsync();

            #region Deleted

            ////Val BIts
            ///dish.DishRequirements
            //    .Any(r=>r.SupplyCategory!=null||r.Dish!=null||!_context.SupplyCategories
            //    .Any(c=>c.SupplyCategoryId==r.SupplyCategoryId)) ||
            //dish.MenuItems
            //    .Any(i => i.Menu!=null||i.Dish!=null||!_context.Menus
            //    .Any(c => c.MenuId == i.MenuId)))

            //// Check if any of the desired Nav Props are dupes
            //var menuIds = dish.MenuItems.Select(i => i.MenuId).ToArray();
            //var catIds = dish.DishRequirements.Select(r => r.SupplyCategoryId).ToArray();

            //if (menuIds.Count() > menuIds.Distinct().Count()||
            //    catIds.Count() > catIds.Distinct().Count()) 
            //    return BadRequest();

            ///Manually Adds DishRequirements and MenuItems
            //if (dish.DishRequirements.Any(x=>true))
            //    foreach (var req in dish.DishRequirements)
            //    {
            //        if ((!await _context.SupplyCategories
            //                .AnyAsync(c => 
            //                c.SupplyCategoryId == req.SupplyCategoryId))||
            //            (await _context.DishRequirements
            //                .AnyAsync(r=>
            //                r.SupplyCategoryId==req.SupplyCategoryId&&r.DishId==dish.DishId)))
            //            return BadRequest();

            //        _context.DishRequirements.Add(new DishRequirement 
            //        {
            //            SupplyCategoryId=req.SupplyCategoryId, DishId=dish.DishId 
            //        });
            //    }

            //if (dish.MenuItems.Any(x=>true))
            //    foreach (var item in dish.MenuItems)
            //    {
            //        if ((!await _context.Menus.AnyAsync(m => m.MenuId == item.MenuId)) ||
            //            (await _context.MenuItems.AnyAsync(i => i.MenuId == item.MenuId &&
            //            i.DishId == dish.DishId))) return BadRequest();

            //        _context.MenuItems.Add(new MenuItem
            //        {
            //            MenuId = item.MenuId,
            //            DishId = dish.DishId
            //        });
            //    }
            #endregion

            _logger.LogDebug("Dishes Controller succesfully creted new Dish with ID: " + dish.DishId);
            return CreatedAtAction("GetDishBasic", new { id = dish.DishId }, dish);
        }

        /// <summary>
        /// Delete Dish
        /// </summary>
        /// <param name="id">Id of Dish set for Deletion</param>
        /// <remarks>
        /// Removes a Dish from the Database.
        /// If there is no Dish, whose Id matches the given Id parameter, 
        /// Not Found is returned
        /// If the desired Dish is the last in the Database, and it has no
        /// Menu Item or Dish Requirement relationships, it is removed from the Database
        /// and its Dish Id will be used by the next Dish to be inserted.
        /// If the desired Dish is not the last in the Database, and it has no
        /// Menu Item or Dish Requirement relationships, it is swapped with the last
        /// Dish in the Database before it is removed, giving the last Dish in the Database
        /// the Id of the deleted Dish and freeing up the last Dish's Id for
        /// the next Dish to be inserted.
        /// If the desired Dish has any Menu Item or Dish Requirement relationships,
        /// it is turned into a blank Dish, so a not to violate its relationships.
        /// Its Name will be changed to reflect that fact.
        /// </remarks>
        // DELETE: api/Dishes/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Dish>> DeleteDish(int id)
        {
            _logger.LogDebug("Dishes Controller trying to delete Dish with ID: " + id);

            var dish = await _context.Dishes.FindAsync(id);
            if (dish == null)
            {
                _logger.LogDebug("Dishes Controller found no Dish with ID: " + id);
                return NotFound();
            }

            // If it has MenuItems or Requirements, turn it into a blank dish
            if (await _context.MenuItems.AnyAsync(i => i.DishId == id)||
                await _context.DishRequirements.AnyAsync(r=>r.DishId == id))
            {
                _logger.LogDebug("Dishes Controller found that Dish with ID: " + id + ". " +
                    "contains Menu Items and/or Dish Requirements. Instead of Deletion, " +
                    "the Dish will be turned into a blank Dish.");
                dish.Name = string.Format("Unknown {0}:{1}",
                    DateTime.Now.ToShortDateString(), id);
                await _context.SaveChangesAsync();
                _logger.LogDebug("Dishes Controller turned Dish with ID: " + id + " into a blank dish.");
                return Ok();
            }

            var lastDishId = await _context.Dishes.CountAsync();

            if (id == lastDishId)/*If it's last, delete it*/
            {
                _logger.LogDebug("Dish Controller found that Dish with ID: " + id + " has no" +
                    " relationships and is the last in the Database and will remove it.");
                _context.Dishes.Remove(dish);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[Dishes]', RESEED, " + (lastDishId - 1) + ")");
                await _context.SaveChangesAsync();
                _logger.LogDebug("Dishes Controller deleted DIsh with ID: " + id);
                return Ok();
            }

            //If it's not last, swap it with last and del that

            _logger.LogDebug("Dishes Controller found that Dish with ID: " + id + " has no " +
                "relationships and appears before the last Dish. It will switch places with the last Dish.");
            /*Migrate Requirements and MenuItems from Last Position to posId*/
            var reqForLastDish = await _context.DishRequirements
                    .Where(r => r.DishId == lastDishId)
                    .ToArrayAsync();

            var itemsWithLastDish = await _context.MenuItems
                .Where(i => i.DishId == lastDishId)
                .ToArrayAsync();

            foreach (var req in reqForLastDish)
                req.DishId = id;

            foreach (var itm in itemsWithLastDish)
                itm.DishId = id;

            await _context.SaveChangesAsync();

            /*Swap and del last*/
            var lastDish = await _context.Dishes.FindAsync(lastDishId);
            dish.Name = lastDish.Name;

            _context.Dishes.Remove(lastDish);
            await _context.SaveChangesAsync();
            await _context.Database.ExecuteSqlRawAsync
                ("DBCC CHECKIDENT('[Dishes]', RESEED, " + (lastDishId - 1) + ")");
            await _context.SaveChangesAsync();
            _logger.LogDebug("Dishes Controller succesfully turned Dish with ID: " + id + " into " +
                "the last Dish, and removed the last Dish from its position.");
            return Ok();
        }

        [NonAction]
        private async Task<bool> DishExists(int id) => await _context.Dishes.FindAsync(id)!=null;
    }
}
