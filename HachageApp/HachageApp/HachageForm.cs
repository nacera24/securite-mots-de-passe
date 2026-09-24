using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FontAwesome.Sharp;


namespace HachageApp
{
    public partial class HachageForm : Form
    {
        private TextBox txtHash;
        private Button btnValider, btnAnnuler;
        private IconButton btnSettings;

        private Label lblTentatives, lblTemps, lblNombreMots, lblCheminDictionnaire;
        private OpenFileDialog dialogueOuverture;

        private string? cheminDictionnaire;
        private long totalMots = 0;

        private CancellationTokenSource? sourceAnnulation;
        private BcryptCracker? cracker;

    
        private ToolTip toolTip;

        public HachageForm()
        {
            Text = "Hachage ";
            Width = 640;
            Height = 300;
            StartPosition = FormStartPosition.CenterScreen;

            ConstruireInterface();
        }

        private void ConstruireInterface()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(12)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34)); // Hash
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); // Dictionnaire 
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); // Boutons
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46)); // Barre bas

            // Hash 
            layout.Controls.Add(new Label
            {
                Text = "Hash bcrypt :",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            }, 0, 0);

            txtHash = new TextBox { Dock = DockStyle.Fill };
            layout.Controls.Add(txtHash, 1, 0);

            //  Dictionnaire chemin complet 
            layout.Controls.Add(new Label
            {
                Text = "Dictionnaire :",
                AutoSize = true,
                Anchor = AnchorStyles.Left
            }, 0, 1);

            lblCheminDictionnaire = new Label
            {
                Text = "Aucun fichier sélectionné",
                Dock = DockStyle.Fill,
                AutoSize = false,
                AutoEllipsis = false,
                TextAlign = ContentAlignment.TopLeft
            };
            lblCheminDictionnaire.MaximumSize = new Size(0, 0);
            layout.Controls.Add(lblCheminDictionnaire, 1, 1);

            // Boutons 
            layout.Controls.Add(new Label { Text = "", AutoSize = true }, 0, 2);

            var panneauBoutons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            btnValider = new Button { Text = "Valider !", Width = 120, Height = 30 };
            btnValider.Click += async (_, __) => await LancerVerificationAsync();

            btnAnnuler = new Button { Text = "Annuler", Width = 120, Height = 30, Enabled = false };
            btnAnnuler.Click += (_, __) => sourceAnnulation?.Cancel();

            panneauBoutons.Controls.Add(btnValider);
            panneauBoutons.Controls.Add(btnAnnuler);
            layout.Controls.Add(panneauBoutons, 1, 2);

            // Barre du bas : Tentatives | Temps | Mots 
            var barreBas = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                Padding = new Padding(0, 6, 0, 0)
            };

            //  colonnes 
            barreBas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30)); // Tentatives
            barreBas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30)); // Temps
            barreBas.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30)); // Mots
            barreBas.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 44)); // pour dossier

            lblTentatives = new Label
            {
                Text = "Tentatives : 0",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            lblTemps = new Label
            {
                Text = "Temps écoulé : 0s",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblNombreMots = new Label
            {
                Text = "Mots dans le dictionnaire : 0",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };

            barreBas.Controls.Add(lblTentatives, 0, 0);
            barreBas.Controls.Add(lblTemps, 1, 0);
            barreBas.Controls.Add(lblNombreMots, 2, 0);

            btnSettings = new IconButton
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4),

                //IconChar = IconChar.FolderOpen,
                IconChar = IconChar.Gear,
                IconColor = Color.Black,
                IconSize = 20,

                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Text = ""
            };

            btnSettings.FlatAppearance.BorderSize = 0;
            btnSettings.FlatAppearance.MouseOverBackColor = Color.Gainsboro;
            btnSettings.FlatAppearance.MouseDownBackColor = Color.Silver;

            btnSettings.Click += async (_, __) => await ChargerDictionnaireAsync();

            
            toolTip = new ToolTip();
            toolTip.SetToolTip(btnSettings, "Charger dictionnaire");

            barreBas.Controls.Add(btnSettings, 3, 0);

            layout.Controls.Add(barreBas, 0, 3);
            layout.SetColumnSpan(barreBas, 2);

            // Dialog
            dialogueOuverture = new OpenFileDialog
            {
                Filter = "Fichiers texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*"
            };

            Controls.Add(layout);

            // Force le retour à la ligne du chemin 
            Shown += (_, __) =>
            {
                lblCheminDictionnaire.MaximumSize = new Size(lblCheminDictionnaire.Width, 0);
            };
        }

        private async Task ChargerDictionnaireAsync()
        {
            if (dialogueOuverture.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                cheminDictionnaire = dialogueOuverture.FileName;

                // Afficher le chemin complet 
                lblCheminDictionnaire.Text = cheminDictionnaire;

                lblNombreMots.Text = "Mots dans le dictionnaire : (calcul...)";

                totalMots = await Task.Run(() => BcryptCracker.CountWordsInFile(cheminDictionnaire));

                lblNombreMots.Text = $"Mots dans le dictionnaire : {totalMots}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Une erreur est survenue lors de la lecture du dictionnaire :\n\n" + ex.Message,
                    "Erreur",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async Task LancerVerificationAsync()
        {
            string hash = txtHash.Text.Trim();

            if (string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(cheminDictionnaire) || totalMots == 0)
            {
                MessageBox.Show(this,
                    "Veuillez saisir un hash et charger un dictionnaire avant de lancer la vérification.",
                    "Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                var flux = BcryptCracker.StreamDictionaryFromFile(cheminDictionnaire);
                cracker = new BcryptCracker(hash, flux);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Le hash fourni est invalide ou une erreur est survenue :\n\n" + ex.Message,
                    "Erreur",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            btnValider.Enabled = false;
            btnSettings.Enabled = false;
            btnAnnuler.Enabled = true;

            sourceAnnulation = new CancellationTokenSource();

            var progression = new Progress<(long tentatives, long total, TimeSpan tempsEcoule)>(t =>
            {
                lblTentatives.Text = $"Tentatives : {t.tentatives} / {t.total}";
                lblTemps.Text = $"Temps écoulé : {t.tempsEcoule.Minutes}m{t.tempsEcoule.Seconds:00}s";
            });

            try
            {
                var resultat = await cracker.RunAsync(totalMots, progression, sourceAnnulation.Token);

                if (resultat.Found)
                {
                    MessageBox.Show(this,
                        "Votre hachage :\n" +
                        $"{resultat.Hash}\n\n" +
                        "Correspond au mot suivant :\n" +
                        $"{resultat.Plain}\n\n" +
                        $"Sel : {resultat.Salt}\n" +
                        $"Tentatives : {resultat.Attempts}\n" +
                        $"Temps : {resultat.Elapsed.Minutes}m{resultat.Elapsed.Seconds:00}s",
                        "Congratulations!!!",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(this,
                        "Aucun mot ne correspond au hash fourni.\n\n" +
                        $"Tentatives : {resultat.Attempts}\n" +
                        $"Temps : {resultat.Elapsed.Minutes}m{resultat.Elapsed.Seconds:00}s",
                        "Résultat",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show(this,
                    "Opération annulée par l'utilisateur.",
                    "Annulation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Une erreur est survenue :\n\n" + ex.Message,
                    "Erreur",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                btnValider.Enabled = true;
                btnSettings.Enabled = true;
                btnAnnuler.Enabled = false;

                sourceAnnulation?.Dispose();
                sourceAnnulation = null;
            }
        }
    }
}
