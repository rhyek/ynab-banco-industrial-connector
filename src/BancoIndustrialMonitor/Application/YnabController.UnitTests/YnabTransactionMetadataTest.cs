using BancoIndustrialMonitor.Application.YnabController.Models;
using Xunit;

namespace YnabController.UnitTests;

public class YnabTransactionMetadataTest
{
  [Theory]
  [InlineData("3345", false, null, "reference:3345")]
  [InlineData(null, false, null, "")]
  [InlineData(null, false, null, "hello, there!")]
  [InlineData("3345", true, "hi, mate", "reference:3345; auto:1; description:hi, mate")]
  [InlineData("3345", true, "hi, mate", "ref:3345; auto:1; desc:hi, mate")]
  [InlineData("3345", true, "HI, MATE", "ref: 3345; auto: 1; desc: HI, MATE")]
  public void CanFormMetadataObjectFromMemo(
    string? reference,
    bool auto,
    string? description,
    string memo)
  {
    var expected = new YnabTransactionMetadata(reference, auto, description);
    var actual = YnabTransactionMetadata.DeserializeMemo(memo);
    Assert.Equal(expected, actual);
  }
  
  [Theory]
  [InlineData("3345", false, null, "ref: 3345; auto: 0")]
  [InlineData(null, false, null, "auto: 0")]
  [InlineData("3345", true, "hi, mate", "ref: 3345; desc: hi, mate; auto: 1")]
  [InlineData("3345", true, "HI, MATE", "ref: 3345; desc: HI, MATE; auto: 1")]
  public void CanFormMemoFromMetadataObject(
    string? reference,
    bool auto,
    string? description,
    string memo)
  {
    var actual = new YnabTransactionMetadata(reference, auto, description)
      .SerializeMemo();
    Assert.Equal(memo, actual);
  }
}
