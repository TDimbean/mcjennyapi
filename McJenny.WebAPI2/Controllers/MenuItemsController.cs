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
    public class MenuItemsController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;
        public MenuItemsController(FoodChainsDbContext context) => _context = context;

        #region Gets

        #region Basic

        // GET: api/MenuItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> GetMenuItems()
        {
            var items = await _context.MenuItems.ToArrayAsync();

            var dishIds = items.Select(i => i.DishId).Distinct().ToArray();

            var dishes = await _context.Dishes
                .Where(d => dishIds.Contains(d.DishId))
                .Select(d => new { d.DishId, d.Name })
                .ToArrayAsync();

            var result = new string[items.Length];
            for (int i = 0; i < items.Length; i++)
                result[i] = string.Format("Item [{0}]: ({1}) {2} available on Menu: {3}",
                    items[i],
                    items[i].DishId,
                    dishes.SingleOrDefault(d => d.DishId == items[i].DishId).Name,
                    items[i].MenuId);

            return result;
        }

        // GET: api/MenuItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> GetMenuItem(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);

            if (menuItem == null) return NotFound();

            var dish = await _context.Dishes
                .Select(d => new { d.DishId, d.Name })
                .FirstAsync(d => d.DishId == menuItem.DishId);

            if (dish == null) return NotFound();

            return string.Format("({0}) {1} available on Menu: {2}",
                    menuItem,
                    menuItem.DishId,
                    dish.Name,
                    menuItem.MenuId);
        }

        // GET: api/MenuItems/5/basic
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<MenuItem>> GetMenuItemBasic(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);

            if (menuItem == null) return NotFound();

            return menuItem;
        }

    //    {
    //    #region Navigational

    //    // GET: api/MenuItems/Locations
    //    [HttpGet("{id}/locations")]
    //    public async Task<ActionResult<IEnumerable<string>>> GetMenuItemLocations(int id)
    //    {
    //        if (!await MenuItemExists(id)) return NotFound();
    //        var menuId = (await _context.MenuItems.FindAsync(id)).MenuId;

    //        var result = await _context.Locations
    //            .Where(l => l.MenuId == menuId)
    //            .Select(l => string.Format("({0}) {1}, {2}{3}, {4}",
    //                l.LocationId,
    //                l.AbreviatedCountry,
    //                l.AbreviatedState == "N/A" ? string.Empty :
    //                l.AbreviatedState + ", ",
    //                l.City,
    //                l.Street
    //            ))
    //            .ToArrayAsync();

    //        return result;
    //    }

    //    #endregion
    //}
    #endregion

    #endregion

    // POST: api/MenuItems
    // To protect from overposting attacks, please enable the specific properties you want to bind to, for
    // more details see https://aka.ms/RazorPagesCRUD.
    [HttpPost]
        public async Task<ActionResult<MenuItem>> CreateMenuItem(MenuItem menuItem)
        {
            //Validation

            if (menuItem.MenuItemId != 0 ||
                menuItem.Dish != null || menuItem.Menu != null ||
                await _context.Menus.FindAsync(menuItem.MenuId) == null ||
                await _context.Dishes.FindAsync(menuItem.DishId) == null ||
                await _context.MenuItems.AnyAsync(i =>
                    i.DishId == menuItem.DishId &&
                    i.MenuId == menuItem.MenuId))
                return BadRequest();

            //Validation

            _context.MenuItems.Add(menuItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetMenuItemBasic", new { id = menuItem.MenuItemId }, menuItem);
        }

        // DELETE: api/MenuItems/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<MenuItem>> DeleteMenuItem(int id)
        {
            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem == null) return NotFound();

            var lastItmId = await _context.MenuItems.CountAsync();

            if (id == lastItmId)
            {
                _context.MenuItems.Remove(menuItem);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[MenuItems]', RESEED, "+(lastItmId-1)+")");
                await _context.SaveChangesAsync();
                return Ok();
            }

            var lastItm = await _context.MenuItems.FindAsync(lastItmId);
            menuItem.DishId = lastItm.DishId;
            menuItem.MenuId = lastItm.MenuId;

            _context.MenuItems.Remove(lastItm);
            await _context.SaveChangesAsync();
            await _context.Database.ExecuteSqlRawAsync
                ("DBCC CHECKIDENT('[MenuItems]', RESEED, " + (lastItmId - 1) + ")");
            await _context.SaveChangesAsync();
            return Ok();
        }

        private async Task<bool> MenuItemExists(int id)
            => await _context.MenuItems.AnyAsync(e => e.MenuItemId == id);
    }
}
