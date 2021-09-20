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

namespace McJenny.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DishRequirementsController : ControllerBase
    {
        private readonly FoodChainsDbContext _context;

        public DishRequirementsController(FoodChainsDbContext context) => _context = context;

        #region Gets

        #region Basic

        // GET: api/DishRequirements
        [HttpGet]
        public async Task<ActionResult<IEnumerable<string>>> GetDishRequirements()
        {
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

            return result;
        }

        // GET: api/DishRequirements/5
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> GetDishRequirement(int id)
        {
            var dishRequirement = await _context.DishRequirements.FindAsync(id);

            if (dishRequirement == null) return NotFound();

            var dish = (await _context.Dishes.FindAsync(dishRequirement.DishId)).Name;
            var cat = (await _context.SupplyCategories.FindAsync(dishRequirement.SupplyCategoryId)).Name;

            if (dish == null || cat == null) return NotFound();

            return dish + "requires" + cat;
        }

        // GET: api/DishRequirements/5/basic
        [HttpGet("{id}/basic")]
        public async Task<ActionResult<DishRequirement>> GetDishRequirementBasic(int id)
        {
            var dishRequirement = await _context.DishRequirements.FindAsync(id);

            if (dishRequirement == null) return NotFound();

            return dishRequirement;
        }

        #endregion

        #endregion

        // POST: api/DishRequirements
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        [HttpPost]
        public async Task<ActionResult<DishRequirement>> CreateDishRequirement(DishRequirement dishRequirement)
        {
            //Validation

            if (dishRequirement.DishRequirementId != 0 ||
                dishRequirement.Dish != null || dishRequirement.SupplyCategory != null ||
                await _context.Dishes.FindAsync(dishRequirement.DishId) == null ||
                await _context.SupplyCategories.FindAsync(dishRequirement.SupplyCategoryId) == null ||
                await _context.DishRequirements.AnyAsync(r => r.DishId == dishRequirement.DishId &&
                    r.SupplyCategoryId == dishRequirement.SupplyCategoryId))
                return BadRequest();

            //Validation

            _context.DishRequirements.Add(dishRequirement);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetDishRequirementBasic",
                new { id = dishRequirement.DishRequirementId }, dishRequirement);
        }

        // DELETE: api/DishRequirements/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<DishRequirement>> DeleteDishRequirement(int id)
        {
            var dishRequirement = await _context.DishRequirements.FindAsync(id);
            if (dishRequirement == null) return NotFound();

            var lastReqId = await _context.DishRequirements.CountAsync();
            if (id==lastReqId)
            {
                _context.DishRequirements.Remove(dishRequirement);
                await _context.SaveChangesAsync();

                await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT ('[DishRequirements]', RESEED, " + (lastReqId - 1) + ");");
                return Ok();
            }

            var lastReq = await _context.DishRequirements.FindAsync(lastReqId);
            dishRequirement.DishId = lastReq.DishId;
            dishRequirement.SupplyCategoryId = lastReq.SupplyCategoryId;

            await _context.SaveChangesAsync();

            _context.DishRequirements.Remove(lastReq);
            await _context.SaveChangesAsync();

            await _context.Database.ExecuteSqlRawAsync
                    ("DBCC CHECKIDENT ('[DishRequirements]', RESEED, " + (lastReqId - 1) + ");");
            return Ok();
        }

        private async Task<bool> DishRequirementExists(int id)
            => await _context.DishRequirements.AnyAsync(e => e.DishRequirementId == id);
    }
}
