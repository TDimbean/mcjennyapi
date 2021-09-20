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
    public class MenusController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;
        private readonly ILogger<MenusController> _logger;

        public MenusController(FoodChainsDbContext context,
            ILogger<MenusController> logger)
        {
            _context = context;
            _logger = logger;
            _logger.LogDebug("Logger injected into Menus Controller.");
        }

        #region Gets

        /// <summary>
        /// Get Menu Basic
        /// </summary>
        /// <param name="id">Id of desired Menu</param>
        /// <remarks>
        /// Returns a Menu object of the Menu whose Menu Id matches the 
        /// given 'id' parameter.
        /// If no such Menu exists, Not Found will be returned instead.
        /// </remarks>
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
            _logger.LogDebug("Menus Controller fetching Menu with ID: " + id);

            var menu = await _context.Menus.FindAsync(id);
            if (menu == null)
            {
                _logger.LogDebug("Menus Controller found no Menu with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Menus Controller returning basic Menu with ID: " + id);
            return menu;
        }

        /// <summary>
        /// Get Menu Requirements
        /// </summary>
        /// <param name="id">Id of desired Menu</param>
        /// <remarks>
        /// Returns a collection of the Supply Categories required to
        /// sustain the Menu whose Menu Id matches the given 'id' parameter, 
        /// formated to contain each Supply Category's Id and Name.
        /// If no Menu whose Id matches the given 'id' parameter exists,
        /// Not Found will be returned instead.
        /// </remarks>
        // GET: api/Menus/5/requirements
        [HttpGet("{id}/requirements")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetRequirements(int id)
        {
            _logger.LogDebug("Menus Controller fetching Requirements " +
                "for Menu with ID: " + id);
            var menu = await _context.Menus.FindAsync(id);
            if (menu == null)
            {
                _logger.LogDebug("Menus Controller found no Menu with ID: " + id);
                return NotFound();
            }

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

            _logger.LogDebug("Menus Controller returning Requirements for " +
                "Menu with ID: " + id);
            return await _context.SupplyCategories
                .Where(c => reqIds.Contains(c.SupplyCategoryId))
                .Select(c => new
                {
                    c.SupplyCategoryId,
                    c.Name
                })
                .ToArrayAsync();
        }

        /// <summary>
        /// Get Menu Locations
        /// </summary>
        /// <param name="id">Id of desired Menu</param>
        /// <remarks>
        /// Returns a collection of Locations that stock the Menu whose
        /// Menu Id matches the given 'id' parameter, formatted to include
        /// the Location Id, Abreviated Country, Abreviated State, City and Street 
        /// of each Location.
        /// If no Menu whose Menu Id matches the given 'id' parameter,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/Menus/5/locations
        [HttpGet("{id}/locations")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetLocations(int id)
        {
            _logger.LogDebug("Menus Controller fetching Locations for Menu " +
                "with ID: " + id);
            var menu = await _context.Menus.FindAsync(id);
            if (menu == null)
            {
                _logger.LogDebug("Menus Controller found no Menu with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Menus Controller returning Locations for Menu " +
                "with ID: " + id);
            return await _context.Locations.Where(l => l.MenuId == id).Select(l => new
            {
                l.LocationId,
                l.AbreviatedCountry,
                l.AbreviatedState,
                l.City,
                l.Street
            }).ToArrayAsync();
        }

        /// <summary>
        /// Get Menu Dishes
        /// </summary>
        /// <param name="id">Id of desired Menu</param>
        /// <remarks>
        /// Returns a collection of strings containing the Names of
        /// the Dishes available on the Menu
        /// whose Menu Id matches the given 'id' parameter.
        /// If no Menu whose Menu Id matches the 'id' parameter exists,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/Menus/5/dishes
        [HttpGet("{id}/dishes")]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetDishes(int id)
        {
            _logger.LogDebug("Menus Controller fetching Dishes for " +
                "Menu with ID: " + id);

            var menu = await _context.Menus.FindAsync(id);
            if (menu == null)
            {
                _logger.LogDebug("Menus Controller found no Menu with ID: " + id);
                return NotFound();
            }

            var dishIds = await _context.MenuItems
                .Where(i => i.MenuId == id)
                .Select(i => i.DishId)
                .ToArrayAsync();
            var dishes = await _context.Dishes
                .Where(d => dishIds.Contains(d.DishId))
                .ToArrayAsync();

            _logger.LogDebug("Menus Controller returning Dishes for " +
                "Menu with ID: " + id);
            return dishes.Select(d => d.Name).ToArray();
        }

        #endregion

        /// <summary>
        /// Create Menu
        /// </summary>
        /// <param name="menu">Menu to be inserted into the Database</param>
        /// <remarks>
        /// Creates a new Menu and inserts it into the Database.
        /// A Menu object is just a container for a Menu Id to be used in relationships.
        /// The Menu Id is automatically set by the Database, according to the last
        /// Menu's Id + 1.
        /// As such, the 'menu' parameter should be an empty Menu object.
        /// If the 'menu' parameter is incorrect, Bad Request is returned.
        /// Menu parameter errors:
        /// (1) The Menu Id is not 0, 
        /// (2) Locations or Dishes are not empty collections,
        /// (3) Locations or Dishes are null. 
        /// </remarks>
        // POST: api/Menus
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<Menu>> CreateMenu(Menu menu)
        {
            _logger.LogDebug("Menus Controller trying to create new Menu.");

            //Validation

            if (menu.MenuId != 0 ||
                menu.Locations == null || menu.Locations.Count != 0 ||
                menu.MenuItems == null || menu.MenuItems.Count != 0)
            {
                _logger.LogDebug("Menus Controller found provided 'menu' " +
                    "parameter to violate creation constraints.");
                return BadRequest();
            }
            
            //Validation

            _context.Menus.Add(menu);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Menus Controller successfully created new Menu" +
                " at ID: " + menu.MenuId);
            return CreatedAtAction("GetMenuBasic", new { id = menu.MenuId }, menu);
        }

        /// <summary>
        /// Delete Menu
        /// </summary>
        /// <param name="id">Id of target Menu</param>
        /// <remarks>
        /// Removes a Menu from the Database.
        /// If there is no Menu, whose Id matches the given 'id' parameter, 
        /// Not Found is returned
        /// If the desired Menuis the last in the Database, and it has no
        /// Menu Item relationships or Locations, it is removed from the Database
        /// and its Menu Id will be used by the next Menu to be inserted.
        /// If the desired Menu is not the last in the Database, and it has no
        /// Menu Item relationships or Locations, it is swapped with the last
        /// Menu in the Database, taking on its Location and Menu Items, while
        /// before the last Menu is removed, freeing up the last Menu's Id for
        /// the next Menu to be inserted.
        /// If the desired Menu has any Menu Item relationships or Locations,
        /// it cannot be deleted, and Bad Request will be returned.
        /// </remarks>
        // DELETE: api/Menus/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<Menu>> DeleteMenu(int id)
        {
            _logger.LogDebug("Menus Controller trying to delete Menu with ID: " + id);

            var menu = await _context.Menus.FindAsync(id);
            if (menu == null)
            {
                _logger.LogDebug("Menus Controller found no Menu with ID: " + id);
                return NotFound();
            }

            // If it has MenuItems do nothing
            if (await _context.MenuItems.AnyAsync(i => i.MenuId == id) ||
                await _context.Locations.AnyAsync(l => l.MenuId == id))
            {
                _logger.LogDebug("Menus Controller found Menu with ID: " + id +
                    " to have Dishes or Locations that depend on it. As such, it will" +
                    " not delete this menu.");
                return BadRequest("Menu contains Dishes or has Locations that depend on it." +
                    " Must be emptied before deletion.");
            }

            var lastMenuId = await _context.Menus.CountAsync();

            if (id == lastMenuId)/*If it's last, delete it*/
            {
                _logger.LogDebug("Menus Controller found Menu with ID: "+id+
                    "to have no relationships and be the last in the Database. "+
                    "As such, it will be deleted.");

                _context.Menus.Remove(menu);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[Menus]', RESEED, " + (lastMenuId - 1) + ")");
                await _context.SaveChangesAsync();

                _logger.LogDebug("Menus Controller successfully deleted Menu with " +
                    "ID :" + id);
                return Ok();
            }

            //If it's not last, swap it with last and del that

            _logger.LogDebug("Menus Controller found Menu with ID: " + id +
                " to have no relationships and appear before the last position " +
                "in the Database. As such, it will swap it with the last Menu, then" +
                " delete that one.");

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

            _logger.LogDebug("Menus Controller successfully swapped Menu with ID: " +
                id + " with the last Menu, then deleted the last Menu.");
            return Ok();
        }

        [NonAction]
        private async Task<bool> MenuExists(int id) => (await _context.Menus.FindAsync(id)) != null;
    }
}
