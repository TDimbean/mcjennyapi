using EntityFrameworkCore3Mock;
using FluentAssertions;
using McJenny.WebAPI.Controllers;
using McJenny.WebAPI.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace McJenny.UnitTests.ControlerTests
{
    [TestClass]
    public class DishRequirementsControllerTests
    {
        private static DishRequirementsController _controller;
        private static FoodChainsDbContext _context;
        private static ILogger<DishRequirementsController> _logger;

        [ClassInitialize]
        public static void Init(TestContext testContext)
        {
            _logger = (new Mock<ILogger<DishRequirementsController>>()).Object;
            Setup();
        }

        [TestInitialize]
        public void InitTest() => Setup();

        #region Gets

        #region Basic

        [TestMethod]
        public async Task GetDishRequirements_HappyFlow_ShouldReturnAllDishRequirements()
        {
            // Arrange
            var requirements = await _context.DishRequirements
               .Select(r => new { r.DishRequirementId, r.DishId, r.SupplyCategoryId })
               .ToArrayAsync();

            var dishIds = requirements.Select(r => r.DishId).Distinct().ToArray();
            var catIds = requirements.Select(r => r.SupplyCategoryId).Distinct().ToArray();

            var dishes = await _context.Dishes
                .Where(d => dishIds.Contains(d.DishId))
                .Select(d => new { d.Name, d.DishId })
                .ToArrayAsync();

            var cats = await _context.SupplyCategories
                .Where(c => catIds.Contains(c.SupplyCategoryId))
                .Select(c => new { c.Name, c.SupplyCategoryId })
                .ToArrayAsync();

            var strings = new string[requirements.Length];
            for (int i = 0; i < requirements.Length; i++)
                strings[i] = string.Format("Requirement [{0}]: ({1}) {2} requires ({3}) {4}",
                    requirements[i].DishRequirementId,
                    requirements[i].DishId,
                    dishes.SingleOrDefault(d => d.DishId == requirements[i].DishId).Name,
                    requirements[i].SupplyCategoryId,
                    cats.SingleOrDefault(c => c.SupplyCategoryId == requirements[i].SupplyCategoryId).Name); ;

            var expected = (object)strings;

            // Act
            var result = (object)(await _controller.GetDishRequirements()).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishRequirement_HappyFlow_ShouldReturnRequirement()
        {
            // Arrange
            var dishRequirement = await _context.DishRequirements.FindAsync(GetFirstDishRequirementId);

            var dish = (await _context.Dishes.FindAsync(dishRequirement.DishId)).Name;
            var cat = (await _context.SupplyCategories.FindAsync(dishRequirement.SupplyCategoryId)).Name;

            var expected = (object)(dish + "requires" + cat);

            // Act
            var result = (object)(await _controller
                .GetDishRequirement(GetFirstDishRequirementId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishRequirement_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (object)(await _controller
                .GetDishRequirement(GenBadDishRequirementId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetDishRequirementBasic_HappyFlow_ShouldReturnRequirement()
        {
            // Arrange
            var expected = (object)(await _context.DishRequirements.FirstOrDefaultAsync());

            // Act
            var result = (object)(await _controller
                .GetDishRequirementBasic(GetFirstDishRequirementId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishRequirementBasic_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (object)(await _controller
                .GetDishRequirementBasic(GenBadDishRequirementId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        #endregion

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateDishRequirement_HappyFlow_ShouldCreateAndReturnDishRequirement()
        {
            // Arrange
            var dish = new Dish { Name = "Crudités" };
            await _context.Dishes.AddAsync(dish);
            var dishId = await _context.Dishes.CountAsync();

            var requirement = new DishRequirement { DishId = dishId, SupplyCategoryId = 1 };

            // Act
            await _controller.CreateDishRequirement(requirement);
            var result = (object)(await _controller.GetDishRequirementBasic(requirement.DishRequirementId)).Value;

            requirement.DishRequirementId = await _context.DishRequirements.CountAsync();
            var expected = (object)requirement;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task CreateDishRequirement_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var dish = new Dish { Name = "Crudités" };
            await _context.Dishes.AddAsync(dish);

            var requirement = new DishRequirement
            {
                DishRequirementId = await _context.DishRequirements
                .Select(d => d.DishRequirementId).LastOrDefaultAsync() + 1,
                DishId = dish.DishId,
                SupplyCategoryId = 1
            };

            // Act
            var result = (object)(await _controller.CreateDishRequirement(requirement)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateDishRequirement_HasDish_ShouldReturnBadRequest()
        {
            // Arrange
            var dish = new Dish { Name = "Crudités" };
            await _context.Dishes.AddAsync(dish);

            var requirement = new DishRequirement
            {
                Dish = new Dish(),
                DishId = dish.DishId,
                SupplyCategoryId = 1
            };

            // Act
            var result = (object)(await _controller.CreateDishRequirement(requirement)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateDishRequirement_HasSupplyCategory_ShouldReturnBadRequest()
        {
            // Arrange
            var dish = new Dish { Name = "Crudités" };
            await _context.Dishes.AddAsync(dish);

            var requirement = new DishRequirement
            {
                SupplyCategory = new SupplyCategory(),
                DishId = dish.DishId,
                SupplyCategoryId = 1
            };

            // Act
            var result = (object)(await _controller.CreateDishRequirement(requirement)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateDishRequirement_InexistentDishId_ShouldReturnBadRequest()
        {
            // Arrange
            var requirement = new DishRequirement
            {
                DishId = await _context.Dishes.Select(d => d.DishId).LastOrDefaultAsync() + 1,
                SupplyCategoryId = 1
            };

            // Act
            var result = (object)(await _controller.CreateDishRequirement(requirement)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateDishRequirement_InexistentSupplyCategoryId_ShouldReturnBadRequest()
        {
            // Arrange
            var dish = new Dish { Name = "Crudités" };
            await _context.Dishes.AddAsync(dish);

            var requirement = new DishRequirement
            {
                DishId = dish.DishId,
                SupplyCategoryId = await _context.SupplyCategories.Select(s =>
                    s.SupplyCategoryId).LastOrDefaultAsync() + 1
            };

            // Act
            var result = (object)(await _controller.CreateDishRequirement(requirement)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateDishRequirement_DuplicateRequirement_ShouldReturnBadRequest()
        {
            // Arrange
            var source = await _context.DishRequirements
                .Select(r => new { r.DishId, r.SupplyCategoryId })
                .FirstOrDefaultAsync();

            var requirement = new DishRequirement
            {
                DishId = source.DishId,
                SupplyCategoryId = source.SupplyCategoryId
            };

            // Act
            var result = (object)(await _controller.CreateDishRequirement(requirement)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        #endregion

        [TestMethod]
        public void DishRequirementExists_ItDoes_ShoudReturnTrue()
        {
            //Arrange
            var methodInfo = typeof(DishRequirementsController).GetMethod("DishRequirementExists",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GetFirstDishRequirementId };

            // Act
            var result = ((methodInfo.Invoke(_controller, parameters)) as Task<bool>).Result;

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void DishRequirementExists_ItDoesNot_ShoudReturnFalse()
        {
            //Arrange
            var methodInfo = typeof(DishRequirementsController).GetMethod("DishRequirementExists",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GenBadDishRequirementId };

            // Act
            var result = ((methodInfo.Invoke(_controller, parameters)) as Task<bool>).Result;

            // Assert
            result.Should().BeFalse();
        }

        [ClassCleanup]
        public static void CleanUp()
        {
            _controller = null;
            _context.Dispose();
        }

        [TestCleanup]
        public void TestClear()
        {
            _context.Dispose();
            _controller = new DishRequirementsController(_context, _logger);
        }

        /// Helpers

        int GetFirstDishRequirementId => _context.DishRequirements
            .Select(r => r.DishRequirementId)
            .OrderBy(id => id)
            .FirstOrDefault();
        int GenBadDishRequirementId => _context.DishRequirements
            .Select(r => r.DishRequirementId)
            .ToArray()
            .OrderBy(id => id)
            .LastOrDefault() + 1;
    
        private static void Setup()
        {
            var requirements = FakeRepo.DishRequirements;
            var dishes = FakeRepo.Dishes;
            var categories = FakeRepo.SupplyCategories;

            var DummyOptions = new DbContextOptionsBuilder<FoodChainsDbContext>().Options;

            var dbContextMock = new DbContextMock<FoodChainsDbContext>(DummyOptions);
            var requirementsDbSetMock = dbContextMock.CreateDbSetMock(x => x.DishRequirements, requirements);
            var dishesDbSetMock = dbContextMock.CreateDbSetMock(x => x.Dishes, dishes);
            var catsDbSetMock = dbContextMock.CreateDbSetMock(x => x.SupplyCategories, categories);
            dbContextMock.Setup(m => m.DishRequirements).Returns(requirementsDbSetMock.Object);
            dbContextMock.Setup(m => m.Dishes).Returns(dishesDbSetMock.Object);
            dbContextMock.Setup(m => m.SupplyCategories).Returns(catsDbSetMock.Object);
            _context = dbContextMock.Object;
            _controller = new DishRequirementsController(_context, _logger);
        }
    }
}
