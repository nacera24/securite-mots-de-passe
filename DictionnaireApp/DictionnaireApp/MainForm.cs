using System.Text;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;



namespace DictionnaireApp
{
  
    public class MainForm : Form
    {
        NumericUpDown nudLongueurMin, nudLongueurMax;
        CheckBox chkMinuscules, chkMajuscules, chkChiffres, chkSpeciaux;
        TextBox txtCaracteresPerso, txtCheminSortie;
        Button btnParcourir, btnGenerer, btnAnnuler;
        ProgressBar barreProgression;
        Label lblStatut;

        private bool generationEnCours = false;
        private CancellationTokenSource? sourceAnnulation;

        public MainForm()
        {
            Text = "Générateur de dictionnaire";
            StartPosition = FormStartPosition.CenterScreen;

            MinimumSize = new Size(820, 500);
            Width = 820;
            Height = 500;

            ConstruireInterface();
        }

        private void ConstruireInterface()
        {
            var racine = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 8,
                Padding = new Padding(16),
                AutoSize = false
            };

            racine.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240f));
            racine.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            racine.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            racine.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            racine.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
            racine.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            racine.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            racine.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            racine.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            racine.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

            // Longueur minimale
            racine.Controls.Add(new Label { Text = "Longueur minimale", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            nudLongueurMin = new NumericUpDown { Minimum = 1, Maximum = 20, Value = 1, Width = 140, Anchor = AnchorStyles.Left };
            racine.Controls.Add(nudLongueurMin, 1, 0);

            // Longueur maximale
            racine.Controls.Add(new Label { Text = "Longueur maximale", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            nudLongueurMax = new NumericUpDown { Minimum = 1, Maximum = 20, Value = 3, Width = 140, Anchor = AnchorStyles.Left };
            racine.Controls.Add(nudLongueurMax, 1, 1);

            // Caractères autorisés
            racine.Controls.Add(new Label { Text = "Caractères autorisés", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);

            var grilleCases = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(0, 6, 0, 0)
            };
            grilleCases.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            grilleCases.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            grilleCases.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            grilleCases.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));

            chkMinuscules = new CheckBox { Text = "minuscules (a-z)", AutoSize = true, Anchor = AnchorStyles.Left };
            chkMajuscules = new CheckBox { Text = "majuscules (A-Z)", AutoSize = true, Anchor = AnchorStyles.Left };
            chkChiffres = new CheckBox { Text = "chiffres (0-9)", AutoSize = true, Anchor = AnchorStyles.Left };
            chkSpeciaux = new CheckBox { Text = "spéciaux (!@#...)", AutoSize = true, Anchor = AnchorStyles.Left };

            grilleCases.Controls.Add(chkMinuscules, 0, 0);
            grilleCases.Controls.Add(chkMajuscules, 1, 0);
            grilleCases.Controls.Add(chkChiffres, 0, 1);
            grilleCases.Controls.Add(chkSpeciaux, 1, 1);

            racine.Controls.Add(grilleCases, 1, 2);

            // Caractères personnalisés
            racine.Controls.Add(new Label { Text = "ou caractères personnalisés", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
            txtCaracteresPerso = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "ex: abc123!@ (optionnel)" };
            racine.Controls.Add(txtCaracteresPerso, 1, 3);

            // Fichier de sortie
            racine.Controls.Add(new Label { Text = "Fichier de sortie (.txt)", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 4);

            var panneauSortie = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            panneauSortie.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            panneauSortie.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150f));

            txtCheminSortie = new TextBox { Dock = DockStyle.Fill };
            btnParcourir = new Button { Text = "Parcourir...", Dock = DockStyle.Fill };
            btnParcourir.Click += (_, __) => ChoisirFichierSortie();

            panneauSortie.Controls.Add(txtCheminSortie, 0, 0);
            panneauSortie.Controls.Add(btnParcourir, 1, 0);
            racine.Controls.Add(panneauSortie, 1, 4);

            // Boutons
            racine.Controls.Add(new Label { Text = "", AutoSize = true }, 0, 5);

            var ligneBoutons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
            ligneBoutons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            ligneBoutons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f));
            ligneBoutons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f));

            btnGenerer = new Button { Text = "Générer", Dock = DockStyle.Fill, Height = 36 };
            btnGenerer.Click += async (_, __) => await DemarrerGenerationAsync();

            btnAnnuler = new Button { Text = "Annuler", Dock = DockStyle.Fill, Height = 36, Enabled = false };
            btnAnnuler.Click += (_, __) => sourceAnnulation?.Cancel();

            ligneBoutons.Controls.Add(new Label(), 0, 0);
            ligneBoutons.Controls.Add(btnGenerer, 1, 0);
            ligneBoutons.Controls.Add(btnAnnuler, 2, 0);

            racine.Controls.Add(ligneBoutons, 1, 5);

            // Progression
            racine.Controls.Add(new Label { Text = "Progression", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 6);
            barreProgression = new ProgressBar { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100, Value = 0 };
            racine.Controls.Add(barreProgression, 1, 6);

            // Statut
            racine.Controls.Add(new Label { Text = "Statut", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 7);
            lblStatut = new Label { Text = "Prêt.", AutoSize = true, Dock = DockStyle.Fill };
            racine.Controls.Add(lblStatut, 1, 7);

            Controls.Add(racine);
        }

        private void ChoisirFichierSortie()
        {
            using var dialogue = new SaveFileDialog
            {
                Filter = "Fichier texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*",
                DefaultExt = "txt",
                AddExtension = true,
                FileName = "pass.txt"
            };

            if (dialogue.ShowDialog(this) == DialogResult.OK)
                txtCheminSortie.Text = dialogue.FileName;
        }

        private async Task DemarrerGenerationAsync()
        {
            if (generationEnCours) return;

            int longueurMin = (int)nudLongueurMin.Value;
            int longueurMax = (int)nudLongueurMax.Value;

            if (longueurMin > longueurMax)
            {
                MessageBox.Show(this, "La longueur minimale doit être inférieure ou égale à la longueur maximale.");
                return;
            }

            string cheminSortie = txtCheminSortie.Text.Trim();
            if (string.IsNullOrWhiteSpace(cheminSortie))
            {
                MessageBox.Show(this, "Veuillez choisir un fichier de sortie.");
                return;
            }

            string? dossier = Path.GetDirectoryName(cheminSortie);
            if (string.IsNullOrWhiteSpace(dossier) || !Directory.Exists(dossier))
            {
                MessageBox.Show(this, "Le dossier de sortie n'existe pas.");
                return;
            }

            string alphabet = ConstruireAlphabet();
            if (alphabet.Length == 0)
            {
                MessageBox.Show(this, "Choisis des caractères OU saisis des caractères personnalisés.");
                return;
            }

            long total = DictionaryGenerator.CompterCombinaisonsTotales(alphabet.Length, longueurMin, longueurMax);

            var confirmation = MessageBox.Show(
                this,
                $"Alphabet : {alphabet.Length} caractères\n" +
                $"Longueur : {longueurMin} → {longueurMax}\n\n" +
                $"Nombre total de lignes : {total:N0}\n\n" +
                "Continuer ?",
                "Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirmation != DialogResult.Yes) return;

            generationEnCours = true;
            sourceAnnulation = new CancellationTokenSource();

            btnGenerer.Enabled = false;
            btnParcourir.Enabled = false;
            btnAnnuler.Enabled = true;
            barreProgression.Value = 0;
            lblStatut.Text = "Génération en cours...";

            var progression = new Progress<InfosProgression>(info =>
            {
                barreProgression.Value = Math.Max(0, Math.Min(100, info.Pourcentage));
                lblStatut.Text = $"Écrit: {info.LignesEcrites:N0} / {info.Total:N0} ({info.Pourcentage}%)";
            });

            try
            {
                await DictionaryGenerator.GenerateAsync(
                    cheminSortie: cheminSortie,
                    alphabet: alphabet,
                    longueurMin: longueurMin,
                    longueurMax: longueurMax,
                    progression: progression,
                    jetonAnnulation: sourceAnnulation.Token
                );

                barreProgression.Value = 100;
                lblStatut.Text = "Terminé";
                MessageBox.Show(
                    this,
                    $"Le fichier a été généré avec succès.\n\n{cheminSortie}",
                    "Génération terminée",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

            }
            catch (OperationCanceledException)
            {
                lblStatut.Text = "Annulé.";
                MessageBox.Show(this, "Génération annulée.");
            }
            catch (Exception ex)
            {
                lblStatut.Text = "Erreur";
                MessageBox.Show(
                    this,
                    "Une erreur est survenue lors de la génération du fichier :\n\n" + ex.Message,
                    "Erreur",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

            }
            finally
            {
                generationEnCours = false;
                sourceAnnulation?.Dispose();
                sourceAnnulation = null;

                btnGenerer.Enabled = true;
                btnParcourir.Enabled = true;
                btnAnnuler.Enabled = false;
            }
        }

        private string ConstruireAlphabet()
        {
            string caracteresPerso = txtCaracteresPerso.Text;
            if (!string.IsNullOrWhiteSpace(caracteresPerso))
                return new string(caracteresPerso.Distinct().ToArray());

            var sb = new StringBuilder();
            if (chkMinuscules.Checked) sb.Append("abcdefghijklmnopqrstuvwxyz");
            if (chkMajuscules.Checked) sb.Append("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            if (chkChiffres.Checked) sb.Append("0123456789");
            if (chkSpeciaux.Checked) sb.Append("!@#$%^&*()-_=+[]{};:,.?/\\|");

            return new string(sb.ToString().Distinct().ToArray());
        }
    }
}
