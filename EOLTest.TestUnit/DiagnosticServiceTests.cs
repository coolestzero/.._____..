using EOLTest.Models;
using EOLTest.Services;
using EOLTest.Services.Impl.Mdb;
using EOLTest.Services.Mdb;
using Moq;
using Serilog;
using System.Data;
using Xunit;

namespace EOLTest.TestUnit
{
    public class DiagnosticServiceTests
    {
        private readonly Mock<IDataBaseService> _mockDb;
        private readonly DiagnosticService _service;

        public DiagnosticServiceTests()
        {
            // 创建真实的Logger（会生成日志文件）
            var logger = new LoggerConfiguration()
                .WriteTo.File("logs/test_log.txt")
                .CreateLogger();
            _mockDb = new Mock<IDataBaseService>();
            _service = new DiagnosticService(_mockDb.Object, logger);
        }

        [Fact]
        public async Task GetEcuCanIdAsync_ValidEcuName_ReturnsEcuCanId()
        {
            // Arrange
            var testTable = new DataTable();
            testTable.Columns.Add("modname");
            testTable.Columns.Add("tx_address");
            testTable.Columns.Add("rx_address");
            testTable.Columns.Add("LinID");
            testTable.Columns.Add("Doip");

            testTable.Rows.Add("ECU_Engine", "0x7E0", "0x7E8", "0x12", "0x0E80");

            _mockDb.Setup(db => db.QueryAsync(It.IsAny<string>()))
                   .ReturnsAsync(testTable);

            // Act
            var result = await _service.GetEcuCanIdAsync("ECU_Engine");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ECU_Engine", result.EcuName);
            Assert.Equal("0x7E0", result.TxId);
            Assert.Equal("0x7E8", result.RxId);
        }

        [Fact]
        public async Task GetEcuCanIdAsync_EcuNotFound_ReturnsNull()
        {
            // Arrange
            var emptyTable = new DataTable();
            emptyTable.Columns.Add("modname");
            emptyTable.Columns.Add("tx_address");

            _mockDb.Setup(db => db.QueryAsync(It.IsAny<string>()))
                   .ReturnsAsync(emptyTable);

            // Act
            var result = await _service.GetEcuCanIdAsync("NonExistentECU");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetEcuCanIdAsync_DbThrowsException_PropagatesException()
        {
            // Arrange
            _mockDb.Setup(db => db.QueryAsync(It.IsAny<string>()))
                   .ThrowsAsync(new Exception("数据库连接失败"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(
                () => _service.GetEcuCanIdAsync("ECU_Engine"));

            // 注意：实际消息包含表名
            Assert.Contains("查询表 CANID 失败", exception.Message);
        }
    }
}