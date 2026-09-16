using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;

namespace TerrarianCompendium.Localization
{
    internal sealed class CompendiumLocalization
    {
        public const string SourceCultureName = "en-US";
        private const string ResourcePrefix = "TerrarianCompendium.Localization.Locales.";
        private const string ResourceSuffix = ".json";

        private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _locales;
        private readonly IReadOnlyDictionary<string, string> _source;
        private IReadOnlyDictionary<string, string> _active;
        private string _activeResourceCultureName;

        private CompendiumLocalization(
            Dictionary<string, IReadOnlyDictionary<string, string>> locales,
            string initialCultureName)
        {
            _locales = locales ?? throw new ArgumentNullException(nameof(locales));

            if (!_locales.TryGetValue(SourceCultureName, out _source))
                throw new InvalidOperationException($"Source locale '{SourceCultureName}' is missing.");

            SetInitialCulture(initialCultureName);
        }

        public string CultureName { get; private set; }

        public long Revision { get; private set; }

        internal IReadOnlyDictionary<string, string> SourceEntries => _source;

        public static CompendiumLocalization LoadFromAssembly(Assembly assembly, string initialCultureName)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            var locales = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            foreach (string resourceName in assembly.GetManifestResourceNames())
            {
                if (!resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal) ||
                    !resourceName.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string cultureName = resourceName.Substring(
                    ResourcePrefix.Length,
                    resourceName.Length - ResourcePrefix.Length - ResourceSuffix.Length);

                using Stream stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                    continue;

                locales[cultureName] = ReadLocale(stream, resourceName);
            }

            return new CompendiumLocalization(locales, initialCultureName);
        }

        internal static CompendiumLocalization CreateForTesting(
            IReadOnlyDictionary<string, string> source,
            IReadOnlyDictionary<string, string> translated = null,
            string translatedCultureName = "test")
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var locales = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                [SourceCultureName] = CopyLocale(source)
            };

            if (translated != null)
            {
                if (string.IsNullOrWhiteSpace(translatedCultureName))
                    throw new ArgumentException(
                        "Translated culture name must not be empty.",
                        nameof(translatedCultureName));

                locales[translatedCultureName] = CopyLocale(translated);
            }

            return new CompendiumLocalization(locales, SourceCultureName);
        }

        public bool SynchronizeCulture(string cultureName)
        {
            string normalized = NormalizeCultureName(cultureName);
            if (string.Equals(CultureName, normalized, StringComparison.OrdinalIgnoreCase))
                return false;

            CultureName = normalized;
            SelectActiveLocale(normalized);
            Revision++;
            return true;
        }

        public string Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Localization key must not be empty.", nameof(key));

            if (_active.TryGetValue(key, out string translated) && !string.IsNullOrEmpty(translated))
                return translated;

            if (_source.TryGetValue(key, out string source) && !string.IsNullOrEmpty(source))
                return source;

            return key;
        }

        public string Format(string key, params object[] arguments)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Localization key must not be empty.", nameof(key));

            arguments ??= Array.Empty<object>();

            bool useActive = _active.TryGetValue(key, out string template) && !string.IsNullOrEmpty(template);
            string cultureName = useActive ? _activeResourceCultureName : SourceCultureName;

            if (!useActive)
            {
                if (!_source.TryGetValue(key, out template) || string.IsNullOrEmpty(template))
                    return key;
            }
            else if (_source.TryGetValue(key, out string sourceTemplate) &&
                     !HaveCompatibleFormatItems(sourceTemplate, template))
            {
                template = sourceTemplate;
                cultureName = SourceCultureName;
            }

            try
            {
                return string.Format(GetFormatCulture(cultureName), template, arguments);
            }
            catch (FormatException)
            {
                if (_source.TryGetValue(key, out string sourceTemplate) &&
                    !string.Equals(template, sourceTemplate, StringComparison.Ordinal))
                {
                    try
                    {
                        return string.Format(GetFormatCulture(SourceCultureName), sourceTemplate, arguments);
                    }
                    catch (FormatException)
                    {
                    }
                }

                return template;
            }
        }

        internal static bool HaveCompatibleFormatItems(string source, string translated)
        {
            return GetFormatItemIndices(source).SetEquals(GetFormatItemIndices(translated));
        }

        private static Dictionary<string, string> ReadLocale(Stream stream, string resourceName)
        {
            var settings = new DataContractJsonSerializerSettings
            {
                UseSimpleDictionaryFormat = true
            };
            var serializer = new DataContractJsonSerializer(typeof(Dictionary<string, string>), settings);
            var entries = serializer.ReadObject(stream) as Dictionary<string, string>;

            if (entries == null)
                throw new InvalidDataException($"Localization resource '{resourceName}' is invalid.");

            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> pair in entries)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    throw new InvalidDataException($"Localization resource '{resourceName}' contains an empty key.");

                if (result.ContainsKey(pair.Key))
                    throw new InvalidDataException(
                        $"Localization resource '{resourceName}' contains duplicate key '{pair.Key}'.");

                result.Add(pair.Key, pair.Value ?? string.Empty);
            }

            return result;
        }

        private static IReadOnlyDictionary<string, string> CopyLocale(IReadOnlyDictionary<string, string> locale)
        {
            var copy = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> pair in locale)
                copy.Add(pair.Key, pair.Value ?? string.Empty);
            return copy;
        }

        private static HashSet<int> GetFormatItemIndices(string template)
        {
            var result = new HashSet<int>();
            if (string.IsNullOrEmpty(template))
                return result;

            for (var index = 0; index < template.Length; index++)
            {
                if (template[index] != '{')
                    continue;

                if (index + 1 < template.Length && template[index + 1] == '{')
                {
                    index++;
                    continue;
                }

                int cursor = index + 1;
                var value = 0;
                var hasDigit = false;

                while (cursor < template.Length && char.IsDigit(template[cursor]))
                {
                    hasDigit = true;
                    value = checked(value * 10 + template[cursor] - '0');
                    cursor++;
                }

                if (hasDigit)
                    result.Add(value);
            }

            return result;
        }

        private static CultureInfo GetFormatCulture(string cultureName)
        {
            try
            {
                return CultureInfo.GetCultureInfo(cultureName);
            }
            catch (CultureNotFoundException)
            {
                return CultureInfo.InvariantCulture;
            }
        }

        private static string NormalizeCultureName(string cultureName)
        {
            return string.IsNullOrWhiteSpace(cultureName) ? SourceCultureName : cultureName.Trim();
        }

        private void SetInitialCulture(string cultureName)
        {
            CultureName = NormalizeCultureName(cultureName);
            SelectActiveLocale(CultureName);
            Revision = 0;
        }

        private void SelectActiveLocale(string cultureName)
        {
            if (_locales.TryGetValue(cultureName, out IReadOnlyDictionary<string, string> active))
            {
                _active = active;
                _activeResourceCultureName = cultureName;
                return;
            }

            _active = _source;
            _activeResourceCultureName = SourceCultureName;
        }
    }
}