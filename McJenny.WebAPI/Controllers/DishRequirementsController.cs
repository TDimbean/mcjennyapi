using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using McJenny.WebAPI.Data.Models;
using Microsoft.Data.SqlClient;
using System.Text;
using Microsoft.Extensions.Logging;

namespace McJenny.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DishRequirementsController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;
        private readonly ILogger<DishRequirementsController> _logger;
        public DishRequirementsController(FoodChainsDbContext context,
            ILogger<DishRequirementsController> logger)
        { 
            _context = context; 
            _logger = logger;
            _logger.LogDebug("Logger injected into Dish Requirements Controller.");
        }

        #region Gets

        #region Basic

        /// <summary>
        /// Get Dish Requirements
        /// </summary>
        /// <remarks>
        /// Returns a collection of strings explaining the Dish Requirement relationships
        /// between Dishes and the Supply Categories required for their preparation.
        /// </remarks>
        // GET: api/DishRequirements
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> GetDishRequirements()
        {
            _logger.LogDebug("Dish Requirements Controller fetching all Dish Requirements.");

            var requirements = await _context.DishRequirements
                .Select(r=>new {r.DishRequirementId, r.DishId, r.SupplyCategoryId })
                .ToArrayAsync();

            var dishIds = requirements.Select(r => r.DishId).Distinct().ToArray();
            var catIds = requirements.Select(r => r.SupplyCategoryId).Distinct().ToArray();

            var dishes = await _context.Dishes
                .Where(d => dishIds.Contains(d.DishId))
                .Select(d => new { d.Name, d.DishId })
                .ToArrayAsync();

            var cats = await _context.SupplyCategories
                .Where(c=> catIds.Contains(c.SupplyCategoryId))
                .Select(c => new { c.Name, c.SupplyCategoryId })
                .ToArrayAsync();

            var result = new string[requirements.Length];
            for (int i = 0; i < requirements.Length; i++)
                result[i] = string.Format("Requirement [{0}]: ({1}) {2} requires ({3}) {4}",
                    requirements[i].DishRequirementId,
                    requirements[i].DishId,
                    dishes.SingleOrDefault(d => d.DishId == requirements[i].DishId).Name,
                    requirements[i].SupplyCategoryId,
                    cats.SingleOrDefault(c => c.SupplyCategoryId == requirements[i].SupplyCategoryId).Name);                    ;

            _logger.LogDebug("Dish Requirements Controller returning " + result.Length + " results.");
            return result;
        }

        /// <summary>
        /// Get Dish Requirement
        /// </summary>
        /// <param name="id">Id of target Dish Requirement</param>
        /// <remarks>
        /// Returns a string explaining the Dish Requirement relationship 
        /// between a Dish and Supply Category whose IDs match those of
        /// the Dish Requirement.
        /// If no Dish Requirement matches the given 'id' parameter,
        /// Not Found is returned.
        /// </remarks>
        // GET: api/DishRequirements/5
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> GetDishRequirement(int id)
        {
            _logger.LogDebug("Dish Requirements Controller fetching Dish Requirement with ID: " + id);
            var dishRequirement = await _context.DishRequirements.FindAsync(id);
            if (dishRequirement == null)
            {
                _logger.LogDebug("Dish Requirements Controller found no Dish Requirement with ID: " + id);
                return NotFound();
            }

            var dish = (await _context.Dishes.FindAsync(dishRequirement.DishId)).Name;
            var cat = (await _context.SupplyCategories.FindAsync(dishRequirement.SupplyCategoryId)).Name;

            if (dish == null || cat == null) return NotFound();

            _logger.LogDebug("Dish Requirements Controller returning Dish Requirement with ID: " + id);
            return dish + "requires" + cat;
        }

        /// <summary>
        /// Get Dish Requirement Basic
        /// </summary>
        /// <param name="id">Id of target Dish Requirement</param>
        /// <remarks>
        /// Returns a Dish Requirement Object whose Dish Requirement Id
        /// matches the given 'id' parameter.
        /// If no such Dish Requirement exists,
        /// Not Found is returned instead.
        /// </remarks>
        // GET: api/DishRequirements/5/basic
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<DishRequirement>> GetDishRequirementBasic(int id)
        {
            _logger.LogDebug("Dish Requirements Controller fetching Dish Requirement with ID: " + id);
            var dishRequirement = await _context.DishRequirements.FindAsync(id);

            if (dishRequirement == null)
            {
                _logger.LogDebug("Dish Requirements Controller found no Dish Requirement with ID: " + id);
                return NotFound();
            }

            _logger.LogDebug("Dish Requirements Controller returning Dish Requirement with ID: " + id);
            return dishRequirement;
        }

        #endregion

        #endregion

        /// <summary>
        /// Create Dish Requirement
        /// </summary>
        /// <param name="dishRequirement">Dish Requirement to Create</param>
        /// <remarks>
        /// Creates a new Dish Requirement and inserts it into the Database
        /// using the values from the provided 'dishRequirement' parameter.
        /// Only the Dish Id and Supply Category Id of the Dish Requirement may be set.
        /// Its Dish and Supply Category will be automatically set based on the
        /// Dish Id and Supply Category Id.
        /// The Dish Requirement Id may not be set. The Databse will automatically
        /// assign it a new Id, equivalent to the last Dish Requirement Id in the Database + 1.
        /// The given parameter of type Dish Requirement must contain a Dish Id and
        /// Supply Category Id, belonging to a valid Dish and Supply Category.
        /// If the 'dishRequirement' parameter is incorrect, Bad Request is returned.
        /// Dish Requirement parameter errors:
        /// (1) The Dish Requirement Id is not 0, 
        /// (2) The Dish Id does not match any Dish's Dish Id,
        /// (3) The Supply Category Id does not match any Supply Category's 
        /// Supply Category Id,
        /// (4) Another Dish Requirement with the same Dish Id and Supply Category Id values
        /// already exists,
        /// (5) Dish or Supply Category are not null.
        /// </remarks>
        // POST: api/DishRequirements
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<DishRequirement>> CreateDishRequirement(DishRequirement dishRequirement)
        {
            _logger.LogDebug("Dish Requirements Controller creating new Dish Requirement.");

            //Validation

            if (dishRequirement.DishRequirementId != 0 ||
                dishRequirement.Dish != null || dishRequirement.SupplyCategory != null ||
                await _context.Dishes.FindAsync(dishRequirement.DishId) == null ||
                await _context.SupplyCategories.FindAsync(dishRequirement.SupplyCategoryId) == null ||
                await _context.DishRequirements.AnyAsync(r => r.DishId == dishRequirement.DishId &&
                    r.SupplyCategoryId == dishRequirement.SupplyCategoryId))
            {
                _logger.LogDebug("Dish Requirement Controller failed to create new Dish Requirement. " +
                    "Provided 'dishRequirement' parameter violated creation constraints.");
                return BadRequest();
            }

            //Validation

            _context.DishRequirements.Add(dishRequirement);
            await _context.SaveChangesAsync();

            _logger.LogDebug("Dish Requirements Controller succesfully created new Dish Requirement with " +
                "ID: " + dishRequirement.DishRequirementId);
            return CreatedAtAction("GetDishRequirementBasic",
                new { id = dishRequirement.DishRequirementId }, dishRequirement);
        }

        /// <summary>
        /// Delete Dish Requirement
        /// </summary>
        /// <param name="id">Id of target Dish Requirement</param>
        /// <remarks>
        /// Removes a Dish Requirement from the Database.
        /// If there is no Dish Requirement, whose Id matches the given 'id' parameter, 
        /// Not Found is returned.
        /// If the desired Dish Requirement is the last in the Database it is removed from the Database
        /// and its Dish Requirement Id will be used by the next Dish Requirement to be inserted.
        /// If the desired Dish Requirement is not the last in the Database, it is swapped with the last
        /// Dish Requirement in the Database before it is removed, taking on the last Dish Requirement's
        /// Dish Id and Supply Category Id before removing it and freeing up the last 
        /// Dish Requirement's Id for the next Dish Requirement to be inserted.
        /// </remarks>
        // DELETE: api/DishRequirements/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<DishRequirement>> DeleteDishRequirement(int id)
        {
            _logger.LogDebug("Dish Requirements Controller deleting Dish Requirement with ID: " + id);
            var dishRequirement = await _context.DishRequirements.FindAsync(id);
            if (dishRequirement == null)
            {
                _logger.LogDebug("Dish Requirements Controller found no Dish Requirement with ID: " + id);
                return NotFound();
            }

            var lastReqId = await _context.DishRequirements.CountAsync();
            if (id==lastReqId)
            {
                _logger.LogDebug("Dish Requirements Controller found that Dish Requirement with ID: " + id + " " +
                    "is last and will delete it.");
                _context.DishRequirements.Remove(dishRequirement);
                await _context.SaveChangesAsync();

                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT ('[DishRequirements]', RESEED, " + (lastReqId - 1) + ");");
                _logger.LogDebug("Dish Requirements Controller removed Dish Requirement with ID: " + id);
                return Ok();
            }

            _logger.LogDebug("Dish Requirements Controller found that Dish Requirement with ID: " + id + " " +
                "is not last in the Database and will swap it with last.");

            var lastReq = await _context.DishRequirements.FindAsync(lastReqId);
            dishRequirement.DishId = lastReq.DishId;
            dishRequirement.SupplyCategoryId = lastReq.SupplyCategoryId;

            await _context.SaveChangesAsync();

            _context.DishRequirements.Remove(lastReq);
            await _context.SaveChangesAsync();

            await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT ('[DishRequirements]', RESEED, " + (lastReqId - 1) + ");");
            _logger.LogDebug("Dish Requirements Controller swapped Dish Requirement with ID: " + id +
                " with the last Dish Requirement, then deleted that.");
            return Ok();
        }

        [NonAction]
        private async Task<bool> DishRequirementExists(int id)
            => await _context.DishRequirements.AnyAsync(e => e.DishRequirementId == id);
    }
}
