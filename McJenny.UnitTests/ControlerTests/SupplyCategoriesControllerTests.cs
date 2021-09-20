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
    public class SupplyCategoriesControllerTests
    {
        private static SupplyCategoriesController _controller;
        private static FoodChainsDbContext _context;
        private static ILogger<SupplyCategoriesController> _logger;

        [ClassInitialize]
        public static void Init(TestContext testContext)
        {
            _logger = (new Mock<ILogger<SupplyCategoriesController>>()).Object;
            Setup();
        }

        [TestInitialize]
        public void InitTest() => Setup();

        #region Gets

        #region Basic

        [TestMethod]
        public async Task GetSuppplyCategories_HappyFlow_ShouldReturnAllSupplyCategories()
        {
            // Arrange
            var expected = (object)await _context.SupplyCategories
                .Select(c => new { c.SupplyCategoryId, c.Name })
                .ToListAsync();

            // Act
            var result = (object)(await _controller.GetSupplyCategories()).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyCategory_HappyFlow_ShouldReturnSupplyCategory()
        {
            // Arrange
            var cat = await _context.SupplyCategories.FirstOrDefaultAsync();
            var expected = (object)cat.Name;

            // Act
            var result = (object)(await _controller.GetSupplyCategory(cat.SupplyCategoryId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyCategory_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetSupplyCategory(GenBadSupplyCategoryId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetSupplyCategoryBasic_HappyFlow_ShouldReturnSupplyCategory()
        {
            // Arrange
            var expected = await _context.SupplyCategories.FirstOrDefaultAsync();

            // Act
            var result = (await _controller.GetSupplyCategoryBasic(GetFirstSupplyCategoryId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyCategoryBasic_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetSupplyCategoryBasic(GenBadSupplyCategoryId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetSupplyCategoryLinks_HappyFlow_ShouldReturnSupplyLinks()
        {
            // Arrange
            var catId = GetFirstSupplyCategoryId;

            var supplyLinks = await _context.SupplyLinks
                .Where(l => l.SupplyCategoryId == catId)
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

            var expected = new string[supplyLinks.Length];

            for (int i = 0; i < expected.Length; i++)
            {
                var sup = suppliers.FirstOrDefault(s => s.SupplierId == supplyLinks[i].SupplierId);
                var loc = locations.FirstOrDefault(l => l.LocationId == supplyLinks[i].LocationId);

                expected[i] = string.Format("(Supply Link: {0}): ({1}) {2}, {3}, {4}{5} " +
                    "supplies ({6}) {7}, {8}{9}, {10}.",
                    i,
                    sup.SupplierId,
                    sup.Name,
                    sup.AbreviatedCountry,
                    sup.AbreviatedState == "N/A" ? string.Empty :
                    sup.AbreviatedState,
                    sup.City,
                    loc.LocationId,
                    loc.AbreviatedCountry,
                    loc.AbreviatedState,
                    loc.City,
                    loc.Street);
            }

            // Act
            var result = (await _controller.GetSupplyCategoryLinks(catId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyCategoryLinks_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = GenBadSupplyCategoryId;

            // Act
            var result = (await _controller.GetSupplyCategoryLinks(badId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetDishes_HappyFlow_ShouldReturnDishes()
        {
            // Arrange
            var catId = GetFirstSupplyCategoryId;

            var dishRequirements = await _context.DishRequirements
               .Where(r => r.SupplyCategoryId == catId)
               .ToArrayAsync();

            var dishIds = new List<int>();

            foreach (var req in dishRequirements)
                dishIds.Add(req.DishId);

            dishIds = dishIds.Distinct().ToList();

            var expected = await _context.Dishes
                .Select(d => new
                {
                    d.DishId,
                    d.Name
                })
                .Where(d => dishIds.Contains(d.DishId))
                .ToArrayAsync();

            // Act
            var result = (await _controller.GetDishes(catId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetDishes_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = GenBadSupplyCategoryId;

            // Act
            var result = (await _controller.GetDishes(badId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetSuppliers_HappyFlow_ShouldReturnSuppliers()
        {
            // Arrange
            var catId = GetFirstSupplyCategoryId;

            var supplierStocks = await _context.SupplierStocks
                .Where(r => r.SupplyCategoryId == catId)
                .ToArrayAsync();

            var supIds = new List<int>();

            foreach (var stock in supplierStocks)
                supIds.Add(stock.SupplierId);

            supIds = supIds.Distinct().ToList();

            var expected = await _context.Suppliers
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

            // Act
            var result = (await _controller.GetSuppliers(catId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSuppliers_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var badId = GenBadSupplyCategoryId;

            // Act
            var result = (await _controller.GetSuppliers(badId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        #endregion

        #region Advanced

        [TestMethod]
        public async Task GetSupplyCategoriesFiltered_NameMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.SupplyCategories.Select(c => c.Name).FirstOrDefaultAsync();

            var expected = (object)await _context.SupplyCategories
                  .Select(c => new { c.SupplyCategoryId, c.Name })
                 .Where(c => c.Name.ToUpper()
                     .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetSupplyCategoriesFiltered(filter)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyCategoriesPaged_HappyFlow_ShouldReturnPaged()
        {
            // Arrange
            var pgSz = 2;
            var pgInd = 1;

            var expected = (object)await _context.SupplyCategories
                  .Select(c => new { c.SupplyCategoryId, c.Name })
                 .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetSupplyCategoriesPaged(pgInd, pgSz)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyCategoriesPaged_EndPageIncomplete_ShouldReturnPaged()
        {
            // Arrange
            var SupplyCategoryCount = await _context.SupplyCategories.CountAsync();
            var pgSz = 1;

            while (true)
            {
                if (SupplyCategoryCount % pgSz == 0) pgSz++;
                else break;
            }

            var pgInd = SupplyCategoryCount / (pgSz - 1);

            var expected = (object)await _context.SupplyCategories
                 .Select(c => new { c.SupplyCategoryId, c.Name })
                 .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();

            // Act
            var call = (await _controller.GetSupplyCategoriesPaged(pgInd, pgSz)).Value;
            var result = (object)call;
            var resCount = (result as IEnumerable<dynamic>).Count();

            // Assert
            result.Should().BeEquivalentTo(expected);
            resCount.Should().BeLessThan(pgSz);
        }

        [TestMethod]
        public async Task GetSupplyCategoriesPaged_SizeOver_ShouldReturnPaged()
        {
            // Arrange
            var SupplyCategoryCount = await _context.SupplyCategories.CountAsync();
            var pgSz = SupplyCategoryCount + 1;
            var pgInd = 1;

            var expected = (object)await _context.SupplyCategories
                 .Select(c => new { c.SupplyCategoryId, c.Name })
                 .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetSupplyCategoriesPaged(pgInd, pgSz)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyCategoriesPaged_IndexOver_ShouldReturnPaged()
        {
            // Arrange
            var SupplyCategoryCount = await _context.SupplyCategories.CountAsync();
            var pgInd = SupplyCategoryCount + 1;
            var pgSz = 1;

            var expected = (object)await _context.SupplyCategories
                 .Select(c => new { c.SupplyCategoryId, c.Name })
                 .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetSupplyCategoriesPaged(pgInd, pgSz)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyCategoriesSorted_ByNameAsc_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "name";

            var expected = (object)await _context.SupplyCategories
                            .Select(c => new { c.SupplyCategoryId, c.Name })
                            .OrderBy(c => c.Name)
                            .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetSupplyCategoriesSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyCategoriesSorted_Desc_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "name";

            var expected = (object)await _context.SupplyCategories
                            .Select(c => new { c.SupplyCategoryId, c.Name })
                            .OrderByDescending(c => c.Name)
                            .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetSupplyCategoriesSorted(sortBy, true)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetSupplyCategoriesSorted_GarbledText_ShouldDefaultoIdSort()
        {
            // Arrange
            var sortBy = "asd";

            var expected = (object)await _context.SupplyCategories
                            .Select(c => new { c.SupplyCategoryId, c.Name })
                            .OrderBy(c => c.SupplyCategoryId)
                            .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetSupplyCategoriesSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        #endregion

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreateSupplyCategory_HappyFlow_ShouldCreateAndReturnSupplyCategory()
        {
            // Arrange
            var supplyCategory = new SupplyCategory { Name = "Greek Olives, Oils and Cheese" };

            // Act
            await _controller.CreateSupplyCategory(supplyCategory);
            var result = (object)(await _controller.GetSupplyCategoryBasic
                (supplyCategory.SupplyCategoryId)).Value;

            var expected = (object)supplyCategory;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task CreateSupplyCategory_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var supplyCategory = new SupplyCategory
            {
                SupplyCategoryId = await _context.SupplyCategories
                    .Select(c => c.SupplyCategoryId)
                    .LastOrDefaultAsync() + 1,
                Name = "Greek Olives, Oils and Cheese"
            };

            // Act
            var result = (await _controller.CreateSupplyCategory(supplyCategory)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow(null)]
        [DataRow("0123456789112345678921234567893123456789412345678951")]
        public async Task CreateSupplyCategory_BadNames_ShouldReturnBadRequest(string name)
        {
            // Arrange
            var supplyCategory = new SupplyCategory { Name = name };

            // Act
            var result = (await _controller.CreateSupplyCategory(supplyCategory)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupply_DuplicateName_ShouldReturnBadRequest()
        {
            // Arrange
            var exName = await _context.SupplyCategories.Select(c => c.Name).FirstOrDefaultAsync();
            var supplyCategory = new SupplyCategory { Name = exName };
            var lwrSupplyCategory = new SupplyCategory { Name = exName.ToLower() };
            var uprSupplyCategory = new SupplyCategory { Name = exName.ToUpper() };

            // Act
            var result = (await _controller.CreateSupplyCategory(supplyCategory)).Result;
            var lwrResult = (await _controller.CreateSupplyCategory(lwrSupplyCategory)).Result;
            var uprResult = (await _controller.CreateSupplyCategory(uprSupplyCategory)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            lwrResult.Should().BeOfType<BadRequestResult>();
            uprResult.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplyCategory_DishRequirementsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var supplyCategory = new SupplyCategory
            {
                Name = "Greek Olives, Oils and Cheese",
                DishRequirements = null
            };

            // Act
            var result = (await _controller.CreateSupplyCategory(supplyCategory)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplyCategory_SupplierStocksNull_ShouldReturnBadRequest()
        {
            // Arrange
            var supplyCategory = new SupplyCategory
            {
                Name = "Greek Olives, Oils and Cheese",
                SupplierStocks = null
            };

            // Act
            var result = (await _controller.CreateSupplyCategory(supplyCategory)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplyCategory_SupplyLinksNull_ShouldReturnBadRequest()
        {
            // Arrange
            var supplyCategory = new SupplyCategory
            {
                Name = "Greek Olives, Oils and Cheese",
                SupplyLinks = null
            };

            // Act
            var result = (await _controller.CreateSupplyCategory(supplyCategory)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplyCategory_DishRequirementsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var supplyCategory = new SupplyCategory
            {
                Name = "Greek Olives, Oils and Cheese",
                DishRequirements = new DishRequirement[] { new DishRequirement() }
            };

            // Act
            var result = (await _controller.CreateSupplyCategory(supplyCategory)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplyCategory_SupplierStocksNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var supplyCategory = new SupplyCategory
            {
                Name = "Greek Olives, Oils and Cheese",
                SupplierStocks = new SupplierStock[] { new SupplierStock() }
            };

            // Act
            var result = (await _controller.CreateSupplyCategory(supplyCategory)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreateSupplyCategory_SupplyLinksNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var supplyCategory = new SupplyCategory
            {
                Name = "Greek Olives, Oils and Cheese",
                SupplyLinks = new SupplyLink[] { new SupplyLink() }
            };

            // Act
            var result = (await _controller.CreateSupplyCategory(supplyCategory)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        #endregion

        #region Updates

        [TestMethod]
        public async Task UpdateSupplyCategory_HappyFlow_ShouldUpdateAndReturnSupplyCategory()
        {
            // Arrange
            var oldCat = await _context.SupplyCategories.FindAsync(GetFirstSupplyCategoryId);
            var oldCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldCat.SupplyCategoryId,
                Name = oldCat.Name,
                SupplierStocks = oldCat.SupplierStocks,
                SupplyLinks = oldCat.SupplyLinks,
                DishRequirements = oldCat.DishRequirements
            };

            var cat = new SupplyCategory { Name = "Soy Products" };

            // Act
            await _controller.UpdateSupplyCategory(oldCat.SupplyCategoryId, cat);
            var result = (await _controller.GetSupplyCategoryBasic(oldCat.SupplyCategoryId)).Value;

            cat.SupplierStocks = oldCatCopy.SupplierStocks;
            cat.SupplyLinks = oldCatCopy.SupplyLinks;
            cat.DishRequirements = oldCatCopy.DishRequirements;
            cat.SupplyCategoryId = oldCatCopy.SupplyCategoryId;

            // Assert
            result.Should().BeEquivalentTo(cat);
            result.Should().NotBeEquivalentTo(oldCatCopy);
        }

        [TestMethod]
        public async Task UpdateSupplyCategory_InexistentId_ShouldUpdateAndReturnSupplyCategory()
        {
            // Arrange
            var oldCat = await _context.SupplyCategories.FindAsync(GetFirstSupplyCategoryId);
            var oldCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldCat.SupplyCategoryId,
                Name = oldCat.Name,
                SupplierStocks = oldCat.SupplierStocks,
                SupplyLinks = oldCat.SupplyLinks,
                DishRequirements = oldCat.DishRequirements
            };

            var cat = new SupplyCategory { Name = "Soy Products" };

            // Act
            var result = await _controller.UpdateSupplyCategory(GenBadSupplyCategoryId, cat);
            var res = (await _controller.GetSupplyCategoryBasic(oldCat.SupplyCategoryId)).Value;

            cat.SupplierStocks = oldCatCopy.SupplierStocks;
            cat.SupplyLinks = oldCatCopy.SupplyLinks;
            cat.DishRequirements = oldCatCopy.DishRequirements;
            cat.SupplyCategoryId = oldCatCopy.SupplyCategoryId;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
            res.Should().BeEquivalentTo(oldCatCopy);
            res.Should().NotBeEquivalentTo(cat);
        }

        [TestMethod]
        public async Task UpdateSupplyCategory_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var oldCat = await _context.SupplyCategories.FindAsync(GetFirstSupplyCategoryId);
            var oldCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldCat.SupplyCategoryId,
                Name = oldCat.Name,
                SupplierStocks = oldCat.SupplierStocks,
                SupplyLinks = oldCat.SupplyLinks,
                DishRequirements = oldCat.DishRequirements
            };

            var cat = new SupplyCategory { Name = "Soy Products", SupplyCategoryId = GenBadSupplyCategoryId };

            // Act
            var result = await _controller.UpdateSupplyCategory(oldCat.SupplyCategoryId, cat);
            var updCat = (await _controller.GetSupplyCategoryBasic(oldCat.SupplyCategoryId)).Value;

            cat.SupplierStocks = oldCatCopy.SupplierStocks;
            cat.SupplyLinks = oldCatCopy.SupplyLinks;
            cat.DishRequirements = oldCatCopy.DishRequirements;
            cat.SupplyCategoryId = oldCatCopy.SupplyCategoryId;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updCat.Should().BeEquivalentTo(oldCatCopy);
            updCat.Should().NotBeEquivalentTo(cat);
        }

        [DataTestMethod]
        [DataRow(" ")]
        [DataRow("012345678911234567892123456789312345678941234567890")]
        public async Task UpdateSupplyCategory_BadNames_ShouldReturnBadRequest(string name)
        {
            // Arrange
            var oldCat = await _context.SupplyCategories.FindAsync(GetFirstSupplyCategoryId);
            var oldCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldCat.SupplyCategoryId,
                Name = oldCat.Name,
                SupplierStocks = oldCat.SupplierStocks,
                SupplyLinks = oldCat.SupplyLinks,
                DishRequirements = oldCat.DishRequirements
            };

            var cat = new SupplyCategory { Name = name };

            // Act
            var result = await _controller.UpdateSupplyCategory(oldCat.SupplyCategoryId, cat);
            var updCat = (await _controller.GetSupplyCategoryBasic(oldCat.SupplyCategoryId)).Value;

            cat.SupplierStocks = oldCatCopy.SupplierStocks;
            cat.SupplyLinks = oldCatCopy.SupplyLinks;
            cat.DishRequirements = oldCatCopy.DishRequirements;
            cat.SupplyCategoryId = oldCatCopy.SupplyCategoryId;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updCat.Should().BeEquivalentTo(oldCatCopy);
            updCat.Should().NotBeEquivalentTo(cat);
        }

        [TestMethod]
        public async Task UpdateSupplyCategory_DuplicateName_ShouldReturnBadRequest()
        {
            // Arrange
            var dupe = await _context.SupplyCategories
                .Select(c => c.Name)
                .Skip(1)
                .FirstOrDefaultAsync();

            var oldCat = await _context.SupplyCategories.FindAsync(GetFirstSupplyCategoryId);
            var oldCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldCat.SupplyCategoryId,
                Name = oldCat.Name,
                SupplierStocks = oldCat.SupplierStocks,
                SupplyLinks = oldCat.SupplyLinks,
                DishRequirements = oldCat.DishRequirements
            };

            var cat = new SupplyCategory { Name = dupe };

            // Act
            var result = await _controller.UpdateSupplyCategory(oldCat.SupplyCategoryId, cat);
            var updCat = (await _controller.GetSupplyCategoryBasic(oldCat.SupplyCategoryId)).Value;

            cat.SupplierStocks = oldCatCopy.SupplierStocks;
            cat.SupplyLinks = oldCatCopy.SupplyLinks;
            cat.DishRequirements = oldCatCopy.DishRequirements;
            cat.SupplyCategoryId = oldCatCopy.SupplyCategoryId;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updCat.Should().BeEquivalentTo(oldCatCopy);
            updCat.Should().NotBeEquivalentTo(cat);
        }

        [TestMethod]
        public async Task UpdateSupplyCategory_SupplierStocksNull_ShouldReturnBadRequest()
        {
            // Arrange
            var oldCat = await _context.SupplyCategories.FindAsync(GetFirstSupplyCategoryId);
            var oldCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldCat.SupplyCategoryId,
                Name = oldCat.Name,
                SupplierStocks = oldCat.SupplierStocks,
                SupplyLinks = oldCat.SupplyLinks,
                DishRequirements = oldCat.DishRequirements
            };

            var cat = new SupplyCategory { Name = "Soy Products", SupplierStocks = null };

            // Act
            var result = await _controller.UpdateSupplyCategory(oldCat.SupplyCategoryId, cat);
            var updCat = (await _controller.GetSupplyCategoryBasic(oldCat.SupplyCategoryId)).Value;

            cat.SupplierStocks = oldCatCopy.SupplierStocks;
            cat.SupplyLinks = oldCatCopy.SupplyLinks;
            cat.DishRequirements = oldCatCopy.DishRequirements;
            cat.SupplyCategoryId = oldCatCopy.SupplyCategoryId;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updCat.Should().BeEquivalentTo(oldCatCopy);
            updCat.Should().NotBeEquivalentTo(cat);
        }

        [TestMethod]
        public async Task UpdateSupplyCategory_SupplierStocksNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var oldCat = await _context.SupplyCategories.FindAsync(GetFirstSupplyCategoryId);
            var oldCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldCat.SupplyCategoryId,
                Name = oldCat.Name,
                SupplierStocks = oldCat.SupplierStocks,
                SupplyLinks = oldCat.SupplyLinks,
                DishRequirements = oldCat.DishRequirements
            };

            var cat = new SupplyCategory
            {
                Name = "Soy Products",
                SupplierStocks = new SupplierStock[] { new SupplierStock() }
            };

            // Act
            var result = await _controller.UpdateSupplyCategory(oldCat.SupplyCategoryId, cat);
            var updCat = (await _controller.GetSupplyCategoryBasic(oldCat.SupplyCategoryId)).Value;

            cat.SupplierStocks = oldCatCopy.SupplierStocks;
            cat.SupplyLinks = oldCatCopy.SupplyLinks;
            cat.DishRequirements = oldCatCopy.DishRequirements;
            cat.SupplyCategoryId = oldCatCopy.SupplyCategoryId;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updCat.Should().BeEquivalentTo(oldCatCopy);
            updCat.Should().NotBeEquivalentTo(cat);
        }

        [TestMethod]
        public async Task UpdateSupplyCategory_SupplyLinksNull_ShouldReturnBadRequest()
        {
            // Arrange
            var oldCat = await _context.SupplyCategories.FindAsync(GetFirstSupplyCategoryId);
            var oldCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldCat.SupplyCategoryId,
                Name = oldCat.Name,
                SupplyLinks = oldCat.SupplyLinks,
                SupplierStocks = oldCat.SupplierStocks,
                DishRequirements = oldCat.DishRequirements
            };

            var cat = new SupplyCategory { Name = "Soy Products", SupplyLinks = null };

            // Act
            var result = await _controller.UpdateSupplyCategory(oldCat.SupplyCategoryId, cat);
            var updCat = (await _controller.GetSupplyCategoryBasic(oldCat.SupplyCategoryId)).Value;

            cat.SupplyLinks = oldCatCopy.SupplyLinks;
            cat.SupplyLinks = oldCatCopy.SupplyLinks;
            cat.DishRequirements = oldCatCopy.DishRequirements;
            cat.SupplyCategoryId = oldCatCopy.SupplyCategoryId;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updCat.Should().BeEquivalentTo(oldCatCopy);
            updCat.Should().NotBeEquivalentTo(cat);
        }

        [TestMethod]
        public async Task UpdateSupplyCategory_SupplyLinksNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var oldCat = await _context.SupplyCategories.FindAsync(GetFirstSupplyCategoryId);
            var oldCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldCat.SupplyCategoryId,
                Name = oldCat.Name,
                SupplyLinks = oldCat.SupplyLinks,
                SupplierStocks = oldCat.SupplierStocks,
                DishRequirements = oldCat.DishRequirements
            };

            var cat = new SupplyCategory
            {
                Name = "Soy Products",
                SupplyLinks = new SupplyLink[] { new SupplyLink() }
            };

            // Act
            var result = await _controller.UpdateSupplyCategory(oldCat.SupplyCategoryId, cat);
            var updCat = (await _controller.GetSupplyCategoryBasic(oldCat.SupplyCategoryId)).Value;

            cat.SupplyLinks = oldCatCopy.SupplyLinks;
            cat.SupplyLinks = oldCatCopy.SupplyLinks;
            cat.DishRequirements = oldCatCopy.DishRequirements;
            cat.SupplyCategoryId = oldCatCopy.SupplyCategoryId;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updCat.Should().BeEquivalentTo(oldCatCopy);
            updCat.Should().NotBeEquivalentTo(cat);
        }

        [TestMethod]
        public async Task UpdateSupplyCategory_DishRequirementsNull_ShouldReturnBadRequest()
        {
            // Arrange
            var oldCat = await _context.SupplyCategories.FindAsync(GetFirstSupplyCategoryId);
            var oldCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldCat.SupplyCategoryId,
                Name = oldCat.Name,
                DishRequirements = oldCat.DishRequirements,
                SupplyLinks = oldCat.SupplyLinks,
                SupplierStocks = oldCat.SupplierStocks
            };

            var cat = new SupplyCategory { Name = "Soy Products", DishRequirements = null };

            // Act
            var result = await _controller.UpdateSupplyCategory(oldCat.SupplyCategoryId, cat);
            var updCat = (await _controller.GetSupplyCategoryBasic(oldCat.SupplyCategoryId)).Value;

            cat.DishRequirements = oldCatCopy.DishRequirements;
            cat.SupplyLinks = oldCatCopy.SupplyLinks;
            cat.DishRequirements = oldCatCopy.DishRequirements;
            cat.SupplyCategoryId = oldCatCopy.SupplyCategoryId;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updCat.Should().BeEquivalentTo(oldCatCopy);
            updCat.Should().NotBeEquivalentTo(cat);
        }

        [TestMethod]
        public async Task UpdateSupplyCategory_DishRequirementsNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var oldCat = await _context.SupplyCategories.FindAsync(GetFirstSupplyCategoryId);
            var oldCatCopy = new SupplyCategory
            {
                SupplyCategoryId = oldCat.SupplyCategoryId,
                Name = oldCat.Name,
                DishRequirements = oldCat.DishRequirements,
                SupplyLinks = oldCat.SupplyLinks,
                SupplierStocks = oldCat.SupplierStocks
            };

            var cat = new SupplyCategory
            {
                Name = "Soy Products",
                DishRequirements = new DishRequirement[] { new DishRequirement() }
            };

            // Act
            var result = await _controller.UpdateSupplyCategory(oldCat.SupplyCategoryId, cat);
            var updCat = (await _controller.GetSupplyCategoryBasic(oldCat.SupplyCategoryId)).Value;

            cat.DishRequirements = oldCatCopy.DishRequirements;
            cat.SupplyLinks = oldCatCopy.SupplyLinks;
            cat.DishRequirements = oldCatCopy.DishRequirements;
            cat.SupplyCategoryId = oldCatCopy.SupplyCategoryId;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updCat.Should().BeEquivalentTo(oldCatCopy);
            updCat.Should().NotBeEquivalentTo(cat);
        }

        #endregion

        [TestMethod]
        public void SupplyCategoryExists_ItDoes_ShoudReturnTrue()
        {
            //Arrange
            var methodInfo = typeof(SupplyCategoriesController)
                .GetMethod("SupplyCategoryExists", BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GetFirstSupplyCategoryId };

            // Act
            var result = ((methodInfo.Invoke(_controller, parameters)) as Task<bool>).Result;

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void SupplyCategoryExists_ItDoesNot_ShoudReturnFalse()
        {
            //Arrange
            var methodInfo = typeof(SupplyCategoriesController)
                .GetMethod("SupplyCategoryExists", BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GenBadSupplyCategoryId };

            // Act
            var result = (methodInfo.Invoke(_controller, parameters) as Task<bool>).Result;

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
            _controller = new SupplyCategoriesController(_context, _logger);
        }

        /// Helpers
        int GetFirstSupplyCategoryId => _context.SupplyCategories.Select(c => c.SupplyCategoryId)
            .OrderBy(id => id).FirstOrDefault();
        int GenBadSupplyCategoryId => _context.SupplyCategories.Select(c => c.SupplyCategoryId)
            .ToArray().OrderBy(id => id).LastOrDefault() + 1;

        private static void Setup()
        {
            var supplyCategories = FakeRepo.SupplyCategories;
            var links = FakeRepo.SupplyLinks;
            var sups = FakeRepo.Suppliers;
            var locs = FakeRepo.Locations;
            var dishes = FakeRepo.Dishes;
            var reqs = FakeRepo.DishRequirements;
            var stocks = FakeRepo.SupplierStocks;

            var DummyOptions = new DbContextOptionsBuilder<FoodChainsDbContext>().Options;

            var dbContextMock = new DbContextMock<FoodChainsDbContext>(DummyOptions);
            var categoriesDbSetMock = dbContextMock.CreateDbSetMock(x => x.SupplyCategories, supplyCategories);
            var linksDbSetMock = dbContextMock.CreateDbSetMock(x => x.SupplyLinks, links);
            var supsDbSetMock = dbContextMock.CreateDbSetMock(x => x.Suppliers, sups);
            var locsDbSetMock = dbContextMock.CreateDbSetMock(x => x.Locations, locs);
            var dishesDbSetMock = dbContextMock.CreateDbSetMock(x => x.Dishes, dishes);
            var reqsDbSetMock = dbContextMock.CreateDbSetMock(x => x.DishRequirements, reqs);
            var stocksDbSetMock = dbContextMock.CreateDbSetMock(x => x.SupplierStocks, stocks);
            dbContextMock.Setup(m => m.SupplyCategories).Returns(categoriesDbSetMock.Object);
            dbContextMock.Setup(m => m.SupplyLinks).Returns(linksDbSetMock.Object);
            dbContextMock.Setup(m => m.Suppliers).Returns(supsDbSetMock.Object);
            dbContextMock.Setup(m => m.Locations).Returns(locsDbSetMock.Object);
            dbContextMock.Setup(m => m.Dishes).Returns(dishesDbSetMock.Object);
            dbContextMock.Setup(m => m.DishRequirements).Returns(reqsDbSetMock.Object);
            dbContextMock.Setup(m => m.SupplierStocks).Returns(stocksDbSetMock.Object);
            _context = dbContextMock.Object;
            _controller = new SupplyCategoriesController(_context, _logger);
        }
    }
}
