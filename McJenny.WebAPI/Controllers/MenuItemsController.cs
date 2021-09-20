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
    /// <summary>
    /// Menu Items API Controller
    /// </summary>
    /// <remarks>
    /// Manages the Menu Item Relationships between Menus and Dishes
    /// A Menu Item is a Many to Many relationship between
    /// Menus and Dishes
    /// Indicating which Dishes appear on which Menus
    /// Does not include a PUT/PATCH method
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    public class MenuItemsController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;
        private readonly ILogger<MenuItemsController> _logger;

        public MenuItemsController(FoodChainsDbContext context,
            ILogger<MenuItemsController> logger)
        {
            _context = context;
            _logger = logger;
            _logger.LogDebug("Logger injected into Menu Items Controller.");
        }

        #region Gets

        /// <summary>
        /// Get All Menu Items
        /// </summary>
        /// <remarks>
        /// Get A list of all the Relations between Menus and Dishes, 
        /// If a Menu Item exists between two entities, it means that Menu contains that Dish
        /// It is a Many to Many relationship
        /// A Menu may contain as many Dishes as desired,
        /// Likewise a Dish may appear on just as many Menus
        /// </remarks>
        /// <returns>200 Ok</returns>
        // GET: api/MenuItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> GetMenuItems()
        {
            _logger.LogDebug("Menu Items Controller fetching all Menu Items...");

            var items = await _context.MenuItems.ToArrayAsync();

            var dishIds = items.Select(i => i.DishId).Distinct().ToArray();

            var dishes = await _context.Dishes
                .Where(d => dishIds.Contains(d.DishId))
                .Select(d => new { d.DishId, d.Name })
                .ToArrayAsync();

            var result = new string[items.Length];
            for (int i = 0; i < items.Length; i++)
                result[i] = string.Format("Item [{0}]: Dish({1}) \"{2}\" available on Menu: {3}",
                    items[i],
                    items[i].DishId,
                    dishes.SingleOrDefault(d => d.DishId == items[i].DishId).Name,
                    items[i].MenuId);

            _logger.LogDebug("Menu Items Controller returning all Menu Items.");
            return result;
        }

        /// <summary>
        /// Get Menu Item
        /// </summary>
        /// <remarks>
        /// Gets a statement about a Menu Item of the specified Id
        /// Explains which Dish is contained in which Menu, according to the Menu Item relanshionship
        /// Specifies both the Name and ID of the Dish
        /// As well as the ID of the Menu
        /// If it returns NotFound, it means that no relation of that Id exists
        /// </remarks>
        /// <returns></returns>
        // GET: api/MenuItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> GetMenuItem(int id)
        {
            _logger.LogDebug("Menu Items Controller fetching Menu Item with ID: " + id);
            var menuItem = await _context.MenuItems.FindAsync(id);

            if (menuItem == null)
            {
                _logger.LogDebug("Menu Items Controller found no Menu Item with ID: " + id);
                return NotFound();
            }

            var dish = await _context.Dishes
                .Select(d => new { d.DishId, d.Name })
                .FirstAsync(d => d.DishId == menuItem.DishId);

            if (dish == null) return NotFound();

            _logger.LogDebug("Menu Items Controller returning formated Menu Item with ID: " + id);
            return string.Format("(MenuItem {0}): Dish {1} \"{2}\" available on Menu {3}",
                    menuItem.MenuItemId,
                    menuItem.DishId,
                    dish.Name,
                    menuItem.MenuId);
        }

        /// <summary>
        /// Get Menu Item
        /// </summary>
        /// <remarks>
        /// Returns a Menu Item of given Id 
        /// Contains the Id of the Menu Item, the Dish and the Menu
        /// If it returns NotFound, it means that no relation of that Id exists
        /// </remarks>
        /// <returns></returns>
        // GET: api/MenuItems/5/basic
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<MenuItem>> GetMenuItemBasic(int id)
        {
            _logger.LogDebug("Menu Items Controller fetching Menu Item with ID: " + id);
            var menuItem = await _context.MenuItems.FindAsync(id);

            if (menuItem == null)
            {
                _logger.LogDebug("Menu Items Controller found no Menu Item with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Menu Items Controller returning Basic Menu Item with ID: " + id);
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

        /// <summary>
        /// Post Menu Item
        /// </summary>
        /// <remarks>
        /// Creates a Menu Item
        /// A Menu Item is a Link between a Menu and a Dish
        /// If a Menu Item Relationship exists betwee a Menu and a Dish
        /// It means that Dish can be found on that Menu
        /// It is a Many to Many Relationship
        /// Meaning a Menu may contain as many Items as desired
        /// Likewise a Dish may appear on just as many Menus
        /// The Object required must contain a MenuId and a DishId
        /// Both must come from existing Menus and Dishes
        /// They must not appear in another Menu Item
        /// As duplicates would be redundant
        /// The Object must not contain the MenuItemId
        /// That is assigned automatically
        /// It will be the Id of the last MenuItem+1
        /// </remarks>
        /// <returns></returns>
        // POST: api/MenuItems
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<MenuItem>> CreateMenuItem(MenuItem menuItem)
        {
            _logger.LogDebug("Menu Items Controller trying to create new Menu Item...");

            //Validation

            if (menuItem.MenuItemId != 0 ||
                menuItem.Dish != null || menuItem.Menu != null ||
                await _context.Menus.FindAsync(menuItem.MenuId) == null ||
                await _context.Dishes.FindAsync(menuItem.DishId) == null ||
                await _context.MenuItems.AnyAsync(i =>
                    i.DishId == menuItem.DishId &&
                    i.MenuId == menuItem.MenuId))
            {
                _logger.LogDebug("Menu Items Controller found provided 'menuItem' " +
                    "parameter to violate creation constraints.");
                return BadRequest();
            }

            //Validation

            _context.MenuItems.Add(menuItem);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Menu Items Controller successfully created new Menu " +
                "Item at ID: " + menuItem.MenuItemId);
            return CreatedAtAction("GetMenuItemBasic", new { id = menuItem.MenuItemId }, menuItem);
        }

        /// <summary>
        /// Delete Menu Item
        /// </summary>
        /// <remarks>
        /// Removes a MenuItem relationship 
        /// Once a MenuItem is removed
        /// The Dish corresponding to its DishId will no longer appear on the
        /// Menu corresponding to its MenuId
        /// The specified Id must belong to an existing MenuItem
        /// Once deleted, the Database will run an Identity Check and the Id 
        /// That the Menu Item previously occupied will be taken by the 
        /// Next Menu Item to be inserted into the Database
        /// </remarks>
        /// <returns></returns>
        // DELETE: api/MenuItems/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<MenuItem>> DeleteMenuItem(int id)
        {
            _logger.LogDebug("Menu Items Controller trying to delete Menu Item " +
                "with ID: " + id);

            var menuItem = await _context.MenuItems.FindAsync(id);
            if (menuItem == null)
            {
                _logger.LogDebug("Menu Items Controller found no Menu Item " +
                    "with ID: " + id);
                return NotFound();
            }

            var lastItmId = await _context.MenuItems.CountAsync();

            if (id == lastItmId)
            {
                _logger.LogDebug("Menu Items Controller found that Menu Item " +
                    "with ID: " + id + " is last in the Database.");

                _context.MenuItems.Remove(menuItem);
                await _context.SaveChangesAsync();
                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT('[MenuItems]', RESEED, "+(lastItmId-1)+")");
                await _context.SaveChangesAsync();

                _logger.LogDebug("Menu Items Controller successfully deleted " +
                    "Menu Item with ID: " + id);
                return Ok();
            }

            _logger.LogDebug("Menu Items Controller found that Menu Item with ID: " +
                id + " is not the last in the Database. At such, it will be swaped with " +
                "the last Menu Item then that one will be deleted.");

            var lastItm = await _context.MenuItems.FindAsync(lastItmId);
            menuItem.DishId = lastItm.DishId;
            menuItem.MenuId = lastItm.MenuId;

            _context.MenuItems.Remove(lastItm);
            await _context.SaveChangesAsync();
            await _context.Database.ExecuteSqlRawAsync
                ("DBCC CHECKIDENT('[MenuItems]', RESEED, " + (lastItmId - 1) + ")");
            await _context.SaveChangesAsync();

            _logger.LogDebug("Menu Items Controller turned Menu Item with ID: " + id +
                "into the last Menu Item and deleted that.");
            return Ok();
        }

        [NonAction]
        private async Task<bool> MenuItemExists(int id)
            => await _context.MenuItems.AnyAsync(e => e.MenuItemId == id);
    }
}
