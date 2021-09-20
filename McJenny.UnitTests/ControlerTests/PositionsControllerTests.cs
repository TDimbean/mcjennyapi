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
    public class PositionsControllerTests
    {
        private static PositionsController _controller;
        private static FoodChainsDbContext _context;
        private static ILogger<PositionsController> _logger;

        [ClassInitialize]
        public static void Init(TestContext testContext)
        {
            _logger = (new Mock<ILogger<PositionsController>>()).Object;
            Setup();
        }

        [TestInitialize]
        public void InitTest() => Setup();

        #region Gets

        #region Basic

        [TestMethod]
        public async Task GetPositions_HappyFlow_ShouldReturnAllPositions()
        {
            // Arrange
            var expected = (object)await _context.Positions
                .Select(p => new
                {
                    ID = p.PositionId,
                    p.Title,
                    HourlyWage = string.Format("{0:C}",
                    p.Wage)
                })
                .ToListAsync();

            // Act
            var result = (object)(await _controller.GetPositions()).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetPosition_HappyFlow_ShouldReturnPosition()
        {
            // Arrange
            var position = await GetFirstPosition();
            var expected = (object)new
            {
                position.Title,
                HourlyWage = string.Format("{0:C}", position.Wage)
            };

            // Act
            var result = (object)(await _controller.GetPosition(position.PositionId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetPosition_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetPosition(GenBadPositionId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetPositionBasic_HappyFlow_ShouldReturnPosition()
        {
            // Arrange
            var expected = await GetFirstPosition();

            // Act
            var result = (await _controller.GetPositionBasic(GetFirstPositionId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetPositionBasic_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetPositionBasic(GenBadPositionId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetEmployees_HappyFlow_ShouldReturnPosition()
        {
            // Arrange
            var posId = GetFirstPositionId;

            var expected = (object)await _context.Employees
                .Where(e => e.PositionId == posId)
                .Select(e => string.Format("({0}) {1}, {2}", e.EmployeeId, e.LastName, e.FirstName))
                .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetEmployees(posId)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetEmployees_InexistentId_ShouldReturnNotFound()
        {
            // Act
            var result = (await _controller.GetEmployees(GenBadPositionId)).Result;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        #endregion

        #region Advanced

        [TestMethod]
        public async Task GetPositionsFiltered_TitleMatch_ShouldReturnFiltered()
        {
            // Arrange
            var filter = await _context.Positions.Select(p => p.Title).FirstOrDefaultAsync();

            var expected = (object)await _context.Positions
                 .Select(p => new { p.PositionId, p.Title, p.Wage })
                 .Where(p => p.Title.ToUpper()
                     .Contains(filter.ToUpper()))
                 .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetPositionsFiltered(filter)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetPositionsPaged_HappyFlow_ShouldReturnPaged()
        {
            // Arrange
            var pgSz = 2;
            var pgInd = 1;

            var expected = (object)await _context.Positions
                 .Select(p => new { p.PositionId, p.Title, p.Wage })
                 .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetPositionsPaged(pgInd, pgSz)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetPositionsPaged_EndPageIncomplete_ShouldReturnPaged()
        {
            // Arrange
            var PositionCount = await _context.Positions.CountAsync();
            var pgSz = 1;

            while (true)
            {
                if (PositionCount % pgSz == 0) pgSz++;
                else break;
            }

            var pgInd = PositionCount / (pgSz - 1);

            var expected = (object)await _context.Positions
                 .Select(p => new { p.PositionId, p.Title, p.Wage })
                 .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();

            // Act
            var call = (await _controller.GetPositionsPaged(pgInd, pgSz)).Value;
            var result = (object)call;
            var resCount = (result as IEnumerable<dynamic>).Count();

            // Assert
            result.Should().BeEquivalentTo(expected);
            resCount.Should().BeLessThan(pgSz);
        }

        [TestMethod]
        public async Task GetPositionsPaged_SizeOver_ShouldReturnPaged()
        {
            // Arrange
            var PositionCount = await _context.Positions.CountAsync();
            var pgSz = PositionCount + 1;
            var pgInd = 1;

            var expected = (object)await _context.Positions
                                  .Select(p => new { p.PositionId, p.Title, p.Wage })
                 .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetPositionsPaged(pgInd, pgSz)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetPositionsPaged_IndexOver_ShouldReturnPaged()
        {
            // Arrange
            var PositionCount = await _context.Positions.CountAsync();
            var pgInd = PositionCount + 1;
            var pgSz = 1;

            var expected = (object)await _context.Positions
                .Select(p => new { p.PositionId, p.Title, p.Wage })
                 .Skip(pgSz * (pgInd - 1))
                .Take(pgSz)
                .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetPositionsPaged(pgInd, pgSz)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetPositionsSorted_ByTitle_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "title";

            var expected = (object)await _context.Positions
                            .Select(p => new { p.PositionId, p.Title, p.Wage })
                            .OrderBy(p => p.Title)
                            .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetPositionsSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetPositionsSorted_ByWage_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "wage";

            var expected = (object)await _context.Positions
                            .Select(p => new { p.PositionId, p.Title, p.Wage })
                            .OrderBy(p => p.Wage)
                            .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetPositionsSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetPositionsSorted_Desc_ShouldReturnSorted()
        {
            // Arrange
            var sortBy = "title";

            var expected = (object)await _context.Positions
                            .Select(p => new { p.PositionId, p.Title, p.Wage })
                            .OrderByDescending(p => p.Title)
                            .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetPositionsSorted(sortBy, true)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task GetPositionsSorted_GarbledText_ShouldDefaultoIdSort()
        {
            // Arrange
            var sortBy = "asd";

            var expected = (object)await _context.Positions
                            .Select(p => new { p.PositionId, p.Title, p.Wage })
                            .OrderBy(p => p.PositionId)
                            .ToArrayAsync();

            // Act
            var result = (object)(await _controller.GetPositionsSorted(sortBy, false)).Value;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        #endregion

        #endregion

        #region Creates

        [TestMethod]
        public async Task CreatePosition_HappyFlow_ShouldCreateAndReturnPosition()
        {
            // Arrange
            var position = new Position { Title = "Plumber", Wage = 24.30m };

            // Act
            await _controller.CreatePosition(position);
            var result = (object)(await _controller.GetPositionBasic(position.PositionId)).Value;

            var expected = (object)position;

            // Assert
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public async Task CreatePosition_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var position = new Position
            {
                PositionId = await _context.Positions.Select(p => p.PositionId).LastOrDefaultAsync() + 1,
                Title = "Plumber",
                Wage = 24.30m
            };

            // Act
            var result = (await _controller.CreatePosition(position)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow(null)]
        [DataRow("123456789012345678911234567892123456789412345678941")]
        public async Task CreatePosition_BadTitles_ShouldReturnBadRequest(string title)
        {
            // Arrange
            var position = new Position { Title = title, Wage = 24.30m };

            // Act
            var result = (await _controller.CreatePosition(position)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreatePosition_DuplicateTitle_ShouldReturnBadRequest()
        {
            // Arrange
            var exTitle = await _context.Positions.Select(p => p.Title).FirstOrDefaultAsync();
            var position = new Position { Title = exTitle };
            var lwrPos = new Position { Title = exTitle.ToLower() };
            var uprPos = new Position { Title = exTitle.ToUpper() };

            // Act
            var result = (await _controller.CreatePosition(position)).Result;
            var lwrResult = (await _controller.CreatePosition(lwrPos)).Result;
            var uprResult = (await _controller.CreatePosition(uprPos)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            lwrResult.Should().BeOfType<BadRequestResult>();
            uprResult.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreatePosition_WageTooLow_ShouldReturnBadRequest()
        {
            // Arrange
            var position = new Position { Title = "Plumber", Wage = 0m };

            // Act
            var result = (await _controller.CreatePosition(position)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreatePosition_EmployeesNull_ShouldReturnBadRequest()
        {
            // Arrange
            var position = new Position { Title = "Plumber", Wage = 24.30m, Employees = null };

            // Act
            var result = (await _controller.CreatePosition(position)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        [TestMethod]
        public async Task CreatePosition_EmployeesNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var position = new Position
            {
                Title = "Plumber",
                Wage = 24.30m,
                Employees = new Employee[] { new Employee() }
            };

            // Act
            var result = (await _controller.CreatePosition(position)).Result;

            // Assert
            result.Should().BeOfType<BadRequestResult>();
        }

        #endregion

        #region Updates

        [TestMethod]
        public async Task UpdatePosition_HappyFlow_ShouldCreateAndReturnPosition()
        {
            // Arrange
            var oldPos = await _context.Positions.FindAsync(GetFirstPositionId);
            var oldPosCopy = new Position
            {
                Employees = oldPos.Employees,
                PositionId = oldPos.PositionId,
                Title = oldPos.Title,
                Wage = oldPos.Wage
            };

            var position = new Position
            {
                Title = "Plumber",
                Wage = 22m
            };

            // Act
            await _controller.UpdatePosition(oldPos.PositionId, position);
            var result = (await _controller.GetPositionBasic(oldPos.PositionId)).Value;

            position.PositionId = oldPosCopy.PositionId;
            position.Employees = await _context.Employees
                .Where(e => e.PositionId == position.PositionId)
                .ToArrayAsync();

            var resEmps = await _context.Employees
                .Where(e => e.PositionId == result.PositionId)
                .ToArrayAsync();
            result.Employees = resEmps;

            // Assert
            result.Should().BeEquivalentTo(position);
            result.Should().NotBeEquivalentTo(oldPosCopy);
        }

        [TestMethod]
        public async Task UpdatePosition_InexistentId_ShouldReturnNotFound()
        {
            // Arrange
            var oldPos = await _context.Positions.FindAsync(GetFirstPositionId);
            var oldPosCopy = new Position
            {
                Employees = oldPos.Employees,
                PositionId = oldPos.PositionId,
                Title = oldPos.Title,
                Wage = oldPos.Wage
            };

            var position = new Position
            {
                Title = "Plumber",
                Wage = 22m
            };

            // Act
            var result = await _controller.UpdatePosition(GenBadPositionId, position);
            var updPos = (await _controller.GetPositionBasic(oldPos.PositionId)).Value;

            // Assert
            result.Should().BeOfType<NotFoundResult>();
            updPos.Should().BeEquivalentTo(oldPosCopy);
        }

        [TestMethod]
        public async Task UpdatePosition_TriesToSetId_ShouldReturnBadRequest()
        {
            // Arrange
            var oldPos = await _context.Positions.FindAsync(GetFirstPositionId);
            var oldPosCopy = new Position
            {
                Employees = oldPos.Employees,
                PositionId = oldPos.PositionId,
                Title = oldPos.Title,
                Wage = oldPos.Wage
            };

            var position = new Position
            {
                Title = "Plumber",
                Wage = 22m,
                PositionId = GenBadPositionId
            };

            // Act
            var result = await _controller.UpdatePosition(oldPos.PositionId, position);
            var updatedPos = await _context.Positions.FindAsync(oldPos.PositionId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedPos.Should().BeEquivalentTo(oldPosCopy);
        }

        [DataTestMethod]
        [DataRow(" ")]
        [DataRow("123456789012345678911234567892123456789412345678941")]
        public async Task UpdatePosition_BadTitles_ShouldReturnBadRequest(string title)
        {
            // Arrange
            var oldPos = await _context.Positions.FindAsync(GetFirstPositionId);
            var oldPosCopy = new Position
            {
                Employees = oldPos.Employees,
                PositionId = oldPos.PositionId,
                Title = oldPos.Title,
                Wage = oldPos.Wage
            };

            var position = new Position
            {
                Wage = 22m,
                Title = title
            };

            // Act
            var result = await _controller.UpdatePosition(oldPos.PositionId, position);
            var updatedPos = await _context.Positions.FindAsync(oldPos.PositionId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedPos.Should().BeEquivalentTo(oldPosCopy);
        }

        [TestMethod]
        public async Task UpdatePosition_DuplicateTitle_ShouldReturnBadRequest()
        {
            // Arrange
            var dupeSource = await _context.Positions
                .Select(p => p.Title)
                .Skip(1)
                .FirstOrDefaultAsync();

            var oldPos = await _context.Positions.FindAsync(GetFirstPositionId);
            var oldPosCopy = new Position
            {
                Employees = oldPos.Employees,
                PositionId = oldPos.PositionId,
                Title = oldPos.Title,
                Wage = oldPos.Wage
            };

            var position = new Position
            {
                Wage = 22m,
                Title = dupeSource
            };

            // Act
            var result = await _controller.UpdatePosition(oldPos.PositionId, position);
            var updatedPos = await _context.Positions.FindAsync(oldPos.PositionId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedPos.Should().BeEquivalentTo(oldPosCopy);
        }

        [TestMethod]
        public async Task UpdatePosition_WageTooLow_ShouldReturnBadRequest()
        {
            // Arrange
            var oldPos = await _context.Positions.FindAsync(GetFirstPositionId);
            var oldPosCopy = new Position
            {
                Employees = oldPos.Employees,
                PositionId = oldPos.PositionId,
                Title = oldPos.Title,
                Wage = oldPos.Wage
            };

            var position = new Position
            {
                Title = "Plumber",
                Wage = -1m,
            };

            // Act
            var result = await _controller.UpdatePosition(oldPos.PositionId, position);
            var updatedPos = await _context.Positions.FindAsync(oldPos.PositionId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedPos.Should().BeEquivalentTo(oldPosCopy);
        }

        [TestMethod]
        public async Task UpdatePosition_EmployeesNull_ShouldReturnBadRequest()
        {
            // Arrange
            var oldPos = await _context.Positions.FindAsync(GetFirstPositionId);
            var oldPosCopy = new Position
            {
                Employees = oldPos.Employees,
                PositionId = oldPos.PositionId,
                Title = oldPos.Title,
                Wage = oldPos.Wage
            };

            var position = new Position
            {
                Title = "Plumber",
                Wage = 22m,
                Employees = null
            };

            // Act
            var result = await _controller.UpdatePosition(oldPos.PositionId, position);
            var updatedPos = await _context.Positions.FindAsync(oldPos.PositionId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedPos.Should().BeEquivalentTo(oldPosCopy);
        }

        [TestMethod]
        public async Task UpdatePosition_EmployeesNotEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var oldPos = await _context.Positions.FindAsync(GetFirstPositionId);
            var oldPosCopy = new Position
            {
                Employees = oldPos.Employees,
                PositionId = oldPos.PositionId,
                Title = oldPos.Title,
                Wage = oldPos.Wage
            };

            var position = new Position
            {
                Title = "Plumber",
                Wage = 22m,
                Employees = new Employee[] { new Employee() }
            };

            // Act
            var result = await _controller.UpdatePosition(oldPos.PositionId, position);
            var updatedPos = await _context.Positions.FindAsync(oldPos.PositionId);

            // Assert
            result.Should().BeOfType<BadRequestResult>();
            updatedPos.Should().BeEquivalentTo(oldPosCopy);
        }

        #endregion

        [TestMethod]
        public void PositionExists_ItDoes_ShoudReturnTrue()
        {
            //Arrange
            var methodInfo = typeof(PositionsController).GetMethod("PositionExists", BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GetFirstPositionId };

            // Act
            var result = ((methodInfo.Invoke(_controller, parameters)) as Task<bool>).Result;

            // Assert
            result.Should().BeTrue();
        }

        [TestMethod]
        public void PositionExists_ItDoesNot_ShoudReturnFalse()
        {
            //Arrange
            var methodInfo = typeof(PositionsController).GetMethod("PositionExists", BindingFlags.NonPublic | BindingFlags.Instance);
            var parameters = new object[] { GenBadPositionId };

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
            _controller = new PositionsController(_context, _logger);
        }

        /// Helpers
        int GetFirstPositionId => _context.Positions.Select(p => p.PositionId).OrderBy(id => id).FirstOrDefault();
        int GenBadPositionId => _context.Positions.Select(p => p.PositionId).ToArray().OrderBy(id => id).LastOrDefault() + 1;
        async Task<Position> GetFirstPosition() => await _context.Positions.FirstOrDefaultAsync();

        private static void Setup()
        {
            var positions = FakeRepo.Positions;
            var employees = FakeRepo.Employees;

            var DummyOptions = new DbContextOptionsBuilder<FoodChainsDbContext>().Options;

            var dbContextMock = new DbContextMock<FoodChainsDbContext>(DummyOptions);
            var positionsDbSetMock = dbContextMock.CreateDbSetMock(x => x.Positions, positions);
            var employeesDbSetMock = dbContextMock.CreateDbSetMock(x => x.Employees, employees);
            dbContextMock.Setup(m => m.Positions).Returns(positionsDbSetMock.Object);
            dbContextMock.Setup(m => m.Employees).Returns(employeesDbSetMock.Object);
            _context = dbContextMock.Object;
            _controller = new PositionsController(_context, _logger);
        }
    }
}
