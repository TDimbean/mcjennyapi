using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using McJenny.WebAPI.Data.Models;
using System.Text;
using McJenny.WebAPI.Helpers;
using McJenny.WebAPI.Data;
using Microsoft.Extensions.Logging;

namespace McJenny.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DishesController : ControllerBase
    {
        //private readonly FoodChainsDbContext _context;
        //private readonly ILogger<DishesController> _logger;

        //public DishesController(FoodChainsDbContext context, ILogger<DishesController> logger)
        //{
        //    _context = context;
        //    _logger = logger;
        //    if (_logger!=null)_logger.LogDebug("Logger injected into Dishes Controller");
        //}

        #region Gets

        #region Basic

        // GET: api/Dishes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetDishes()
        => new[] { "dish1", "dish2" };

        // GET: api/Dishes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<dynamic>> GetDish(int id)
            => new { Id = id, Dish = "Cherry Tart" };

        // GET: api/Dishes/5/basic
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<Dish>> GetDishBasic(int id)
            => new Dish { DishId = id, Name = "BASIC Cherry Tart" };

        [NonAction]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetPaged() => new[] { "dishes", "paged" };
        [NonAction]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetSorted() => new[] { "dishes", "sorted" };
        [NonAction]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetFiltered() => new[] { "dishes", "filtered" };

        // GET: api/Dishes/query
        [HttpGet("query:{query}")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetDishesQueried(string query)
        {
            query = query.ToUpper();
            switch (query)
            {
                case "PAGED": return await GetPaged();
                case "FILTERED":return  await GetFiltered();
                case "SORTED": return   await GetSorted();
                default: return await GetDishes();
            }
        }

        #endregion

        #endregion
    }
}

//        // GET: api/Dishes
//        [HttpGet]
//        public async Task<ActionResult<IEnumerable<dynamic>>> GetDishes()
//        => await _context.Dishes.Select(d => new { ID = d.DishId, d.Name }).ToArrayAsync();

//        // GET: api/Dishes/5
//        [HttpGet("{id}")]
//        public async Task<ActionResult<dynamic>> GetDish(int id)
//        {
//            var dish = string.Empty;
//            try
//            {
//                dish = (await _context.Dishes.FindAsync(id)).Name;
//            }
//            catch (NullReferenceException ex)
//            {
//                //LOG
//                return NotFound();
//            }
//            catch (Exception ex)
//            {
//                //LOG
//                return BadRequest();
//            }

//            var supCatIds = await _context.DishRequirements
//                .Where(r => r.DishId == id)
//                .Select(r => r.SupplyCategoryId)
//                .ToArrayAsync();

//            var supplyCategories = await _context.SupplyCategories
//                .Where(c => supCatIds.Contains(c.SupplyCategoryId))
//                .Select(c => new { ID = c.SupplyCategoryId, c.Name })
//                .ToArrayAsync();

//            return new { dish, supplyCategories };
//        }

//        // GET: api/Dishes/5/basic
//        [HttpGet("{id}/basic")]
//        public async Task<ActionResult<Dish>> GetDishBasic(int id)
//        {
//            var dish = await _context.Dishes.FindAsync(id);
//            if (dish == null) return NotFound();

//            return dish;
//        }

//        // GET: api/Dishes/5/locations
//        [HttpGet("{id}/locations")]
//        public async Task<ActionResult<IEnumerable<string>>> GetLocations(int id)
//        {
//            if (!await DishExists(id)) return NotFound();

//            var menuIds = (await GetMenus(id)).Value;

//            return (await _context.Locations
//                .Where(l => menuIds
//                    .Contains(l.MenuId))
//                .ToArrayAsync())
//                .Select(l => string.Format(
//                  "({0}) {1}, {2}{3} {4}",
//                  l.LocationId,
//                  l.AbreviatedCountry,
//                  l.AbreviatedState == "N/A" ? string.Empty : l.AbreviatedState + ", ",
//                  l.City, l.Street)).ToArray();
//        }

//        // GET: api/Dishes/5/requirements
//        [HttpGet("{id}/requirements")]
//        public async Task<ActionResult<IEnumerable<string>>> GetRequirements(int id)
//        {
//            if (!await DishExists(id)) return NotFound();

//            var catIds = await _context.DishRequirements
//                .Where(r => r.DishId == id)
//                .Select(r => r.SupplyCategoryId)
//                .ToArrayAsync();

//            return await _context.SupplyCategories
//                .Where(c => catIds.Contains(c.SupplyCategoryId))
//                .Select(c => string.Format("({0}) {1}", c.SupplyCategoryId, c.Name))
//                .ToArrayAsync();
//        }

//        // GET: api/Dishes/5/menus
//        [HttpGet("{id}/menus")]
//        public async Task<ActionResult<IEnumerable<int>>> GetMenus(int id)
//        {
//            if (!await DishExists(id)) return NotFound();

//            return await _context.MenuItems
//                .Where(i => i.DishId == id)
//                .Select(i => i.MenuId)
//                .Distinct()
//                .ToArrayAsync();
//        }

//        #endregion

//        #region Advanced

//        // GET: api/Dishes/query
//        [HttpGet("query:{query}")]
//        public async Task<ActionResult<IEnumerable<dynamic>>> GetDishesQueried(string query)
//        {
//            var quer = query.ToUpper().Replace("QUERY:", string.Empty);

//            bool isPaged, isFiltered, isSorted, desc;

//            var pgInd = 0;
//            var pgSz = 0;
//            var filter = string.Empty;
//            var sortBy = string.Empty;

//            isPaged = quer.Contains("PGIND=") && quer.Contains("PGSZ=");
//            isFiltered = quer.Contains("FILTER=");
//            isSorted = quer.Contains("SORTBY=");
//            desc = quer.Contains("DESC=TRUE");

//            if (isPaged)
//            {
//                var indStr = quer.QueryToParam("PGIND");
//                var szStr = quer.QueryToParam("PGSZ");

//                var indParses = int.TryParse(indStr, out pgInd);
//                var szParses = int.TryParse(szStr, out pgSz);

//                if (!indParses || !szParses) isPaged = false;
//                else
//                {
//                    pgInd = Math.Abs(pgInd);
//                    pgSz = pgSz == 0 ? 10 : Math.Abs(pgSz);
//                }
//            }

//            if (isFiltered) filter = quer.QueryToParam("FILTER");
//            if (isSorted) sortBy = quer.QueryToParam("SORTBY");

//            return (isPaged, isSorted, isFiltered)
//switch
//            {
//                (false, false, true) => await GetDishesFiltered(filter),
//                (false, true, false) => await GetDishesSorted(sortBy, desc),
//                (true, false, false) => await GetDishesPaged(pgInd, pgSz),
//                (false, true, true) => await GetDishesSorted(sortBy, desc,
//(await GetDishesFiltered(filter)).Value),
//                (true, false, true) => await GetDishesPaged(pgInd, pgSz,
//(await GetDishesFiltered(filter)).Value),
//                (true, true, false) => await GetDishesPaged(pgInd, pgSz,
//(await GetDishesSorted(sortBy, desc)).Value),
//                (true, true, true) => await GetDishesPaged(pgInd, pgSz,
//(await GetDishesSorted(sortBy, desc,
//(await GetDishesFiltered(filter)).Value)).Value),
//                _ => await GetDishes(),
//            };
//        }

//        public async Task<ActionResult<IEnumerable<dynamic>>> GetDishesFiltered(string filter, IEnumerable<dynamic> col = null)
//        => col == null ?
//                (dynamic)await _context.Dishes
//                 .Select(d => new { d.DishId, d.Name })
//                 .Where(d => d.Name.ToUpper()
//                     .Contains(filter.ToUpper()))
//                 .ToArrayAsync() :
//                (dynamic)col
//                     .Where(d => d.Name.ToUpper()
//                     .Contains(filter.ToUpper()))
//                        .ToArray();

//        public async Task<ActionResult<IEnumerable<dynamic>>> GetDishesPaged(int pgInd, int pgSz, IEnumerable<dynamic> col = null)
//            => col == null ?
//            (dynamic)await _context.Dishes
//                .Select(d => new { d.DishId, d.Name })
//                .Skip(pgSz * (pgInd - 1))
//                .Take(pgSz)
//                .ToArrayAsync() :
//            (dynamic)col
//                .Select(d => new { d.DishId, d.Name })
//                .Skip(pgSz * (pgInd - 1))
//                .Take(pgSz)
//                .ToArray();

//        public async Task<ActionResult<IEnumerable<dynamic>>> GetDishesSorted(string sortBy, bool? desc, IEnumerable<dynamic> col = null)
//            => col == null ?
//                sortBy.ToUpper() switch
//                {
//                    "NAME" => desc.GetValueOrDefault() ?
//                        (dynamic)await _context.Dishes
//                            .Select(d => new { d.DishId, d.Name })
//                            .OrderByDescending(d => d.Name).ToArrayAsync() :
//                        (dynamic)await _context.Dishes
//                            .Select(d => new { d.DishId, d.Name })
//                            .OrderBy(d => d.Name).ToArrayAsync(),
//                    _ => desc.GetValueOrDefault() ?
//                        (dynamic)await _context.Dishes
//                            .Select(d => new { d.DishId, d.Name })
//                            .OrderByDescending(d => d.DishId).ToArrayAsync() :
//                        (dynamic)await _context.Dishes
//                            .Select(d => new { d.DishId, d.Name })
//                            .ToArrayAsync()
//                } :
//                  sortBy.ToUpper() switch
//                  {
//                      "NAME" => desc.GetValueOrDefault() ?
//                          (dynamic)col
//                              .Select(d => new { d.DishId, d.Name })
//                              .OrderByDescending(d => d.Name).ToArray() :
//                          (dynamic)col
//                              .Select(d => new { d.DishId, d.Name })
//                              .OrderBy(d => d.Name).ToArray(),
//                      _ => desc.GetValueOrDefault() ?
//                          (dynamic)col
//                              .Select(d => new { d.DishId, d.Name })
//                              .OrderByDescending(d => d.DishId).ToArray() :
//                          (dynamic)col
//                              .Select(d => new { d.DishId, d.Name })
//                              .ToArray()
//                  };

//        #endregion

//        #endregion

//        // PUT: api/Dishes/5
//        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
//        // more details see https://aka.ms/RazorPagesCRUD.
//        [HttpPut("{id}")]
//        public async Task<IActionResult> UpdateDish(int id, Dish dish)
//        {
//            // Validation

//            if (dish.DishId != 0 ||
//                (!string.IsNullOrEmpty(dish.Name) &&
//                    (string.IsNullOrWhiteSpace(dish.Name) ||
//                    dish.Name.Length > 50)) ||
//                await _context.Dishes
//                    .AnyAsync
//                    (d =>d.Name!=null&&
//                    d.Name.ToUpper() == dish.Name.ToUpper() &&
//                    d.DishId != id) ||
//                dish.DishRequirements == null || dish.DishRequirements.Count != 0 ||
//                dish.MenuItems == null|| dish.MenuItems.Count != 0)
//                return BadRequest();

//            // Validation

//            //var oldDish = (await GetDishBasic(id)).Value;
//            var oldDish = await _context.Dishes.FindAsync(id);
//            if (oldDish == null) return NotFound();

//            oldDish.Name = dish.Name;
//            await _context.SaveChangesAsync();

//            return NoContent();
//        }


//        // POST: api/Dishes
//        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
//        // more details see https://aka.ms/RazorPagesCRUD.
//        [HttpPost]
//        public async Task<ActionResult<Dish>> CreateDish(Dish dish)
//        {
//            // Validation

//            if (dish.DishId != 0 || 
//                string.IsNullOrEmpty(dish.Name)||
//                string.IsNullOrWhiteSpace(dish.Name)||
//                dish.Name.Length>50||
//                await _context.Dishes.AnyAsync(d=>d.Name.ToUpper()==dish.Name.ToUpper())||
//                dish.MenuItems==null||
//                dish.MenuItems.Count!=0||
//                dish.DishRequirements==null||
//                dish.DishRequirements.Count!=0)
//                    return BadRequest();

//            // Validation

//            _context.Dishes.Add(dish);
//            await _context.SaveChangesAsync();

//            #region Deleted

//            ////Val BIts
//            ///dish.DishRequirements
//            //    .Any(r=>r.SupplyCategory!=null||r.Dish!=null||!_context.SupplyCategories
//            //    .Any(c=>c.SupplyCategoryId==r.SupplyCategoryId)) ||
//            //dish.MenuItems
//            //    .Any(i => i.Menu!=null||i.Dish!=null||!_context.Menus
//            //    .Any(c => c.MenuId == i.MenuId)))

//            //// Check if any of the desired Nav Props are dupes
//            //var menuIds = dish.MenuItems.Select(i => i.MenuId).ToArray();
//            //var catIds = dish.DishRequirements.Select(r => r.SupplyCategoryId).ToArray();

//            //if (menuIds.Count() > menuIds.Distinct().Count()||
//            //    catIds.Count() > catIds.Distinct().Count()) 
//            //    return BadRequest();

//            ///Manually Adds DishRequirements and MenuItems
//            //if (dish.DishRequirements.Any(x=>true))
//            //    foreach (var req in dish.DishRequirements)
//            //    {
//            //        if ((!await _context.SupplyCategories
//            //                .AnyAsync(c => 
//            //                c.SupplyCategoryId == req.SupplyCategoryId))||
//            //            (await _context.DishRequirements
//            //                .AnyAsync(r=>
//            //                r.SupplyCategoryId==req.SupplyCategoryId&&r.DishId==dish.DishId)))
//            //            return BadRequest();

//            //        _context.DishRequirements.Add(new DishRequirement 
//            //        {
//            //            SupplyCategoryId=req.SupplyCategoryId, DishId=dish.DishId 
//            //        });
//            //    }

//            //if (dish.MenuItems.Any(x=>true))
//            //    foreach (var item in dish.MenuItems)
//            //    {
//            //        if ((!await _context.Menus.AnyAsync(m => m.MenuId == item.MenuId)) ||
//            //            (await _context.MenuItems.AnyAsync(i => i.MenuId == item.MenuId &&
//            //            i.DishId == dish.DishId))) return BadRequest();

//            //        _context.MenuItems.Add(new MenuItem
//            //        {
//            //            MenuId = item.MenuId,
//            //            DishId = dish.DishId
//            //        });
//            //    }
//            #endregion

//            return CreatedAtAction("GetDishBasic", new { id = dish.DishId }, dish);
//        }

//        // DELETE: api/Dishes/5
//        [HttpDelete("{id}")]
//        public async Task<ActionResult<Dish>> DeleteDish(int id)
//        {
//            var dish = await _context.Dishes.FindAsync(id);
//            if (dish == null) return NotFound();

//            // If it has MenuItems or Requirements, turn it into a blank dish
//            if (await _context.MenuItems.AnyAsync(i => i.DishId == id)||
//                await _context.DishRequirements.AnyAsync(r=>r.DishId == id))
//            {
//                dish.Name = string.Format("Unknown {0}:{1}",
//                    DateTime.Now.ToShortDateString(), id);
//                await _context.SaveChangesAsync();
//                return Ok();
//            }

//            var lastDishId = await _context.Dishes.CountAsync();

//            if (id == lastDishId)/*If it's last, delete it*/
//            {
//                _context.Dishes.Remove(dish);
//                await _context.SaveChangesAsync();
//                await _context.Database.ExecuteSqlRawAsync
//                    ("DBCC CHECKIDENT('[Dishes]', RESEED, " + (lastDishId - 1) + ")");
//                await _context.SaveChangesAsync();
//                return Ok();
//            }

//            //If it's not last, swap it with last and del that

//            /*Migrate Requirements and MenuItems from Last Position to posId*/
//            var reqForLastDish = await _context.DishRequirements
//                    .Where(r => r.DishId == lastDishId)
//                    .ToArrayAsync();

//            var itemsWithLastDish = await _context.MenuItems
//                .Where(i => i.DishId == lastDishId)
//                .ToArrayAsync();

//            foreach (var req in reqForLastDish)
//                req.DishId = id;

//            foreach (var itm in itemsWithLastDish)
//                itm.DishId = id;

//            await _context.SaveChangesAsync();

//            /*Swap and del last*/
//            var lastDish = await _context.Dishes.FindAsync(lastDishId);
//            dish.Name = lastDish.Name;

//            _context.Dishes.Remove(lastDish);
//            await _context.SaveChangesAsync();
//            await _context.Database.ExecuteSqlRawAsync
//                ("DBCC CHECKIDENT('[Dishes]', RESEED, " + (lastDishId - 1) + ")");
//            await _context.SaveChangesAsync();
//            return Ok();
//        }

//        private async Task<bool> DishExists(int id) => await _context.Dishes.FindAsync(id)!=null;
//    }
//}
