using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HealthCheckAPI.Services
{
    public class ChatQueryService : IChatQueryService
    {
        private readonly DatabaseService _databaseService;
        private readonly IAppStatisticsService _appStatisticsService;
        private List<ApplicationModel> _applications;

        private static readonly Regex DowntimeCountRegex = new(@"(ποσες\s+φορες|ποια\s+ηταν\s+η\s+συχνοτητα|ποσο\s+συχνα|καθε\s+ποτε|ποιο\s+ειναι\s+το\s+συνολο).*(επεσε|πεφτει|εχει\s*πεσει|ειχε\s*πεσει|πτωσεις?)", RegexOptions.IgnoreCase);
        private static readonly Regex AvailabilityRegex = new(@"(ποσοστο|επιπεδο|βαθμος).*(διαθεσιμ[οη]τητας?)", RegexOptions.IgnoreCase);
        private static readonly Regex LastDownRegex = new(@"(ποτε|πoια\s+ηταν\s+η).*τελευταια.*(φορα|πτωση|επεσε|πτωσ[ηη]?)", RegexOptions.IgnoreCase);


        private enum IntentType
        {
            DowntimeCount,
            Availability,
            LastDown,
            Unknown
        }

        public ChatQueryService(DatabaseService databaseService, IAppStatisticsService appStatisticsService)
        {
            _databaseService = databaseService;
            _appStatisticsService = appStatisticsService;
            _applications = new List<ApplicationModel>();
            InitializeApplicationsAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeApplicationsAsync()
        {
            _applications = await _databaseService.GetAllApplicationsAsync();
        }

        public async Task<string> AnswerQuestionAsync(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return "Παρακαλώ δώστε μια ερώτηση.";

            var normalizedQuestion = RemoveGreekAccents(question.ToLowerInvariant());

            var intent = GetIntent(normalizedQuestion);
            var identifier = ExtractIdentifier(normalizedQuestion);

            if (string.IsNullOrEmpty(identifier))
                return "Δεν βρήκα το όνομα ή το id της εφαρμογής/βάσης δεδομένων.";

            var stats = await _appStatisticsService.GetStatisticsAsync(identifier);
            if (stats == null)
                return $"Δεν βρέθηκαν δεδομένα για την εφαρμογή ή βάση δεδομένων '{identifier}'.";

            switch (intent)
            {
                case IntentType.DowntimeCount:
                    if (normalizedQuestion.Contains("σημερα"))
                    {
                        int count = await _databaseService.CountErrorsTodayAsync(identifier);
                        return $"Η εφαρμογή ή βάση δεδομένων '{identifier}' έπεσε {count} φορές σήμερα.";
                    }
                    else if (TryExtractDate(normalizedQuestion, out DateTime date))
                    {
                        int count = await _databaseService.CountErrorsOnDateAsync(identifier, date);
                        return $"Η εφαρμογή ή βάση δεδομένων '{identifier}' έπεσε {count} φορές στις {date:dd-MM-yyyy}.";
                    }
                    else
                    {
                        return $"Η εφαρμογή ή βάση δεδομένων '{identifier}' έπεσε συνολικά {stats.DowntimesCount} φορές.";
                    }

                case IntentType.Availability:
                    return $"Το ποσοστό διαθεσιμότητας της '{identifier}' είναι {stats.AvailabilityPercent:F2}%.";

                case IntentType.LastDown:
                    var lastDate = await _databaseService.GetLastFailureDateAsync(identifier);
                    return lastDate.HasValue
                        ? $"Η εφαρμογή ή βάση δεδομένων '{identifier}' έπεσε τελευταία φορά στις {lastDate:dd-MM-yyyy HH:mm}."
                        : $"Δεν βρέθηκαν καταγραφές για την εφαρμογή ή βάση δεδομένων '{identifier}'.";

                default:
                    return "Λυπάμαι, δεν κατάλαβα την ερώτηση.";
            }
        }


        private string ExtractIdentifier(string input)
        {
            // Αναζήτηση με id = <id>
            var idMatch = Regex.Match(input, @"με\s+id\s*=\s*([a-z0-9\-]+)", RegexOptions.IgnoreCase);
            if (idMatch.Success)
                return idMatch.Groups[1].Value;

            // Αναζήτηση ονόματος εφαρμογής ή βάσης δεδομένων (greek)
            var nameMatch = Regex.Match(input, @"(?:εφαρμογ(?:η|ης)|βαση\s+δεδομενων)\s+([a-z0-9\-]+)", RegexOptions.IgnoreCase);
            if (nameMatch.Success)
                return nameMatch.Groups[1].Value;

            // Fallback: αναζήτηση λέξης-identifier
            var fallbackMatch = Regex.Match(input, @"(?:\s|^)([a-z0-9\-]{3,})(?:\s|$)", RegexOptions.IgnoreCase);
            if (fallbackMatch.Success)
                return fallbackMatch.Groups[1].Value;

            return string.Empty;
        }

        private bool TryExtractDate(string input, out DateTime date)
        {
            var match = Regex.Match(input, @"\b(\d{1,2})[-/](\d{1,2})[-/](\d{4})\b");
            if (match.Success)
            {
                return DateTime.TryParseExact(match.Value, "d-M-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
            }

            date = default;
            return false;
        }

        private static string RemoveGreekAccents(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var replacements = new Dictionary<char, char>()
            {
                ['ά'] = 'α',
                ['έ'] = 'ε',
                ['ή'] = 'η',
                ['ί'] = 'ι',
                ['ό'] = 'ο',
                ['ύ'] = 'υ',
                ['ώ'] = 'ω',
                ['Ά'] = 'Α',
                ['Έ'] = 'Ε',
                ['Ή'] = 'Η',
                ['Ί'] = 'Ι',
                ['Ό'] = 'Ο',
                ['Ύ'] = 'Υ',
                ['Ώ'] = 'Ω'
            };

            var sb = new StringBuilder(text.Length);
            foreach (var ch in text)
            {
                if (replacements.TryGetValue(ch, out var replacement))
                    sb.Append(replacement);
                else
                    sb.Append(ch);
            }

            return sb.ToString();
        }

        private IntentType GetIntent(string question)
        {
            if (DowntimeCountRegex.IsMatch(question))
                return IntentType.DowntimeCount;

            if (AvailabilityRegex.IsMatch(question))
                return IntentType.Availability;

            if (LastDownRegex.IsMatch(question))
                return IntentType.LastDown;

            return IntentType.Unknown;
        }
    }
}
