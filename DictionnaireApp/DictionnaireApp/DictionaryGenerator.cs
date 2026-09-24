using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DictionnaireApp
{
    // Classe de progression 
    public sealed class InfosProgression
    {
        public long LignesEcrites { get; init; }
        public long Total { get; init; }
        public int Pourcentage { get; init; }
    }

    
    public static class DictionaryGenerator
    {
        public static long CompterCombinaisonsTotales(int tailleAlphabet, int longueurMin, int longueurMax)
        {
            long total = 0;

            for (int longueur = longueurMin; longueur <= longueurMax; longueur++)
            {
                long totalPourLongueur = 1;

                for (int i = 0; i < longueur; i++)
                {
                    if (totalPourLongueur > long.MaxValue / tailleAlphabet)
                        return long.MaxValue;

                    totalPourLongueur *= tailleAlphabet;
                }

                if (total > long.MaxValue - totalPourLongueur)
                    return long.MaxValue;

                total += totalPourLongueur;
            }

            return total;
        }

        public static Task GenerateAsync(
            string cheminSortie,
            string alphabet,
            int longueurMin,
            int longueurMax,
            IProgress<InfosProgression>? progression,
            CancellationToken jetonAnnulation)
        {
            return Task.Run(() =>
            {
                long total = CompterCombinaisonsTotales(alphabet.Length, longueurMin, longueurMax);
                long lignesEcrites = 0;

  
                void SurLigneEcrite()
                {
                    lignesEcrites++;

                    if (lignesEcrites % 2000 == 0)
                    {
                        int pourcentage = (total <= 0 || total == long.MaxValue)
                            ? 0
                            : (int)Math.Min(100, (lignesEcrites * 100L) / total);

                        progression?.Report(new InfosProgression
                        {
                            LignesEcrites = lignesEcrites,
                            Total = total,
                            Pourcentage = pourcentage
                        });
                    }
                }

                using var ecrivain = new StreamWriter(cheminSortie, false, Encoding.UTF8);

                for (int longueur = longueurMin; longueur <= longueurMax; longueur++)
                {
                    jetonAnnulation.ThrowIfCancellationRequested();
                    GenererLongueur(ecrivain, alphabet, longueur, SurLigneEcrite, jetonAnnulation);
                }

               
                progression?.Report(new InfosProgression
                {
                    LignesEcrites = lignesEcrites,
                    Total = total,
                    Pourcentage = 100
                });

            }, jetonAnnulation);
        }

        private static void GenererLongueur(
            StreamWriter ecrivain,
            string alphabet,
            int longueur,
            Action surLigneEcrite,
            CancellationToken jetonAnnulation)
        {
            char[] tampon = new char[longueur];
            int baseAlphabet = alphabet.Length;

            void Recursion(int position)
            {
                jetonAnnulation.ThrowIfCancellationRequested();

                if (position == longueur)
                {
                    ecrivain.WriteLine(tampon);
                    surLigneEcrite();
                    return;
                }

                for (int i = 0; i < baseAlphabet; i++)
                {
                    tampon[position] = alphabet[i];
                    Recursion(position + 1);
                }
            }

            Recursion(0);
        }
    }
}
