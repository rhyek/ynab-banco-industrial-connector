using System.Collections;
using System.Collections.Generic;
using YnabBancoIndustrialConnector.Infrastructure.YnabController.YnabApiResponses;
using Xunit;

namespace YnabBancoIndustrialConnector.Infrastructure.YnabController.UnitTests;

public class YnabTransactionMetadataTest
{
  [Theory]
  [MemberData(nameof(CanFormMetadataObjectFromMemoData))]
  public void CanFormMetadataObjectFromMemo(
    string memo,
    YnabTransactionMetadata metadataObject)
  {
    var actual = YnabTransactionMetadata.DeserializeMemo(memo);
    Assert.Equal(metadataObject, actual);
  }

  public static IEnumerable<object[]> CanFormMetadataObjectFromMemoData =
    new List<object[]> {
      new object[]
        {"reference:3345", new YnabTransactionMetadata("3345", false, null)},
      new object[] {"", new YnabTransactionMetadata(null, false, null)},
      new object[]
        {"hello, there!", new YnabTransactionMetadata(null, false, null)},
      new object[] {
        "reference:3345; auto:1; description:hi, mate",
        new YnabTransactionMetadata("3345", true, "hi, mate")
      },
      new object[] {
        "ref:3345; auto:1; desc:hi, mate",
        new YnabTransactionMetadata("3345", true, "hi, mate")
      },
      new object[] {
        "ref: 3345; auto: 1; desc: HI, MATE",
        new YnabTransactionMetadata("3345", true, "HI, MATE")
      },
      new object[] {
        "ref: 3345; desc: BELWEB TRANSF TERC mayra_ pago par; auto: 1",
        new YnabTransactionMetadata("3345", true,
          "BELWEB TRANSF TERC mayra; pago par")
      },
      new object[] {
        "ref: 3345; desc: BELWEB TRANSF TERC mayra_ pago par; mydesc: otrodato; auto: 1",
        new YnabTransactionMetadata("3345", true,
          "BELWEB TRANSF TERC mayra; pago par", "otrodato")
      },
      new object[] {
        "ref: 3345; desc: BELWEB TRANSF TERC mayra_ pago par; midesc: otrodato; auto: 1",
        new YnabTransactionMetadata("3345", true,
          "BELWEB TRANSF TERC mayra; pago par", "otrodato")
      },
      new object[] {
        "ref: 3345; desc: a description; ovrddate: 2012-03-22; auto: 1",
        new YnabTransactionMetadata("3345", true, "a description",
          overrideDate: new(2012, 03, 22)),
      }
    };

  [Theory]
  [MemberData(nameof(CanFormMemoFromMetadataObjectData))]
  public void CanFormMemoFromMetadataObject(
    YnabTransactionMetadata metadataObject,
    string memo)
  {
    var actualMemo = metadataObject
      .SerializeMemo();
    Assert.Equal(memo, actualMemo);
  }

  public static IEnumerable<object[]> CanFormMemoFromMetadataObjectData =>
    new List<object[]> {
      new object[] {
        new YnabTransactionMetadata("3345", false, null), "ref: 3345; auto: 0"
      },
      new object[] {new YnabTransactionMetadata(null, false, null), "auto: 0"},
      new object[] {
        new YnabTransactionMetadata("3345", true, "hi, mate"),
        "ref: 3345; desc: hi, mate; auto: 1"
      },
      new object[] {
        new YnabTransactionMetadata("3345", true, "HI, MATE"),
        "ref: 3345; desc: HI, MATE; auto: 1"
      },
      new object[] {
        new YnabTransactionMetadata("3345", true,
          "BELWEB TRANSF TERC mayra; pago par"),
        "ref: 3345; desc: BELWEB TRANSF TERC mayra_ pago par; auto: 1"
      },
      new object[] {
        new YnabTransactionMetadata("3345", true, "a description",
          overrideDate: new(2012, 03, 22)),
        "ref: 3345; desc: a description; ovrddate: 2012-03-22; auto: 1"
      },
    };
}
