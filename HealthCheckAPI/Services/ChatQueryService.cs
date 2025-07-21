using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Dapper;
using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HealthCheckAPI.Services
{
    public class ChatQueryService
    {
        private readonly DatabaseService _databaseService;
        private readonly IAppStatisticsService _appStatisticsService;
        private readonly List<ApplicationModel> _applications;


        private static readonly Regex DowntimeCountRegex = new(@"(ποσες\s+φορες|ποια\s+ειναι\s+η\sυχνοτητα|ποσο\s+συχνα).*(επεσε|πεφτει|εχει\s*πεσει|ειχε\s*πεσει|επεφτε)", RegexOptions.IgnoreCase);
        private static readonly Regex AvailabilityRegex = new(@"(ποσοστο|επιπεδο|βαθμος).*(διαθεσιμ[οη]τητας?)", RegexOptions.IgnoreCase);
        private static readonly Regex LastDownRegex = new(@"(ποτε|πoια\s+ηταν\s+η).*τελευταια.*(φορα|πτωση|επεσε|πτωσ[ηη]?)", RegexOptions.IgnoreCase);

        public ChatQueryService(DatabaseService databaseService, IAppStatisticsService appStatisticsService)
        {
            _databaseService = databaseService;
            _appStatisticsService = appStatisticsService;
            _applications = _databaseService.GetAllApplicationsAsync().Result; 

        }

        public async Task<string> GetAnswerAsync(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                return "Παρακαλώ δώστε μια ερώτηση.";

            question = RemoveGreekAccents(question.ToLower());

            if (DowntimeCountRegex.IsMatch(question))
            {
                var identifier = ExtractIdentifier(question);
                if (string.IsNullOrEmpty(identifier))
                    return "Δεν βρήκα το όνομα ή το id της εφαρμογής/βάσης δεδομένων.";

                var stats = await _appStatisticsService.GetStatisticsAsync(identifier);
                if (stats == null)
                    return $"Δεν βρέθηκαν δεδομένα για την εφαρμογή ή βάση δεδομένων '{identifier}'.";

                if (question.Contains("σημερα"))
                {
                    int count = await _databaseService.CountErrorsTodayAsync(identifier);
                    return $"Η εφαρμογή ή βάση δεδομένων '{identifier}' έπεσε {count} φορές σήμερα.";
                }
                else if (TryExtractDate(question, out DateTime date))
                {
                    int count = await _databaseService.CountErrorsOnDateAsync(identifier, date);
                    return $"Η εφαρμογή ή βάση δεδομένων '{identifier}' έπεσε {count} φορές στις {date:dd-MM-yyyy}.";
                }
                else
                {
                    return $"Η εφαρμογή ή βάση δεδομένων '{identifier}' έπεσε συνολικά {stats.DowntimesCount} φορές.";
                }
            }

            if (AvailabilityRegex.IsMatch(question))
            {
                var identifier = ExtractIdentifier(question);
                if (string.IsNullOrEmpty(identifier))
                    return "Δεν βρήκα το όνομα ή το id της εφαρμογής/βάσης δεδομένων.";

                var stats = await _appStatisticsService.GetStatisticsAsync(identifier);
                return $"Το ποσοστό διαθεσιμότητας της '{identifier}' είναι {stats.AvailabilityPercent:F2}%.";
            }

            if (LastDownRegex.IsMatch(question))
            {
                var identifier = ExtractIdentifier(question);
                if (string.IsNullOrEmpty(identifier))
                    return "Δεν βρήκα το όνομα ή το id της εφαρμογής/βάσης δεδομένων.";

                var lastDate = await _databaseService.GetLastFailureDateAsync(identifier);

                return lastDate.HasValue
                    ? $"Η εφαρμογή ή βάση δεδομένων '{identifier}' έπεσε τελευταία φορά στις {lastDate.Value:dd-MM-yyyy HH:mm}."
                    : $"Δεν βρέθηκαν καταγραφές για την εφαρμογή ή βάση δεδομένων '{identifier}'.";
            }

            return "Λυπάμαι, δεν κατάλαβα την ερώτηση.";
        }

        private string ExtractIdentifier(string input)
        {
            
            var idMatch = Regex.Match(input, @"με\s+id\s*=\s*([a-z0-9\-]+)");
            if (idMatch.Success)
                return idMatch.Groups[1].Value;

            
            var nameMatch = Regex.Match(input, @"(?:εφαρμογ(?:η|ης)|βαση\s+δεδομενων)\s+([a-z0-9\-]+)");
            if (nameMatch.Success)
                return nameMatch.Groups[1].Value;

            
            var fallbackMatch = Regex.Match(input, @"(?:\s|^)([a-z0-9\-]{3,})(?:\s|$)");
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

            var replacements = new System.Collections.Generic.Dictionary<char, char>()
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
    }
}
