using System;

namespace BotApp.Model
{
    public class Sleep
    {
        public Sleepactivity[] sleepActivities { get; set; }
        public int itemCount { get; set; }
    }

    public class Sleepactivity
    {
        public string activityType { get; set; }
        public Activitysegment[] activitySegments { get; set; }
        public string awakeDuration { get; set; }
        public string sleepDuration { get; set; }
        public int numberOfWakeups { get; set; }
        public string fallAsleepDuration { get; set; }
        public int sleepEfficiencyPercentage { get; set; }
        public string totalRestlessSleepDuration { get; set; }
        public string totalRestfulSleepDuration { get; set; }
        public int restingHeartRate { get; set; }
        public DateTime fallAsleepTime { get; set; }
        public DateTime wakeupTime { get; set; }
        public string id { get; set; }
        public DateTime startTime { get; set; }
        public DateTime endTime { get; set; }
        public DateTime dayId { get; set; }
        public string duration { get; set; }
        public Caloriesburnedsummary caloriesBurnedSummary { get; set; }
        public Heartratesummary heartRateSummary { get; set; }
    }

    public class Caloriesburnedsummary
    {
        public string period { get; set; }
        public int totalCalories { get; set; }
    }

    public class Heartratesummary
    {
        public string period { get; set; }
        public int averageHeartRate { get; set; }
        public int peakHeartRate { get; set; }
        public int lowestHeartRate { get; set; }
    }

    public class Activitysegment
    {
        public DateTime dayId { get; set; }
        public string sleepType { get; set; }
        public long segmentId { get; set; }
        public DateTime startTime { get; set; }
        public DateTime endTime { get; set; }
        public string duration { get; set; }
        public Heartratesummary1 heartRateSummary { get; set; }
        public Caloriesburnedsummary1 caloriesBurnedSummary { get; set; }
        public string segmentType { get; set; }
    }

    public class Heartratesummary1
    {
        public string period { get; set; }
        public int averageHeartRate { get; set; }
        public int peakHeartRate { get; set; }
        public int lowestHeartRate { get; set; }
    }

    public class Caloriesburnedsummary1
    {
        public string period { get; set; }
        public int totalCalories { get; set; }
    }

    public class MSHealthUserText
    {
        public string query { get; set; }
        public Intent[] intents { get; set; }
        public Entity[] entities { get; set; }
    }

    public class Intent
    {
        public string intent { get; set; }
        public float score { get; set; }
    }

    public class Entity
    {
        public string entity { get; set; }
        public string type { get; set; }
        public int startIndex { get; set; }
        public int endIndex { get; set; }
        public float score { get; set; }
        public Resolution resolution { get; set; }
    }

    public class Resolution
    {
        public string date { get; set; }
        public string duration { get; set; }
        public string time { get; set; }
        public string comment { get; set; }
    }
}