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
    public class MenusController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;

        public MenusController(FoodChainsDbContext context) => _context = context;

        #region Gets

        #region Basic

        //// GET: api/Menus
        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<dynamic>>> GetMenus()
        //{
        //    var menuIds = await _context.Menus.Select(m=>m.MenuId).ToArrayAsync();

        //    var dbLocations = await _context.Locations.Select(l => 
        //        new
        //        {
        //            l.MenuId,
        //            Line = string.Format("({0}) {1}, {2}{3}, {4}",
        //                            l.LocationId, l.AbreviatedCountry,
        //                            l.AbreviatedState == "N/A" ? string.Empty : l.AbreviatedState + ", ",
        //                            l.City, l.Street)
        //        }).ToListAsync();

        //    var menuItems = await _context.MenuItems.Select(i => new
        //    {
        //        i.DishId, i.MenuId
        //    }).ToArrayAsync();
        //    var dishes = await _context.Dishes.Select(d => new { d.DishId, d.Name }).ToArrayAsync();

        //    var menus = new dynamic[menuIds.Length];

        //    for (int i = 0; i < menuIds.Length; i++)
        //    {
        //        var menuId = menuIds[i];

        //        var dishIds = menuItems.Where(i => i.MenuId == menuId).Select(i=>i.DishId).ToArray();
        //        var menuDishes = dishes.Where(d =>dishIds.Contains( d.DishId)).Select(d=>d.Name).ToArray();

        //        var locations = dbLocations.Where(l => l.MenuId == menuId).Select(l=>l.Line).ToArray();

        //        menus[i] = new { locations, dishes = menuDishes };
        //    }
        //    return menus; 
        //}

        // GET: api/Menus/5

        //[HttpGet("{id}")]
        //public async Task<ActionResult<dynamic>> GetMenu(int id)
        //{
        //    var menu = await _context.Menus.FindAsync(id);
        //    if (menu == null) return NotFound();

        //    var dbLocations = await _context.Locations.Where(l => l.MenuId == id).ToArrayAsync();
        //    var dishIds = await _context.MenuItems.Where(i => i.MenuId == id).Select(i => i.DishId).ToArrayAsync();
        //    var dbDishes = await _context.Dishes.Where(d => dishIds.Contains(d.DishId)).ToArrayAsync();

        //    var locations = dbLocations.Select(l => new {
        //        l.LocationId,
        //        l.AbreviatedCountry,
        //        l.AbreviatedState,
        //        l.City,
        //        l.Street
        //    });

        //    var dishes = dbDishes.Select(d => new
        //    {
        //        d.DishId,
        //        d.Name
        //    });

        //    return new { locations, dishes }; 
        //}
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<Menu>> GetMenuBasic(int id)
        {
            var menu = await _context.Menus.FindAsync(id);
            if (menu == null) return NotFound();

            return menu;
        }

        // GET: api/Menus/5/requirements
        [HttpGet("{id}/requirements")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetRequirements(int id)
        {
            var menu = await _context.Menus.FindAsync(id);
            if (menu == null) return NotFound();

            var dishIds = await _context.MenuItems
                .Where(i => i.MenuId == id)
                .Select(i => i.DishId)
                .Distinct()
                .ToArrayAsync();

            var reqIds = await _context.DishRequirements
                .Where(r => dishIds.Contains(r.DishId))
                .Select(r => r.SupplyCategoryId)
                .Distinct()
                .ToArrayAsync();

            return await _context.SupplyCategories
                .Where(c => reqIds.Contains(c.SupplyCategoryId))
                .Select(c => new
                {
                    c.SupplyCategoryId,
                    c.Name
                })
                .ToArrayAsync();
        }

        // GET: api/Menus/5/locations
        [HttpGet("{id}/locations")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetLocations(int id)
        {
            var menu = await _context.Menus.FindAsync(id);
            if (menu == null) return NotFound();

            return await _context.Locations.Where(l => l.MenuId == id).Select(l => new
            {
                l.LocationId,
                l.AbreviatedCountry,
                l.AbreviatedState,
                l.City,
                l.Street
            }).ToArrayAsync();
        }

        // GET: api/Menus/5/dishes
        [HttpGet("{id}/dishes")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetDishes(int id)
        {
            var menu = await _context.Menus.FindAsync(id);
            if (menu == null) return NotFound();

            var dishIds = await _context.MenuItems.Where(i => i.MenuId == id).Select(i => i.DishId).ToArrayAsync();
            var dishes = (await _context.Dishes.Where(d => dishIds.Contains(d.DishId)).ToArrayAsync());
            return dishes.Select(d => d.Name).ToArray();
        }

        #endregion

        #endregion

        // POST: api/Menus
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<Menu>> CreateMenu(Menu menu)
        {
            //Validation
            if (menu.MenuId != 0 ||
                menu.Locations == null || menu.Locations.Count != 0 ||
                menu.MenuItems == null || menu.MenuItems.Count != 0) return BadRequest();
            //Validation

            _context.Menus.Add(menu);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetMenuBasic", new { id = menu.MenuId }, menu);
        }

        // DELETE: api/Menus/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Menu>> DeleteMenus(int id)
        {
            var menu = await _context.Menus.FindAsync(id);
            if (menu == null) return NotFound();

            // If it has MenuItems do nothing
            if (await _context.MenuItems.AnyAsync(i =>i.MenuId==id)||
                await _context.Locations.AnyAsync(l=>l.MenuId==id))
                return BadRequest("Menu contains Dishes or has Locations that depend on it."+
                    " Must be emptied before deletion.");

            var lastMenuId = await _context.Menus.CountAsync();

            if (id == lastMenuId)/*If it's last, delete it*/
            {
                _context.Menus.Remove(menu);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[Menus]', RESEED, " + (lastMenuId - 1) + ")");
                await _context.SaveChangesAsync();
                return Ok();
            }

            //If it's not last, swap it with last and del that

            /*Migrate MenuItems and Locations from Last Menu to menuId*/
            var items = await _context.MenuItems
                .Where(i => i.MenuId == lastMenuId)
                .ToArrayAsync();
            var locations = await _context.Locations
                .Where(l => l.MenuId == lastMenuId)
                .ToArrayAsync();

            foreach (var itm in items)
                itm.MenuId = id;

            foreach (var loc in locations)
                loc.MenuId = id;

            await _context.SaveChangesAsync();

            /*Swap and del last*/
            var lastMenu = await _context.Menus.FindAsync(lastMenuId);
            await _context.SaveChangesAsync();

            _context.Menus.Remove(lastMenu);
            await _context.SaveChangesAsync();
            await _context.Database.ExecuteSqlRawAsync
                ("DBCC CHECKIDENT('[Menus]', RESEED, " + (lastMenuId - 1) + ")");
            await _context.SaveChangesAsync();
            return Ok();
        }

        private async Task<bool> MenuExists(int id) => (await _context.Menus.FindAsync(id)) != null;
    }
}
