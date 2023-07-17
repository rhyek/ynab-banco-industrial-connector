using System;
using System.Collections.Generic;
using YnabBancoIndustrialConnector.Infrastructure.BancoIndustrialScraper.Models;
using Xunit;

namespace BIMonitor.UnitTests;

public class MobileNotificationTransactionTests
{
  [Theory]
  [MemberData(nameof(ParseMobileNotificationsData))]
  public void ParseMobileNotifications(string message,
    MobileNotificationTransaction? expected)
  {
    var currentDateTime = new DateTime(2022, 2, 28, 11, 0, 0);
    var actual =
      MobileNotificationTransaction.FromMessage(message, currentDateTime);
    Assert.Equal(expected, actual);
  }

  public static IEnumerable<object?[]> ParseMobileNotificationsData =>
    new List<object?[]> {
      new object?[] {
        "BiMovil: Se ha realizado un consumo por US.5.00 en el Establecimiento: PAYPAL *MEDIUM.COM Cuenta: BICHEQUE4 28-Feb 04:11 Aut.122454.",
        new MobileNotificationTransaction() {
          Reference = "122454", Currency = "USD",
          Amount = 5.0m, Type = TransactionType.Debit,
          Description = "PAYPAL *MEDIUM.COM",
          Account = "BICHEQUE4", DateTime = new(2022, 2, 28, 4, 11, 0),
          Origin = TransactionOrigin.Establishment
        }
      },
      new object?[] {
        "BiMovil: Se ha realizado un consumo por Q.649.00 en el Establecimiento: FITNESS ONE CAYALA Cuenta: BICHEQUE4 01-Feb 18:38 Autorizacion: 201910.",
        new MobileNotificationTransaction() {
          Reference = "201910", Currency = "GTQ",
          Amount = 649.00m, Type = TransactionType.Debit,
          Description = "FITNESS ONE CAYALA",
          Account = "BICHEQUE4", DateTime = new(2022, 2, 1, 18, 38, 0),
          Origin = TransactionOrigin.Establishment
        }
      },
      new object?[] {
        "BiMovil: Se ha operado un debito por US.1,278.99 en la Agencia: PAGO ELECTRONICO BCA.TOTAL Cuenta: MONE 01-Feb 15:30 Autorizacion: 894839.",
        new MobileNotificationTransaction() {
          Reference = "894839", Currency = "USD",
          Amount = 1278.99m, Type = TransactionType.Debit,
          Description = "PAGO ELECTRONICO BCA.TOTAL",
          Account = "MONE", DateTime = new(2022, 2, 1, 15, 30, 0),
          Origin = TransactionOrigin.Agency
        }
      },
      new object?[] {
        "BiMovil: Se ha operado un credito por US.2,000.00 en la Agencia: CENTRAL Cuenta: MONE 28-Feb 11:58 Autorizacion: 1782905.",
        new MobileNotificationTransaction() {
          Reference = "1782905", Currency = "USD",
          Amount = 2000.0m, Type = TransactionType.Credit,
          Description = "CENTRAL",
          Account = "MONE", DateTime = new(2022, 2, 28, 11, 58, 0),
          Origin = TransactionOrigin.Agency
        }
      },
      new object?[] {
        "BiMovil: Se ha realizado un consumo por Q.163.34 en el Establecimiento: SUPERMERCADOS LA TORRE Cuenta: BICHEQUE4 04-Mar 19:40 Aut.238022.",
        new MobileNotificationTransaction() {
          Reference = "238022", Currency = "GTQ",
          Amount = 163.34m, Type = TransactionType.Debit,
          Description = "SUPERMERCADOS LA TORRE",
          Account = "BICHEQUE4", DateTime = new(2022, 3, 4, 19, 40, 0),
          Origin = TransactionOrigin.Establishment
        }
      },
      new object?[] {
        "BiMovil: Se ha realizado un consumo por CRC.62054.00 en el Establecimiento: LA ESQUINA DE BUENOS AI Cuenta: BICHEQUE4 21-Abr 14:39 Aut.183575.",
        new MobileNotificationTransaction() {
          Reference = "183575",
          Currency = "CRC",
          Amount = 62054.00m,
          Type = TransactionType.Debit,
          Description = "LA ESQUINA DE BUENOS AI",
          Account = "BICHEQUE4",
          DateTime = new(2022, 4, 21, 14, 39, 0),
          Origin = TransactionOrigin.Establishment
        }
      },
      new object?[] {
        "BiMovil: Se ha realizado un consumo por Q.519.68 en el Establecimiento: VOLARIS Cuenta: TCREDITO 11-Abr 22:48 Aut.238449.",
        new MobileNotificationTransaction() {
          Reference = "238449",
          Currency = "GTQ",
          Amount = 519.68m,
          Type = TransactionType.Debit,
          Description = "VOLARIS",
          Account = "TCREDITO",
          DateTime = new(2022, 4, 11, 22, 48, 0),
          Origin = TransactionOrigin.Establishment
        }
      },
      new object?[] {
        "BiMovil: Se ha realizado un consumo por US.519.68 en el Establecimiento: VOLARIS Cuenta: TCREDITO 11-Abr 22:48 Aut.238449.",
        new MobileNotificationTransaction() {
          Reference = "238449",
          Currency = "USD",
          Amount = 519.68m,
          Type = TransactionType.Debit,
          Description = "VOLARIS",
          Account = "TCREDITO",
          DateTime = new(2022, 4, 11, 22, 48, 0),
          Origin = TransactionOrigin.Establishment
        }
      },
      new object?[] {
        "BiMovil: Consumo por Q.56.00 en el Establecimiento: PedidosYa RESTAURANTES Cuenta: BICHEQUE6 29-Jun 19:35 Aut.233207.",
        new MobileNotificationTransaction() {
          Reference = "233207",
          Currency = "GTQ",
          Amount = 56.00m,
          Type = TransactionType.Debit,
          Description = "PedidosYa RESTAURANTES",
          Account = "BICHEQUE6",
          DateTime = new(2022, 6, 29, 19, 35, 0),
          Origin = TransactionOrigin.Establishment
        }
      },      new object?[] {
        "BiMovil: Consumo por Q.60.00 en: PedidosYa RESTAURANTES Cuenta: BICHEQUE6 17-Jul 12:10 Aut.187045.",
        new MobileNotificationTransaction() {
          Reference = "187045",
          Currency = "GTQ",
          Amount = 60.00m,
          Type = TransactionType.Debit,
          Description = "PedidosYa RESTAURANTES",
          Account = "BICHEQUE6",
          DateTime = new(2022, 7, 17, 12, 10, 0),
          Origin = TransactionOrigin.Establishment
        }
      },
      new object?[] {
        "This is gibberish",
        null
      }
    };

  [Theory]
  [MemberData(nameof(ParseDateTimeData))]
  public void ParseDateTime(string message, DateTime currentDateTime,
    DateTime expectedDateTime)
  {
    var actual =
      MobileNotificationTransaction.ParseDateTime(message, currentDateTime);
    Assert.Equal(expectedDateTime, actual);
  }

  public static IEnumerable<object?[]> ParseDateTimeData =>
    new List<object?[]> {
      new object?[] {
        "28-Feb 11:58", new DateTime(2022, 2, 28),
        new DateTime(2022, 2, 28, 11, 58, 0)
      },
      new object?[] {
        "28-Abr 11:58", new DateTime(2022, 4, 28),
        new DateTime(2022, 4, 28, 11, 58, 0)
      },
      new object?[] {
        "25-Dic 11:58", new DateTime(2022, 2, 28),
        new DateTime(2021, 12, 25, 11, 58, 0)
      },
      new object?[] {
        "25-Dic 11:58", new DateTime(2021, 12, 28),
        new DateTime(2021, 12, 25, 11, 58, 0)
      },
      new object?[] {
        "25-Dic 11:58", new DateTime(2022, 12, 28),
        new DateTime(2022, 12, 25, 11, 58, 0)
      },
    };
}
