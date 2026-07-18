using Project.API.Controllers.V1;
using Project.Core.Entities.Business;
using Project.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace Project.UnitTest.API
{
    public class ProductControllerTests
    {
        private Mock<IProductService> _productServiceMock = null!;
        private Mock<ILogger<ProductController>> _loggerMock = null!;
        private MemoryCache _memoryCache = null!;
        private ProductController _productController = null!;

        [SetUp]
        public void Setup()
        {
            _productServiceMock = new Mock<IProductService>();
            _loggerMock = new Mock<ILogger<ProductController>>();
            _memoryCache = new MemoryCache(
                new MemoryCacheOptions()
            );
            _productController = new ProductController(_loggerMock.Object, _productServiceMock.Object, _memoryCache);
        }

        [Test]
        public async Task Get_ReturnsOkWithListOfProducts()
        {
            // Arrange
            var products = new List<ProductViewModel>
            {
                new()
                { 
                    Id = 1, 
                    Code = "P001", 
                    Name = "Product A", 
                    Price = 9.99f, 
                    IsActive = true 
                },
                new()
                { 
                    Id = 2, 
                    Code = "P002", 
                    Name = "Product B", 
                    Price = 19.99f, 
                    IsActive = true 
                }
            };

            _productServiceMock.Setup(service => service.GetAll(CancellationToken.None))
                      .ReturnsAsync(products);

            // Act
            IActionResult result = await _productController.Get(CancellationToken.None);

            // Assert
            var okResult = result as OkObjectResult;

            Assert.That(okResult, Is.Not.Null);

            var response = okResult!.Value as ResponseViewModel<IEnumerable<ProductViewModel>>;

            Assert.That(response, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(response!.Success, Is.True);

                Assert.That(
                    response.Message,
                    Is.EqualTo("Products retrieved successfully")
                );

                Assert.That(response.Data, Is.Not.Null);

                Assert.That(
                    response.Data!.Count(),
                    Is.EqualTo(products.Count)
                );

                Assert.That(
                    response.Data.Select(products => products.Id),
                    Is.EqualTo(products.Select(product => product.Id))
                );
            });
        }

        [TearDown]
        public void TearDown()
        {
            _memoryCache.Dispose();
        }

        // Add more test methods for other controller actions, such as Create, Update, Delete, etc.

    }
}
