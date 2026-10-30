using ImmichFrame.Core.Interfaces;
using ImmichFrame.WebApi.Controllers;
using ImmichFrame.WebApi.Helpers;
using ImmichFrame.WebApi.Models;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace ImmichFrame.WebApi.Tests.Controllers
{
    [TestFixture]
    public class ClientIdentifierTests
    {
        private static readonly string LongId = new string('a', 100) + "\n\u001b[31m";
        private static readonly string Expected = new string('a', ClientIdentifier.MaxLength);

        [Test]
        public void Sanitize_KeepsAllowedCharactersAndCapsTheLength()
        {
            Assert.Multiple(() =>
            {
                Assert.That(ClientIdentifier.Sanitize("frame-1.kitchen_2 Küche"), Is.EqualTo("frame1.kitchen2 Küche"));
                Assert.That(ClientIdentifier.Sanitize(LongId), Is.EqualTo(Expected));
                Assert.That(ClientIdentifier.Sanitize(null), Is.Empty);
            });
        }

        [Test]
        public async Task WeatherController_LogsTheCappedIdentifier()
        {
            var logger = new Mock<ILogger<AssetController>>();
            var controller = new WeatherController(logger.Object, Mock.Of<IWeatherService>());

            await controller.GetWeather(LongId);

            VerifyLoggedIdentifier(logger);
        }

        [Test]
        public void ConfigController_LogsTheCappedIdentifier()
        {
            var logger = new Mock<ILogger<AssetController>>();
            var controller = new ConfigController(logger.Object, new GeneralSettings());

            controller.GetConfig(LongId);

            VerifyLoggedIdentifier(logger);
        }

        [Test]
        public async Task CalendarController_LogsTheCappedIdentifier()
        {
            var logger = new Mock<ILogger<AssetController>>();
            var calendar = new Mock<ICalendarService>();
            calendar.Setup(c => c.GetAppointments()).ReturnsAsync(new List<IAppointment>());
            var controller = new CalendarController(logger.Object, calendar.Object);

            await controller.GetAppointments(LongId);

            VerifyLoggedIdentifier(logger);
        }

        private static void VerifyLoggedIdentifier(Mock<ILogger<AssetController>> logger) =>
            logger.Verify(l => l.Log(LogLevel.Debug, It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains($"'{Expected}'")),
                It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
