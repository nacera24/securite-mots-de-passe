using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HachageApp
{
    public class CrackResult
    {
        public bool Found { get; init; }

        //  on ajoute un état Annulé
        public bool Canceled { get; init; }

        public string? Plain { get; init; }
        public string Hash { get; init; } = "";
        public string Salt { get; init; } = "";
        public long Attempts { get; init; }
        public TimeSpan Elapsed { get; init; }
    }

    public class BcryptCracker
    {
        private readonly string hashCible;
        private readonly string sel;
        private readonly IEnumerable<string> dictionnaireStream;

        public BcryptCracker(string hash, IEnumerable<string> dictionaryStream)
        {
            hashCible = hash ?? throw new ArgumentNullException(nameof(hash));
            dictionnaireStream = dictionaryStream ?? throw new ArgumentNullException(nameof(dictionaryStream));

            if (hashCible.Length < 29)
                throw new ArgumentException("Hash bcrypt invalide (trop court).");

            // Sel = 29 premiers caractères d’un hash bcrypt standard
            sel = hashCible.Substring(0, 29);
        }

        public string Salt => sel;

        // progress reçoit : (tentatives, total, tempsEcoule)
        
        public async Task<CrackResult> RunAsync(
            long totalWords,
            IProgress<(long attempts, long total, TimeSpan elapsed)>? progress = null,
            CancellationToken token = default)
        {
            return await Task.Run(() =>
            {
                var chrono = Stopwatch.StartNew();
                long tentatives = 0;

                long nextUiUpdateMs = 0;

                try
                {
                    foreach (var mot in dictionnaireStream)
                    {
                        token.ThrowIfCancellationRequested();
                        tentatives++;

                        string hashCalcule = BCrypt.Net.BCrypt.HashPassword(mot, sel);

                        long now = chrono.ElapsedMilliseconds;
                        if (progress != null && now >= nextUiUpdateMs)
                        {
                            progress.Report((tentatives, totalWords, chrono.Elapsed));
                            nextUiUpdateMs = now + 200; 
                        }

                        if (hashCalcule == hashCible)
                        {
                            chrono.Stop();
                            progress?.Report((tentatives, totalWords, chrono.Elapsed));

                            return new CrackResult
                            {
                                Found = true,
                                Canceled = false,
                                Plain = mot,
                                Hash = hashCible,
                                Salt = sel,
                                Attempts = tentatives,
                                Elapsed = chrono.Elapsed
                            };
                        }
                    }

                    chrono.Stop();
                    progress?.Report((tentatives, totalWords, chrono.Elapsed));

                    return new CrackResult
                    {
                        Found = false,
                        Canceled = false,
                        Plain = null,
                        Hash = hashCible,
                        Salt = sel,
                        Attempts = tentatives,
                        Elapsed = chrono.Elapsed
                    };
                }
                catch (OperationCanceledException)
                {
                  
                    chrono.Stop();
                    progress?.Report((tentatives, totalWords, chrono.Elapsed));

                    return new CrackResult
                    {
                        Found = false,
                        Canceled = true,
                        Plain = null,
                        Hash = hashCible,
                        Salt = sel,
                        Attempts = tentatives,
                        Elapsed = chrono.Elapsed
                    };
                }
            }, token);
        }

        // On  ne charge pas tout en mémoire
        public static IEnumerable<string> StreamDictionaryFromFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Fichier dictionnaire introuvable.", path);

            foreach (var line in File.ReadLines(path))
            {
                var mot = line.Trim();
                if (!string.IsNullOrWhiteSpace(mot))
                    yield return mot;
            }
        }

        // Compter les mots 
        public static long CountWordsInFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Fichier dictionnaire introuvable.", path);

            long count = 0;
            foreach (var line in File.ReadLines(path))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    count++;
            }
            return count;
        }
    }
}
