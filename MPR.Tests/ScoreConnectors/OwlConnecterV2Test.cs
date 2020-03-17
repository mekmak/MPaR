using System.Linq;
using MPR.ScoreConnectors;
using NUnit.Framework;

namespace MPR.Tests.ScoreConnectors
{
    [TestFixture]
    [Parallelizable(ParallelScope.Children)]
    public class OwlConnecterV2Test
    {
        #region GetWeeksToFetch

        [TestCase(1, 3, 10, "1,2,3")]
        [TestCase(4, 4, 10, "3,4,5,6")]
        [TestCase(1, 5, 3, "1,2,3")]
        [TestCase(10, 5, 11, "7,8,9,10,11")]

        [TestCase(8, 1, 20, "8")]
        [TestCase(8, 2, 20, "8,9")]
        [TestCase(8, 3, 20, "7,8,9")]
        [TestCase(8, 4, 20, "7,8,9,10")]
        [TestCase(8, 5, 20, "6,7,8,9,10")]
        [TestCase(8, 6, 20, "6,7,8,9,10,11")]
        public void NoSkip(int currentWeek, int numOfWeeks, int lastWeek, string expectedWeeksCsv)
        {
            var connector = new OwlConnectorV2();

            int[] expectedWeeks = ParseCsvNumbers(expectedWeeksCsv);
            int[] actualWeeks = connector.GetWeeksToFetch(currentWeek, numOfWeeks, lastWeek, new int[0]);

            Assert.That(actualWeeks, Is.EquivalentTo(expectedWeeks));
        }

        [TestCase(4, 3, 20, "6", "3,4,5")]
        [TestCase(5, 3, 20, "6", "4,5,7")]
        [TestCase(6, 3, 20, "6", "5,7,8")]
        [TestCase(7, 3, 20, "6", "5,7,8")]
        [TestCase(8, 3, 20, "6", "7,8,9")]

        [TestCase(4, 4, 20, "6", "3,4,5,7")]
        [TestCase(5, 4, 20, "6", "4,5,7,8")]
        [TestCase(6, 4, 20, "6", "5,7,8,9")]
        [TestCase(7, 4, 20, "6", "5,7,8,9")]
        [TestCase(8, 4, 20, "6", "7,8,9,10")]

        [TestCase(10, 5, 12, "12", "7,8,9,10,11")]
        [TestCase(1, 10, 5, "1","2,3,4,5")]
        public void WithOneSkip(int currentWeek, int numOfWeeks, int lastWeek, string skipWeeksCsv, string expectedWeeksCsv)
        {
            var connector = new OwlConnectorV2();

            int[] weeksToSkip = ParseCsvNumbers(skipWeeksCsv);
            int[] expectedWeeks = ParseCsvNumbers(expectedWeeksCsv);
            int[] actualWeeks = connector.GetWeeksToFetch(currentWeek, numOfWeeks, lastWeek, weeksToSkip);

            Assert.That(actualWeeks, Is.EquivalentTo(expectedWeeks));
        }

        [TestCase(8, 4, 20, "7,9", "6,8,10,11")]
        [TestCase(9, 4, 20, "9,11", "8,10,12,13")]
        [TestCase(9, 4, 20, "10,11", "8,9,12,13")]

        [TestCase(8, 3, 20, "7,9", "6,8,10")]
        [TestCase(8, 3, 20, "7,8", "6,9,10")]

        [TestCase(8, 5, 20, "6,7,9,10", "4,5,8,11,12")]
        public void WithMultSkip(int currentWeek, int numOfWeeks, int lastWeek, string skipWeeksCsv, string expectedWeeksCsv)
        {
            var connector = new OwlConnectorV2();

            int[] weeksToSkip = ParseCsvNumbers(skipWeeksCsv);
            int[] expectedWeeks = ParseCsvNumbers(expectedWeeksCsv);
            int[] actualWeeks = connector.GetWeeksToFetch(currentWeek, numOfWeeks, lastWeek, weeksToSkip);

            Assert.That(actualWeeks, Is.EquivalentTo(expectedWeeks));
        }


        private int[] ParseCsvNumbers(string input)
        {
            return input.Split(new[] {','}).Select(int.Parse).ToArray();
        }

        #endregion
    }
}
