using System;
using Microsoft.Extensions.Logging.Abstractions;
using POC_Facturation.Services;
using POC_Facturation.Tests.Mocks;
using Xunit;

namespace POC_Facturation.Tests.Services;

/// <summary>
/// Tests unitaires pour GlobalExceptionHandler respectant les conventions AAA et F.I.R.S.T.
/// </summary>
public class GlobalExceptionHandlerTests
{
    [Fact]
    public void Should_NotifyUserWithErrorMessage_When_StandardExceptionIsHandled()
    {
        // Arrange
        var fakeNotifier = new FakeUserNotifier();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, fakeNotifier);
        var exception = new InvalidOperationException("Test d'échec de base de données");

        // Act
        handler.HandleException(exception, "TestUI", isFatal: false);

        // Assert
        Assert.Equal(1, fakeNotifier.NotificationCount);
        Assert.Equal("Erreur inattendue", fakeNotifier.LastTitle);
        Assert.NotNull(fakeNotifier.LastMessage);
        Assert.Contains("Test d'échec de base de données", fakeNotifier.LastMessage);
        Assert.Contains("TestUI", fakeNotifier.LastMessage);
    }

    [Fact]
    public void Should_UnwrapInnerException_When_AggregateExceptionIsHandled()
    {
        // Arrange
        var fakeNotifier = new FakeUserNotifier();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, fakeNotifier);
        var innerEx = new TimeoutException("Délai de connexion dépassé");
        var aggregateEx = new AggregateException("Erreur globale", innerEx);

        // Act
        handler.HandleException(aggregateEx, "TaskScheduler", isFatal: false);

        // Assert
        Assert.Equal(1, fakeNotifier.NotificationCount);
        Assert.NotNull(fakeNotifier.LastMessage);
        Assert.Contains("Délai de connexion dépassé", fakeNotifier.LastMessage);
    }

    [Fact]
    public void Should_NotifyWithFatalTitle_When_IsFatalIsTrue()
    {
        // Arrange
        var fakeNotifier = new FakeUserNotifier();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, fakeNotifier);
        var exception = new OutOfMemoryException("Mémoire insuffisante");

        // Act
        handler.HandleException(exception, "AppDomain", isFatal: true);

        // Assert
        Assert.Equal(1, fakeNotifier.NotificationCount);
        Assert.Equal("Erreur critique de l'application", fakeNotifier.LastTitle);
        Assert.NotNull(fakeNotifier.LastMessage);
        Assert.Contains("critique", fakeNotifier.LastMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Should_NotNotify_When_ExceptionIsNull()
    {
        // Arrange
        var fakeNotifier = new FakeUserNotifier();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, fakeNotifier);

        // Act
        handler.HandleException(null, "Test");

        // Assert
        Assert.Equal(0, fakeNotifier.NotificationCount);
        Assert.Null(fakeNotifier.LastMessage);
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_LoggerIsNull()
    {
        // Arrange
        var fakeNotifier = new FakeUserNotifier();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GlobalExceptionHandler(null!, fakeNotifier));
    }

    [Fact]
    public void Should_ThrowArgumentNullException_When_NotifierIsNull()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, null!));
    }

    [Fact]
    public void Should_NotThrow_When_NotifierThrowsException()
    {
        // Arrange
        var throwingNotifier = new ThrowingUserNotifier();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, throwingNotifier);
        var exception = new InvalidOperationException("Erreur de test");

        // Act & Assert (Should not throw)
        var exceptionOccurred = Record.Exception(() => handler.HandleException(exception, "TestIsolation"));
        Assert.Null(exceptionOccurred);
    }

    private class ThrowingUserNotifier : IUserNotifier
    {
        public void NotifyWarning(string message, string title = "Avertissement") => throw new InvalidOperationException("Simulation échec UI");
        public void NotifyError(string message, string title = "Erreur inattendue") => throw new InvalidOperationException("Simulation échec UI");
    }
}
